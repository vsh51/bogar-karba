using Application.Common;
using Application.Interfaces;
using Application.UseCases.AddChecklistItem;
using Application.UseCases.CreateChecklist;
using Application.UseCases.DeleteChecklist;
using Application.UseCases.EditChecklist;
using Application.UseCases.ExportChecklist;
using Application.UseCases.ExportChecklist.Markdown;
using Application.UseCases.GetPublishedChecklist;
using Application.UseCases.RemoveChecklistItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Mappings;
using Web.Models.Checklist;

namespace Web.Controllers;

[Route("checklist")]
public sealed class ChecklistController : BaseController
{
    private readonly GetPublishedChecklistQueryHandler _handler;
    private readonly CreateChecklistCommandHandler _createHandler;
    private readonly DeleteChecklistCommandHandler _deleteHandler;
    private readonly EditChecklistCommandHandler _editHandler;
    private readonly ExportMarkdownQueryHandler _exportHandler;
    private readonly AddChecklistItemCommandHandler _addItemHandler;
    private readonly RemoveChecklistItemCommandHandler _removeItemHandler;
    private readonly IChecklistReadOnlyRepository _readRepository;
    private readonly ILogger<ChecklistController> _logger;

    public ChecklistController(
        GetPublishedChecklistQueryHandler handler,
        CreateChecklistCommandHandler createHandler,
        DeleteChecklistCommandHandler deleteHandler,
        EditChecklistCommandHandler editHandler,
        ExportMarkdownQueryHandler exportHandler,
        AddChecklistItemCommandHandler addItemHandler,
        RemoveChecklistItemCommandHandler removeItemHandler,
        IChecklistReadOnlyRepository readRepository,
        ILogger<ChecklistController> logger)
    {
        _handler = handler;
        _createHandler = createHandler;
        _deleteHandler = deleteHandler;
        _editHandler = editHandler;
        _exportHandler = exportHandler;
        _addItemHandler = addItemHandler;
        _removeItemHandler = removeItemHandler;
        _readRepository = readRepository;
        _logger = logger;
    }

    [HttpGet("create")]
    [Authorize]
    public IActionResult Create()
    {
        var userId = CurrentUserId ?? "unknown-user";
        _logger.LogInformation("Checklist create page requested by user {UserId}", userId);
        return View();
    }

