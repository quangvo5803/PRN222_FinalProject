using BusinessObject.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.UnitOfWork;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var feedbacks = _unitOfWork.Feedback.GetAll();
            ViewBag.AvgRating = feedbacks.Any() ? feedbacks.Average(f => f.FeedbackStars) : 0;
            ViewBag.UserCount = _unitOfWork.User.GetAll().ToList().Count;
            return View();
        }

        //Start CRUD Product
        public IActionResult ProductList()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetAllProduct()
        {
            var products = _unitOfWork
                .Product.GetAll(includeProperties: "Category,ProductAvatar,Feedbacks")
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    CategoryName = p.Category?.Name,
                    AvgRating = p.Feedbacks.Any() ? p.Feedbacks.Average(f => f.FeedbackStars) : 0,
                    FeedbackCount = p.Feedbacks.Count(),
                })
                .ToList();
            ;
            return Json(new { data = products });
        }

        [HttpGet]
        public IActionResult CreateProduct()
        {
            ViewBag.Categories = _unitOfWork.Category.GetAll(); //Take all categories to show in dropdown list
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(
            Product product,
            IFormFile? avatar,
            List<IFormFile>? gallery
        )
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Product.Add(product);
                _unitOfWork.Save();
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img/products");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }
                //Add Avatar
                if (avatar != null)
                {
                    string avatarName =
                        Guid.NewGuid().ToString() + "_" + Path.GetExtension(avatar.FileName);
                    string avatarPath = Path.Combine(uploadFolder, avatarName);

                    using (var steam = new FileStream(avatarPath, FileMode.Create))
                    {
                        await avatar.CopyToAsync(steam);
                    }

                    //Save to database
                    var productAvatar = new ItemImage
                    {
                        ImagePath = avatarName,
                        ProductId = product.Id,
                    };
                    _unitOfWork.ItemImage.Add(productAvatar);
                    _unitOfWork.Save();
                    product.ProductAvatarId = productAvatar.Id;
                }
                //Add Gallery
                if (gallery != null && gallery.Count > 0)
                {
                    foreach (var image in gallery)
                    {
                        string imageName =
                            Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                        string imagePath = Path.Combine(uploadFolder, imageName);
                        using (var steam = new FileStream(imagePath, FileMode.Create))
                        {
                            await image.CopyToAsync(steam);
                        }
                        //Save to database
                        var productImage = new ItemImage
                        {
                            ImagePath = imageName,
                            ProductId = product.Id,
                        };
                        _unitOfWork.ItemImage.Add(productImage);
                    }
                    _unitOfWork.Save();
                }
                TempData["success"] = "Product created successfully";
                return RedirectToAction("ProductList");
            }
            TempData["error"] = "Product created unsuccessfully";
            ViewBag.Categories = _unitOfWork.Category.GetAll();
            return View(product);
        }

        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            var product = _unitOfWork.Product.Get(
                p => p.Id == id,
                includeProperties: "Category,ProductAvatar,ProductImages"
            );
            if (product == null)
            {
                TempData["error"] = "Product not found";
                return RedirectToAction("ProductList");
            }
            ViewBag.Categories = _unitOfWork.Category.GetAll();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(
            Product product,
            IFormFile? avatar,
            List<IFormFile>? gallery
        )
        {
            if (ModelState.IsValid)
            {
                //Tracking product
                var existingProduct = _unitOfWork.Product.Get(
                    p => p.Id == product.Id,
                    includeProperties: "ProductAvatar,ProductImages"
                );

                if (existingProduct == null)
                {
                    TempData["error"] = "Error! Not found product";
                    return RedirectToAction("ProductList");
                }
                //Update product
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.ProductAvatarId = product.ProductAvatarId;

                //Get image location
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img/products");

                //Delete old avatar  if exist
                if (avatar != null)
                {
                    if (existingProduct.ProductAvatar != null)
                    {
                        var oldAvatarPath = Path.Combine(
                            uploadFolder,
                            existingProduct.ProductAvatar.ImagePath
                        );
                        if (System.IO.File.Exists(oldAvatarPath))
                        {
                            System.IO.File.Delete(oldAvatarPath);
                        }
                        _unitOfWork.ItemImage.Remove(existingProduct.ProductAvatar);
                    }
                    var newAvatarFileName = Guid.NewGuid() + Path.GetExtension(avatar.FileName);
                    var newAvatarPath = Path.Combine(uploadFolder, newAvatarFileName);
                    using (var stream = new FileStream(newAvatarPath, FileMode.Create))
                    {
                        await avatar.CopyToAsync(stream);
                    }

                    var newAvatar = new ItemImage
                    {
                        ImagePath = newAvatarFileName,
                        ProductId = product.Id,
                    };
                    _unitOfWork.ItemImage.Add(newAvatar);
                    _unitOfWork.Save();
                    existingProduct.ProductAvatarId = newAvatar.Id;
                }

                //Delete old gallery if exist

                if (gallery != null && gallery.Count > 0)
                {
                    if (existingProduct.ProductImages != null)
                    {
                        foreach (var image in existingProduct.ProductImages)
                        {
                            if (image.Id != existingProduct.ProductAvatarId)
                            {
                                var oldImagePath = Path.Combine(uploadFolder, image.ImagePath);
                                if (System.IO.File.Exists(oldImagePath))
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                                _unitOfWork.ItemImage.Remove(image);
                            }
                        }

                        List<ItemImage> newGalleryImages = new();
                        foreach (var image in gallery)
                        {
                            var newImageName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                            var newImagePath = Path.Combine(uploadFolder, newImageName);
                            using (var stream = new FileStream(newImagePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            newGalleryImages.Add(
                                new ItemImage { ImagePath = newImageName, ProductId = product.Id }
                            );
                        }

                        product.ProductImages = newGalleryImages;
                        _unitOfWork.ItemImage.AddRange(newGalleryImages);
                    }
                }

                _unitOfWork.Save();

                TempData["success"] = "Update product successfully";
                return RedirectToAction("ProductList");
            }

            ViewBag.Categories = _unitOfWork.Category.GetAll();
            return View(product);
        }

        [HttpDelete]
        public IActionResult DeleteProduct(int id)
        {
            string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img/products");

            var product = _unitOfWork.Product.Get(
                p => p.Id == id,
                includeProperties: "ProductImages"
            );

            if (product == null)
            {
                TempData["error"] = "Error when delete";
                return Json(new { success = false, message = "Error when delete" });
            }
            if (product.ProductAvatar != null)
            {
                var oldAvatarPath = Path.Combine(uploadFolder, product.ProductAvatar.ImagePath);
                if (System.IO.File.Exists(oldAvatarPath))
                {
                    System.IO.File.Delete(oldAvatarPath);
                }
                _unitOfWork.ItemImage.Remove(product.ProductAvatar);
            }
            if (product.ProductImages != null)
            {
                foreach (var image in product.ProductImages)
                {
                    if (image.Id != product.ProductAvatarId)
                    {
                        var oldImagePath = Path.Combine(uploadFolder, image.ImagePath);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                        _unitOfWork.ItemImage.Remove(image);
                    }
                }
            }
            _unitOfWork.Save();

            _unitOfWork.Product.Remove(product);
            _unitOfWork.Save();
            TempData["success"] = "Delete Successfully";
            return Json(new { success = true, message = "Delete Success" });
        }

        //End CRUD Product


        //Start CRUD Customer

        public IActionResult CustomerList()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetAllCustomer()
        {
            var customers = _unitOfWork.User.GetRange(u => u.Role == UserRole.Customer);
            return Json(new { data = customers });
        }

        [HttpGet]
        public IActionResult CustomerDetail(Guid id)
        {
            var customer = _unitOfWork.User.Get(u => u.Id == id);
            if (customer == null || customer.Role != UserRole.Customer)
            {
                return NotFound();
            }
            return View(customer);
        }

        //End CRUD Customer


        //Start CRUD Category
        public IActionResult CategoryList()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetAllCategory()
        {
            var categories = _unitOfWork.Category.GetAll();
            return Json(new { data = categories });
        }

        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Add(category);
                _unitOfWork.Save();
                TempData["success"] = "Category created successfully";
                return RedirectToAction("CategoryList");
            }
            TempData["error"] = "Category created successfully";
            return RedirectToAction("CategoryList");
        }

        [HttpGet]
        public IActionResult EditCategory(int id)
        {
            var category = _unitOfWork.Category.Get(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Update(category);
                _unitOfWork.Save();
                TempData["success"] = "Category updated successfully";
                return RedirectToAction("CategoryList");
            }
            TempData["error"] = "Category created successfully";
            return RedirectToAction("CategoryList");
        }

        [HttpDelete]
        public IActionResult DeleteCategory(int id)
        {
            var category = _unitOfWork.Category.Get(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            // Kiểm tra xem danh mục có sản phẩm nào không
            var productsInCategory = _unitOfWork.Product.GetRange(p => p.Id == id);
            if (productsInCategory.Any())
            {
                TempData["error"] = "Cannot delete this category because it contains products.";
                return Json(new { success = false, message = "Error when delete" });
            }

            _unitOfWork.Category.Remove(category);
            _unitOfWork.Save();
            TempData["success"] = "Delete successfully";
            return Json(new { success = true, message = "Delete Success" });
        }

        //End CRUD Category

        public IActionResult ViewProductFeedback(int id)
        {
            var product = _unitOfWork.Product.Get(p => p.Id == id);
            if (product == null)
            {
                TempData["error"] = "Product not found";
                return RedirectToAction("ProductList");
            }
            ViewBag.Product = product;
            var feedbackOfProducts = _unitOfWork.Feedback.GetRange(
                f => f.ProductId == id,
                includeProperties: "User,Images"
            );
            return View(feedbackOfProducts);
        }

        //Admin Statistic


        // End Admin Statistic


        // Admin manage order
        public IActionResult OrderList()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetAllOrders()
        {
            var orders = _unitOfWork
                .Order.GetAll(includeProperties: "User")
                .Select(o => new
                {
                    o.Id,
                    UserName = o.User?.UserName ?? "N/A",
                    o.TotalPrice,
                    OrderDate = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                    Status = o.Status.ToString(),
                })
                .ToList();
            return Json(new { data = orders });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteOrder(int id)
        {
            var order = _unitOfWork.Order.Get(o => o.Id == id);
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." });
            }
            if (order.Status != OrderStatus.Pending)
            {
                return Json(
                    new { success = false, message = "Only pending orders can be completed." }
                );
            }

            order.Status = OrderStatus.Completed;
            _unitOfWork.Order.Update(order);
            _unitOfWork.Save();
            TempData["success"] = "Order completed successfully.";
            return Json(new { success = true, message = "Order completed successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int id)
        {
            var order = _unitOfWork.Order.Get(o => o.Id == id);
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." });
            }
            if (order.Status != OrderStatus.Pending)
            {
                return Json(
                    new { success = false, message = "Only pending orders can be canceled." }
                );
            }

            order.Status = OrderStatus.Cancelled;
            _unitOfWork.Order.Update(order);
            _unitOfWork.Save();
            TempData["success"] = "Order canceled successfully.";
            return Json(new { success = true, message = "Order canceled successfully." });
        }
        // End Admin manage order
    }
}
