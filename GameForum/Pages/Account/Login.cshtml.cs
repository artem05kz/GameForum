using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using GameForum.Data;
using GameForum.Services;
using Microsoft.Extensions.Logging;

namespace GameForum.Pages.Account;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    private readonly ILogger<LoginModel> _logger;
    public LoginModel(AppDbContext db, AuthService auth, ILogger<LoginModel> logger) 
    { 
        _db = db; 
        _auth = auth; 
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _db.AuthUsers.FirstOrDefaultAsync(u => u.Username == Input.Username);
        if (user == null || !_auth.VerifyPassword(Input.Password, user.PasswordHash, user.PasswordSalt))
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
            return Page();
        }
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("displayName", user.DisplayName),
            new Claim("isAdmin", user.IsAdmin.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        await HttpContext.Session.LoadAsync();
        var sessionId = HttpContext.Session.Id;
        _logger.LogInformation("[Login] Session ID before write: {SessionId}", sessionId);
        
        HttpContext.Session.SetString("uid", user.Id.ToString());
        HttpContext.Session.SetString("uname", user.Username);
        HttpContext.Session.SetString("displayName", user.DisplayName);
        HttpContext.Session.SetString("loginTime", DateTime.UtcNow.ToString("O"));
        
        _logger.LogInformation("[Login] Session data written, committing to store...");

        await HttpContext.Session.CommitAsync();
        
        _logger.LogInformation("[Login] Session committed successfully. SessionId={SessionId}", sessionId);
        
        return RedirectToPage("/Forum/Index");
    }

    public class InputModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}

