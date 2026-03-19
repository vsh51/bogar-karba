using Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class AdminController(SearchChecklistsService searchService) : Controller
{
    public IActionResult Index(string? searchTerm)
    {
        var results = searchService.Execute(searchTerm);

        ViewData["SearchTerm"] = searchTerm;
        return View(results);
    }
}
