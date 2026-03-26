using System.Security.Claims;
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

        // Map ViewModel to Application DTO
        var request = new CreateChecklistRequest(
            model.Title,
            model.Description,
            model.Sections.Select(s => new CreateSectionRequest(
                s.Name,
                s.Position,
                s.Tasks.Select(t => new CreateTaskRequest(t.Content, t.Position)).ToList())).ToList());

        var result = await _createHandler.HandleAsync(request, userId);

        if (result.Succeeded)
        {
            return Json(new { success = true, id = result.Id, redirectUrl = Url.Action("Show", "Checklist", new { id = result.Id }) });
        }

        return BadRequest("An error occurred while creating the checklist.");
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

        if (result is null)
        {
            _logger.LogInformation("Checklist {ChecklistId} not found or not published", id);
            return NotFound();
        }

        var viewModel = new ChecklistViewModel
        {
            Id = result.Id,
            Title = result.Title,
            Description = result.Description,
            Sections = result.Sections
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

        await _deleteHandler.HandleAsync(new DeleteChecklistCommand(id, userId));

        return RedirectToAction("Index", "Author");
    }
}
