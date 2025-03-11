using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace core.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Author { get; set; } = "";
        
        [Required]
        public string Content { get; set; } = "";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [NotMapped]
        public string TimeAgo { get; set; } = "";
        
        public int Votes { get; set; }
    
        [ForeignKey("PostId")]
        public int PostId { get; set; }
    
        public Post Post { get; set; }
    
        public int? ParentCommentId { get; set; }
    
        [ForeignKey("ParentCommentId")]
        public Comment? ParentComment { get; set; }
    
        [NotMapped]
        public int UserVote { get; set; } // 1 for upvote, -1 for downvote, 0 for no vote
    
        [NotMapped]
        public List<Comment> Replies { get; set; } = new List<Comment>();
    
        public List<Vote> Votes_Relations { get; set; } = new List<Vote>();
    }
}
