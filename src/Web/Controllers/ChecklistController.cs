using System.Security.Claims;
using Application.Common;
using Application.UseCases.CreateChecklist;
using Application.UseCases.DeleteChecklist;
using Application.UseCases.ExportChecklist;
using Application.UseCases.ExportChecklist.Markdown;
using Application.UseCases.GetPublishedChecklist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Web.Models.Checklist;

namespace Web.Controllers;

[Route("checklist")]
public sealed class ChecklistController(
    GetPublishedChecklistQueryHandler handler,
    CreateChecklistCommandHandler createHandler,
    DeleteChecklistCommandHandler deleteHandler,
    ExportMarkdownQueryHandler exportHandler,
    ILogger<ChecklistController> logger) : Controller
{
    private readonly GetPublishedChecklistQueryHandler _handler = handler;
    private readonly CreateChecklistCommandHandler _createHandler = createHandler;
    private readonly DeleteChecklistCommandHandler _deleteHandler = deleteHandler;
    private readonly ExportMarkdownQueryHandler _exportHandler = exportHandler;
    private readonly ILogger<ChecklistController> _logger = logger;

    [HttpGet("create")]
    [Authorize]
    public IActionResult Create()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown-user";
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

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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

        _logger.LogInformation("Anonymous user requested checklist page for {ChecklistId}", id);

        var result = await _handler.HandleAsync(
            new GetPublishedChecklistQuery(id), cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            _logger.LogInformation("Checklist {ChecklistId} not found or not published", id);
            return NotFound();
        }

        var viewModel = new ChecklistViewModel
        {
            Id = result.Value.Id,
            Title = result.Value.Title,
            Description = result.Value.Description,
            Sections = result.Value.Sections
                .OrderBy(s => s.Position)
                .Select(section => new ChecklistSectionViewModel
                {
                    Id = section.Id,
                    Name = section.Name,
                    Position = section.Position,
                    Items = section.Items
                        .OrderBy(i => i.Position)
                        .Select(item => new ChecklistItemViewModel
                        {
                            Id = item.Id,
                            Content = item.Content
                        })
                        .ToList()
                })
                .ToList()
        };
        _logger.LogInformation("Checklist {ChecklistId} retrieved and displayed successfully", id);

        return View("Show", viewModel);
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

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
}
