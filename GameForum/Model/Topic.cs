using System.ComponentModel.DataAnnotations;

namespace GameForum.Model;

public class Topic
{
    public int Id { get; set; }
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;
    [StringLength(200)]
    public string Tags { get; set; } = string.Empty; // РЅР°РїСЂРёРјРµСЂ: #Р±Р°Рі #РёРіСЂР°
    public int CreatorId { get; set; }
    public AuthUser? Creator { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

