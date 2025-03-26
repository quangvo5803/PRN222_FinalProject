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
            return View();
        }

        //Start CRUD Product
        public IActionResult ProductList()
        {
            var products = _unitOfWork.Product.GetAll(includeProperties: "Category,ProductAvatar");
            return View(products);
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
                includeProperties: "Category,ProductAvatar"
            );
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.Categories = _unitOfWork.Category.GetAll();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Product.Update(product);
                _unitOfWork.Save();
                return RedirectToAction("ProductList");
            }
            ViewBag.Categories = _unitOfWork.Category.GetAll();
            return View(product);
        }

        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            var product = _unitOfWork.Product.Get(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            _unitOfWork.Product.Remove(product);
            _unitOfWork.Save();
            return RedirectToAction("ProductList");
        }

        //End CRUD Product


        //Start CRUD Customer
        public IActionResult CustomerList()
        {
            var customers = _unitOfWork.User.GetRange(u => u.Role == UserRole.Customer);
            return View(customers);
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

        [HttpPost]
        public IActionResult DeleteCustomer(Guid id)
        {
            var customer = _unitOfWork.User.Get(u => u.Id == id);
            if (customer == null || customer.Role != UserRole.Customer)
            {
                return NotFound();
            }
            _unitOfWork.User.Remove(customer);
            _unitOfWork.Save();
            return RedirectToAction("CustomerList");
        }

        public IActionResult FeedbackList()
        {
            var feedbacks = _unitOfWork.Feedback.GetAll(includeProperties: "User,Product");
            return View(feedbacks);
        }

        //End CRUD Customer


        //Start CRUD Category
        public IActionResult CategoryList()
        {
            var categories = _unitOfWork.Category.GetAll();
            return View(categories);
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
                return RedirectToAction("CategoryList");
            }
            return View(category);
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
                return RedirectToAction("CategoryList");
            }
            return View(category);
        }

        [HttpPost]
        public IActionResult DeleteCategory(int id)
        {
            var category = _unitOfWork.Category.Get(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            // Kiểm tra xem danh mục có sản phẩm nào không
            var productsInCategory = _unitOfWork.Product.GetRange(p => p.CategoryId == id);
            if (productsInCategory.Any())
            {
                TempData["Error"] = "Cannot delete this category because it contains products.";
                return RedirectToAction("CategoryList");
            }

            _unitOfWork.Category.Remove(category);
            _unitOfWork.Save();
            return RedirectToAction("CategoryList");
        }
        //End CRUD Category
    }
}
