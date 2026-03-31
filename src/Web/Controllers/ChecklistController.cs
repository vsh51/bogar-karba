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
using Web.Mappings;
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
        return View();
    }

    [HttpPost("create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateChecklistViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

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
            return Json(new { success = true, id = result.Value, redirectUrl = Url.Action("Show", "Checklist", new { id = result.Value }) });
        }

        return BadRequest(result.ErrorMessage ?? "An error occurred while creating the checklist.");
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Show(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _handler.HandleAsync(
            new GetPublishedChecklistQuery(id, userId), cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            _logger.LogInformation("Checklist {ChecklistId} not found or not available", id);
            return NotFound();
        }

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
            return BadRequest(ModelState);
        }

        var completedTaskIds = (request.CompletedTaskIds ?? Array.Empty<string>())
            .Where(s => Guid.TryParse(s, out _))
            .Select(Guid.Parse)
            .ToList();

        var query = new ExportChecklistQuery(id, completedTaskIds);
        var result = await _exportHandler.HandleAsync(query, cancellationToken);

        if (!result.Succeeded)
        {
            return NotFound();
        }

        return Json(new { content = result.Value!.Content });
    }

    [HttpPost("delete/{id:guid}")]
    [Authorize]
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
            return Unauthorized();
        }

        var result = await _deleteHandler.HandleAsync(new DeleteChecklistCommand(id, userId));

        if (!result.Succeeded)
        {
            return BadRequest(result.ErrorMessage);
        }

        return RedirectToAction("Index", "Author");
    }
}
