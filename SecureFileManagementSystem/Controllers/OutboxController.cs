using Microsoft.AspNetCore.Mvc;
using SecureFileManagementSystem.Data;
using System.Linq;

namespace SecureFileManagementSystem.Controllers
{
    public class OutboxController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public OutboxController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var senderUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(senderUsername))
            {
                return RedirectToAction("Login", "Account");
            }

            var sentFiles = _dbContext.FileMetaDatas
                .Where(f => f.Sender == senderUsername)
                .OrderByDescending(f => f.UploadedAt)
                .ToList();

            return View(sentFiles);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Delete(int fileId)
        //{
        //    var fileMeta = _dbContext.FileMetaDatas.FirstOrDefault(f => f.Id == fileId);
        //    if (fileMeta != null)
        //    {
        //        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileMeta.FilePath);
        //        if (System.IO.File.Exists(fullPath))
        //        {
        //            System.IO.File.Delete(fullPath);
        //        }

        //        _dbContext.FileMetaDatas.Remove(fileMeta);
        //        _dbContext.SaveChanges();
        //        return Ok();
        //    }

        //    return BadRequest();
        //}

    }
}
