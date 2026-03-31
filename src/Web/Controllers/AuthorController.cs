using System.Security.Claims;
using Application.UseCases.CloneChecklist;
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
    private readonly CloneChecklistCommandHandler _cloneHandler;
    private readonly ILogger<AuthorController> _logger;

    public AuthorController(
        GetUserChecklistsQueryHandler handler,
        DeleteChecklistCommandHandler deleteHandler,
        CloneChecklistCommandHandler cloneHandler,
        ILogger<AuthorController> logger)
    {
        _handler = handler;
        _deleteHandler = deleteHandler;
        _cloneHandler = cloneHandler;
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
            Checklists = result.Succeeded ? result.Value! : new()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Author checklist delete validation failed for checklist {ChecklistId}", id);
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        _logger.LogInformation("User {UserId} requested deletion for checklist {ChecklistId}", userId, id);
        var result = await _deleteHandler.HandleAsync(new DeleteChecklistCommand(id, userId));

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to delete checklist {ChecklistId} for user {UserId}: {Error}", id, userId, result.ErrorMessage);
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            _logger.LogInformation("Checklist {ChecklistId} deleted successfully for user {UserId}", id, userId);
            TempData["Success"] = "Checklist deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/author/clone/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clone(Guid id)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Author checklist clone validation failed for checklist {ChecklistId}", id);
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        _logger.LogInformation("User {UserId} requested clone for checklist {ChecklistId}", userId, id);
        var result = await _cloneHandler.HandleAsync(new CloneChecklistCommand(id, userId));

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to clone checklist {ChecklistId} for user {UserId}: {Error}", id, userId, result.ErrorMessage);
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            _logger.LogInformation("Checklist {ChecklistId} successfully cloned for user {UserId}", id, userId);
            TempData["Success"] = "Checklist cloned successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
