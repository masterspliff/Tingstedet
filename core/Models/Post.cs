using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace core.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "";
        
        public string Content { get; set; } = "";
        
        [Required]
        [MaxLength(50)]
        public string Author { get; set; } = "";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [NotMapped]
        public string TimeAgo { get; set; } = "";
        
        public int Votes { get; set; }
        
        [NotMapped]
        public int Comments => CommentsList?.Count ?? 0;
        
        public string? Url { get; set; }
        
        [NotMapped]
        public int UserVote { get; set; } // 1 for upvote, -1 for downvote, 0 for no vote
        
        public List<Comment> CommentsList { get; set; } = new List<Comment>();
        
        public List<Vote> Votes_Relations { get; set; } = new List<Vote>();
    }

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
        
        [NotMapped]
        public int UserVote { get; set; } // 1 for upvote, -1 for downvote, 0 for no vote
        
        [NotMapped]
        public List<Comment> Replies { get; set; } = new List<Comment>();
        
        public List<Vote> Votes_Relations { get; set; } = new List<Vote>();
    }
    
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
