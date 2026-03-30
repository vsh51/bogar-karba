using System.Security.Claims;
using Application.Common;
using Application.UseCases.DeleteChecklist;
using Application.UseCases.GetUserChecklists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models.Author;

namespace Web.Controllers;

[Authorize]
public sealed class AuthorController : Controller
{
    private readonly GetUserChecklistsQueryHandler _handler;
    private readonly DeleteChecklistCommandHandler _deleteHandler;
    private readonly ILogger<AuthorController> _logger;

    public AuthorController(
        GetUserChecklistsQueryHandler handler,
        DeleteChecklistCommandHandler deleteHandler,
        ILogger<AuthorController> logger)
    {
        _handler = handler;
        _deleteHandler = deleteHandler;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        _logger.LogInformation("Author {UserId} requested their checklist page", userId);

        var result = await _handler.HandleAsync(new GetUserChecklistsQuery(userId));

        var viewModel = new AuthorChecklistsViewModel
        {
            Checklists = result.Value ?? new()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _deleteHandler.HandleAsync(new DeleteChecklistCommand(id, userId));

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to delete checklist {ChecklistId} for user {UserId}: {Error}", id, userId, result.ErrorMessage);
            TempData["Error"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index));
    }
}
