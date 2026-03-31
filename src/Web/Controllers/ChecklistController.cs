using System.Security.Claims;
using Application.Common;
using Application.UseCases.CreateChecklist;
using Application.UseCases.DeleteChecklist;
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
    ILogger<ChecklistController> logger) : Controller
{
    private readonly GetPublishedChecklistQueryHandler _handler = handler;
    private readonly CreateChecklistCommandHandler _createHandler = createHandler;
    private readonly DeleteChecklistCommandHandler _deleteHandler = deleteHandler;
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

        return View("Show", viewModel);
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
