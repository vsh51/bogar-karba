using Application.UseCases.GetPublishedChecklist;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Web.Models.Checklist;

namespace Web.Controllers;

[Route("checklist")]
public sealed class ChecklistController : Controller
{
    private readonly GetPublishedChecklistQueryHandler _handler;
    private readonly ILogger<ChecklistController> _logger;

    public ChecklistController(
        GetPublishedChecklistQueryHandler handler,
        ILogger<ChecklistController> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Show(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Anonymous user requested checklist page for {ChecklistId}", id);

        GetPublishedChecklistResult? result;

        result = await _handler.HandleAsync(
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
