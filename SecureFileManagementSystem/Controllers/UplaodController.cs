// Corrected File: Controllers/UploadController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; // Make sure this is included
using SecureFileManagement.Cryptography;
using SecureFileManagementSystem.Data;
using SecureFileManagementSystem.Hubs;     // Make sure this is included
using SecureFileManagementSystem.Models;
using SecureFileManagementSystem.Services;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SecureFileManagementSystem.Controllers
{
    public class UploadController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<NotificationHub> _hubContext; // Inject Hub

        // --- CONSTRUCTOR FIXED ---
        public UploadController(ApplicationDbContext dbContext, IHubContext<NotificationHub> hubContext)
        {
            _dbContext = dbContext;
            _hubContext = hubContext; // Semicolon was missing and parameter was missing
        }

        // Action to show the upload form, and pre-fill receiver if provided in URL
        [HttpGet]
        public IActionResult Index(string? receiver, bool isP2P = false) // Add the 'isP2P' parameter
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
                return RedirectToAction("Login", "Account");

            var model = new FileUploadViewModel();

            // If a receiver is specified (likely from the Peers page), pre-fill it.
            if (!string.IsNullOrEmpty(receiver))
            {
                model.ReceiverUsername = receiver;
            }

            // This is the key part. We pass the 'isP2P' flag to the view.
            ViewBag.IsP2P = isP2P;

            // If it's not a P2P transfer and no receiver was specified, the user will have to type one in.
            return View(model);
        }

        // This view is shown after a P2P link is generated
        [HttpGet]
        public IActionResult HostAndShare()
        {
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
                    return View("Index", model);
                }

                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await formFile.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }

                byte[] aesKey = EncryptionService.GenerateAesKey();
                byte[] iv;
                byte[] encryptedBytes = EncryptionService.EncryptBytes(fileBytes, aesKey, out iv);
                byte[] encryptedBytesWithIv = iv.Concat(encryptedBytes).ToArray();

                var keyParts = receiverUser.PublicKey.Split('|');
                var n = BigInteger.Parse(keyParts[0]);
                var e = BigInteger.Parse(keyParts[1]);
                string aesKeyAsBase64 = Convert.ToBase64String(aesKey);
                string finalEncryptedAesKey = RSACrypto.Encrypt(aesKeyAsBase64, n, e);

                var p2pHost = new P2PStreamHost(encryptedBytesWithIv, formFile.FileName);
                p2pHost.Start();
                string downloadUrl = p2pHost.GetDownloadUrl();

                var fileMetaData = new FileMetaData
                {
                    FileName = formFile.FileName,
                    Sender = sender,
                    Receiver = model.ReceiverUsername!,
                    EncryptedAESKey = finalEncryptedAesKey,
                    FilePath = null, // No file path for P2P
                    UploadedAt = DateTime.UtcNow
                };
                _dbContext.FileMetaDatas.Add(fileMetaData);
                await _dbContext.SaveChangesAsync();

                // --- ADD THIS TO SEND THE NOTIFICATION ---
                await _hubContext.Clients.Group(model.ReceiverUsername!).SendAsync(
                    "ReceiveFileNotification", // Function name the client will listen for
                    sender,
                    formFile.FileName,
                    downloadUrl
                );
                // --- END OF NEW CODE ---

                ViewBag.P2PDownloadUrl = downloadUrl;
                ViewBag.FileName = formFile.FileName;
                return View("HostAndShare");
            }

            // ================== SERVER-BASED LOGIC ==================
            // (This part remains the same)
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            foreach (var formFile in model.Files)
            {
                if (formFile.Length > 0)
                {
                    byte[] fileBytes;
                    using (var ms = new MemoryStream()) { await formFile.CopyToAsync(ms); fileBytes = ms.ToArray(); }

                    byte[] aesKey = EncryptionService.GenerateAesKey();
                    string fileName = Path.GetFileName(formFile.FileName);
                    string storedFileName = Guid.NewGuid().ToString() + ".encrypted";
                    string encryptedFilePath = Path.Combine(uploadsFolder, storedFileName);
                    EncryptionService.EncryptFileFromBytes(fileBytes, encryptedFilePath, aesKey, out byte[] iv);

                    var keyParts = receiverUser.PublicKey.Split('|');
                    var n = BigInteger.Parse(keyParts[0]);
                    var e = BigInteger.Parse(keyParts[1]);
                    string aesKeyAsBase64 = Convert.ToBase64String(aesKey);
                    string finalEncryptedAesKey = RSACrypto.Encrypt(aesKeyAsBase64, n, e);

                    var fileMetaData = new FileMetaData
                    {
                        FileName = fileName,
                        Sender = sender,
                        Receiver = model.ReceiverUsername!,
                        EncryptedAESKey = finalEncryptedAesKey,
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