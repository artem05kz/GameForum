using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;
using GameForum.Model;

namespace GameForum.Pages.Admin.Users;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public IList<AuthUser> Users { get; private set; } = new List<AuthUser>();

    public async Task OnGet()
    {
        Users = await _db.AuthUsers.AsNoTracking().OrderBy(u => u.Id).ToListAsync();
    }
}

