using System.ComponentModel.DataAnnotations;

namespace GameForum.Model;

public class Message
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public Topic? Topic { get; set; }
    public int AuthorId { get; set; }
    public AuthUser? Author { get; set; }
    [Required, StringLength(2000)]
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

