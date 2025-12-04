using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameForum.Pages
{
    public class DrawerModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        public bool HasId => !string.IsNullOrEmpty(Id);
        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            if (HasId)
            {
                // Проверяем, что ID - целое число
                if (!int.TryParse(Id, out _))
                {
                    ErrorMessage = "Error: The ID must contain only numbers.";
                    return;
                }
            }
        }
    }
}
