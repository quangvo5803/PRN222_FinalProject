using System.Security.Claims;
using BusinessObject.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Repositories.UnitOfWork;
using Ultility;
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

        public IActionResult OrderHistory()
        {
            return View();
        }

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
            else
            {
                TempData["error"] = "No images uploaded";
            }
            //return Ok("Images count: " + Images?.Count);
            TempData["success"] = "Feedback submitted successfully";
            return RedirectToAction("Menu", "Home");

        public IActionResult CartIndex()
        {
            var cartItems = _unitOfWork.ShoppingCart.GetRange(
                c => c.UserId.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier),
                includeProperties: "Product"
            );
            return View(cartItems);
        }

        public IActionResult Product()
        {
            var products = _unitOfWork.Product.GetAll();
            return View(products);
        }

        public IActionResult ProductDetail(int id)
        {
            var product = _unitOfWork.Product.Get(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        public IActionResult AddToCart(int productId, int count = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "User");
            }

            var product = _unitOfWork.Product.Get(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            var existingCartItem = _unitOfWork.ShoppingCart.Get(c =>
                c.UserId.ToString() == userId && c.ProductId == productId
            );

            if (existingCartItem != null)
            {
                existingCartItem.Count += count;
                _unitOfWork.ShoppingCart.Update(existingCartItem);
            }
            else
            {
                var cartItem = new ShoppingCart
                {
                    UserId = Guid.Parse(userId),
                    ProductId = productId,
                    Count = count,
                };
                _unitOfWork.ShoppingCart.Add(cartItem);
            }

            _unitOfWork.Save();
            return RedirectToAction("CartIndex");
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int cartId)
        {
            var cartItem = _unitOfWork.ShoppingCart.Get(
                c => c.Id == cartId,
                includeProperties: "Product"
            );
            if (cartItem == null)
            {
                return Json(
                    new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." }
                );
            }

            _unitOfWork.ShoppingCart.Remove(cartItem);
            _unitOfWork.Save();

            // Tính lại tổng giá
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = _unitOfWork
                .ShoppingCart.GetRange(c => c.UserId.ToString() == userId)
                .ToList();
            double totalPrice = 0;
            if (cartItems.Count > 0)
            {
                foreach (var item in cartItems)
                {
                    if (item.Product != null)
                    {
                        totalPrice += item.Product.Price * item.Count;
                    }
                }
            }
            return Json(
                new
                {
                    success = true,
                    totalPrice = totalPrice,
                    cartCount = cartItems.Count,
                }
            );
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int cartId, int change)
        {
            var cartItem = _unitOfWork.ShoppingCart.Get(
                c => c.Id == cartId,
                includeProperties: "Product"
            );
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (cartItem == null)
            {
                return Json(
                    new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." }
                );
            }

            // Tính số lượng mới
            int newCount = cartItem.Count + change;

            // Không cho phép số lượng nhỏ hơn 1
            if (newCount < 1)
            {
                var cartItems = _unitOfWork
                    .ShoppingCart.GetRange(c => c.UserId.ToString() == userId)
                    .ToList();

                double totalPrice = cartItems
                    .Where(item => item.Product != null)
                    .Sum(item => item.Product.Price * item.Count);

                return Json(
                    new
                    {
                        success = false,
                        message = "Số lượng không thể nhỏ hơn 1.",
                        newCount = cartItem.Count,
                        itemTotal = cartItem.Product.Price * cartItem.Count,
                        totalPrice = totalPrice,
                        cartCount = cartItems.Count,
                        removed = false,
                    }
                );
            }

            // Cập nhật số lượng
            cartItem.Count = newCount;
            _unitOfWork.ShoppingCart.Update(cartItem);
            _unitOfWork.Save();

            // Tính lại tổng giá sau khi cập nhật
            var cartItemsUpdated = _unitOfWork
                .ShoppingCart.GetRange(c => c.UserId.ToString() == userId)
                .ToList();

            double totalPriceUpdated = cartItemsUpdated
                .Where(item => item.Product != null)
                .Sum(item => item.Product.Price * item.Count);

            return Json(
                new
                {
                    success = true,
                    newCount = cartItem.Count,
                    itemTotal = cartItem.Product.Price * cartItem.Count,
                    totalPrice = totalPriceUpdated,
                    cartCount = cartItemsUpdated.Count,
                    removed = false,
                }
            );
        }

        public IActionResult Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "User");
            }

            var cartItems = _unitOfWork
                .ShoppingCart.GetRange(
                    c => c.UserId.ToString() == userId,
                    includeProperties: "Product"
                )
                .ToList();

            if (!cartItems.Any())
            {
                TempData["error"] = "Giỏ hàng trống, không thể tiến hành thanh toán.";
                return RedirectToAction("CartIndex");
            }

            var user = _unitOfWork.User.Get(u => u.Id.ToString() == userId);
            double totalPrice = cartItems.Sum(item => item.Product.Price * item.Count);
            double finalPrice = totalPrice - 60 + 14;

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
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "User");
            }

            var cartItems = _unitOfWork
                .ShoppingCart.GetRange(
                    c => c.UserId.ToString() == userId,
                    includeProperties: "Product"
                )
                .ToList();

            if (!cartItems.Any())
            {
                TempData["error"] = "Giỏ hàng trống, không thể tiến hành thanh toán.";
                return RedirectToAction("CartIndex");
            }

            // Tạo đơn hàng
            var order = new Order
            {
                UserId = Guid.Parse(userId),
                TotalPrice = cartItems.Sum(item => item.Product.Price * item.Count),
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

            // Xử lý thanh toán
            if (PaymentMethod == "PayByCash")
            {
                _unitOfWork.ShoppingCart.RemoveRange(cartItems);
                _unitOfWork.Save();

                TempData["success"] = "Đơn hàng đã được đặt thành công (Thanh toán khi nhận hàng).";
                return RedirectToAction("Order", new { orderId = order.Id });
            }
            else if (PaymentMethod == "VNPay")
            {
                string vnpayUrl = GenerateVNPayUrl(order);
                return Redirect(vnpayUrl);
            }

            TempData["error"] = "Phương thức thanh toán không hợp lệ.";
            return RedirectToAction("Checkout");
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
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        public IActionResult CancelOrder(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = _unitOfWork.Order.Get(o =>
                o.Id == orderId && o.UserId.ToString() == userId
            );

            if (order == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại." });
            }

            if (order.Status != OrderStatus.Pending)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = "Không thể hủy đơn hàng đã hoàn tất hoặc đã bị hủy.",
                    }
                );
            }

            order.Status = OrderStatus.Cancelled;
            _unitOfWork.Save();

            return Json(new { success = true, message = "Đơn hàng đã được hủy thành công." });
        }

        private string GenerateVNPayUrl(Order order)
        {
            string vnp_Returnurl = "https://localhost:7129/Customer/VNPayReturn";
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string vnp_TmnCode = "804CH13L";
            string vnp_HashSecret = "UN5PBLKPRFANRBRLN969HT37IOW48ZN7";

            VnPayLibrary vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((int)(order.TotalPrice * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {order.Id}");
            vnpay.AddRequestData("vnp_OrderType", "250000");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString());

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return paymentUrl;
        }

        public IActionResult VNPayReturn()
        {
            var vnpayData = Request.Query;
            VnPayLibrary vnpay = new VnPayLibrary();
            foreach (var key in vnpayData.Keys)
            {
                vnpay.AddResponseData(key, vnpayData[key]);
            }

            string vnp_TmnCode = "804CH13L";
            string vnp_HashSecret = "UN5PBLKPRFANRBRLN969HT37IOW48ZN7";
            long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_SecureHash = vnpay.GetResponseData("vnp_SecureHash");

            //Console.WriteLine($"vnp_ResponseCode: {vnp_ResponseCode}");
            //Console.WriteLine($"vnp_SecureHash: {vnp_SecureHash}");
            bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
            //Console.WriteLine($"ValidateSignature: {isValidSignature}");

            if (isValidSignature && vnp_ResponseCode == "00")
            {
                var order = _unitOfWork.Order.Get(o => o.Id == orderId);
                if (order != null && order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Completed;
                    var cartItems = _unitOfWork
                        .ShoppingCart.GetRange(c => c.UserId == order.UserId)
                        .ToList();
                    _unitOfWork.ShoppingCart.RemoveRange(cartItems);
                    _unitOfWork.Save();

                    TempData["success"] = "Thanh toán VNPay thành công!";
                }
            }
            else
            {
                TempData["error"] = $"Thanh toán VNPay thất bại. Mã lỗi: {vnp_ResponseCode}";
            }

            return RedirectToAction("Order", new { orderId = orderId });
        }
    }
}
