using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.UnitOfWork;
using BusinessObject.Model;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminChatController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminChatController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index(Guid? customerId = null)
        {
            IEnumerable<Message> messages;
            if (customerId.HasValue)
            {
                messages = _unitOfWork.Message.GetRange(m => m.SenderId == customerId || m.AdminId == customerId, "Sender");
            }
            else
            {
                messages = _unitOfWork.Message.GetAll("Sender");
                customerId = messages.FirstOrDefault()?.SenderId;
            }

            ViewBag.CustomerId = customerId?.ToString();
            return View(messages);
        }

        [HttpGet]
        public IActionResult GetMessages(Guid customerId)
        {
            var messages = _unitOfWork.Message.GetRange(m => m.SenderId == customerId || m.AdminId == customerId, "Sender")
                .Select(m => new
                {
                    Id = m.Id,
                    SenderId = m.SenderId.ToString(),
                    SenderName = m.Sender?.UserName ?? "Unknown",
                    ReceiverId = m.AdminId?.ToString(),
                    Content = m.Content,
                    SentAt = m.SentAt.ToString("g"),
                    IsRead = m.IsRead
                })
                .ToList();

            return Json(messages);
        }
    }
} 