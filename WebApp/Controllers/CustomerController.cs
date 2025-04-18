﻿using System.Net;
using System.Security.Claims;
using BusinessObject.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.UnitOfWork;
using WebApp.Services.Interface;
using WebApp.Utility;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Customer,Admin")]
    public partial class CustomerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IVnPayService _vnPayService;

        public CustomerController(
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment,
            IVnPayService vnPayService
        )
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _vnPayService = vnPayService;
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
    }
}
