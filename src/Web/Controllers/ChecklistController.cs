using Application.Common;
using Application.UseCases.AddChecklistItem;
using Application.UseCases.CreateChecklist;
using Application.UseCases.DeleteChecklist;
using Application.UseCases.EditChecklist;
using Application.UseCases.ExportChecklist;
using Application.UseCases.ExportChecklist.Markdown;
using Application.UseCases.GetChecklistForEdit;
using Application.UseCases.GetChecklistsByIds;
using Application.UseCases.GetPublishedChecklist;
using Application.UseCases.GroupTasksIntoSection;
using Application.UseCases.QuickCreateChecklist;
using Application.UseCases.RemoveChecklistItem;
using Application.UseCases.ReorderChecklistItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Filters;
using Web.Mappings;
using Web.Models.Checklist;

namespace Web.Controllers;

[Route("checklist")]
[ValidateModelState]
public sealed class ChecklistController : BaseController
{
    private readonly GetPublishedChecklistQueryHandler _handler;
    private readonly CreateChecklistCommandHandler _createHandler;
    private readonly DeleteChecklistCommandHandler _deleteHandler;
    private readonly EditChecklistCommandHandler _editHandler;
    private readonly ExportMarkdownQueryHandler _exportHandler;
    private readonly GetChecklistForEditQueryHandler _getForEditHandler;
    private readonly ReorderChecklistItemCommandHandler _reorderItemHandler;
    private readonly GroupTasksIntoSectionCommandHandler _groupTasksHandler;
    private readonly AddChecklistItemCommandHandler _addItemHandler;
    private readonly RemoveChecklistItemCommandHandler _removeItemHandler;
    private readonly GetChecklistsByIdsQueryHandler _getByIdsHandler;
    private readonly QuickCreateChecklistCommandHandler _quickCreateHandler;
    private readonly ILogger<ChecklistController> _logger;

    public ChecklistController(
        GetPublishedChecklistQueryHandler handler,
        CreateChecklistCommandHandler createHandler,
        DeleteChecklistCommandHandler deleteHandler,
        EditChecklistCommandHandler editHandler,
        ExportMarkdownQueryHandler exportHandler,
        GetChecklistForEditQueryHandler getForEditHandler,
        ReorderChecklistItemCommandHandler reorderItemHandler,
        GroupTasksIntoSectionCommandHandler groupTasksHandler,
        AddChecklistItemCommandHandler addItemHandler,
        RemoveChecklistItemCommandHandler removeItemHandler,
        GetChecklistsByIdsQueryHandler getByIdsHandler,
        QuickCreateChecklistCommandHandler quickCreateHandler,
        ILogger<ChecklistController> logger)
    {
        _handler = handler;
        _createHandler = createHandler;
        _deleteHandler = deleteHandler;
        _editHandler = editHandler;
        _exportHandler = exportHandler;
        _getForEditHandler = getForEditHandler;
        _reorderItemHandler = reorderItemHandler;
        _groupTasksHandler = groupTasksHandler;
        _addItemHandler = addItemHandler;
        _removeItemHandler = removeItemHandler;
        _getByIdsHandler = getByIdsHandler;
        _quickCreateHandler = quickCreateHandler;
        _logger = logger;
    }

    [HttpGet("create")]
    [Authorize]
    public IActionResult Create()
    {
        _logger.LogInformation("Checklist create page requested by user {UserId}", RequiredUserId);
        return View();
    }

