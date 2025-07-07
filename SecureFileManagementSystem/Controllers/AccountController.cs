using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureFileManagement.Cryptography;
using SecureFileManagementSystem.Data;
using SecureFileManagementSystem.Models;
using System.Linq;
using System.Security.Cryptography;

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
            // Generate a random salt
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            // Prepend the salt to the password and then hash
            string saltedPassword = salt + model.Password;


            string hash = SHA256Hasher.ComputeHash(saltedPassword);

            // Store the salt and hash together
            string storedPasswordHash = $"{salt}:{hash}";

            var (n, e, d) = RSAKeyGenerator.GenerateKeys(1024);

            string publicKey = $"{n}|{e}";
            string privateKey = $"{n}|{d}";

            var userRecord = new UserRecord
            {
                Username = model.Username,
                PasswordHash = storedPasswordHash,
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

            if (userRecord == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }
            // Split the stored string to get the salt and the original hash
            var passwordParts = userRecord.PasswordHash.Split(':');
            if (passwordParts.Length != 2)
            {
                // Handle error for malformed hash in DB
                ViewBag.Error = "Invalid credential format.";
                return View();
            }
            string salt = passwordParts[0];
            string storedHash = passwordParts[1];

            // 2. Apply the same salt to the login password attempt and hash it
            string saltedPasswordAttempt = salt + password;
            string newHash = SHA256Hasher.ComputeHash(saltedPasswordAttempt);

            // 3. Compare the new hash with the stored hash
            if (newHash != storedHash)
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
