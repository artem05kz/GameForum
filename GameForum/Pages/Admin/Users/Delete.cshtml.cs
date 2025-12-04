using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;
using GameForum.Model;

namespace GameForum.Pages.Admin.Users;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    public DeleteModel(AppDbContext db) => _db = db;

    [BindProperty]
    public int Id { get; set; }

    public AuthUser? TargetUser { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        TargetUser = await _db.AuthUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (TargetUser == null) return NotFound();
        Id = id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _db.AuthUsers.FindAsync(Id);
        if (user == null) return NotFound();
        _db.AuthUsers.Remove(user);
        await _db.SaveChangesAsync();
        return RedirectToPage("/Admin/Users/Index");
    }
}

