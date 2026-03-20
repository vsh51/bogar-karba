using Application.UseCases.Auth.LoginAdmin;
using Application.UseCases.Auth.Logout;
using Application.UseCases.DeleteChecklist;
using Application.UseCases.SearchChecklists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly LoginAdminCommandHandler _loginHandler;
    private readonly LogoutCommandHandler _logoutHandler;
    private readonly SearchChecklistsQueryHandler _searchHandler;
    private readonly DeleteChecklistCommandHandler _deleteHandler;

    public AdminController(
        LoginAdminCommandHandler loginHandler,
        LogoutCommandHandler logoutHandler,
        SearchChecklistsQueryHandler searchHandler,
        DeleteChecklistCommandHandler deleteHandler)
    {
        _loginHandler = loginHandler;
        _logoutHandler = logoutHandler;
        _searchHandler = searchHandler;
        _deleteHandler = deleteHandler;
    }

    public IActionResult Index(string? searchTerm)
    {
        var result = _searchHandler.Handle(new SearchChecklistsQuery(searchTerm));

        ViewData["SearchTerm"] = searchTerm;
        return View(result.Checklists);
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

        var result = await _loginHandler.HandleAsync(
            new LoginAdminCommand(model.UserName, model.Password));
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

        await _deleteHandler.HandleAsync(new DeleteChecklistCommand(id));
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
        await _logoutHandler.HandleAsync(new LogoutCommand(DateTime.UtcNow));
        return RedirectToAction("Index", "Home");
    }
}
