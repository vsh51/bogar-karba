using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers;

public class AdminController : Controller
{
    private readonly IAdminAuthService _authService;

    public AdminController(IAdminAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Login()
    {
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

        var result = await _authService.LoginAsync(model.UserName, model.Password);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Login failed.");
            return View(model);
        }

        return RedirectToAction("Dashboard");
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction("Index", "Home");
    }
}
