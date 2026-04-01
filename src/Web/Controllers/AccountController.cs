using Application.Common;
using Application.UseCases.Auth.LoginUser;
using Application.UseCases.Auth.Logout;
using Application.UseCases.Auth.RegisterUser;
using Microsoft.AspNetCore.Mvc;
using Web.Models.Account;

namespace Web.Controllers;

public sealed class AccountController : BaseController
{
    private readonly RegisterUserCommandHandler _registerHandler;
    private readonly LoginUserCommandHandler _loginHandler;
    private readonly LogoutCommandHandler _logoutHandler;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        RegisterUserCommandHandler registerHandler,
        LoginUserCommandHandler loginHandler,
        LogoutCommandHandler logoutHandler,
        ILogger<AccountController> logger)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _logoutHandler = logoutHandler;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _logger.LogInformation("User registration attempt for {Email}", model.Email);

        var result = await _registerHandler.HandleAsync(
            new RegisterUserCommand(model.Name, model.Surname, model.Email, model.Password));

        if (!result.Succeeded)
        {
            _logger.LogWarning("Registration failed for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Registration failed.");
            return View(model);
        }

        _logger.LogInformation("User registered successfully: {Email}", model.Email);
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _logger.LogInformation("User login attempt for {Email}", model.Email);

        var result = await _loginHandler.HandleAsync(
            new LoginUserCommand(model.Email, model.Password));
        if (!result.Succeeded)
        {
            _logger.LogWarning("Login failed for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid email or password.");
            return View(model);
        }

        _logger.LogInformation("User logged in: {Email}", model.Email);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _logoutHandler.HandleAsync(new LogoutCommand(DateTime.UtcNow));
        _logger.LogInformation("User logged out");
        return RedirectToAction("Login", "Account");
    }
}
