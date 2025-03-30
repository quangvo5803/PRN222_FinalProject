using Microsoft.AspNetCore.SignalR;
using BusinessObject.Model;
using Repositories.UnitOfWork;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatHub(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task SendMessage(Guid senderId, Guid? receiverId, string content)
        {
            var sender = _unitOfWork.User.Get(u => u.Id == senderId);
            if (sender == null) return;

            var message = new Message
            {
                SenderId = senderId,
                AdminId = receiverId, // Nếu null thì gửi đến tất cả Admin (nếu người gửi là Customer)
                Content = content,
                SentAt = DateTime.Now,
                IsRead = false,
                IsResolved = false
            };

            _unitOfWork.Message.Add(message);
            _unitOfWork.Save();

            // Chuẩn bị dữ liệu gửi qua SignalR
            var messageData = new
            {
                Id = message.Id,
                SenderId = senderId.ToString(),
                SenderName = sender.UserName ?? sender.Email,
                ReceiverId = receiverId?.ToString(),
                Content = message.Content,
                SentAt = message.SentAt.ToString("g"),
                IsRead = message.IsRead
            };

            // Gửi tin nhắn đến người nhận
            if (receiverId.HasValue)
            {
                // Gửi đến người nhận cụ thể (Customer hoặc Admin)
                await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", messageData);
            }
            else if (sender.Role == UserRole.Customer)
            {
                // Nếu Customer gửi và không chỉ định Admin, gửi đến tất cả Admin
                var admins = _unitOfWork.User.GetRange(u => u.Role == UserRole.Admin);
                foreach (var admin in admins)
                {
                    await Clients.User(admin.Id.ToString()).SendAsync("ReceiveMessage", messageData);
                }
            }

            // Gửi lại tin nhắn cho người gửi để hiển thị trong box chat
            await Clients.User(senderId.ToString()).SendAsync("ReceiveMessage", messageData);
        }

        public async Task RequestSupport(Guid customerId)
        {
            var customer = _unitOfWork.User.Get(u => u.Id == customerId);
            if (customer != null)
            {
                var admins = _unitOfWork.User.GetRange(u => u.Role == UserRole.Admin);
                foreach (var admin in admins)
                {
                    await Clients.User(admin.Id.ToString()).SendAsync("SupportRequested", customer.UserName ?? customer.Email);
                }
            }
        }

        public async Task MarkAsRead(int messageId)
        {
            var message = _unitOfWork.Message.Get(m => m.Id == messageId);
            if (message != null && !message.IsRead)
            {
                message.IsRead = true;
                _unitOfWork.Message.Update(message);
                _unitOfWork.Save();
                await Clients.All.SendAsync("MessageRead", messageId);
            }
        }
    }
}