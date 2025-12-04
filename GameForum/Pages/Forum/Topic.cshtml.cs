using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;
using GameForum.Model;

namespace GameForum.Pages.Forum;

public class TopicModel : PageModel
{
    private readonly AppDbContext _db;
    public TopicModel(AppDbContext db) => _db = db;

    public TopicVm? Topic { get; private set; }
    public List<MessageVm> Messages { get; private set; } = new();
    public bool CanDeleteTopic { get; private set; }

    private int? CurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idStr, out var id)) return id;
        return null;
    }

    private bool IsAdmin() => string.Equals(User.FindFirst("isAdmin")?.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var topic = await _db.Topics.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (topic == null) return NotFound();
        var creatorName = await _db.AuthUsers.Where(u => u.Id == topic.CreatorId).Select(u => u.DisplayName).FirstOrDefaultAsync() ?? "Unknown";
        Topic = new TopicVm
        {
            Id = topic.Id,
            Title = topic.Title,
            Tags = topic.Tags,
            CreatorId = topic.CreatorId,
            CreatorName = creatorName,
            CreatedAt = topic.CreatedAt
        };

        var userId = CurrentUserId();
        var isAdmin = IsAdmin();
        CanDeleteTopic = isAdmin || (userId.HasValue && userId.Value == Topic.CreatorId);

        var msgs = await _db.Messages
            .Where(m => m.TopicId == id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
        var authorIds = msgs.Select(m => m.AuthorId).Distinct().ToList();
        var names = await _db.AuthUsers
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName);

        Messages = msgs.Select(m => new MessageVm
        {
            Id = m.Id,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt,
            AuthorId = m.AuthorId,
            AuthorName = names.TryGetValue(m.AuthorId, out var n) ? n : "Unknown",
            CanEdit = userId.HasValue && userId.Value == m.AuthorId,
            CanDelete = isAdmin || (userId.HasValue && userId.Value == m.AuthorId)
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostCreateMessageAsync(int id, string content)
    {
        if (!(User.Identity?.IsAuthenticated ?? false)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(content)) return await OnGetAsync(id);
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        var exists = await _db.Topics.AnyAsync(t => t.Id == id);
        if (!exists) return NotFound();
        _db.Messages.Add(new Message
        {
            TopicId = id,
            AuthorId = userId.Value,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("/Forum/Topic", new { id });
    }

    public async Task<IActionResult> OnPostDeleteMessageAsync(int id, int messageId)
    {
        var userId = CurrentUserId();
        var isAdmin = IsAdmin();
        var msg = await _db.Messages.FirstOrDefaultAsync(m => m.Id == messageId && m.TopicId == id);
        if (msg == null) return NotFound();
        if (!(isAdmin || (userId.HasValue && msg.AuthorId == userId.Value))) return Forbid();
        _db.Messages.Remove(msg);
        await _db.SaveChangesAsync();
        return RedirectToPage("/Forum/Topic", new { id });
    }

    public async Task<IActionResult> OnPostDeleteTopicAsync(int id)
    {
        var userId = CurrentUserId();
        var isAdmin = IsAdmin();
        var topic = await _db.Topics.FirstOrDefaultAsync(t => t.Id == id);
        if (topic == null) return NotFound();
        if (!(isAdmin || (userId.HasValue && topic.CreatorId == userId.Value))) return Forbid();
        var msgs = _db.Messages.Where(m => m.TopicId == id);
        _db.Messages.RemoveRange(msgs);
        _db.Topics.Remove(topic);
        await _db.SaveChangesAsync();
        return RedirectToPage("/Forum/Index");
    }

    public class TopicVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public int CreatorId { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class MessageVm
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}

