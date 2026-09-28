using AI200FoundryChat.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AI200FoundryChat.Pages;

public class IndexModel : PageModel
{
    private readonly FoundryService _foundryService;

    public IndexModel(
        FoundryService foundryService)
    {
        _foundryService = foundryService;
    }
        [BindProperty]
    public string Prompt { get; set; } = string.Empty;
     public string? ModelResponse { get; set; }
    public void OnGet()
    {

    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Prompt))
        {
            ModelState.AddModelError(
                "Prompt",
                "Please enter a prompt.");

            return Page();
        }

        ModelResponse =
            await _foundryService.GetResponseAsync(
                Prompt);

        return Page();
    }
}
