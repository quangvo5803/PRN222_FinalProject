using System.Security.Claims;
using BusinessObject.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Customer")]
    public partial class CustomerController : Controller
    {
        public IActionResult Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = _unitOfWork
                .ShoppingCart.GetRange(
                    c => c.UserId.ToString() == userId,
                    includeProperties: "Product,Product.ProductAvatar"
                )
                .ToList();

            if (!cartItems.Any())
            {
                TempData["error"] = "Cart is empty, cannot proceed to checkout.";
                return RedirectToAction("CartIndex");
            }

            var user = _unitOfWork.User.Get(u => u.Id.ToString() == userId);
            double totalPrice = 0;
            foreach (var item in cartItems)
            {
                if (item.Product != null)
                {
                    totalPrice += item.Product.Price * item.Count;
                }
            }
            double finalPrice = totalPrice;

            ViewBag.CartItems = cartItems;
            ViewBag.User = user;
            ViewBag.TotalPrice = totalPrice;
            ViewBag.FinalPrice = finalPrice;

            return View();
        }

        [HttpPost]
        public IActionResult ProcessPayment(
            string PhoneNumber,
            string Address,
            string PaymentMethod
        )
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.User.Get(u => u.Id.ToString() == userId);
            var cartItems = _unitOfWork
                .ShoppingCart.GetRange(
                    c => c.UserId.ToString() == userId,
                    includeProperties: "Product"
                )
                .ToList();
            double totalPrice = 0;
            foreach (var item in cartItems)
            {
                if (item.Product != null)
                {
                    totalPrice += item.Product.Price * item.Count;
                }
            }
            // Xử lý thanh toán
            if (PaymentMethod == "PayByCash")
            {
                var order = CreateOrder(cartItems, totalPrice, PhoneNumber, Address, PaymentMethod);
                TempData["success"] = "Đơn hàng đã được đặt thành công (Thanh toán khi nhận hàng).";
                return RedirectToAction("Order", new { orderId = order.Id });
            }
            else if (PaymentMethod == "VNPay")
            {
                var order = CreateOrder(cartItems, totalPrice, PhoneNumber, Address, PaymentMethod);
                var vnpayModel = new VnPaymentRequestModel
                {
                    OrderId = new Random().Next(10000, 100000),
                    Description = "Thanh toán đơn hàng",
                    CreateDate = DateTime.Now,
                    Amount = totalPrice,
                    Order = order,
                };
                string vnpayUrl = _vnPayService.CreatePaymentUrl(HttpContext, vnpayModel);
                return Redirect(vnpayUrl);
            }

            TempData["error"] = "Invalid payment method.";
            return RedirectToAction("Checkout");
        }

        public IActionResult VNPayReturn()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (response == null || response.VnPayResponseCode != "00")
            {
                if (int.TryParse(response.OrderDescription, out int orderId))
                {
                    var orderDetailList = _unitOfWork.OrderDetail.GetRange(o => o.Id == orderId);
                    _unitOfWork.OrderDetail.RemoveRange(orderDetailList);
                    _unitOfWork.Save();
                    var order = _unitOfWork.Order.Get(o => o.Id == orderId);
                    _unitOfWork.Order.Remove(order);
                    _unitOfWork.Save();
                }
                ;
                TempData["error"] = "Payment failed.";
                return RedirectToAction("Checkout");
            }
            int.TryParse(response.OrderDescription, out int orderSuccessId);
            var cartItems = _unitOfWork.ShoppingCart.GetRange(c => c.UserId.ToString() == userId);

            _unitOfWork.ShoppingCart.RemoveRange(cartItems);
            _unitOfWork.Save();
            TempData["success"] = "Payment successful.";
            return RedirectToAction("Order", new { orderId = orderSuccessId });
        }
    }
}
