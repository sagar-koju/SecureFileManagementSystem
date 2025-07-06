using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureFileManagementSystem.Data;
using SecureFileManagementSystem.Models;
using SecureFileManagementSystem.Services;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SecureFileManagementSystem.Controllers
{
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public FileController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Upload()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UploadFile(FileUploadViewModel model)
        {
            if (!ModelState.IsValid || model.Files == null || model.Files.Count == 0)
            {
                ViewBag.Error = "Please select at least one file.";
                return View("Upload", model);
            }

            var sender = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(sender))
                return RedirectToAction("Login", "Account");

            var receiverUser = _dbContext.UserRecords.FirstOrDefault(u => u.Username == model.ReceiverUsername);
            if (receiverUser == null)
            {
                ModelState.AddModelError("", "Receiver username does not exist.");
                return View("Upload", model);
            }

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            foreach (var file in model.Files)
            {
                if (file.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    // Generate AES key
                    byte[] aesKey = EncryptionService.GenerateAesKey();

                    // Encrypt file
                    string storedFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}.encrypted";
                    string encryptedFilePath = Path.Combine(uploadsFolder, storedFileName);
                    EncryptionService.EncryptFileFromBytes(fileBytes, encryptedFilePath, aesKey, out byte[] iv);

                    // Encrypt AES key with receiver's RSA public key
                    byte[] encryptedKeyBytes = RSAService.EncryptAESKeyWithRSA(aesKey, receiverUser.PublicKey);
                    string encryptedAesKey = Convert.ToBase64String(encryptedKeyBytes);

                    // Save metadata
                    var fileMetaData = new FileMetaData
                    {
                        FileName = file.FileName,
                        Sender = sender,
                        Receiver = model.ReceiverUsername!,
                        FilePath = Path.Combine("uploads", storedFileName).Replace("\\", "/"),
                        EncryptedAESKey = encryptedAesKey,
                        UploadedAt = DateTime.Now
                    };

                    _dbContext.FileMetaDatas.Add(fileMetaData);
                }
            }

            _dbContext.SaveChanges();
            ViewBag.Message = "Files encrypted and uploaded successfully.";
            return View("Upload");
        }

        [HttpGet]
        public IActionResult Download(int fileId)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            // Find file metadata for current user
            var fileMeta = _dbContext.FileMetaDatas
                .FirstOrDefault(f => f.Id == fileId && f.Receiver == username);

            if (fileMeta == null)
                return NotFound("File not found or access denied.");

            var user = _dbContext.UserRecords.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return Unauthorized();

            var fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileMeta.FilePath);

            if (!System.IO.File.Exists(fullFilePath))
                return NotFound("Encrypted file not found.");

            // Decrypt AES key using private key
            byte[] aesKey;
            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(user.PrivateKey);
                aesKey = rsa.Decrypt(Convert.FromBase64String(fileMeta.EncryptedAESKey), RSAEncryptionPadding.Pkcs1);
            }

            byte[] decryptedBytes;

            using (var fsIn = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read))
            {
                byte[] iv = new byte[16];
                int bytesRead = fsIn.Read(iv, 0, iv.Length);
                if (bytesRead < iv.Length)
                    return BadRequest("Invalid encrypted file format.");

                using var aes = Aes.Create();
                aes.Key = aesKey;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var cryptoStream = new CryptoStream(fsIn, decryptor, CryptoStreamMode.Read);
                using var msOut = new MemoryStream();
                cryptoStream.CopyTo(msOut);

                decryptedBytes = msOut.ToArray();
            }

            return File(decryptedBytes, "application/octet-stream", fileMeta.FileName);
        }
    }
}
