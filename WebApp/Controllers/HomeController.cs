using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantBusiness.Model;
using RestaurantRepositories.UnitOfWork;
using WebApp.Utility;

namespace WebApp.Controllers;

public class HomeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public HomeController(IConfiguration configuration, IUnitOfWork unitOfWork)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = _unitOfWork.User.Get(u => u.Email == email);
        var success = user != null && PasswordHasher.VerifyPassword(password, user.PasswordHash);
        if (!success)
        {
            TempData["error"] = "Email hoặc mật khẩu không đúng";
            return View();
        }
        if (user == null || !user.IsEmailConfirmed)
        {
            {
                TempData["error"] = "Vui lòng xác thực email trước khi đang nhập";
                return View();
            }
        }
        await SignInUser(HttpContext, user);
        return user!.Role switch
        {
            UserRole.Admin => RedirectToAction("Index", "Admin"),
            UserRole.Customer => RedirectToAction("Index", "Customer"),
            _ => RedirectToAction("Index", "Home"),
        };
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string email, string password, string repassword)
    {
        if (password != repassword)
        {
            TempData["error"] = "Mật khẩu không khớp.";
            ViewBag.Email = email;
            return View();
        }
        if (!IsValidPassword(password))
        {
            TempData["error"] = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 số và 1 ký tự đặc biệt.";
            ViewBag.Email = email;
            return View();
        }
        if (_unitOfWork.User.Get(u => u.Email == email) != null)
        {
            TempData["error"] = "Email đã có người sử dụng !!!";
            ViewBag.Email = email;
            return View();
        }
        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHasher.HashPassword(password),
            Role = UserRole.Customer,
        };
        _unitOfWork.User.Add(user);
        _unitOfWork.Save();
        //Send Email Comfirm
        var emailSender = new EmailSender(
            _configuration,
            new LoggerFactory().CreateLogger<EmailSender>()
        );
        var token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{user.Email}:{Guid.NewGuid()}")
        );
        string confirmUrl =
            $"{_configuration["AppSettings:BaseUrl"]}/Home/ConfirmEmail?token={token}";
        string subject = "Xác nhận đăng kí tài khoản GreenCloset";
        string body =
            $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%;\">"
            + $"<tr>"
            + $"<td></td>"
            + $"<td class=\"container\" style=\"margin: 0 auto !important; max-width: 600px; padding: 0; padding-top: 24px; width: 600px;\">"
            + $"<div class=\"content\" style=\"box-sizing: border-box; display: block; margin: 0 auto; max-width: 600px; padding: 0;\">"
            + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"main\" style=\"background: #f0f8f0; border: 1px solid #2e7d32; border-radius: 16px; width: 100%; text-align: center;\">"
            + $"<tr>"
            + $"<td class=\"wrapper\" style=\"box-sizing: border-box; padding: 24px;\">"
            + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #1b5e20;\">Chào bạn</p>"
            + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #1b5e20;\">Chúng tôi cần xác thực email của bạn trước khi hoàn tất đăng kí tài khoản. Vui lòng bấm vào nút bên dưới.</p>"
            + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"btn btn-primary\" style=\"min-width: 100% !important; width: 100%;\">"
            + $"<tbody>"
            + $"<tr>"
            + $"<td align=\"center\" style=\"padding-bottom: 16px;\">"
            + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">"
            + $"<tbody>"
            + $"<tr>"
            + $"<td><a href='{confirmUrl}' style=\"background-color: #2e7d32; border: solid 2px #2e7d32; border-radius: 4px; box-sizing: border-box; color: #ffffff; cursor: pointer; display: inline-block; font-size: 16px; font-weight: bold; margin: 0; padding: 12px 24px; text-decoration: none; text-transform: capitalize;\">Bấm vào đây</a></td>"
            + $"</tr>"
            + $"</tbody>"
            + $"</table>"
            + $"</td>"
            + $"</tr>"
            + $"</tbody>"
            + $"</table>"
            + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #1b5e20;\">Cảm ơn bạn đã tin tưởng Vi-Learning</p>"
            + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #1b5e20;\">Vi-Learning</p>"
            + $"<div style=\"text-align: center; margin-top: 20px;\">"
            + $"<img src=\"https://images.squarespace-cdn.com/content/v1/6362b1bc8f40907828c799e7/1668306393339-OG3FFQ3CK5MAZ16P6BX2/localgreencloset+case+study++%281%29.jpg\" alt=\"Vi-Learning Logo\" style=\"max-width: 200px; height: auto; border-radius: 8px;\">"
            + $"</div>"
            + $"</td>"
            + $"</tr>"
            + $"</table>"
            + $"</div>"
            + $"</td>"
            + $"<td></td>"
            + $"</tr>"
            + $"</table>";
        await emailSender.SendEmailAsync(email, subject, body);
        TempData["success"] = "Đăng kí thành công! Vui lòng xác thực email trước khi đăng nhập!";
        return RedirectToAction("Login", "Home");
    }

    [HttpGet("login-google")]
    public async Task LoginWithGoogle()
    {
        await HttpContext.ChallengeAsync(
            GoogleDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") }
        );
    }

    [HttpGet]
    public async Task<IActionResult> GoogleResponse()
    {
        var authResult = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        if (!authResult.Succeeded || authResult.Principal == null)
        {
            return RedirectToAction("Login", "User");
        }
        var email = authResult
            .Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)
            ?.Value;
        var name = authResult
            .Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)
            ?.Value;

        if (string.IsNullOrEmpty(email))
        {
            TempData["error"] = "Không thể lấy thông tin email từ Google";
            return RedirectToAction("Login", "User");
        }

        var user = _unitOfWork.User.Get(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                Email = email,
                UserName = name,
                PasswordHash = PasswordHasher.HashPassword("Abc123@"),
                Role = UserRole.Customer,
                IsEmailConfirmed = true,
            };
            _unitOfWork.User.Add(user);
            _unitOfWork.Save();
            var emailSender = new EmailSender(
                _configuration,
                new LoggerFactory().CreateLogger<EmailSender>()
            );
            string subject = "Xác nhận đăng kí tài khoản GreenCloset";
            string body =
                $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%;\">"
                + $"<tr>"
                + $"<td></td>"
                + $"<td class=\"container\" style=\"margin: 0 auto !important; max-width: 600px; padding: 0; padding-top: 24px; width: 600px;\">"
                + $"<div class=\"content\" style=\"box-sizing: border-box; display: block; margin: 0 auto; max-width: 600px; padding: 0;\">"
                + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"main\" style=\"background: #f0f8f0; border: 1px solid #2e7d32; border-radius: 16px; width: 100%; text-align: center;\">"
                + $"<tr>"
                + $"<td class=\"wrapper\" style=\"box-sizing: border-box; padding: 24px;\">"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #1b5e20;\">Chào bạn</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #1b5e20;\">Chúc mừng bạn đã đăng ký tài khoản thành công tại Green Closet!</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #1b5e20;\">Mật khẩu mặc định của bạn là: <strong>Abc123@</strong></p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #1b5e20;\">Vui lòng đổi mật khẩu ngay sau khi đăng nhập để bảo mật tài khoản của bạn.</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #1b5e20;\">Cảm ơn bạn đã tin tưởng Green Closet</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #1b5e20;\">Green Closet</p>"
                + $"<div style=\"text-align: center; margin-top: 20px;\">"
                + $"<img src=\"https://images.squarespace-cdn.com/content/v1/6362b1bc8f40907828c799e7/1668306393339-OG3FFQ3CK5MAZ16P6BX2/localgreencloset+case+study++%281%29.jpg\" alt=\"Vi-Learning Logo\" style=\"max-width: 200px; height: auto; border-radius: 8px;\">"
                + $"</div>"
                + $"</td>"
                + $"</tr>"
                + $"</table>"
                + $"</div>"
                + $"</td>"
                + $"<td></td>"
                + $"</tr>"
                + $"</table>";
            await emailSender.SendEmailAsync(email, subject, body);
        }
        await SignInUser(HttpContext, user);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ConfirmEmail(string token)
    {
        try
        {
            var decodedBytes = Convert.FromBase64String(token);
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            var email = decodedString.Split(':')[0];

            var user = _unitOfWork.User.Get(u => u.Email == email);
            if (user == null)
            {
                return BadRequest("Token không hợp lệ.");
            }
            user.IsEmailConfirmed = true;
            _unitOfWork.User.Update(user);
            _unitOfWork.Save();
            TempData["success"] = "Xác thực email thành công";
            return RedirectToAction("Login", "Home");
        }
        catch
        {
            return BadRequest("Token không hợp lệ hoặc đã hết hạn.");
        }
    }

    [Authorize]
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
        if (ModelState.IsValid)
        {
            _unitOfWork.User.Update(user);
            _unitOfWork.Save();
            TempData["success"] = "Cập nhật thông tin thành công";
            return View(user);
        }
        TempData["error"] = "Cập nhật thông tin không thành công";
        return View(user);
    }

    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Home");
    }

    //Support Login
    private bool IsValidPassword(string password)
    {
        return Regex.IsMatch(password, @"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$");
    }

    private async Task SignInUser(HttpContext httpContext, User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity)
        );
    }
}