    [HttpPost("create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateChecklistViewModel model)
    {
        var userId = RequiredUserId;

        _logger.LogInformation(
            "Checklist creation requested by user {UserId}: title '{Title}', sections {SectionCount}",
            userId,
            model.Title,
            model.Sections.Count);

        var request = new CreateChecklistCommand(
            model.Title,
            model.Description,
            model.Deadline,
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

    [HttpGet("quick-create")]
    [Authorize]
    public IActionResult QuickCreate()
    {
        _logger.LogInformation("Quick checklist create page requested by user {UserId}", RequiredUserId);
        return View();
    }

    [HttpPost("quick-create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreate([FromBody] QuickCreateChecklistViewModel model)
    {
        var userId = RequiredUserId;

        _logger.LogInformation(
            "Quick checklist creation requested by user {UserId}: input length {Length}",
            userId,
            model.RawText?.Length ?? 0);

        var command = new QuickCreateChecklistCommand(model.RawText ?? string.Empty);
        var result = await _quickCreateHandler.HandleAsync(command, userId);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "Checklist {ChecklistId} quick-created successfully for user {UserId}",
                result.Value,
                userId);
            return Json(new { success = true, id = result.Value, redirectUrl = Url.Action("Show", "Checklist", new { id = result.Value }) });
        }

        _logger.LogWarning(
            "Quick checklist creation failed for user {UserId}: {Error}",
            userId,
            result.ErrorMessage ?? "Unknown error");

        return BadRequest(result.ErrorMessage ?? "An error occurred while creating the checklist.");
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Show(Guid id, CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(
            new GetPublishedChecklistQuery(id, CurrentUserId), cancellationToken);

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
        var userId = RequiredUserId;

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
        var userId = RequiredUserId;
        var result = await _getForEditHandler.HandleAsync(new GetChecklistForEditQuery(id, userId));

        if (!result.Succeeded)
        {
            return result.ErrorMessage == ResultErrors.NotChecklistOwner
                ? Forbid()
                : NotFound();
        }

        var data = result.Value!;
        ViewData["ChecklistId"] = data.Id;
        var viewModel = new EditChecklistViewModel
        {
            Title = data.Title,
            Description = data.Description,
            Deadline = data.Deadline,
            Sections = data.Sections
                .Select(s => new EditSectionViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Tasks = s.Tasks
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
        var userId = RequiredUserId;

        var command = new EditChecklistCommand(
            id,
            userId,
            model.Title,
            model.Description,
            model.Deadline,
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

    [HttpPost("{id:guid}/items/reorder")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderItem(Guid id, [FromBody] ReorderChecklistItemViewModel model)
    {
        var userId = RequiredUserId;

        _logger.LogInformation(
            "Reorder item {TaskId} to section {TargetSectionId} position {Position} requested by user {UserId} for checklist {ChecklistId}",
            model.TaskId,
            model.TargetSectionId,
            model.NewPosition,
            userId,
            id);

        var command = new ReorderChecklistItemCommand(id, userId, model.TaskId, model.TargetSectionId, model.NewPosition);
        var result = await _reorderItemHandler.HandleAsync(command);

        if (result.Succeeded)
        {
            return Json(new { success = true });
        }

        return BadRequest(result.ErrorMessage ?? "Failed to reorder item.");
    }

    [HttpPost("{id:guid}/sections/group")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GroupTasksIntoSection(Guid id, [FromBody] GroupTasksIntoSectionViewModel model)
    {
        var userId = RequiredUserId;

        _logger.LogInformation(
            "Group {Count} tasks into section '{SectionName}' requested by user {UserId} for checklist {ChecklistId}",
            model.TaskIds.Count,
            model.SectionName,
            userId,
            id);

        var command = new GroupTasksIntoSectionCommand(id, userId, model.SectionName, model.TaskIds);
        var result = await _groupTasksHandler.HandleAsync(command);

        if (result.Succeeded)
        {
            return Json(new { success = true, id = result.Value });
        }

        return BadRequest(result.ErrorMessage ?? "Failed to group tasks into section.");
    }

    [HttpPost("{id:guid}/items/add")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddChecklistItemViewModel model)
    {
        var userId = RequiredUserId;

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
        var userId = RequiredUserId;

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

    [HttpPost("get-by-ids")]
    public async Task<IActionResult> GetByIds([FromBody] List<Guid> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            _logger.LogInformation("GetByIds requested with empty list");
            return Json(new List<object>());
        }

        _logger.LogInformation(
            "Fetching multiple checklists by IDs. Count: {Count}",
            ids.Count);

        var query = new GetChecklistsByIdsQuery(ids);
        var result = await _getByIdsHandler.HandleAsync(query);

        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Failed to fetch checklists by IDs: {Error}",
                result.ErrorMessage);
            return BadRequest(result.ErrorMessage);
        }

        return Json(result.Value);
    }
}
