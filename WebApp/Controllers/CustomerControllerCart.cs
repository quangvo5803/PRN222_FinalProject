using System.Security.Claims;
using BusinessObject.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Customer")]
    public partial class CustomerController : Controller
    {
        public IActionResult CartIndex()
        {
            var cartItems = _unitOfWork.ShoppingCart.GetRange(
                c => c.UserId.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier),
                includeProperties: "Product,Product.ProductAvatar"
            );
            return View(cartItems);
        }

        public IActionResult AddToCart(int productId, int count = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var product = _unitOfWork.Product.Get(p => p.Id == productId);
            if (product == null)
            {
                TempData["error"] = " Product not found";
                return RedirectToAction("Index", "Home");
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
            TempData["success"] = "Product added to cart successfully.";
            return RedirectToAction("ProductDetail", "Home", new { id = product.Id });
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
                return Json(new { success = false, message = "Product does not exist in cart." });
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
                    totalPrice,
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

                return Json(
                    new
                    {
                        success = false,
                        message = "The quantity cannot be less than 1.",
                        newCount = cartItem.Count,
                        itemTotal = cartItem.Product?.Price * cartItem.Count,
                        totalPrice,
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
                .ShoppingCart.GetRange(
                    c => c.UserId.ToString() == userId,
                    includeProperties: "Product"
                )
                .ToList();

            double totalPriceUpdated = 0;
            foreach (var item in cartItemsUpdated)
            {
                if (item.Product != null)
                {
                    totalPriceUpdated += item.Product.Price * item.Count;
                }
            }

            return Json(
                new
                {
                    success = true,
                    newCount = cartItem.Count,
                    itemTotal = cartItem.Product?.Price * cartItem.Count,
                    totalPrice = totalPriceUpdated,
                    cartCount = cartItemsUpdated.Count,
                    removed = false,
                }
            );
        }
    }
}
