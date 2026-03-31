using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public abstract class BaseController : Controller
{
    /// <summary>
    /// Gets the current user's ID from the NameIdentifier claim.
    /// Returns null if the user is not authenticated.
    /// </summary>
    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Gets the current user's username.
    /// Returns null if the user is not authenticated.
    /// </summary>
    protected string? CurrentUserName => User.Identity?.Name;

    /// <summary>
    /// Standardized redirect to the Login page.
    /// </summary>
    protected IActionResult RedirectToLogin() => RedirectToAction("Login", "Account");

    /// <summary>
    /// Sets a success message to be displayed on the next request using TempData.
    /// </summary>
    protected void SetSuccessMessage(string message)
    {
        TempData["SuccessMessage"] = message;
    }

    /// <summary>
    /// Sets a warning message to be displayed on the next request using TempData.
    /// </summary>
    protected void SetWarningMessage(string message)
    {
        TempData["WarningMessage"] = message;
    }

    /// <summary>
    /// Sets an error message to be displayed on the next request using TempData.
    /// </summary>
    protected void SetErrorMessage(string message)
    {
        TempData["ErrorMessage"] = message;
    }
}
