using Microsoft.AspNetCore.Mvc;
using SecureFileManagementSystem.Services;
using System.IO;
using System.Threading.Tasks;

namespace SecureFileManagementSystem.Controllers
{
    public class P2PController : Controller
    {
        [HttpGet]
        public IActionResult Host()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> HostUploadFile()
        {
            var file = Request.Form.Files["file"];
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a file.");
                return View("Host");
            }

            // Read file into memory
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            // Create a new host with a dynamic port
            var host = new P2PStreamHost(fileBytes, file.FileName);
            host.Start();

            // Store in TempData if needed (or ViewBag is fine here)
            ViewBag.DownloadUrl = host.GetDownloadUrl();
            ViewBag.Message = $"Hosting file: {file.FileName}";

            return View("Host");
        }

        [HttpGet]
        public IActionResult Receiver()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ReceiverDownload(string downloadUrl)
        {
            if (string.IsNullOrEmpty(downloadUrl))
            {
                ModelState.AddModelError("", "Please enter a valid URL.");
                return View("Receiver");
            }

            try
            {
                var client = new System.Net.Http.HttpClient();
                var response = await client.GetAsync(downloadUrl);

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Failed to download file from sender.");
                    return View("Receiver");
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                var contentDisposition = response.Content.Headers.ContentDisposition;
                var fileName = contentDisposition?.FileName?.Trim('"') ?? "downloaded_file";

                // Save file directly to receiver's device (e.g., Inbox folder)
                string inboxPath = Path.Combine(Directory.GetCurrentDirectory(), "Inbox");
                if (!Directory.Exists(inboxPath))
                    Directory.CreateDirectory(inboxPath);

                string filePath = Path.Combine(inboxPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);

                ViewBag.Message = $"File downloaded and saved to Inbox: {fileName}";
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
            }

            return View("Receiver");
        }
    }
}
