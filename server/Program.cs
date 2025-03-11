using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using core.Models;
using server.Data;
using server.Service;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
var allowSomeStuff = "_AllowSomeStuff";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowSomeStuff, builder =>
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
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient for Claude API
builder.Services.AddHttpClient();

// Register Services
builder.Services.AddScoped<DataService>();
builder.Services.AddScoped<ClaudeService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors(allowSomeStuff);


// IF NEEDED AND NOT USING CLAUDE AI **********************************************************************************************

// Seed data
//using (var scope = app.Services.CreateScope())
//{
//    var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
//    dataService.SeedData(); // Fills database with data if empty, otherwise does nothing
//}

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

// Generate content using Claude API
app.MapPost("/api/generate-content", async ([FromHeader(Name = "x-claude-api-key")] string apiKey, ClaudeService claudeService, IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.BadRequest("API key is required in the x-claude-api-key header");
    }
    
    var apiRequest = new ClaudeService.ClaudeApiRequest { ApiKey = apiKey };
    var httpClient = httpClientFactory.CreateClient();
    return await claudeService.GenerateContentWithClaudeAsync(httpClient, apiRequest);
})
.WithName("GenerateContent")
.WithOpenApi();

// Delete all content
app.MapDelete("/api/delete-all-content", (DataService dataService) =>
{
    return dataService.DeleteAllContent();
});

app.MapGet("/", () => "Hello World!");

app.Run();
