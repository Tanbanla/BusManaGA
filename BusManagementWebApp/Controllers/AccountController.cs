using Microsoft.AspNetCore.Mvc;
using BusManagement.ViewModels.Account;
using Microsoft.Extensions.Configuration;
using BusManagement.Service.Services.Interfaces;
using BusManagement.Helpers;

namespace BusManagement.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly IConfiguration _configuration;

    public AccountController(IAccountService accountService, IConfiguration configuration)
    {
        _accountService = accountService;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.ReturnUrl ??= Request.Query["returnUrl"].FirstOrDefault();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _accountService.Login(model.Username, model.Password);
        if (!result.Success || string.IsNullOrEmpty(result.Data))
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Login failed");
            return View(model);
        }

        // store access token in cookie
        var token = result.Data;
        // When running over plain HTTP, browsers will reject cookies with SameSite=None unless Secure is set.
        // To support local/IIS HTTP scenarios, set SameSite to Lax when the request is not HTTPS.
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = model.RememberMe ? DateTimeOffset.Now.AddDays(30) : DateTimeOffset.Now.AddDays(1)
        };
        Response.Cookies.Append("AccessToken", token, cookieOptions);

        // determine redirect based on token info
        try
        {
            var user = AccessTokenHelper.FromJwt(token);
            if (user != null && !string.IsNullOrWhiteSpace(user.Name))
            {
                TempData["LoginMessage"] = $"Hi {user.Name}!";
            }
            else if (user != null && !string.IsNullOrWhiteSpace(user.Adid))
            {
                TempData["LoginMessage"] = $"Hi {user.Adid}!";
            }
            else if (user != null && !string.IsNullOrWhiteSpace(user.EmployeeCode))
            {
                TempData["LoginMessage"] = $"Hi {user.EmployeeCode}!";
            }
            else
            {
                TempData["LoginMessage"] = $"Hi {model.Username}!";
            }
        }
        catch { }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("AccessToken");
        TempData["LoginMessage"] = "Đã đăng xuất.";
        return RedirectToAction(nameof(Login));
    }
}
