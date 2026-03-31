using Application.Common;
using Application.UseCases.Auth.LoginAdmin;
using Application.UseCases.Auth.Logout;
using Application.UseCases.BanUser;
using Application.UseCases.DeleteChecklist;
using Application.UseCases.GetSystemStats;
using Application.UseCases.SearchChecklists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models.Admin;

namespace Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController : Controller
{
    private readonly LoginAdminCommandHandler _loginHandler;
    private readonly LogoutCommandHandler _logoutHandler;
    private readonly SearchChecklistsQueryHandler _searchHandler;
    private readonly DeleteChecklistCommandHandler _deleteHandler;
    private readonly BanUserCommandHandler _banUserHandler;
    private readonly GetSystemStatsQueryHandler _systemStatsHandler;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        LoginAdminCommandHandler loginHandler,
        LogoutCommandHandler logoutHandler,
        SearchChecklistsQueryHandler searchHandler,
        DeleteChecklistCommandHandler deleteHandler,
        BanUserCommandHandler banUserHandler,
        GetSystemStatsQueryHandler systemStatsHandler,
        ILogger<AdminController> logger)
    {
        _loginHandler = loginHandler;
        _logoutHandler = logoutHandler;
        _searchHandler = searchHandler;
        _deleteHandler = deleteHandler;
        _banUserHandler = banUserHandler;
        _systemStatsHandler = systemStatsHandler;
        _logger = logger;
    }

    public IActionResult Index(string? searchTerm)
    {
        var result = _searchHandler.Handle(new SearchChecklistsQuery(searchTerm));

        var viewModels = (result.Succeeded ? result.Value! : new())
            .Select(c => new AdminChecklistViewModel
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                UserId = c.UserId
            })
            .ToList();

        ViewData["SearchTerm"] = searchTerm;
        return View(viewModels);
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
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _logger.LogInformation("Admin login attempt for {UserName}", model.UserName);

        var result = await _loginHandler.HandleAsync(
            new LoginAdminCommand(model.UserName, model.Password));
        if (!result.Succeeded)
        {
            _logger.LogWarning("Admin login failed for {UserName}", model.UserName);
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Login failed.");
            return View(model);
        }

        _logger.LogInformation("Admin logged in: {UserName}", model.UserName);
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

        _logger.LogInformation("Admin deleting checklist {ChecklistId}", id);
        await _deleteHandler.HandleAsync(new DeleteChecklistCommand(id));
        return RedirectToAction(nameof(Index), new { searchTerm });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BanUser(string userId, string? searchTerm)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var adminUserName = User.Identity?.Name ?? "unknown-admin";
        _logger.LogInformation("Admin {AdminUserName} requested account blocking for user {UserId}", adminUserName, userId);
        var result = await _banUserHandler.HandleAsync(new BanUserCommand(userId));

        if (result.Succeeded)
        {
            _logger.LogInformation("Admin {AdminUserName} successfully blocked user account {UserId}", adminUserName, userId);
            TempData["AdminAlertType"] = "success";
            TempData["AdminAlertMessage"] = "The user has been blocked.";
        }
        else if (result.ErrorMessage == ResultErrors.UserNotFound)
        {
            _logger.LogWarning("Admin {AdminUserName} attempted to block user {UserId}, but account was not found", adminUserName, userId);
            TempData["AdminAlertType"] = "warning";
            TempData["AdminAlertMessage"] = "User not found.";
        }
        else
        {
            _logger.LogError("Admin {AdminUserName} failed to block user account {UserId}", adminUserName, userId);
            TempData["AdminAlertType"] = "danger";
            TempData["AdminAlertMessage"] = "Failed to block the user.";
        }

        return RedirectToAction(nameof(Index), new { searchTerm });
    }

    public async Task<IActionResult> Dashboard()
    {
        var result = await _systemStatsHandler.HandleAsync(new GetSystemStatsQuery());

        if (!result.Succeeded || result.Value is null)
        {
            _logger.LogError("Failed to load system statistics for dashboard");
            return View(new DashboardViewModel());
        }

        var viewModel = new DashboardViewModel
        {
            TotalChecklists = result.Value.TotalChecklists,
            TotalUsers = result.Value.TotalUsers
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _logoutHandler.HandleAsync(new LogoutCommand(DateTime.UtcNow));
        _logger.LogInformation("Admin logged out");
        return RedirectToAction("Index", "Home");
    }
}
