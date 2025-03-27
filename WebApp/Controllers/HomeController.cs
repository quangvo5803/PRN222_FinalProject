using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BusinessObject.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.UnitOfWork;
using WebApp.Utility;

namespace WebApp.Controllers;

public class HomeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly EmailSender _emailSender;

    public HomeController(IConfiguration configuration, IUnitOfWork unitOfWork)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _emailSender = new EmailSender(
            _configuration,
            new LoggerFactory().CreateLogger<EmailSender>()
        );
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
            TempData["error"] = "Incorrect email or password";
            return View();
        }
        if (user == null || !user.IsEmailConfirmed)
        {
            {
                TempData["error"] = "Please verify email before logging in.";
                return View();
            }
        }
        await SignInUser(HttpContext, user);
        TempData["success"] = "Login successfully";
        return user!.Role switch
        {
            UserRole.Admin => RedirectToAction("Index", "Admin"),
            UserRole.Customer => RedirectToAction("Index", "Home"),
            _ => RedirectToAction("Index", "Home"),
        };
    }

    public IActionResult Register(string? email)
    {
        ViewBag.Email = email;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string email, string password, string repassword)
    {
        if (password != repassword)
        {
            TempData["error"] = "Passwords do not match.";
            ViewBag.Email = email;
            return View();
        }
        if (!IsValidPassword(password))
        {
            TempData["error"] =
                "Password must contain at least 1 uppercase letter, 1 number and 1 special character.";
            ViewBag.Email = email;
            return View();
        }
        if (_unitOfWork.User.Get(u => u.Email == email) != null)
        {
            TempData["error"] = "Email is already taken !!!";
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
        var token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{user.Email}:{Guid.NewGuid()}")
        );
        string confirmUrl =
            $"{_configuration["AppSettings:BaseUrl"]}/Home/ConfirmEmail?token={token}";
        string subject = "Confirm Foodhub account registration";
        string body =
            $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%;\">"
            + $"<tr>"
            + $"<td></td>"
            + $"<td class=\"container\" style=\"margin: 0 auto !important; max-width: 600px; padding: 0; padding-top: 24px; width: 600px;\">"
            + $"<div class=\"content\" style=\"box-sizing: border-box; display: block; margin: 0 auto; max-width: 600px; padding: 0;\">"
            + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"main\" style=\"background: #fff3e0; border: 1px solid #ff5722; border-radius: 16px; width: 100%; text-align: center;\">"
            + $"<tr>"
            + $"<td class=\"wrapper\" style=\"box-sizing: border-box; padding: 24px;\">"
            + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">Hello there</p>"
            + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">We need to verify your email before completing your account registration. Please click the button below.</p>"
            + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"btn btn-primary\" style=\"min-width: 100% !important; width: 100%;\">"
            + $"<tbody>"
            + $"<tr>"
            + $"<td align=\"center\" style=\"padding-bottom: 16px;\">"
            + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">"
            + $"<tbody>"
            + $"<tr>"
            + $"<td><a href='{confirmUrl}' style=\"background-color: #ff5722; border: solid 2px #ff5722; border-radius: 4px; box-sizing: border-box; color: #ffffff; cursor: pointer; display: inline-block; font-size: 16px; font-weight: bold; margin: 0; padding: 12px 24px; text-decoration: none; text-transform: capitalize;\">Click Here</a></td>"
            + $"</tr>"
            + $"</tbody>"
            + $"</table>"
            + $"</td>"
            + $"</tr>"
            + $"</tbody>"
            + $"</table>"
            + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">Thank you for trusting FoodHub</p>"
            + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">Foodhub</p>"
            + $"<div style=\"text-align: center; margin-top: 20px;\">"
            + $"<img src=\"https://scontent.fdad3-4.fna.fbcdn.net/v/t39.30808-6/300087127_364723072543497_3908705445968239834_n.jpg?_nc_cat=105&ccb=1-7&_nc_sid=6ee11a&_nc_ohc=3hSjEixh-3IQ7kNvgGRJhL2&_nc_oc=AdlJBnirwOJnnxV1_sBos5SfsMXM7XcUnTkSh5b7NfL6ab5VLFZRtMbdtQ1otkDDTA2UbCf1cDLKJ6ZRjK40SCw7&_nc_zt=23&_nc_ht=scontent.fdad3-4.fna&_nc_gid=GVBLMMWCMlhJg9crhvdHaQ&oh=00_AYGIy8zoiL-H27Kp2bGYl2cHrwPz5Ql_iLbPuDZyBBDt1w&oe=67EAA35A\" alt=\"Vi-Learning Logo\" style=\"max-width: 200px; height: auto; border-radius: 8px;\">"
            + $"</div>"
            + $"</td>"
            + $"</tr>"
            + $"</table>"
            + $"</div>"
            + $"</td>"
            + $"<td></td>"
            + $"</tr>"
            + $"</table>";
        await _emailSender.SendEmailAsync(email, subject, body);
        TempData["success"] =
            "Registration successful! Please verify your email before logging in!";
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
            TempData["error"] = "Unable to get email information from Google";
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
            string subject = "Confirm FoodHub account registration";
            string body =
                $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%;\">"
                + $"<tr>"
                + $"<td></td>"
                + $"<td class=\"container\" style=\"margin: 0 auto !important; max-width: 600px; padding: 0; padding-top: 24px; width: 600px;\">"
                + $"<div class=\"content\" style=\"box-sizing: border-box; display: block; margin: 0 auto; max-width: 600px; padding: 0;\">"
                + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"main\" style=\"background: #fff4e6; border: 1px solid #ff6b35; border-radius: 16px; width: 100%; text-align: center;\">"
                + $"<tr>"
                + $"<td class=\"wrapper\" style=\"box-sizing: border-box; padding: 24px;\">"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #8b4513;\">Hello!</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #8b4513;\">Congratulations on successfully registering an account with FoodHub!</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #8b4513;\">Your default password is: <strong>Abc123@</strong></p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #8b4513;\">Please change your password immediately after logging in to secure your account.</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #8b4513;\">Thank you for trusting FoodHub</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #8b4513;\">FoodHub</p>"
                + $"<div style=\"text-align: center; margin-top: 20px;\">"
                + $"<img src=\"https://scontent.fdad3-4.fna.fbcdn.net/v/t39.30808-6/300087127_364723072543497_3908705445968239834_n.jpg?_nc_cat=105&ccb=1-7&_nc_sid=6ee11a&_nc_ohc=3hSjEixh-3IQ7kNvgGRJhL2&_nc_oc=AdlJBnirwOJnnxV1_sBos5SfsMXM7XcUnTkSh5b7NfL6ab5VLFZRtMbdtQ1otkDDTA2UbCf1cDLKJ6ZRjK40SCw7&_nc_zt=23&_nc_ht=scontent.fdad3-4.fna&_nc_gid=GVBLMMWCMlhJg9crhvdHaQ&oh=00_AYGIy8zoiL-H27Kp2bGYl2cHrwPz5Ql_iLbPuDZyBBDt1w&oe=67EAA35A\" alt=\"FoodHub Logo\" style=\"max-width: 200px; height: auto; border-radius: 8px;\">"
                + $"</div>"
                + $"</td>"
                + $"</tr>"
                + $"</table>"
                + $"</div>"
                + $"</td>"
                + $"<td></td>"
                + $"</tr>"
                + $"</table>";
            await _emailSender.SendEmailAsync(email, subject, body);
        }
        await SignInUser(HttpContext, user);
        TempData["success"] = "Login successfully";
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
                return BadRequest("Invalid token.");
            }
            user.IsEmailConfirmed = true;
            _unitOfWork.User.Update(user);
            _unitOfWork.Save();
            TempData["success"] = "Email verification successfully";
            return RedirectToAction("Login", "Home");
        }
        catch
        {
            return BadRequest("Token is invalid or expired.");
        }
    }

    public IActionResult ResendEmail()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResendEmail(string email)
    {
        var user = _unitOfWork.User.Get(u => u.Email == email);
        if (user != null)
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{Guid.NewGuid()}"));
            string confirmUrl =
                $"{_configuration["AppSettings:BaseUrl"]}/Home/ConfirmEmail?token={token}";
            string subject = "Confirm Foodhub account registration";
            string body =
                $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%;\">"
                + $"<tr>"
                + $"<td></td>"
                + $"<td class=\"container\" style=\"margin: 0 auto !important; max-width: 600px; padding: 0; padding-top: 24px; width: 600px;\">"
                + $"<div class=\"content\" style=\"box-sizing: border-box; display: block; margin: 0 auto; max-width: 600px; padding: 0;\">"
                + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"main\" style=\"background: #fff3e0; border: 1px solid #ff5722; border-radius: 16px; width: 100%; text-align: center;\">"
                + $"<tr>"
                + $"<td class=\"wrapper\" style=\"box-sizing: border-box; padding: 24px;\">"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">Hello there</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">We need to verify your email before completing your account registration. Please click the button below.</p>"
                + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"btn btn-primary\" style=\"min-width: 100% !important; width: 100%;\">"
                + $"<tbody>"
                + $"<tr>"
                + $"<td align=\"center\" style=\"padding-bottom: 16px;\">"
                + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">"
                + $"<tbody>"
                + $"<tr>"
                + $"<td><a href='{confirmUrl}' style=\"background-color: #ff5722; border: solid 2px #ff5722; border-radius: 4px; box-sizing: border-box; color: #ffffff; cursor: pointer; display: inline-block; font-size: 16px; font-weight: bold; margin: 0; padding: 12px 24px; text-decoration: none; text-transform: capitalize;\">Click Here</a></td>"
                + $"</tr>"
                + $"</tbody>"
                + $"</table>"
                + $"</td>"
                + $"</tr>"
                + $"</tbody>"
                + $"</table>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">Thank you for trusting FoodHub</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">Foodhub</p>"
                + $"<div style=\"text-align: center; margin-top: 20px;\">"
                + $"<img src=\"https://scontent.fdad3-4.fna.fbcdn.net/v/t39.30808-6/300087127_364723072543497_3908705445968239834_n.jpg?_nc_cat=105&ccb=1-7&_nc_sid=6ee11a&_nc_ohc=3hSjEixh-3IQ7kNvgGRJhL2&_nc_oc=AdlJBnirwOJnnxV1_sBos5SfsMXM7XcUnTkSh5b7NfL6ab5VLFZRtMbdtQ1otkDDTA2UbCf1cDLKJ6ZRjK40SCw7&_nc_zt=23&_nc_ht=scontent.fdad3-4.fna&_nc_gid=GVBLMMWCMlhJg9crhvdHaQ&oh=00_AYGIy8zoiL-H27Kp2bGYl2cHrwPz5Ql_iLbPuDZyBBDt1w&oe=67EAA35A\" alt=\"Vi-Learning Logo\" style=\"max-width: 200px; height: auto; border-radius: 8px;\">"
                + $"</div>"
                + $"</td>"
                + $"</tr>"
                + $"</table>"
                + $"</div>"
                + $"</td>"
                + $"<td></td>"
                + $"</tr>"
                + $"</table>";
            await _emailSender.SendEmailAsync(email, subject, body);
        }
        TempData["success"] = "Send email successfully";
        return RedirectToAction("Login", "Home");
    }

    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var user = _unitOfWork.User.Get(u => u.Email == email);
        if (user != null)
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{Guid.NewGuid()}"));
            string confirmUrl =
                $"{_configuration["AppSettings:BaseUrl"]}/Home/UpdatePassword?token={token}";
            string subject = "Email confirmation forgot password";
            string body =
                $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%;\">"
                + $"<tr>"
                + $"<td></td>"
                + $"<td class=\"container\" style=\"margin: 0 auto !important; max-width: 600px; padding: 0; padding-top: 24px; width: 600px;\">"
                + $"<div class=\"content\" style=\"box-sizing: border-box; display: block; margin: 0 auto; max-width: 600px; padding: 0;\">"
                + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"main\" style=\"background: #fff3e0; border: 1px solid #ff5722; border-radius: 16px; width: 100%; text-align: center;\">"
                + $"<tr>"
                + $"<td class=\"wrapper\" style=\"box-sizing: border-box; padding: 24px;\">"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">Hello</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">You have requested to reset your password for your FoodHub account. Click the button below to reset your password.</p>"
                + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"btn btn-primary\" style=\"min-width: 100% !important; width: 100%;\">"
                + $"<tbody>"
                + $"<tr>"
                + $"<td align=\"center\" style=\"padding-bottom: 16px;\">"
                + $"<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">"
                + $"<tbody>"
                + $"<tr>"
                + $"<td><a href='{confirmUrl}' style=\"background-color: #ff5722; border: solid 2px #ff5722; border-radius: 4px; box-sizing: border-box; color: #ffffff; cursor: pointer; display: inline-block; font-size: 16px; font-weight: bold; margin: 0; padding: 12px 24px; text-decoration: none; text-transform: capitalize;\">Reset Password</a></td>"
                + $"</tr>"
                + $"</tbody>"
                + $"</table>"
                + $"</td>"
                + $"</tr>"
                + $"</tbody>"
                + $"</table>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">If you did not request a password reset, please ignore this email or contact our support team if you have any concerns.</p>"
                + $"<p style=\"font-weight: normal; margin: 0; margin-bottom: 16px; color: #bf360c;\">FoodHub</p>"
                + $"<div style=\"text-align: center; margin-top: 20px;\">"
                + $"<img src=\"https://scontent.fdad3-4.fna.fbcdn.net/v/t39.30808-6/300087127_364723072543497_3908705445968239834_n.jpg?_nc_cat=105&ccb=1-7&_nc_sid=6ee11a&_nc_ohc=3hSjEixh-3IQ7kNvgGRJhL2&_nc_oc=AdlJBnirwOJnnxV1_sBos5SfsMXM7XcUnTkSh5b7NfL6ab5VLFZRtMbdtQ1otkDDTA2UbCf1cDLKJ6ZRjK40SCw7&_nc_zt=23&_nc_ht=scontent.fdad3-4.fna&_nc_gid=GVBLMMWCMlhJg9crhvdHaQ&oh=00_AYGIy8zoiL-H27Kp2bGYl2cHrwPz5Ql_iLbPuDZyBBDt1w&oe=67EAA35A\" alt=\"Green Closet Logo\" style=\"max-width: 200px; height: auto; border-radius: 8px;\">"
                + $"</div>"
                + $"</td>"
                + $"</tr>"
                + $"</table>"
                + $"</div>"
                + $"</td>"
                + $"<td></td>"
                + $"</tr>"
                + $"</table>";
            await _emailSender.SendEmailAsync(email, subject, body);
        }
        TempData["success"] = "Change password link was sended.";
        return RedirectToAction("Login", "Home");
    }

    public IActionResult UpdatePassword(string token)
    {
        var decodedBytes = Convert.FromBase64String(token);
        var decodedString = Encoding.UTF8.GetString(decodedBytes);
        var email = decodedString.Split(':')[0];
        var user = _unitOfWork.User.Get(u => u.Email == email);
        if (user == null)
        {
            TempData["error"] = "Error";
            return RedirectToAction("Login", "Home");
        }
        ViewBag.Email = email;
        return View();
    }

    [HttpPost]
    public IActionResult UpdatePassword(string email, string password, string repassword)
    {
        if (password != repassword)
        {
            TempData["error"] = "Passwords do not match.";
            ViewBag.Email = email;
            return View();
        }
        if (!IsValidPassword(password))
        {
            TempData["error"] =
                "Password must contain at least 1 uppercase letter, 1 number and 1 special character.";
            ViewBag.Email = email;
            return View();
        }
        var user = _unitOfWork.User.Get(u => u.Email == email);
        if (user == null)
        {
            TempData["error"] = "Email is not valid";
            return RedirectToAction("Login", "Home");
        }
        user.PasswordHash = PasswordHasher.HashPassword(password);
        _unitOfWork.User.Update(user);
        _unitOfWork.Save();
        TempData["success"] = "Change password successfully";
        return RedirectToAction("Login", "Home");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Home");
    }

    public IActionResult ProductDetail()
    {
        return View();
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
