using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;

namespace GameForum.Pages.Forum.Messages;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public int TopicId { get; set; }

    [BindProperty]
    [Required, StringLength(2000)]
    public new string Content { get; set; } = string.Empty;

    private int? CurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idStr, out var id)) return id;
        return null;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var msg = await _db.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (msg == null) return NotFound();
        var userId = CurrentUserId();
        if (!userId.HasValue || msg.AuthorId != userId.Value) return Forbid();
        TopicId = msg.TopicId;
        Content = msg.Content;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid) return Page();
        var msg = await _db.Messages.FirstOrDefaultAsync(m => m.Id == id);
        if (msg == null) return NotFound();
        var userId = CurrentUserId();
        if (!userId.HasValue || msg.AuthorId != userId.Value) return Forbid();
        msg.Content = Content.Trim();
        msg.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToPage("/Forum/Topic", new { id = msg.TopicId });
    }
}

