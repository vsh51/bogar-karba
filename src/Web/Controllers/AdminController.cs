using Application.Common;
using Application.Enums;
using Application.UseCases.Auth.LoginAdmin;
using Application.UseCases.Auth.Logout;
using Application.UseCases.BanUser;
using Application.UseCases.DeleteChecklist;
using Application.UseCases.GetSystemStats;
using Application.UseCases.SearchChecklists;
using Application.UseCases.SearchUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Filters;
using Web.Mappings;
using Web.Models.Admin;

namespace Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin")]
public sealed class AdminController : BaseController
{
    private readonly LoginAdminCommandHandler _loginHandler;
    private readonly LogoutCommandHandler _logoutHandler;
    private readonly SearchChecklistsQueryHandler _searchHandler;
    private readonly SearchUsersQueryHandler _userSearchHandler;
    private readonly DeleteChecklistCommandHandler _deleteHandler;
    private readonly BanUserCommandHandler _banUserHandler;
    private readonly GetSystemStatsQueryHandler _systemStatsHandler;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        LoginAdminCommandHandler loginHandler,
        LogoutCommandHandler logoutHandler,
        SearchChecklistsQueryHandler searchHandler,
        SearchUsersQueryHandler userSearchHandler,
        DeleteChecklistCommandHandler deleteHandler,
        BanUserCommandHandler banUserHandler,
        GetSystemStatsQueryHandler systemStatsHandler,
        ILogger<AdminController> logger)
    {
        _loginHandler = loginHandler;
        _logoutHandler = logoutHandler;
        _searchHandler = searchHandler;
        _userSearchHandler = userSearchHandler;
        _deleteHandler = deleteHandler;
        _banUserHandler = banUserHandler;
        _systemStatsHandler = systemStatsHandler;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var adminUserName = User.Identity?.Name ?? "unknown-admin";
        _logger.LogInformation("Admin {AdminUserName} requested dashboard list with search term {SearchTerm}", adminUserName, searchTerm ?? "<empty>");

        var result = await _searchHandler.HandleAsync(new SearchChecklistsQuery(searchTerm));

        var viewModels = (result.Succeeded ? result.Value! : new())
            .Select(c => c.ToAdminViewModel())
            .ToList();

        _logger.LogInformation("Admin {AdminUserName} search returned {Count} checklists", adminUserName, viewModels.Count);

        ViewData["SearchTerm"] = searchTerm;
        return View(viewModels);
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(string? searchTerm)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var adminUserName = User.Identity?.Name ?? "unknown-admin";
        _logger.LogInformation("Admin {Admin} requested user list", adminUserName);

        var result = await _userSearchHandler.HandleAsync(new SearchUsersQuery(searchTerm));

        var viewModels = (result.Succeeded ? result.Value! : new())
            .Select(u => u.ToAdminUserViewModel())
            .ToList();

        ViewData["SearchTerm"] = searchTerm;
        ViewData["CurrentAdminId"] = CurrentUserId;
        return View(viewModels);
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login()
    {
        _logger.LogInformation("Admin login page requested");
        return View();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Admin login model validation failed with {ErrorCount} errors", ModelState.ErrorCount);
            return View(model);
        }

        _logger.LogInformation("Admin login attempt for {Identifier}", model.UserName);

        var result = await _loginHandler.HandleAsync(
            new LoginAdminCommand(model.UserName, model.Password));

        if (!result.Succeeded)
        {
            _logger.LogWarning("Admin login failed for {Identifier}", model.UserName);
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Login failed.");
            return View(model);
        }

        _logger.LogInformation("Admin logged in: {Identifier}", model.UserName);
        return RedirectToAction("Index");
    }

    [HttpPost("delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> Delete(Guid id, string? searchTerm)
    {
        _logger.LogInformation("Admin deleting checklist {ChecklistId}", id);
        var result = await _deleteHandler.HandleAsync(new DeleteChecklistCommand(id));

        if (!result.Succeeded)
        {
            _logger.LogWarning("Admin failed to delete checklist {ChecklistId}: {Error}", id, result.ErrorMessage);
            SetErrorMessage(result.ErrorMessage ?? "Failed to delete checklist.");
        }

        return RedirectToAction(nameof(Index), new { searchTerm });
    }

    [HttpPost("ban/{userId}")]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> BanUser(string userId, string? searchTerm, bool fromUsers = false)
    {
        var adminUserName = CurrentUserName ?? "unknown-admin";
        _logger.LogInformation("Admin {Admin} requested ban for user {UserId}. FromUsers: {FromUsers}", adminUserName, userId, fromUsers);

        var result = await _banUserHandler.HandleAsync(new BanUserCommand(userId));

        if (result.Succeeded)
        {
            _logger.LogInformation("Admin {Admin} successfully blocked user {UserId}", adminUserName, userId);
            SetSuccessMessage("The user has been blocked.");
        }
        else
        {
            _logger.LogError("Admin {Admin} failed to block user {UserId}: {Error}", adminUserName, userId, result.ErrorMessage);
            SetErrorMessage(result.ErrorMessage ?? "Failed to block the user.");
        }

        if (fromUsers)
        {
            return RedirectToAction(nameof(Users), new { searchTerm });
        }

        return RedirectToAction(nameof(Index), new { searchTerm });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var result = await _systemStatsHandler.HandleAsync(new GetSystemStatsQuery());

        if (!result.Succeeded || result.Value is null)
        {
            _logger.LogError("Failed to load system statistics for dashboard");
            return View(new DashboardViewModel());
        }

        return View(result.Value.ToDashboardViewModel());
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var adminUserName = User.Identity?.Name ?? "unknown-admin";
        await _logoutHandler.HandleAsync(new LogoutCommand(DateTime.UtcNow));
        _logger.LogInformation("Admin {AdminUserName} logged out successfully", adminUserName);
        return RedirectToAction("Index", "Home");
    }
}
