using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;
using GameForum.Model;

namespace GameForum.Pages.Forum;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<TopicItem> Topics { get; private set; } = new();

    [BindProperty]
    public CreateTopicInput Input { get; set; } = new();

    public async Task OnGet()
    {
        Topics = await _db.Topics
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TopicItem
            {
                Id = t.Id,
                Title = t.Title,
                Tags = t.Tags,
                CreatedAt = t.CreatedAt,
                CreatorName = _db.AuthUsers.Where(u => u.Id == t.CreatorId).Select(u => u.DisplayName).FirstOrDefault() ?? "Unknown"
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return Unauthorized();
        }
        if (!ModelState.IsValid)
        {
            await OnGet();
            return Page();
        }
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }
        var topic = new Topic
        {
            Title = Input.Title,
            Tags = Input.Tags ?? string.Empty,
            CreatorId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Topics.Add(topic);
        await _db.SaveChangesAsync();
        return RedirectToPage("/Forum/Index");
    }

    public class CreateTopicInput
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;
        [StringLength(200)]
        public string? Tags { get; set; }
    }

    public class TopicItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