    [HttpPost("create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateChecklistViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "Checklist creation validation failed with {ErrorCount} errors",
                ModelState.ErrorCount);
            return BadRequest(ModelState);
        }

        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Checklist creation denied: unauthenticated user");
            return Unauthorized();
        }

        _logger.LogInformation(
            "Checklist creation requested by user {UserId}: title '{Title}', sections {SectionCount}",
            userId,
            model.Title,
            model.Sections.Count);

        var request = new CreateChecklistCommand(
            model.Title,
            model.Description,
            model.Sections.Select(s => new CreateSectionRequest(
                s.Name,
                s.Position,
                s.Tasks.Select(t => new CreateTaskRequest(t.Content, t.Position)).ToList())).ToList());

        var result = await _createHandler.HandleAsync(request, userId);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "Checklist {ChecklistId} created successfully for user {UserId}",
                result.Value,
                userId);
            return Json(new { success = true, id = result.Value, redirectUrl = Url.Action("Show", "Checklist", new { id = result.Value }) });
        }

        _logger.LogWarning(
            "Checklist creation failed for user {UserId}: {Error}",
            userId,
            result.ErrorMessage ?? "Unknown error");

        return BadRequest(result.ErrorMessage ?? "An error occurred while creating the checklist.");
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Show(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Checklist page request validation failed for checklist {ChecklistId}", id);
            return BadRequest(ModelState);
        }

        var userId = CurrentUserId;

        var result = await _handler.HandleAsync(
            new GetPublishedChecklistQuery(id, userId), cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            _logger.LogInformation("Checklist {ChecklistId} not found or not available", id);
            return NotFound();
        }

        _logger.LogInformation("Checklist {ChecklistId} retrieved and displayed successfully", id);

        return View("Show", result.Value.ToChecklistViewModel());
    }

    [HttpPost("{id:guid}/export/markdown")]
    public async Task<IActionResult> ExportMarkdown(
        Guid id,
        [FromBody] ExportChecklistRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "Export markdown validation failed for checklist {ChecklistId} with {ErrorCount} errors",
                id,
                ModelState.ErrorCount);
            return BadRequest(ModelState);
        }

        var completedTaskIds = (request.CompletedTaskIds ?? Array.Empty<string>())
            .Where(s => Guid.TryParse(s, out _))
            .Select(Guid.Parse)
            .ToList();

        _logger.LogInformation(
            "Export markdown requested for checklist {ChecklistId} with {CompletedTaskCount} completed tasks",
            id,
            completedTaskIds.Count);

        var query = new ExportChecklistQuery(id, completedTaskIds);
        var result = await _exportHandler.HandleAsync(query, cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Export markdown failed for checklist {ChecklistId}: checklist not found or not published", id);
            return NotFound();
        }

        _logger.LogInformation("Export markdown completed for checklist {ChecklistId}", id);

        return Json(new { content = result.Value!.Content });
    }

    [HttpPost("delete/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "Checklist delete validation failed for checklist {ChecklistId} with {ErrorCount} errors",
                id,
                ModelState.ErrorCount);
            return BadRequest(ModelState);
        }

        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Checklist delete denied for checklist {ChecklistId}: unauthenticated user", id);
            return Unauthorized();
        }

        _logger.LogInformation("Checklist delete requested by user {UserId} for checklist {ChecklistId}", userId, id);

        var result = await _deleteHandler.HandleAsync(new DeleteChecklistCommand(id, userId));

        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Checklist delete failed for user {UserId} and checklist {ChecklistId}: {Error}",
                userId,
                id,
                result.ErrorMessage ?? "Unknown error");
            return BadRequest(result.ErrorMessage);
        }

        _logger.LogInformation("Checklist {ChecklistId} deleted successfully for user {UserId}", id, userId);

        return RedirectToAction("Index", "Author");
    }

    [HttpGet("edit/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var checklist = await _readRepository.GetByIdWithSectionsAsync(id);
        if (checklist is null)
        {
            return NotFound();
        }

        if (checklist.UserId != userId)
        {
            return Forbid();
        }

        ViewData["ChecklistId"] = checklist.Id;
        var viewModel = new EditChecklistViewModel
        {
            Title = checklist.Title,
            Description = checklist.Description,
            Sections = checklist.Sections
                .OrderBy(s => s.Position)
                .Select(s => new EditSectionViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Tasks = s.Tasks
                        .OrderBy(t => t.Position)
                        .Select(t => new EditTaskViewModel
                        {
                            Id = t.Id,
                            Content = t.Content
                        })
                        .ToList()
                })
                .ToList()
        };

        return View(viewModel);
    }

    [HttpPost("edit/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [FromBody] EditChecklistViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new EditChecklistCommand(
            id,
            userId,
            model.Title,
            model.Description,
            model.Sections.Select(s => new EditSectionRequest(
                s.Id,
                s.Name,
                s.Tasks.Select(t => new EditTaskRequest(t.Id, t.Content)).ToList())).ToList());

        var result = await _editHandler.HandleAsync(command);

        if (result.Succeeded)
        {
            return Json(new { success = true, redirectUrl = Url.Action("Show", "Checklist", new { id }) });
        }

        return BadRequest(result.ErrorMessage ?? "An error occurred while updating the checklist.");
    }

    [HttpPost("{id:guid}/items/add")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddChecklistItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation(
            "Add item requested by user {UserId} for checklist {ChecklistId}, section {SectionId}",
            userId,
            id,
            model.SectionId);

        var command = new AddChecklistItemCommand(id, userId, model.SectionId, model.Content);
        var result = await _addItemHandler.HandleAsync(command);

        if (result.Succeeded)
        {
            return Json(new { success = true, id = result.Value });
        }

        return BadRequest(result.ErrorMessage ?? "Failed to add item.");
    }

    [HttpPost("{id:guid}/items/{taskId:guid}/remove")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(Guid id, Guid taskId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation(
            "Remove item {TaskId} requested by user {UserId} for checklist {ChecklistId}",
            taskId,
            userId,
            id);

        var command = new RemoveChecklistItemCommand(id, userId, taskId);
        var result = await _removeItemHandler.HandleAsync(command);

        if (result.Succeeded)
        {
            return Json(new { success = true });
        }

        return BadRequest(result.ErrorMessage ?? "Failed to remove item.");
    }
}
