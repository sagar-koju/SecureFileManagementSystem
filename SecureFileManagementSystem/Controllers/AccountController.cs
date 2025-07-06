using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureFileManagementSystem.Data;
using SecureFileManagementSystem.Models;
using SecureFileManagement.Cryptography;
using System.Linq;

namespace SecureFileManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public AccountController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_dbContext.UserRecords.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("", "Username already exists.");
                return View(model);
            }

            string hashedPassword = SHA256Hasher.ComputeHash(model.Password);
            var (n, e, d) = RSAKeyGenerator.GenerateKeys();

            string publicKey = $"{n}|{e}";
            string privateKey = $"{n}|{d}";

            var userRecord = new UserRecord
            {
                Username = model.Username,
                PasswordHash = hashedPassword,
                PublicKey = publicKey,
                PrivateKey = privateKey
            };

            _dbContext.UserRecords.Add(userRecord);
            _dbContext.SaveChanges();

            // Set success message
            ViewBag.Message = "User registered successfully! You can now login.";
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var userRecord = _dbContext.UserRecords.FirstOrDefault(u => u.Username == username);

            if (userRecord == null || SHA256Hasher.ComputeHash(password) != userRecord.PasswordHash)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            HttpContext.Session.SetString("Username", username);
            return RedirectToAction("Index", "Inbox");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
