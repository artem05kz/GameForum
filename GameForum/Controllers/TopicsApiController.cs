using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;
using GameForum.Model;

namespace GameForum.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TopicsApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public TopicsApiController(AppDbContext db) => _db = db;

    private int? CurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : null;
    }
    private bool IsAdmin() => string.Equals(User.FindFirst("isAdmin")?.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Topics
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new {
                t.Id,
                t.Title,
                t.Tags,
                t.CreatedAt,
                t.CreatorId,
                CreatorName = _db.AuthUsers.Where(u => u.Id == t.CreatorId).Select(u => u.DisplayName).FirstOrDefault()
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var t = await _db.Topics.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        var creator = await _db.AuthUsers.Where(u => u.Id == t.CreatorId).Select(u => u.DisplayName).FirstOrDefaultAsync();
        return Ok(new { t.Id, t.Title, t.Tags, t.CreatedAt, t.CreatorId, CreatorName = creator });
    }

    public record CreateTopicDto(string Title, string? Tags);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTopicDto dto)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required");
        var topic = new Topic { Title = dto.Title.Trim(), Tags = dto.Tags?.Trim() ?? string.Empty, CreatorId = userId.Value, CreatedAt = DateTime.UtcNow };
        _db.Topics.Add(topic);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = topic.Id }, new { topic.Id, topic.Title, topic.Tags, topic.CreatedAt });
    }

    public record UpdateTopicDto(string Title, string? Tags);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTopicDto dto)
    {
        var userId = CurrentUserId();
        var isAdmin = IsAdmin();
        if (!userId.HasValue && !isAdmin) return Unauthorized();
        var topic = await _db.Topics.FirstOrDefaultAsync(t => t.Id == id);
        if (topic == null) return NotFound();
        if (!(isAdmin || topic.CreatorId == userId)) return Forbid();
        if (!string.IsNullOrWhiteSpace(dto.Title)) topic.Title = dto.Title.Trim();
        topic.Tags = (dto.Tags ?? string.Empty).Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId();
        var isAdmin = IsAdmin();
        if (!userId.HasValue && !isAdmin) return Unauthorized();
        var topic = await _db.Topics.FirstOrDefaultAsync(t => t.Id == id);
        if (topic == null) return NotFound();
        if (!(isAdmin || topic.CreatorId == userId)) return Forbid();
        var msgs = _db.Messages.Where(m => m.TopicId == id);
        _db.Messages.RemoveRange(msgs);
        _db.Topics.Remove(topic);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

