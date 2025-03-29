using System.Security.Claims;
using BusinessObject.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Repositories.UnitOfWork;
using WebApp.Utility;
using static System.Net.Mime.MediaTypeNames;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Customer,Admin")]
    public class CustomerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CustomerController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Profile()
        {
            var emailUser = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            if (emailUser == null)
            {
                return NotFound();
            }
            var user = _unitOfWork.User.Get(u => u.Email == emailUser);
            return View(user);
        }

        [HttpPost]
        public IActionResult Profile(User user)
        {
            _unitOfWork.User.Update(user);
            _unitOfWork.Save();
            TempData["success"] = "Update information successfully";
            return View(user);
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string oldpassword, string password, string repassword)
        {
            var emailUser = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            if (emailUser == null)
            {
                TempData["error"] = "User not found";
                return RedirectToAction("Profile", "Customer");
            }
            var user = _unitOfWork.User.Get(u => u.Email == emailUser);

            if (user == null || !PasswordHasher.VerifyPassword(oldpassword, user.PasswordHash))
            {
                TempData["error"] = "Old password is incorrect";
                return View();
            }
            if (password != repassword)
            {
                TempData["error"] = "New password and confirm password are not the same";
                return View();
            }
            user.PasswordHash = PasswordHasher.HashPassword(password);
            _unitOfWork.User.Update(user);
            _unitOfWork.Save();
            TempData["success"] = "Change password successfully";
            return View();
        }


        //Customer Feedback
        [HttpPost]
        public async Task<IActionResult> SubmitFeedBack(int productId, int feedbackStars, string feedbackContent, List<IFormFile> Images)
        {
            var product = _unitOfWork.Product.Get(p => p.Id == productId);

            if (product == null)
            {
                TempData["error"] = "Product not found";
                //tạm thời
                return RedirectToAction("Menu", "Home");
            }

            if (feedbackStars < 1 || feedbackStars > 5)
            {
                TempData["error"] = "Choice ";
                return RedirectToAction("Menu", "Home");
            }

            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var summit = new Feedback
            {
               ProductId = product.Id,
               FeedbackContent = feedbackContent,
               FeedbackStars = feedbackStars,
               UserId = Guid.Parse(userId),
            };
            _unitOfWork.Feedback.Add(summit);
            _unitOfWork.Save();

            //Save img
            string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img/feedbacks");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
            if (Images != null || Images.Count > 0)
            {
                foreach (var steam in Images)
                {
                    string uniqueFileName =
                    Guid.NewGuid().ToString() + "_" + Path.GetExtension(steam.FileName);

                    var filePath = Path.Combine(uploadFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                            await steam.CopyToAsync(fileStream);
                    }

                    var itemImage = new ItemImage
                    {
                        ImagePath = uniqueFileName,
                        ProductId = productId,
                        FeedbackId = summit.Id,

                    };
                    _unitOfWork.ItemImage.Add(itemImage);                   
                }
                _unitOfWork.Save();
            }
            else
            {
                TempData["error"] = "No images uploaded";
            }  
            //return Ok("Images count: " + Images?.Count);
            TempData["success"] = "Feedback submitted successfully";
            return RedirectToAction("Menu", "Home");
        }

    }
}
