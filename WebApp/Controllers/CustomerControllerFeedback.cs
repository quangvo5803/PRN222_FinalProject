using System.Security.Claims;
using BusinessObject.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Customer")]
    public partial class CustomerController : Controller
    {
        //Customer Feedback
        [HttpPost]
        public async Task<IActionResult> SubmitFeedBack(
            int productId,
            int feedbackStars,
            string feedbackContent,
            List<IFormFile> images
        )
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
            if (images != null || images.Count > 0)
            {
                foreach (var steam in images)
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
            //return Ok("Images count: " + Images?.Count);
            TempData["success"] = "Feedback submitted successfully";
            return RedirectToAction("Menu", "Home");
        }
    }
}
