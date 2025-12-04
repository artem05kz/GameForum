using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using GameForum.Data;
using GameForum.Services;

namespace GameForum.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    private readonly IDistributedCache _cache;
    public ProfileModel(AppDbContext db, AuthService auth, IDistributedCache cache) { _db = db; _auth = auth; _cache = cache; }

    [BindProperty]
    [Required, StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [BindProperty]
    public PasswordDto PasswordInput { get; set; } = new();

    [BindProperty]
    public NotesDto NotesInput { get; set; } = new();

    public string? SavedNote { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
        var user = await _db.AuthUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();
        DisplayName = user.DisplayName;
        var noteKey = GetNoteKey(user.Id);
        SavedNote = await _cache.GetStringAsync(noteKey);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateName()
    {
        // ручная изолированная валидация только DisplayName
        ModelState.Clear();
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            ModelState.AddModelError(nameof(DisplayName), "Введите отображаемое имя");
            var userIdStr0 = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr0, out var uid0))
                SavedNote = await _cache.GetStringAsync(GetNoteKey(uid0));
            return Page();
        }
        if (DisplayName.Length > 100)
        {
            ModelState.AddModelError(nameof(DisplayName), "Имя должно быть не длиннее 100 символов");
            var userIdStr0 = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr0, out var uid0))
                SavedNote = await _cache.GetStringAsync(GetNoteKey(uid0));
            return Page();
        }
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
        var user = await _db.AuthUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();
        user.DisplayName = DisplayName.Trim();
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("displayName", user.DisplayName),
            new Claim("isAdmin", user.IsAdmin.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveNote()
    {
        ModelState.Clear();
        TryValidateModel(NotesInput, nameof(NotesInput));
        if (!ModelState.IsValid)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var uid))
            {
                SavedNote = await _cache.GetStringAsync(GetNoteKey(uid));
            }
            return Page();
        }

        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var id)) return Unauthorized();
        var key = GetNoteKey(id);
        await _cache.SetStringAsync(key, NotesInput.Content ?? string.Empty, new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromDays(30)
        });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteNote()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
        await _cache.RemoveAsync(GetNoteKey(userId));
        return RedirectToPage();
    }

    private static string GetNoteKey(int userId) => $"user:{userId}:notes";

    public async Task<IActionResult> OnPostChangePassword()
    {
        // валидируем только PasswordInput
        ModelState.Clear();
        TryValidateModel(PasswordInput, nameof(PasswordInput));
        if (!ModelState.IsValid)
        {
            var userIdStr0 = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr0, out var uid0))
                SavedNote = await _cache.GetStringAsync(GetNoteKey(uid0));
            return Page();
        }
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
        var user = await _db.AuthUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();

        if (!_auth.VerifyPassword(PasswordInput.CurrentPassword, user.PasswordHash, user.PasswordSalt))
        {
            ModelState.AddModelError("PasswordInput.CurrentPassword", "Текущий пароль неверный");
            // вернуть страницу с сохранённой заметкой
            SavedNote = await _cache.GetStringAsync(GetNoteKey(user.Id));
            return Page();
        }
        if (PasswordInput.NewPassword != PasswordInput.ConfirmPassword)
        {
            ModelState.AddModelError("PasswordInput.ConfirmPassword", "Пароли не совпадают");
            SavedNote = await _cache.GetStringAsync(GetNoteKey(user.Id));
            return Page();
        }
        _auth.CreatePasswordHash(PasswordInput.NewPassword, out var hash, out var salt);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _db.SaveChangesAsync();
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("displayName", user.DisplayName),
            new Claim("isAdmin", user.IsAdmin.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return RedirectToPage();
    }

    public class PasswordDto
    {
        [Required, StringLength(100, MinimumLength = 6)]
        public string CurrentPassword { get; set; } = string.Empty;
        [Required, StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
        [Required, StringLength(100, MinimumLength = 6)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class NotesDto
    {
        [Required]
        [StringLength(1000, ErrorMessage = "Заметка слишком длинная (до 1000 символов)")]
        public string Content { get; set; } = string.Empty;
    }
    public IActionResult OnGetSessionDebug()
{
    return new JsonResult(new {
        id = HttpContext.Session.Id,
        hasUid = HttpContext.Session.TryGetValue("uid", out _)
    });
}
}

