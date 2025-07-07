using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SecureFileManagement.Cryptography;
using SecureFileManagementSystem.Data;
using SecureFileManagementSystem.Models;
using SecureFileManagementSystem.Services;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace SecureFileManagementSystem.Controllers
{
    public class InboxController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public InboxController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var receiverUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(receiverUsername))
            {
                return RedirectToAction("Login", "Account");
            }
            
            var inboxFiles = _dbContext.FileMetaDatas.Where(f => f.Receiver == receiverUsername).OrderByDescending(f => f.UploadedAt).ToList();

            // Map FileMetaData to FileInboxViewModel
            var model = inboxFiles.Select(f => new FileInboxViewModel
            {
                Id = f.Id,
                SenderUsername = f.Sender,
                OriginalFileName = f.FileName,
                Timestamp = f.UploadedAt
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public IActionResult Download(int fileId)
        {
            var receiverUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(receiverUsername))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find file metadata by Id and ensure it belongs to current user
            var fileMeta = _dbContext.FileMetaDatas.FirstOrDefault(f => f.Id == fileId && f.Receiver == receiverUsername);
            if (fileMeta == null)
            {
                return NotFound("File not found or you do not have permission.");
            }

            // Find user to get private key
            var user = _dbContext.UserRecords.FirstOrDefault(u => u.Username == receiverUsername);
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            try
            {
                var keyParts = user.PrivateKey.Split('|');
                var n = BigInteger.Parse(keyParts[0]);
                var d = BigInteger.Parse(keyParts[1]);

                // Decrypt AES key using user's private RSA key
                string encryptedAesKey = fileMeta.EncryptedAESKey;
                string decryptedAesKeyString = RSACrypto.Decrypt(encryptedAesKey, n, d);

                // Convert the decrypted Base64 string back to byte
                byte[] aesKey = Convert.FromBase64String(decryptedAesKeyString);

                // Full path to encrypted file in wwwroot/uploads
                string encryptedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileMeta.FilePath);
                if (!System.IO.File.Exists(encryptedFilePath))
                {
                    return NotFound("Encrypted file not found.");
                }

                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(fileMeta.FileName, out string? contentType))
                {
                    contentType = "application/octet-stream";
                }

                // Open encrypted file stream
                var encryptedFileStream = new FileStream(encryptedFilePath, FileMode.Open, FileAccess.Read);

                // Read IV from beginning of file (first 16 bytes)
                byte[] iv = new byte[16];
                encryptedFileStream.Read(iv, 0, iv.Length);

                // Setup AES decryptor
                var aes = Aes.Create();
                aes.Key = aesKey;
                aes.IV = iv;
                var decryptor = aes.CreateDecryptor();

                // Create crypto stream to decrypt on-the-fly
                var cryptoStream = new CryptoStream(encryptedFileStream, decryptor, CryptoStreamMode.Read);

                // Return file stream result, disposing resources when done
                return new FileStreamResult(cryptoStream, contentType)
                {
                    FileDownloadName = fileMeta.FileName
                };
            }
            catch (Exception ex)
            {
                return BadRequest($"Error decrypting file: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int fileId)
        {
            var fileMeta = _dbContext.FileMetaDatas.FirstOrDefault(f => f.Id == fileId);
            if (fileMeta != null)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileMeta.FilePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                _dbContext.FileMetaDatas.Remove(fileMeta);
                _dbContext.SaveChanges();
                return Ok();  // return 200 OK on success
            }

            return BadRequest();  // return 400 if not found
        }

    }
}
