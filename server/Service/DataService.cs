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
                Title = "Just moved to the neighborhood, any recommendations?",
                Content = "Hi everyone! I just moved to the area and I'm looking for recommendations on local restaurants, parks, and community events. What are your favorite spots?",
                Author = "NewNeighbor",
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                Votes = 42
            };
            
            var post2 = new Post
            {
                Title = "Beautiful sunset at the local park yesterday",
                Content = "Caught this amazing view while walking my dog. Thought I'd share with the community!",
                Author = "NatureLover",
                CreatedAt = DateTime.UtcNow.AddHours(-8),
                Votes = 128,
                Url = "https://images.unsplash.com/photo-1495616811223-4d98c6e9c869"
            };
            
            var post3 = new Post
            {
                Title = "Community cleanup this weekend - volunteers needed!",
                Content = "We're organizing a community cleanup this Saturday from 10am to 2pm. Meet at the central square. Gloves and bags will be provided. Hope to see many of you there!",
                Author = "CommunityOrganizer",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Votes = 89
            };
            
            // Add comments to posts
            post1.CommentsList.Add(new Comment
            {
                Content = "You should definitely try 'The Green Table' on Oak Street. They have amazing farm-to-table options and great outdoor seating!",
                Author = "LocalFoodie",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Votes = 15
            });
            
            post1.CommentsList.Add(new Comment
            {
                Content = "Riverside Park is beautiful this time of year. They have walking trails, picnic areas, and a dog park if you have a furry friend!",
                Author = "ParkRanger",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Votes = 8
            });
            
            post2.CommentsList.Add(new Comment
            {
                Content = "Wow, that's absolutely stunning! Where exactly in the park was this taken?",
                Author = "PhotoEnthusiast",
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                Votes = 12
            });
            
            post3.CommentsList.Add(new Comment
            {
                Content = "Count me in! I'll bring some extra trash bags just in case we need them.",
                Author = "EcoWarrior",
                CreatedAt = DateTime.UtcNow.AddHours(-12),
                Votes = 18
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
    
    
    public object DeleteAllContent()
    {
        // Remove all votes, comments, and posts from the database
        db.Votes.RemoveRange(db.Votes);
        db.Comments.RemoveRange(db.Comments);
        db.Posts.RemoveRange(db.Posts);
        db.SaveChanges();
        
        return Results.Ok(new { message = "All content deleted successfully" });
    }
}
