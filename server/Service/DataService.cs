using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using core.Models;
using server.Data;

namespace server.Service;

public class DataService
{
    private AppDbContext db { get; }

    public DataService(AppDbContext db)
    {
        this.db = db;
    }
    
    /// <summary>
    /// Seeds new data in the database if necessary.
    /// </summary>
    public void SeedData()
    {
        if (!db.Posts.Any())
        {
            // Create sample posts with comments
            var post1 = new Post
            {
                Title = "Welcome to Tingstedet!",
                Content = "This is a place for discussions and sharing ideas.",
                Author = "Admin",
                CreatedAt = DateTime.UtcNow,
                Votes = 5
            };
            
            var post2 = new Post
            {
                Title = "How to use this forum",
                Content = "You can post, comment, and vote on content you find interesting.",
                Author = "Moderator",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Votes = 3
            };
            
            var post3 = new Post
            {
                Title = "Introducing new features",
                Content = "We've added voting and commenting features to make discussions more interactive.",
                Author = "Developer",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Votes = 7,
                Url = "https://example.com/features"
            };
            
            // Add comments to posts
            post1.CommentsList.Add(new Comment
            {
                Content = "Great to be here!",
                Author = "User1",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                Votes = 2
            });
            
            post2.CommentsList.Add(new Comment
            {
                Content = "Thanks for the explanation!",
                Author = "User2",
                CreatedAt = DateTime.UtcNow.AddMinutes(-45),
                Votes = 1
            });
            
            post3.CommentsList.Add(new Comment
            {
                Content = "Looking forward to trying these new features!",
                Author = "User3",
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                Votes = 4
            });
            
            // Add posts to database
            db.Posts.Add(post1);
            db.Posts.Add(post2);
            db.Posts.Add(post3);
            
            db.SaveChanges();
        }
    }
    
    public object UpvotePost(int id)
    {
        var post = db.Posts.FirstOrDefault(p => p.Id == id);
        if (post != null)
        {
            post.Votes++;
            db.SaveChanges();
            return Results.Ok(new { post.Votes });
        }
        return Results.NotFound(new { message = "Post not found" });
    }
    
    public object DownvotePost(int id)
    {
        var post = db.Posts.FirstOrDefault(p => p.Id == id);
        if (post != null)
        {
            post.Votes--;
            db.SaveChanges();
            return Results.Ok(new { post.Votes });
        }
        return Results.NotFound(new { message = "Post not found" });
    }
    
    public object UpvoteComment(int postId, int commentId)
    {
        System.Console.WriteLine($"Upvoting comment {commentId} on post {postId}");
        
        var comment = db.Comments
            .FirstOrDefault(c => c.Id == commentId && c.PostId == postId);
            
        if (comment != null)
        {
            comment.Votes++;
            db.SaveChanges();
            return Results.Ok(new { comment.Votes });
        }
        return Results.NotFound(new { message = "Comment not found" });
    }
    
    public object DownvoteComment(int postId, int commentId)
    {
        var comment = db.Comments
            .FirstOrDefault(c => c.Id == commentId && c.PostId == postId);
            
        if (comment != null)
        {
            comment.Votes--;
            db.SaveChanges();
            return Results.Ok(new { comment.Votes });
        }
        return Results.NotFound(new { message = "Comment not found" });
    }
    
    public Post AddPost(Post post)
    {
        post.CreatedAt = DateTime.UtcNow;
        db.Posts.Add(post);
        db.SaveChanges();
        return post;
    }
    
    public object AddComment(int id, Comment comment)
    {
        var post = db.Posts.FirstOrDefault(p => p.Id == id);
        if (post != null)
        {
            comment.PostId = id;
            comment.CreatedAt = DateTime.UtcNow;
            db.Comments.Add(comment);
            db.SaveChanges();
            return Results.Ok(comment);
        }
        return Results.NotFound(new { message = "Post not found" });
    }

    public List<Post> GetPosts()
    {
        return db.Posts
            .OrderByDescending(p => p.CreatedAt)
            .Include(p => p.CommentsList)
            .ToList();
    }

    public Post? GetPostById(int id)
    {
        return db.Posts
            .Include(p => p.CommentsList)
            .FirstOrDefault(p => p.Id == id);
    }
}