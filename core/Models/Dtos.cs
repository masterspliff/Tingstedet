using System.ComponentModel.DataAnnotations;

namespace core.Models;

public class PostCreateDto
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;
    
    public string? Content { get; set; }
    
    public string? Url { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Author { get; set; } = string.Empty;
}

public class CommentCreateDto
{
    [Required]
    public string Content { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Author { get; set; } = string.Empty;
}

public class VoteDto
{
    [Required]
    public int Value { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;
}