using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SecureFileManagementSystem.Models;
using SecureFileManagementSystem.Services;
using SecureFileManagementSystem.Data;
using System.Security.Cryptography;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

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

            if (model.UseP2P)
            {
                // Only allow one file in P2P mode
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

                // Generate AES key and encrypt file bytes
                byte[] aesKey = EncryptionService.GenerateAesKey();
                byte[] iv;
                byte[] encryptedBytes = EncryptionService.EncryptBytes(fileBytes, aesKey, out iv);

                // Prepend IV to encrypted bytes for streaming
                byte[] encryptedBytesWithIv = iv.Concat(encryptedBytes).ToArray();

                // Start P2P host streaming the encrypted bytes with prepended IV
                var p2pHost = new P2PStreamHost(encryptedBytesWithIv, formFile.FileName);
                p2pHost.Start();

                string downloadUrl = p2pHost.GetDownloadUrl();

                // Encrypt AES key with receiver's RSA public key
                byte[] encryptedKeyBytes = RSAService.EncryptAESKeyWithRSA(aesKey, receiverUser.PublicKey);
                string encryptedAesKey = Convert.ToBase64String(encryptedKeyBytes);

                // Save metadata without physical file path since no disk save
                var fileMetaData = new FileMetaData
                {
                    FileName = formFile.FileName,
                    Sender = sender,
                    Receiver = model.ReceiverUsername!,
                    EncryptedAESKey = encryptedAesKey,
                    FilePath = null,
                    UploadedAt = DateTime.UtcNow
                };

                _dbContext.FileMetaDatas.Add(fileMetaData);
                await _dbContext.SaveChangesAsync();

                ViewBag.P2PDownloadUrl = downloadUrl;
                return View("HostAndShare"); // Create this view to show QR + link
            }

            // Server-based encryption and saving
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

                    byte[] aesKey = EncryptionService.GenerateAesKey();

                    string fileName = Path.GetFileName(formFile.FileName);
                    string storedFileName = Guid.NewGuid().ToString() + ".encrypted";
                    string encryptedFilePath = Path.Combine(uploadsFolder, storedFileName);
                    EncryptionService.EncryptFileFromBytes(fileBytes, encryptedFilePath, aesKey, out byte[] iv);

                    byte[] encryptedKeyBytes = RSAService.EncryptAESKeyWithRSA(aesKey, receiverUser.PublicKey);
                    string encryptedAesKey = Convert.ToBase64String(encryptedKeyBytes);

                    var fileMetaData = new FileMetaData
                    {
                        FileName = fileName,
                        Sender = sender,
                        Receiver = model.ReceiverUsername!,
                        EncryptedAESKey = encryptedAesKey,
                        FilePath = storedFileName,
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
