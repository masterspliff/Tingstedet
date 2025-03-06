using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using core.Models;
using server.Data;
using server.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm", policy =>
    {
        policy.WithOrigins("https://localhost:7149", "http://localhost:5094")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JSON serialization
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazorWasm");

// API Endpoints

// GET: Get 50 most recent posts
app.MapGet("/api/posts", async (AppDbContext db) =>
{
    var posts = await db.Posts
        .OrderByDescending(p => p.CreatedAt)
        .Take(50)
        .Include(p => p.CommentsList)
        .ToListAsync();
        
    // Calculate time ago and comments count
    foreach (var post in posts)
    {
        post.TimeAgo = TimeHelper.GetTimeAgo(post.CreatedAt);
    }
    
    return Results.Ok(posts);
})
.WithName("GetPosts")
.WithOpenApi();

// GET: Get post by ID with comments
app.MapGet("/api/posts/{id}", async (int id, AppDbContext db) =>
{
    var post = await db.Posts
        .Include(p => p.CommentsList)
        .FirstOrDefaultAsync(p => p.Id == id);
        
    if (post == null)
        return Results.NotFound();
        
    // Calculate time ago
    post.TimeAgo = TimeHelper.GetTimeAgo(post.CreatedAt);
    
    // Calculate time ago for comments
    foreach (var comment in post.CommentsList)
    {
        comment.TimeAgo = TimeHelper.GetTimeAgo(comment.CreatedAt);
    }
    
    return Results.Ok(post);
})
.WithName("GetPostById")
.WithOpenApi();

// POST: Create a new post
app.MapPost("/api/posts", async (PostCreateDto postDto, AppDbContext db) =>
{
    var post = new Post
    {
        Title = postDto.Title,
        Content = postDto.Content ?? string.Empty,
        Url = postDto.Url,
        Author = postDto.Author,
        CreatedAt = DateTime.UtcNow,
        Votes = 0
    };
    
    db.Posts.Add(post);
    await db.SaveChangesAsync();
    
    post.TimeAgo = TimeHelper.GetTimeAgo(post.CreatedAt);
    
    return Results.Created($"/api/posts/{post.Id}", post);
})
.WithName("CreatePost")
.WithOpenApi();

// POST: Add a comment to a post
app.MapPost("/api/posts/{postId}/comments", async (int postId, CommentCreateDto commentDto, AppDbContext db) =>
{
    var post = await db.Posts.FindAsync(postId);
    
    if (post == null)
        return Results.NotFound();
        
    var comment = new Comment
    {
        Content = commentDto.Content,
        Author = commentDto.Author,
        CreatedAt = DateTime.UtcNow,
        PostId = postId,
        Votes = 0
    };
    
    db.Comments.Add(comment);
    await db.SaveChangesAsync();
    
    comment.TimeAgo = TimeHelper.GetTimeAgo(comment.CreatedAt);
    
    return Results.Created($"/api/comments/{comment.Id}", comment);
})
.WithName("AddComment")
.WithOpenApi();

// POST: Vote on a post
app.MapPost("/api/posts/{postId}/vote", async (int postId, VoteDto voteDto, AppDbContext db) =>
{
    var post = await db.Posts.FindAsync(postId);
    
    if (post == null)
        return Results.NotFound();
        
    // Check if user already voted on this post
    var existingVote = await db.Votes
        .FirstOrDefaultAsync(v => v.PostId == postId && v.Username == voteDto.Username);
        
    if (existingVote != null)
    {
        // Update existing vote
        if (existingVote.Value != voteDto.Value)
        {
            // Change vote direction (remove old vote, add new vote)
            post.Votes = post.Votes - existingVote.Value + voteDto.Value;
            existingVote.Value = voteDto.Value;
        }
        else
        {
            // Remove vote (user clicked same direction again)
            post.Votes -= existingVote.Value;
            db.Votes.Remove(existingVote);
        }
    }
    else
    {
        // Add new vote
        post.Votes += voteDto.Value;
        
        var vote = new Vote
        {
            Username = voteDto.Username,
            Value = voteDto.Value,
            PostId = postId,
            CreatedAt = DateTime.UtcNow
        };
        
        db.Votes.Add(vote);
    }
    
    await db.SaveChangesAsync();
    
    return Results.Ok(new { post.Votes });
})
.WithName("VotePost")
.WithOpenApi();

// POST: Vote on a comment
app.MapPost("/api/comments/{commentId}/vote", async (int commentId, VoteDto voteDto, AppDbContext db) =>
{
    var comment = await db.Comments.FindAsync(commentId);
    
    if (comment == null)
        return Results.NotFound();
        
    // Check if user already voted on this comment
    var existingVote = await db.Votes
        .FirstOrDefaultAsync(v => v.CommentId == commentId && v.Username == voteDto.Username);
        
    if (existingVote != null)
    {
        // Update existing vote
        if (existingVote.Value != voteDto.Value)
        {
            // Change vote direction (remove old vote, add new vote)
            comment.Votes = comment.Votes - existingVote.Value + voteDto.Value;
            existingVote.Value = voteDto.Value;
        }
        else
        {
            // Remove vote (user clicked same direction again)
            comment.Votes -= existingVote.Value;
            db.Votes.Remove(existingVote);
        }
    }
    else
    {
        // Add new vote
        comment.Votes += voteDto.Value;
        
        var vote = new Vote
        {
            Username = voteDto.Username,
            Value = voteDto.Value,
            CommentId = commentId,
            CreatedAt = DateTime.UtcNow
        };
        
        db.Votes.Add(vote);
    }
    
    await db.SaveChangesAsync();
    
    return Results.Ok(new { comment.Votes });
})
.WithName("VoteComment")
.WithOpenApi();

app.Run();