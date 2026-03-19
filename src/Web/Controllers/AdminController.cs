using Application.Interfaces;
using Application.Services;
using Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminAuthService _authService;
    private readonly SearchChecklistsService _searchService;
    private readonly ChecklistService _checklistService;

    public AdminController(
        IAdminAuthService authService,
        SearchChecklistsService searchService,
        ChecklistService checklistService)
    {
        _authService = authService;
        _searchService = searchService;
        _checklistService = checklistService;
    }

    public IActionResult Index(string? searchTerm)
    {
        var results = _searchService.Execute(searchTerm);

        ViewData["SearchTerm"] = searchTerm;
        return View(results);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [AllowAnonymous]
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

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, string? searchTerm)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await _checklistService.DeleteChecklist(id);
        return RedirectToAction(nameof(Index), new { searchTerm });
    }

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
