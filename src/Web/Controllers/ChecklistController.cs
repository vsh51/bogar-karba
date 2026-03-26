using System.Security.Claims;
using Application.UseCases.CreateChecklist;
using Application.UseCases.GetPublishedChecklist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Web.Models.Checklist;

namespace Web.Controllers;

[Route("checklist")]
public sealed class ChecklistController : Controller
{
    private readonly GetPublishedChecklistQueryHandler _handler;
    private readonly CreateChecklistCommandHandler _createHandler;
    private readonly ILogger<ChecklistController> _logger;

    public ChecklistController(
        GetPublishedChecklistQueryHandler handler,
        CreateChecklistCommandHandler createHandler,
        ILogger<ChecklistController> logger)
    {
        _handler = handler;
        _createHandler = createHandler;
        _logger = logger;
    }

    [HttpGet("create")]
    [Authorize]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateChecklistRequest request)
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

        var result = await _createHandler.HandleAsync(request, userId);

        if (result.Succeeded)
        {
            return Json(new { success = true, id = result.Id, redirectUrl = Url.Action("Show", "Checklist", new { id = result.Id }) });
        }

        return BadRequest(result.ErrorMessage);
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
}
