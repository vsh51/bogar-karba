using Microsoft.AspNetCore.Mvc;
using Application.Services;

namespace Web.Controllers;

public class ChecklistController : Controller 
{
    private readonly ChecklistService _service;

    public ChecklistController(ChecklistService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var checklists = await _service.GetAllChecklists();
        
        return View(checklists); 
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteChecklist(id);
        return RedirectToAction(nameof(Index));
    }
}