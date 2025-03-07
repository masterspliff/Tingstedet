using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using core.Models;
using server.Data;
using server.Utils;
using server.Service;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
var AllowSomeStuff = "_AllowSomeStuff";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: AllowSomeStuff, builder =>
    {
        builder.AllowAnyOrigin()
             .AllowAnyHeader()
             .AllowAnyMethod();
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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register DataService
builder.Services.AddScoped<DataService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors(AllowSomeStuff);

// Seed data
using (var scope = app.Services.CreateScope())
{
    var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
    dataService.SeedData(); // Fills database with data if empty, otherwise does nothing
}

// Middleware that runs before each request. Sets ContentType for all responses to "JSON".
app.Use(async (context, next) =>
{
    context.Response.ContentType = "application/json; charset=utf-8";
    await next(context);
});

// API Endpoints

//Get
app.MapGet("/api/posts", (DataService dataService) =>
{
    var posts = dataService.GetPosts();
    return Results.Ok(posts);
});

app.MapGet("/api/posts/{id}", (int id, DataService dataService) =>
{
    var posts = dataService.GetPostById(id);
    return Results.Ok(posts);
});

// /api/posts/{id}/upvote
// This adds an upvote to a specific post
app.MapPut("/api/posts/{id}/upvote", (int id, DataService dataService) =>
{
    return dataService.UpvotePost(id);
});

// /api/posts/{id}/downvote
// This adds a downvote to a specific post
app.MapPut("/api/posts/{id}/downvote", (int id, DataService dataService) =>
{
    return dataService.DownvotePost(id);
});

// /api/posts/{postid}/comments/{commentid}/upvote
// This adds an upvote to a specific comment
app.MapPut("/api/posts/{postid}/comments/{commentid}/upvote", (int postid, int commentid, DataService dataService) =>
{
    System.Console.WriteLine("jeg rammer api kald");
    return dataService.UpvoteComment(postid, commentid);
});

// /api/posts/{postid}/comments/{commentid}/downvote
// This adds a downvote to a specific comment
app.MapPut("/api/posts/{postid}/comments/{commentid}/downvote", (int postid, int commentid, DataService dataService) =>
{
    return dataService.DownvoteComment(postid, commentid);
});

// POST:
// /api/posts
// This adds a new post
app.MapPost("/api/posts", (Post post, DataService dataService) =>
{
    dataService.AddPost(post);
    return Results.Ok(post);
});

// /api/posts/{id}/comments
// This adds a new comment to a specific post
app.MapPost("/api/posts/{id}/comments", (int id, Comment comment, DataService dataService) =>
{
    dataService.AddComment(id, comment);
    return Results.Ok(comment);
});

app.MapGet("/", () => "Hello World!");

app.Run();
