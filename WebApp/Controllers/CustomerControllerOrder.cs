using System.Security.Claims;
using BusinessObject.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Customer")]
    public partial class CustomerController : Controller
    {
        public IActionResult OrderHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orderList = _unitOfWork.Order.GetRange(
                o => o.UserId.ToString() == userId,
                includeProperties: "OrderDetails.Product.ProductAvatar,OrderDetails.Product.Category"
            );
            return View(orderList);
        }

        public IActionResult Order(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = _unitOfWork.Order.Get(
                o => o.Id == orderId && o.UserId.ToString() == userId,
                includeProperties: "OrderDetails.Product"
            );

            if (order == null)
            {
                TempData["error"] = "Order not found.";
                return RedirectToAction("OrderHistory", "Customer");
            }

            return View(order);
        }

        private Order CreateOrder(
            List<ShoppingCart> cartItems,
            double totalPrice,
            string PhoneNumber,
            string Address,
            string PaymentMethod
        )
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Tạo đơn hàng
            var order = new Order
            {
                UserId = Guid.Parse(userId),
                TotalPrice = totalPrice,
                PaymentMethod = PaymentMethod,
                ShippingAddress = Address,
                PhoneNumber = PhoneNumber,
                Status = OrderStatus.Pending,
            };

            _unitOfWork.Order.Add(order);
            _unitOfWork.Save();
            // Tạo chi tiết đơn hàng
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Count,
                    UnitPrice = item.Product.Price,
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
            }
            if (PaymentMethod == "PayByCash")
            {
                _unitOfWork.ShoppingCart.RemoveRange(cartItems);
            }
            _unitOfWork.Save();
            return order;
        }
    }
}
