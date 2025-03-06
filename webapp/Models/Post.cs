namespace webapp.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Author { get; set; } = "";
        public string TimeAgo { get; set; } = "";
        public int Votes { get; set; }
        public int Comments { get; set; }
        public string? ImageUrl { get; set; }
        public int UserVote { get; set; } // 1 for upvote, -1 for downvote, 0 for no vote
    }
}
