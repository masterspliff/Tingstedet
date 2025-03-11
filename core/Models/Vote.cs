using System;
using System.ComponentModel.DataAnnotations;

namespace core.Models
{
    public class Vote
    {
        [Key]
        public int Id { get; set; }
        
        [MaxLength(50)]
        public string Username { get; set; } = "";
        
        public int Value { get; set; } // 1 for upvote, -1 for downvote
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Relations with either Post or Comment (one will be null)
        public int? PostId { get; set; }
        public Post Post { get; set; }
        
        public int? CommentId { get; set; }
        public Comment Comment { get; set; }
    }
}
