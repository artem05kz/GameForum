using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;
using GameForum.Model;

namespace GameForum.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public MessagesApiController(AppDbContext db) => _db = db;

    private int? CurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : null;
    }
    private bool IsAdmin() => string.Equals(User.FindFirst("isAdmin")?.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> GetByTopic([FromQuery] int topicId)
    {
        var msgs = await _db.Messages
            .Where(m => m.TopicId == topicId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new {
                m.Id,
                m.TopicId,
                m.AuthorId,
                AuthorName = _db.AuthUsers.Where(u => u.Id == m.AuthorId).Select(u => u.DisplayName).FirstOrDefault(),
                m.Content,
                m.CreatedAt,
                m.UpdatedAt
            })
            .ToListAsync();
        return Ok(msgs);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var m = await _db.Messages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();
        var author = await _db.AuthUsers.Where(u => u.Id == m.AuthorId).Select(u => u.DisplayName).FirstOrDefaultAsync();
        return Ok(new { m.Id, m.TopicId, m.AuthorId, AuthorName = author, m.Content, m.CreatedAt, m.UpdatedAt });
    }

    public record CreateMessageDto(int TopicId, string Content);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMessageDto dto)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        if (dto.TopicId <= 0 || string.IsNullOrWhiteSpace(dto.Content)) return BadRequest("TopicId and Content are required");
        var exists = await _db.Topics.AnyAsync(t => t.Id == dto.TopicId);
        if (!exists) return NotFound("Topic not found");
        var m = new Message { TopicId = dto.TopicId, AuthorId = userId.Value, Content = dto.Content.Trim(), CreatedAt = DateTime.UtcNow };
        _db.Messages.Add(m);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = m.Id }, new { m.Id, m.TopicId, m.Content });
    }

    public record UpdateMessageDto(string Content);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMessageDto dto)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        var m = await _db.Messages.FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();
        if (m.AuthorId != userId.Value) return Forbid();
        if (!string.IsNullOrWhiteSpace(dto.Content)) m.Content = dto.Content.Trim();
        m.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId();
        var isAdmin = IsAdmin();
        if (!userId.HasValue && !isAdmin) return Unauthorized();
        var m = await _db.Messages.FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();
        if (!(isAdmin || m.AuthorId == userId)) return Forbid();
        _db.Messages.Remove(m);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

