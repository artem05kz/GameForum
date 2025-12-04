using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameForum.Pages.Account;

public class PreferencesModel : PageModel
{
    [BindProperty]
    [Required]
    [RegularExpression("^(light|dark)$")]
    public string Theme { get; set; } = "light";

    [BindProperty]
    [Required]
    [RegularExpression("^(ru-RU|en-US)$")]
    public string Language { get; set; } = "ru-RU";

    public void OnGet()
    {
        Theme = Request.Cookies["theme"] ?? "light";
        Language = Request.Cookies[CookieRequestCultureProvider.DefaultCookieName] is { } cultureCookie
            ? CookieRequestCultureProvider.ParseCookieValue(cultureCookie)?.UICultures.FirstOrDefault().Value ?? "ru-RU"
            : "ru-RU";
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        Response.Cookies.Append("theme", Theme, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, HttpOnly = false, SameSite = SameSiteMode.Lax });
        HttpContext.Session.SetString("theme", Theme);

        var culture = new RequestCulture(Language);
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(culture);
        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, cookieValue, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, HttpOnly = false, SameSite = SameSiteMode.Lax });
        HttpContext.Session.SetString("lang", Language);

        return RedirectToPage("/Account/Preferences");
    }
}

