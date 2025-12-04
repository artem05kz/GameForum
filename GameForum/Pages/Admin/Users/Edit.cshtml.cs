using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;
using GameForum.Model;
using GameForum.Services;

namespace GameForum.Pages.Admin.Users;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    public EditModel(AppDbContext db, AuthService auth) { _db = db; _auth = auth; }

    [BindProperty]
    public EditUserDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _db.AuthUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        Input = new EditUserDto { Id = user.Id, Username = user.Username, DisplayName = user.DisplayName, IsAdmin = user.IsAdmin };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _db.AuthUsers.FirstOrDefaultAsync(u => u.Id == Input.Id);
        if (user == null) return NotFound();
        // Р•СЃР»Рё Р»РѕРіРёРЅ РјРµРЅСЏРµС‚СЃСЏ, РїСЂРѕРІРµСЂРёРј СѓРЅРёРєР°Р»СЊРЅРѕСЃС‚СЊ
        if (!string.Equals(user.Username, Input.Username, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _db.AuthUsers.AnyAsync(u => u.Username == Input.Username && u.Id != user.Id);
            if (exists)
            {
                ModelState.AddModelError("Input.Username", "Р›РѕРіРёРЅ СѓР¶Рµ РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ");
                return Page();
            }
            user.Username = Input.Username.Trim();
        }
        user.DisplayName = Input.DisplayName.Trim();
        user.IsAdmin = Input.IsAdmin;
        if (!string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            _auth.CreatePasswordHash(Input.NewPassword, out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
        }
        await _db.SaveChangesAsync();
        return RedirectToPage("/Admin/Users/Index");
    }

    public class EditUserDto
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        [StringLength(100, MinimumLength = 6)]
        public string? NewPassword { get; set; }
    }
}

