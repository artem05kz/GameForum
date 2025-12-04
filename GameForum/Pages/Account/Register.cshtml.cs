using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;
using GameForum.Model;
using GameForum.Services;

namespace GameForum.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    public RegisterModel(AppDbContext db, AuthService auth) { _db = db; _auth = auth; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var exists = await _db.AuthUsers.AnyAsync(u => u.Username == Input.Username);
        if (exists)
        {
            ModelState.AddModelError("Input.Username", "РџРѕР»СЊР·РѕРІР°С‚РµР»СЊ СЃ С‚Р°РєРёРј Р»РѕРіРёРЅРѕРј СѓР¶Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚");
            return Page();
        }
        _auth.CreatePasswordHash(Input.Password, out var hash, out var salt);
        var user = new AuthUser { Username = Input.Username, DisplayName = Input.DisplayName, PasswordHash = hash, PasswordSalt = salt, IsAdmin = false };
        _db.AuthUsers.Add(user);
        await _db.SaveChangesAsync();

        // sign-in
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("displayName", user.DisplayName),
            new Claim("isAdmin", user.IsAdmin.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return RedirectToPage("/Forum/Index");
    }

    public class InputModel
    {
        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;
        [Required, StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
    }
}

