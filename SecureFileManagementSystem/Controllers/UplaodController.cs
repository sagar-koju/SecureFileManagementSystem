using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SecureFileManagementSystem.Models;
using SecureFileManagementSystem.Services;
using SecureFileManagementSystem.Data;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
// Add these two using statements for your manual crypto
using System.Numerics;
using SecureFileManagement.Cryptography;

namespace SecureFileManagementSystem.Controllers
{
    public class UploadController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public UploadController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
                return RedirectToAction("Login", "Account");

            // You are missing a View named "HostAndShare". Let's create it.
            // If you don't have this view, the P2P part will crash after upload.
            // Create a file at Views/Upload/HostAndShare.cshtml
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(FileUploadViewModel model)
        {
            string? sender = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(sender))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid || model.Files == null || model.Files.Count == 0)
            {
                ViewBag.Error = "Please select at least one file.";
                return View("Index", model);
            }

            var receiverUser = _dbContext.UserRecords.FirstOrDefault(u => u.Username == model.ReceiverUsername);
            if (receiverUser == null)
            {
                ModelState.AddModelError("ReceiverUsername", "Receiver does not exist.");
                return View("Index", model);
            }

            // ================== P2P LOGIC ==================
            if (model.UseP2P)
            {
                var formFile = model.Files.FirstOrDefault();
                if (formFile == null || formFile.Length == 0)
                {
                    ViewBag.Error = "Please select a file to share via P2P.";
                    return View("Index");
                }

                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await formFile.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }

                // 1. Generate AES key (this helper is fine)
                byte[] aesKey = EncryptionService.GenerateAesKey();

                // 2. Encrypt the file content with the AES key
                byte[] iv;
                byte[] encryptedBytes = EncryptionService.EncryptBytes(fileBytes, aesKey, out iv);
                byte[] encryptedBytesWithIv = iv.Concat(encryptedBytes).ToArray();

                // 3. Encrypt the AES key using your manual RSACrypto
                var keyParts = receiverUser.PublicKey.Split('|');
                var n = BigInteger.Parse(keyParts[0]);
                var e = BigInteger.Parse(keyParts[1]);

                // Convert AES key to a string format that your manual RSA can handle
                string aesKeyAsBase64 = Convert.ToBase64String(aesKey);
                // The single source of the encrypted AES key
                string finalEncryptedAesKey = RSACrypto.Encrypt(aesKeyAsBase64, n, e);

                // 4. Start P2P host with the encrypted file
                var p2pHost = new P2PStreamHost(encryptedBytesWithIv, formFile.FileName);
                p2pHost.Start();
                string downloadUrl = p2pHost.GetDownloadUrl();

                // 5. Save metadata to DB
                var fileMetaData = new FileMetaData
                {
                    FileName = formFile.FileName,
                    Sender = sender,
                    Receiver = model.ReceiverUsername!,
                    EncryptedAESKey = finalEncryptedAesKey, // Use the key from your manual RSA
                    FilePath = null, // No file path for P2P
                    UploadedAt = DateTime.UtcNow
                };

                _dbContext.FileMetaDatas.Add(fileMetaData);
                await _dbContext.SaveChangesAsync();

                ViewBag.P2PDownloadUrl = downloadUrl;
                ViewBag.FileName = formFile.FileName;
                return View("HostAndShare"); // Ensure you have a view named HostAndShare.cshtml
            }

            // ================== SERVER-BASED LOGIC ==================
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            foreach (var formFile in model.Files)
            {
                if (formFile.Length > 0)
                {
                    byte[] fileBytes;
                    using (var ms = new MemoryStream())
                    {
                        await formFile.CopyToAsync(ms);
                        fileBytes = ms.ToArray();
                    }

                    // 1. Generate AES key
                    byte[] aesKey = EncryptionService.GenerateAesKey();

                    // 2. Encrypt the file content with the AES key and save to disk
                    string fileName = Path.GetFileName(formFile.FileName);
                    string storedFileName = Guid.NewGuid().ToString() + ".encrypted";
                    string encryptedFilePath = Path.Combine(uploadsFolder, storedFileName);
                    EncryptionService.EncryptFileFromBytes(fileBytes, encryptedFilePath, aesKey, out byte[] iv);

                    // 3. Encrypt the AES key using your manual RSACrypto
                    var keyParts = receiverUser.PublicKey.Split('|');
                    var n = BigInteger.Parse(keyParts[0]);
                    var e = BigInteger.Parse(keyParts[1]);

                    string aesKeyAsBase64 = Convert.ToBase64String(aesKey);
                    // The single source of the encrypted AES key
                    string finalEncryptedAesKey = RSACrypto.Encrypt(aesKeyAsBase64, n, e);

                    // 4. Save metadata to DB
                    var fileMetaData = new FileMetaData
                    {
                        FileName = fileName,
                        Sender = sender,
                        Receiver = model.ReceiverUsername!,
                        EncryptedAESKey = finalEncryptedAesKey, // Use the key from your manual RSA
                        FilePath = storedFileName, // Use the relative path for server storage
                        UploadedAt = DateTime.UtcNow
                    };

                    _dbContext.FileMetaDatas.Add(fileMetaData);
                }
            }

            await _dbContext.SaveChangesAsync();
            ViewBag.Message = "Files encrypted and sent successfully.";
            return View("Index");
        }
    }
}