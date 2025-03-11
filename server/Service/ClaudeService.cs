using System.Text.Json;
using core.Models;
using server.Data;

namespace server.Service;

public class ClaudeService
{
    private readonly AppDbContext _db;

    public ClaudeService(AppDbContext db)
    {
        _db = db;
    }

    public class ClaudeApiRequest
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    public async Task<object> GenerateContentWithClaudeAsync(HttpClient httpClient, ClaudeApiRequest apiRequest)
    {
        try
        {
            // We no longer delete existing content
            // This allows users to keep their existing posts and comments
            Console.WriteLine("Generating new content without deleting existing data");

            // Prepare the API request to Claude
            var requestUri = "https://api.anthropic.com/v1/messages";

            // Create a request payload for Claude
            var requestPayload = new
            {
                model = "claude-3-opus-20240229",
                max_tokens = 4000,
                system = "You are helping generate COMPLETELY RANDOM and VARIED content for a community forum. " +
                         "Generate 3 posts with titles, content, author names, and 1-2 comments for each post. " +
                         "Make sure the topics are UNIQUE, VARIED and NOT about 'neighborhood recommendations', 'sunset photos', or 'community cleanups'. " +
                         "Instead, create content about entirely different topics like hobbies, technology questions, gardening tips, book discussions, local sports, etc. " +
                         "IMPORTANT: Your response MUST be VALID JSON with the following structure and nothing else: " +
                         "{ \"posts\": [ { \"title\": \"...\", \"content\": \"...\", \"author\": \"...\"," +
                         " \"comments\": [ { \"content\": \"...\", \"author\": \"...\" } ] } ] }" +
                         "Do not include any explanations, markdown formatting, or anything else outside the JSON structure.",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content =
                            "Generate COMPLETELY RANDOM community forum posts with UNIQUE topics. Avoid repeating common themes like 'neighborhood recommendations', 'sunset photos', or 'community cleanups'. Create content about entirely different topics instead."
                    }
                }
            };

            // Set up the API request
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
            // The current API uses different headers
            httpRequest.Headers.Add("x-api-key", apiRequest.ApiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            // Add additional header that may be required by newer Claude API
            httpRequest.Headers.Add("accept", "application/json");
            httpRequest.Content = JsonContent.Create(requestPayload);

            // Log the request for debugging
            Console.WriteLine($"Sending Claude API request with API key: {apiRequest.ApiKey.Substring(0, 5)}...");
            Console.WriteLine($"Request payload: {JsonSerializer.Serialize(requestPayload)}");
            Console.WriteLine($"API URI: {requestUri}");

            // Send the request to Claude API
            var response = await httpClient.SendAsync(httpRequest);

            // Check if the request failed and log the error
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Claude API Error - Status Code: {response.StatusCode}");
                Console.WriteLine($"Error Response: {errorContent}");

                // Instead of failing, let's provide fallback content
            }

            response.EnsureSuccessStatusCode();

            // Parse the response
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Claude API Response received, length: {responseContent.Length}");

            var responseData = System.Text.Json.JsonDocument.Parse(responseContent);

            // Extract the content from Claude's response
            var content = responseData.RootElement.GetProperty("content").EnumerateArray().First().GetProperty("text")
                .GetString();
            Console.WriteLine($"Extracted content text: {content}");

            // Parse the generated JSON from Claude using regex to be more robust
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(content!, @"\{[\s\S]*\}");

            if (!jsonMatch.Success)
            {
                Console.WriteLine("Could not find JSON in Claude's response using regex");
                return Results.BadRequest(new { message = "Failed to extract JSON from Claude's response" });
            }

            var jsonContent = jsonMatch.Value;
            Console.WriteLine($"Extracted JSON using regex, length: {jsonContent.Length}");

            // Use more flexible JSON options
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            try
            {
                var generatedData = System.Text.Json.JsonSerializer.Deserialize<GeneratedContent>(jsonContent, options);

                if (generatedData == null || generatedData.Posts == null || !generatedData.Posts.Any())
                {
                    Console.WriteLine("JSON deserialized but no posts were found");
                    return Results.BadRequest(new { message = "No posts were found in the generated content" });
                }

                Console.WriteLine($"Successfully deserialized {generatedData.Posts.Count} posts");
                foreach (var post in generatedData.Posts)
                {
                    Console.WriteLine($"Post: {post.Title}, Comments: {post.Comments?.Count ?? 0}");
                }

                // Create and save posts to the database
                foreach (var postData in generatedData.Posts!)
                {
                    if (string.IsNullOrEmpty(postData.Title) ||
                        string.IsNullOrEmpty(postData.Content) ||
                        string.IsNullOrEmpty(postData.Author))
                    {
                        Console.WriteLine($"Skipping post with missing data: {postData.Title}");
                        continue;
                    }

                    var post = new Post
                    {
                        Title = postData.Title,
                        Content = postData.Content,
                        Author = postData.Author,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-new Random().Next(1, 180)),
                        Votes = new Random().Next(1, 50)
                    };

                    // Add comments to the post
                    if (postData.Comments != null)
                    {
                        foreach (var commentData in postData.Comments)
                        {
                            if (string.IsNullOrEmpty(commentData.Content) ||
                                string.IsNullOrEmpty(commentData.Author))
                            {
                                Console.WriteLine("Skipping comment with missing data");
                                continue;
                            }

                            post.CommentsList.Add(new Comment
                            {
                                Content = commentData.Content,
                                Author = commentData.Author,
                                CreatedAt = DateTime.UtcNow.AddMinutes(-new Random().Next(1, 60)),
                                Votes = new Random().Next(0, 15)
                            });
                        }
                    }

                    // Add post to database
                    _db.Posts.Add(post);
                    Console.WriteLine($"Added post to database: {post.Title}");
                }

                // Make sure to save changes to the database
                var saveResult = _db.SaveChanges();
                Console.WriteLine($"Saved {saveResult} changes to database");

                // Verify posts were actually saved
                var postCount = _db.Posts.Count();
                Console.WriteLine($"Database now contains {postCount} posts");

                return Results.Ok(new
                {
                    message = "Content generated successfully using Claude API",
                    postCount = postCount
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = $"Error generating content: {ex.Message}" });
            }
        }
        finally
        {
            // Ensure we've saved any pending changes
            if (_db.ChangeTracker.HasChanges())
            {
                Console.WriteLine("Saving any pending changes in finally block");
                _db.SaveChanges();
            }
        }
    }

    // Helper classes for deserializing Claude's generated content
    private class GeneratedContent
    {
        public List<GeneratedPost>? Posts { get; set; }
    }
    
    private class GeneratedPost
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Author { get; set; }
        public List<GeneratedComment>? Comments { get; set; }
    }
    
    private class GeneratedComment
    {
        public string? Content { get; set; }
        public string? Author { get; set; }
    }
}
