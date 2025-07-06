using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing.Imaging;
using System.IO;

namespace SecureFileManagementSystem.Controllers
{
    public class ShareController : Controller
    {
        [HttpGet]
        public IActionResult GenerateQRCode(string url)
        {
            var qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new SvgQRCode(qrCodeData);
            string svgImage = qrCode.GetGraphic(5);

            return Content(svgImage, "image/svg+xml");
        }



    }

}
