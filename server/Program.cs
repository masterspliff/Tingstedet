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
             .AllowAnyMethod()
             .WithExposedHeaders("Content-Disposition"); // Needed for file downloads
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

// Get connection string from configuration and replace environment variables
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
connectionString = ReplaceEnvironmentVariables(connectionString);

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Helper method to replace environment variables in connection string
string ReplaceEnvironmentVariables(string input)
{
    if (string.IsNullOrEmpty(input)) return input;
    
    // Replace ${VAR_NAME} with environment variable value
    var result = System.Text.RegularExpressions.Regex.Replace(input, @"\${([^}]+)}", match =>
    {
        var envVarName = match.Groups[1].Value;
        var envVarValue = Environment.GetEnvironmentVariable(envVarName);
        return envVarValue ?? match.Value; // Return original if env var not found
    });
    
    return result;
}

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

// Ensure CORS headers are applied even in production
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
    context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
    context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
    
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
    }
    else
    {
        await next();
    }
});


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

// /api/posts/{postId}/comments/{commentId}/replies
// This adds a reply to a specific comment
app.MapPost("/api/posts/{postId}/comments/{commentId}/replies", (int postId, int commentId, Comment reply, DataService dataService) =>
{
    return dataService.AddReply(postId, commentId, reply);
});

// Generate content using Claude API
app.MapPost("/api/generate-content", async (ClaudeService claudeService, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    return await claudeService.GenerateContentWithClaudeAsync(httpClient);
})
.WithName("GenerateContent")
.WithOpenApi();

// Generate content with user-provided API key
app.MapPost("/api/generate-content-with-key", async ([FromBody] ClaudeService.ClaudeApiRequest request, ClaudeService claudeService, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    return await claudeService.GenerateContentWithClaudeAsync(httpClient, request.ApiKey);
})
.WithName("GenerateContentWithKey")
.WithOpenApi();

// Delete all content
app.MapDelete("/api/delete-all-content", (DataService dataService) =>
{
    return dataService.DeleteAllContent();
});


app.Run();
