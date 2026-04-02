using Application.UseCases.CloneChecklist;
using Application.UseCases.DeleteChecklist;
using Application.UseCases.GetUserChecklists;
using Application.UseCases.ToggleChecklistStatus;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Mappings;
using Web.Models.Author;

namespace Web.Controllers;

[Authorize]
public sealed class AuthorController : BaseController
{
    private readonly GetUserChecklistsQueryHandler _handler;
    private readonly DeleteChecklistCommandHandler _deleteHandler;
    private readonly CloneChecklistCommandHandler _cloneHandler;
    private readonly ToggleChecklistStatusCommandHandler _toggleStatusHandler;
    private readonly ILogger<AuthorController> _logger;

    public AuthorController(
        GetUserChecklistsQueryHandler handler,
        DeleteChecklistCommandHandler deleteHandler,
        CloneChecklistCommandHandler cloneHandler,
        ToggleChecklistStatusCommandHandler toggleStatusHandler,
        ILogger<AuthorController> logger)
    {
        _handler = handler;
        _deleteHandler = deleteHandler;
        _cloneHandler = cloneHandler;
        _toggleStatusHandler = toggleStatusHandler;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToLogin();
        }

        _logger.LogInformation("Author {UserId} requested their checklist page", userId);

        var result = await _handler.HandleAsync(new GetUserChecklistsQuery(userId));

        var viewModel = new AuthorChecklistsViewModel
        {
            Checklists = result.Succeeded
                ? result.Value!.Select(c => c.ToAuthorViewModel()).ToList()
                : new()
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

        var userId = CurrentUserId;

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToLogin();
        }

        _logger.LogInformation("User {UserId} requested deletion for checklist {ChecklistId}", userId, id);
        var result = await _deleteHandler.HandleAsync(new DeleteChecklistCommand(id, userId));

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to delete checklist {ChecklistId} for user {UserId}: {Error}", id, userId, result.ErrorMessage);
            SetErrorMessage(result.ErrorMessage ?? "Failed to delete checklist.");
        }
        else
        {
            _logger.LogInformation("Checklist {ChecklistId} deleted successfully for user {UserId}", id, userId);
            SetSuccessMessage("Checklist deleted successfully.");
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

        var userId = CurrentUserId;

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToLogin();
        }

        _logger.LogInformation("User {UserId} requested clone for checklist {ChecklistId}", userId, id);
        var result = await _cloneHandler.HandleAsync(new CloneChecklistCommand(id, userId));

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to clone checklist {ChecklistId} for user {UserId}: {Error}", id, userId, result.ErrorMessage);
            SetErrorMessage(result.ErrorMessage ?? "Failed to clone checklist.");
        }
        else
        {
            _logger.LogInformation("Checklist {ChecklistId} successfully cloned for user {UserId}", id, userId);
            SetSuccessMessage("Checklist cloned successfully.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await ToggleStatus(id, ChecklistStatus.Published);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await ToggleStatus(id, ChecklistStatus.Draft);
    }

    private async Task<IActionResult> ToggleStatus(Guid id, ChecklistStatus newStatus)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToLogin();
        }

        var result = await _toggleStatusHandler.HandleAsync(
            new ToggleChecklistStatusCommand(id, newStatus, userId));

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to change status of checklist {ChecklistId} for user {UserId}: {Error}", id, userId, result.ErrorMessage);
            SetErrorMessage(result.ErrorMessage ?? "Failed to change checklist status.");
        }

        return RedirectToAction(nameof(Index));
    }
}
