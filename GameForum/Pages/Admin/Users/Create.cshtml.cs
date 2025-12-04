using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;
using GameForum.Model;
using GameForum.Services;

namespace GameForum.Pages.Admin.Users;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    public CreateModel(AppDbContext db, AuthService auth) { _db = db; _auth = auth; }

    [BindProperty]
    public CreateUserDto Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        if (await _db.AuthUsers.AnyAsync(u => u.Username == Input.Username))
        {
            ModelState.AddModelError("Input.Username", "Р›РѕРіРёРЅ СѓР¶Рµ РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ");
            return Page();
        }
        _auth.CreatePasswordHash(Input.Password, out var hash, out var salt);
        var user = new AuthUser { Username = Input.Username.Trim(), DisplayName = Input.DisplayName.Trim(), IsAdmin = Input.IsAdmin, PasswordHash = hash, PasswordSalt = salt };
        _db.AuthUsers.Add(user);
        await _db.SaveChangesAsync();
        return RedirectToPage("/Admin/Users/Index");
    }

    public class CreateUserDto
    {
        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;
        [Required, StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}

