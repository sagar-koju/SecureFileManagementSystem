using Microsoft.AspNetCore.Mvc;
using SecureFileManagement.Cryptography; // Manual crypto
using System.Security.Cryptography; // .NET's built-in crypto
using System.Text;

namespace SecureFileManagementSystem.Controllers
{
    public class DemoController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DemonstrateSHA256(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
            {
                ViewBag.Error = "Please provide some text to hash.";
                return View("Index");
            }

            // 1. Use your manual, corrected SHA256 hasher
            string manualHash = SHA256Hasher.ComputeHash(inputText);
            ViewBag.ManualHash = manualHash;

            // 2. Use the built-in .NET SHA256 for comparison
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputText));
                ViewBag.BuiltInHash = BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }

            ViewBag.InputText = inputText;
            return View("Index");
        }
    }
}