using Application.UseCases.CloneChecklist;
using Application.UseCases.DeleteChecklist;
using Application.UseCases.GetUserChecklists;
using Application.UseCases.SetChecklistVisibility;
using Application.UseCases.ToggleChecklistStatus;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Filters;
using Web.Mappings;
using Web.Models.Author;

namespace Web.Controllers;

[Authorize]
[ValidateModelState]
public sealed class AuthorController : BaseController
{
    private readonly GetUserChecklistsQueryHandler _handler;
    private readonly DeleteChecklistCommandHandler _deleteHandler;
    private readonly CloneChecklistCommandHandler _cloneHandler;
    private readonly ToggleChecklistStatusCommandHandler _toggleStatusHandler;
    private readonly SetChecklistVisibilityCommandHandler _setVisibilityHandler;
    private readonly Application.UseCases.ShareChecklist.ShareChecklistCommandHandler _shareHandler;
    private readonly Application.UseCases.RevokeChecklistAccess.RevokeChecklistAccessCommandHandler _revokeAccessHandler;
    private readonly ILogger<AuthorController> _logger;

    public AuthorController(
        GetUserChecklistsQueryHandler handler,
        DeleteChecklistCommandHandler deleteHandler,
        CloneChecklistCommandHandler cloneHandler,
        ToggleChecklistStatusCommandHandler toggleStatusHandler,
        SetChecklistVisibilityCommandHandler setVisibilityHandler,
        Application.UseCases.ShareChecklist.ShareChecklistCommandHandler shareHandler,
        Application.UseCases.RevokeChecklistAccess.RevokeChecklistAccessCommandHandler revokeAccessHandler,
        ILogger<AuthorController> logger)
    {
        _handler = handler;
        _deleteHandler = deleteHandler;
        _cloneHandler = cloneHandler;
        _toggleStatusHandler = toggleStatusHandler;
        _setVisibilityHandler = setVisibilityHandler;
        _shareHandler = shareHandler;
        _revokeAccessHandler = revokeAccessHandler;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = RequiredUserId;
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
        var userId = RequiredUserId;

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
        var userId = RequiredUserId;

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
        return await ToggleStatus(id, ChecklistStatus.Published);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        return await ToggleStatus(id, ChecklistStatus.Draft);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakePublic(Guid id)
    {
        return await SetVisibility(id, true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakePrivate(Guid id)
    {
        return await SetVisibility(id, false);
    }

    [HttpPost("/author/share/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Share(Guid id, [FromForm] ShareChecklistViewModel model)
    {
        var userId = RequiredUserId;

        if (!ModelState.IsValid)
        {
            SetErrorMessage("Invalid share request.");
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("User {UserId} requested share for checklist {ChecklistId} with user {TargetUsername}", userId, id, model.TargetUsername);

        var command = new Application.UseCases.ShareChecklist.ShareChecklistCommand(id, userId, model.TargetUsername);
        var result = await _shareHandler.HandleAsync(command);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to share checklist {ChecklistId} for user {UserId}: {Error}", id, userId, result.ErrorMessage);
            SetErrorMessage(result.ErrorMessage ?? "Failed to share checklist.");
        }
        else
        {
            _logger.LogInformation("Checklist {ChecklistId} successfully shared with {TargetUsername} by user {UserId}", id, model.TargetUsername, userId);
            SetSuccessMessage($"Checklist shared successfully with {model.TargetUsername}.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/author/revoke/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeAccess(Guid id, [FromForm] string targetUserId)
    {
        var userId = RequiredUserId;

        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            SetErrorMessage("Invalid revoke request.");
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("User {UserId} requested revoke access for checklist {ChecklistId} from user {TargetUserId}", userId, id, targetUserId);

        var command = new Application.UseCases.RevokeChecklistAccess.RevokeChecklistAccessCommand(id, userId, targetUserId);
        var result = await _revokeAccessHandler.HandleAsync(command);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to revoke access for checklist {ChecklistId} by user {UserId}: {Error}", id, userId, result.ErrorMessage);
            SetErrorMessage(result.ErrorMessage ?? "Failed to revoke access.");
        }
        else
        {
            _logger.LogInformation("Checklist {ChecklistId} access successfully revoked for {TargetUserId} by user {UserId}", id, targetUserId, userId);
            SetSuccessMessage("Access revoked successfully.");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ToggleStatus(Guid id, ChecklistStatus newStatus)
    {
        var userId = RequiredUserId;

        var result = await _toggleStatusHandler.HandleAsync(
            new ToggleChecklistStatusCommand(id, newStatus, userId));

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to change status of checklist {ChecklistId} for user {UserId}: {Error}", id, userId, result.ErrorMessage);
            SetErrorMessage(result.ErrorMessage ?? "Failed to change checklist status.");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SetVisibility(Guid id, bool isPublic)
    {
        var userId = RequiredUserId;

        var result = await _setVisibilityHandler.HandleAsync(
            new SetChecklistVisibilityCommand(id, isPublic, userId));

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to change visibility of checklist {ChecklistId} for user {UserId}: {Error}", id, userId, result.ErrorMessage);
            SetErrorMessage(result.ErrorMessage ?? "Failed to change checklist visibility.");
        }

        return RedirectToAction(nameof(Index));
    }
}
