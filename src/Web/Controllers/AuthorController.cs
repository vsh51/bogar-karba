using System.Security.Claims;
using Application.UseCases.GetUserChecklists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models.Author;

namespace Web.Controllers;

[Authorize]
public sealed class AuthorController : Controller
{
    private readonly GetUserChecklistsQueryHandler _handler;
    private readonly ILogger<AuthorController> _logger;

    public AuthorController(
        GetUserChecklistsQueryHandler handler,
        ILogger<AuthorController> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        _logger.LogInformation("Author {UserId} requested their checklist page", userId);

        var result = _handler.Handle(new GetUserChecklistsQuery(userId));

        var viewModel = new AuthorChecklistsViewModel
        {
            Checklists = result.Checklists
        };

        return View(viewModel);
    }
}
