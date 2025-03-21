using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using core.Models;

namespace webapp.Services
{
    public class ContentGenerationResult
    {
        public bool Success { get; set; }
        public bool NeedsApiKey { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
    
    public interface IPostService
    {
        Task<List<Post>> GetPostsAsync();
        Task<Post> GetPostByIdAsync(int id);
        Task<bool> LoadImageAsync(string imageUrl);
        Task<Post> UpvotePostAsync(int postId, string username);
        Task<Post> DownvotePostAsync(int postId, string username);
        Task<Comment> UpvoteCommentAsync(int postId, int commentId, string username);
        Task<Comment> DownvoteCommentAsync(int postId, int commentId, string username);
        Task<Comment> AddCommentAsync(int postId, string commentText, string author);
        Task<Comment> AddReplyAsync(int postId, int commentId, string replyText, string author);
        Task<Post> CreatePostAsync(Post post);
        Task<ContentGenerationResult> GenerateContentAsync(string apiKey = "");
        Task<bool> DeleteAllContentAsync();
        
        // Offline mode compatibility
        void UpvotePost(Post post);
        void DownvotePost(Post post);
        void UpvoteComment(Comment comment);
        void DownvoteComment(Comment comment);
        void AddComment(Post post, string commentText);
        void AddReply(Comment parentComment, string replyText);
    }

    public class PostService : IPostService
    {
        private readonly HttpClient _httpClient;
        private readonly List<Post> _cachedPosts;
        private int _nextCommentId = 1000;
        private readonly string _currentUser = "CurrentUser";
        
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public PostService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Don't override the BaseAddress - it's already set in Program.cs
            _cachedPosts = new List<Post>();
            
            Console.WriteLine($"Using API base address: {_httpClient.BaseAddress}");
        }

        public async Task<List<Post>> GetPostsAsync()
        {
            try
            {
                // Using the new minimal API endpoint for getting posts
                var response = await _httpClient.GetAsync("/api/posts");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Response: {content}");
                    
                    var posts = JsonSerializer.Deserialize<List<Post>>(content, _jsonOptions);
                    if (posts != null)
                    {
                        _cachedPosts.Clear();
                        _cachedPosts.AddRange(posts);
                        return posts;
                    }
                }
                else
                {
                    Console.Error.WriteLine($"API returned status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching posts: {ex.Message}");
                // If API call fails, return cached posts only
                return _cachedPosts;
            }
            
            return new List<Post>();
        }

        public async Task<Post> GetPostByIdAsync(int id)
        {
            try
            {
                // Using the new minimal API endpoint for getting a post by ID
                var response = await _httpClient.GetAsync($"/api/posts/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Response for post {id}: {content}");
                    
                    var post = JsonSerializer.Deserialize<Post>(content, _jsonOptions);
                    return post ?? new Post();
                }
                else
                {
                    Console.Error.WriteLine($"API returned status code: {response.StatusCode} for post {id}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching post {id}: {ex.Message}");
            }
            
            // If API call fails, try to find in cached posts
            var cachedPost = _cachedPosts.FirstOrDefault(p => p.Id == id);
            if (cachedPost != null)
            {
                return cachedPost;
            }
            
            // If not in cache, return fallback
            return new Post { Title = "Post not found", Author = "Unknown", TimeAgo = "unknown" };
        }
        
        public async Task<bool> LoadImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return false;
                
            // In a real app, we would preload the image here
            await Task.Delay(1000); // Minimal delay to simulate async operation
            return true;
        }
        
        public async Task<Post> CreatePostAsync(Post post)
        {
            try
            {
                // Using the new minimal API endpoint for creating posts
                // Send the full Post object directly
                var response = await _httpClient.PostAsJsonAsync("/api/posts", post);
                response.EnsureSuccessStatusCode();
                
                var createdPost = await response.Content.ReadFromJsonAsync<Post>(_jsonOptions);
                if (createdPost != null)
                {
                    // Calculate the time ago or use the value from the server
                    createdPost.TimeAgo = string.IsNullOrEmpty(createdPost.TimeAgo) ? "just now" : createdPost.TimeAgo;
                    
                    // Set UserVote to 1 (auto-upvote)
                    createdPost.UserVote = 1;
                    
                    // Add to cache
                    _cachedPosts.Add(createdPost);
                    return createdPost;
                }
                
                return post;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating post: {ex.Message}");
                
                // Fallback for offline mode - assign a temporary ID
                post.Id = _cachedPosts.Count > 0 ? _cachedPosts.Max(p => p.Id) + 1 : 1;
                _cachedPosts.Add(post);
                return post;
            }
        }
        
        public async Task<Post> UpvotePostAsync(int postId, string username)
        {
            try
            {
                string url = $"/api/posts/{postId}/upvote";
                
                // Post JSON to API, save the HttpResponseMessage
                HttpResponseMessage msg = await _httpClient.PutAsJsonAsync(url, "");
                
                // Get the JSON string from the response
                string json = await msg.Content.ReadAsStringAsync();
                
                // Deserialize the JSON string to a Post object
                Post? updatedPost = JsonSerializer.Deserialize<Post>(json, _jsonOptions);
                
                // Update the post in the cache
                if (updatedPost != null)
                {
                    var cachedPost = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                    if (cachedPost != null)
                    {
                        cachedPost.Votes = updatedPost.Votes;
                        cachedPost.UserVote = 1;
                    }
                    
                    return updatedPost;
                }
            }
            catch (Exception)
            {
                // Fallback to client-side voting
                var post = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                if (post != null)
                {
                    UpvotePost(post);
                    return post;
                }
            }
            
            // If we get here, try to return the post from cache
            return _cachedPosts.FirstOrDefault(p => p.Id == postId) ?? new Post();
        }
        
        public async Task<Post> DownvotePostAsync(int postId, string username)
        {
            try
            {
                string url = $"/api/posts/{postId}/downvote";
                
                // Post JSON to API, save the HttpResponseMessage
                HttpResponseMessage msg = await _httpClient.PutAsJsonAsync(url, "");
                
                // Get the JSON string from the response
                string json = await msg.Content.ReadAsStringAsync();
                
                // Deserialize the JSON string to a Post object
                Post? updatedPost = JsonSerializer.Deserialize<Post>(json, _jsonOptions);
                
                // Update the post in the cache
                if (updatedPost != null)
                {
                    var cachedPost = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                    if (cachedPost != null)
                    {
                        cachedPost.Votes = updatedPost.Votes;
                        cachedPost.UserVote = -1;
                    }
                    
                    return updatedPost;
                }
            }
            catch (Exception)
            {
                // Fallback to client-side voting
                var post = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                if (post != null)
                {
                    DownvotePost(post);
                    return post;
                }
            }
            
            // If we get here, try to return the post from cache
            return _cachedPosts.FirstOrDefault(p => p.Id == postId) ?? new Post();
        }

        public async Task<Comment> UpvoteCommentAsync(int postId, int commentId, string username)
        {
            try
            {
                string url = $"/api/posts/{postId}/comments/{commentId}/upvote";
                
                // Post JSON to API, save the HttpResponseMessage
                HttpResponseMessage msg = await _httpClient.PutAsJsonAsync(url, "");
                
                // Get the JSON string from the response
                string json = await msg.Content.ReadAsStringAsync();
                
                // Deserialize the JSON string to a Comment object
                Comment? updatedComment = JsonSerializer.Deserialize<Comment>(json, _jsonOptions);
                
                // Update the comment in the cache
                if (updatedComment != null)
                {
                    var post = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                    var comment = post?.CommentsList.FirstOrDefault(c => c.Id == commentId);
                    if (comment != null)
                    {
                        comment.Votes = updatedComment.Votes;
                        comment.UserVote = 1;
                    }
                    
                    return updatedComment;
                }
            }
            catch (Exception)
            {
                // Fallback to client-side voting
                var post = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                var comment = post?.CommentsList.FirstOrDefault(c => c.Id == commentId);
                if (comment != null)
                {
                    UpvoteComment(comment);
                    return comment;
                }
            }
            
            // If we get here, try to find the comment in cache
            var cachedPost = _cachedPosts.FirstOrDefault(p => p.Id == postId);
            var cachedComment = cachedPost?.CommentsList.FirstOrDefault(c => c.Id == commentId);
            return cachedComment ?? new Comment();
        }
        
        public async Task<Comment> DownvoteCommentAsync(int postId, int commentId, string username)
        {
            try
            {
                string url = $"/api/posts/{postId}/comments/{commentId}/downvote";
                
                // Post JSON to API, save the HttpResponseMessage
                HttpResponseMessage msg = await _httpClient.PutAsJsonAsync(url, "");
                
                // Get the JSON string from the response
                string json = await msg.Content.ReadAsStringAsync();
                
                // Deserialize the JSON string to a Comment object
                Comment? updatedComment = JsonSerializer.Deserialize<Comment>(json, _jsonOptions);
                
                // Update the comment in the cache
                if (updatedComment != null)
                {
                    var post = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                    var comment = post?.CommentsList.FirstOrDefault(c => c.Id == commentId);
                    if (comment != null)
                    {
                        comment.Votes = updatedComment.Votes;
                        comment.UserVote = -1;
                    }
                    
                    return updatedComment;
                }
            }
            catch (Exception)
            {
                // Fallback to client-side voting
                var post = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                var comment = post?.CommentsList.FirstOrDefault(c => c.Id == commentId);
                if (comment != null)
                {
                    DownvoteComment(comment);
                    return comment;
                }
            }
            
            // If we get here, try to find the comment in cache
            var cachedPost = _cachedPosts.FirstOrDefault(p => p.Id == postId);
            var cachedComment = cachedPost?.CommentsList.FirstOrDefault(c => c.Id == commentId);
            return cachedComment ?? new Comment();
        }
        
        public async Task<Comment> AddCommentAsync(int postId, string commentText, string author)
        {
            try
            {
                // Create a new Comment object with the required properties
                var comment = new Comment
                {
                    Content = commentText,
                    Author = author,
                    CreatedAt = DateTime.UtcNow
                };
                
                Console.WriteLine($"Adding comment to post {postId}: {commentText} by {author}");
                
                // Using the new minimal API endpoint for adding comments to posts
                var response = await _httpClient.PostAsJsonAsync($"/api/posts/{postId}/comments", comment);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Response for adding comment: {responseContent}");
                    
                    var createdComment = JsonSerializer.Deserialize<Comment>(responseContent, _jsonOptions);
                    if (createdComment != null)
                    {
                        // Set TimeAgo if not provided by API
                        if (string.IsNullOrEmpty(createdComment.TimeAgo))
                        {
                            createdComment.TimeAgo = "just now";
                        }
                        
                        // Find the post in the cache and add the comment
                        var post = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                        if (post != null)
                        {
                            post.CommentsList.Add(createdComment);
                            post.Comments = post.CommentsList.Count;
                        }
                        return createdComment;
                    }
                }
                else
                {
                    Console.Error.WriteLine($"API returned status code: {response.StatusCode} for adding comment");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.Error.WriteLine($"Error content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error adding comment: {ex.Message}");
                // Fallback to client-side comment
                var post = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                if (post != null)
                {
                    AddComment(post, commentText);
                    return post.CommentsList.Last();
                }
            }
            
            // If API call failed or post not found, create a placeholder comment
            return new Comment
            {
                Id = _nextCommentId++,
                Content = commentText,
                Author = author,
                TimeAgo = "just now",
                Votes = 1,
                UserVote = 1,
                PostId = postId
            };
        }
        
        public async Task<Comment> AddReplyAsync(int postId, int commentId, string replyText, string author)
        {
            try
            {
                // Create a new Comment object with the required properties
                var reply = new Comment
                {
                    Content = replyText,
                    Author = author,
                    CreatedAt = DateTime.UtcNow
                };
                
                Console.WriteLine($"Adding reply to comment {commentId} on post {postId}: {replyText} by {author}");
                
                // Set the parent comment ID
                reply.ParentCommentId = commentId;
                
                // Call the API endpoint for adding replies
                var response = await _httpClient.PostAsJsonAsync($"/api/posts/{postId}/comments/{commentId}/replies", reply);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Response for adding reply: {responseContent}");
                    
                    var createdReply = JsonSerializer.Deserialize<Comment>(responseContent, _jsonOptions);
                    if (createdReply != null)
                    {
                        // Set TimeAgo if not provided by API
                        if (string.IsNullOrEmpty(createdReply.TimeAgo))
                        {
                            createdReply.TimeAgo = "just now";
                        }
                        
                        // Find the post and comment in the cache and add the reply
                        var post = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                        var comment = post?.CommentsList.FirstOrDefault(c => c.Id == commentId);
                        if (comment != null)
                        {
                            comment.Replies.Add(createdReply);
                        }
                        return createdReply;
                    }
                }
                else
                {
                    Console.Error.WriteLine($"API returned status code: {response.StatusCode} for adding reply");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.Error.WriteLine($"Error content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error adding reply: {ex.Message}");
                // Fallback to client-side reply
                var post = _cachedPosts.FirstOrDefault(p => p.Id == postId);
                var comment = post?.CommentsList.FirstOrDefault(c => c.Id == commentId);
                if (comment != null)
                {
                    AddReply(comment, replyText);
                    return comment.Replies.Last();
                }
            }
            
            // If API call failed or comment not found, create a placeholder reply
            return new Comment
            {
                Id = _nextCommentId++,
                Content = replyText,
                Author = author,
                TimeAgo = "just now",
                Votes = 1,
                UserVote = 1,
                PostId = postId
            };
        }

        // Offline mode compatibility methods
        public void UpvotePost(Post post)
        {
            if (post.UserVote == 1)
            {
                // Remove upvote
                post.Votes--;
                post.UserVote = 0;
            }
            else
            {
                // Add upvote (and remove downvote if exists)
                if (post.UserVote == -1)
                {
                    post.Votes++;
                }
                post.Votes++;
                post.UserVote = 1;
            }
        }

        public void DownvotePost(Post post)
        {
            if (post.UserVote == -1)
            {
                // Remove downvote
                post.Votes++;
                post.UserVote = 0;
            }
            else
            {
                // Add downvote (and remove upvote if exists)
                if (post.UserVote == 1)
                {
                    post.Votes--;
                }
                post.Votes--;
                post.UserVote = -1;
            }
        }

        public void UpvoteComment(Comment comment)
        {
            if (comment.UserVote == 1)
            {
                // Remove upvote
                comment.Votes--;
                comment.UserVote = 0;
            }
            else
            {
                // Add upvote (and remove downvote if exists)
                if (comment.UserVote == -1)
                {
                    comment.Votes++;
                }
                comment.Votes++;
                comment.UserVote = 1;
            }
        }

        public void DownvoteComment(Comment comment)
        {
            if (comment.UserVote == -1)
            {
                // Remove downvote
                comment.Votes++;
                comment.UserVote = 0;
            }
            else
            {
                // Add downvote (and remove upvote if exists)
                if (comment.UserVote == 1)
                {
                    comment.Votes--;
                }
                comment.Votes--;
                comment.UserVote = -1;
            }
        }

        public void AddComment(Post post, string commentText)
        {
            var comment = new Comment
            {
                Id = _nextCommentId++,
                Author = _currentUser,
                Content = commentText,
                TimeAgo = "just now",
                Votes = 1,
                UserVote = 1,
                PostId = post.Id
            };

            post.CommentsList.Add(comment);
            post.Comments = post.CommentsList.Count;
        }

        public void AddReply(Comment parentComment, string replyText)
        {
            var reply = new Comment
            {
                Id = _nextCommentId++,
                Author = _currentUser,
                Content = replyText,
                TimeAgo = "just now",
                Votes = 1,
                UserVote = 1,
                ParentCommentId = parentComment.Id
            };

            parentComment.Replies.Add(reply);
        }

        private List<Post> GenerateFallbackPosts()
        {
            // Return empty list instead of generating fallback data
            // All data is now seeded on the server side
            Console.WriteLine("Unable to connect to server. Please try again later.");
            return new List<Post>();
        }
        
        public async Task<ContentGenerationResult> GenerateContentAsync(string apiKey = "")
        {
            try
            {
                string endpoint = "/api/generate-content";
                HttpContent content;
                
                // If API key is provided, use the endpoint that accepts an API key
                if (!string.IsNullOrEmpty(apiKey))
                {
                    endpoint = "/api/generate-content-with-key";
                    content = new StringContent(
                        JsonSerializer.Serialize(new { ApiKey = apiKey }),
                        Encoding.UTF8,
                        "application/json");
                }
                else
                {
                    // No API key provided, use empty content
                    content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
                }
                
                // Create the request
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = content;
                
                // Send the request
                var response = await _httpClient.SendAsync(request);
                var result = new ContentGenerationResult();
                
                // If response was not successful, check if it's due to missing API key
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.Error.WriteLine($"Server error: {response.StatusCode}");
                    Console.Error.WriteLine($"Error details: {errorContent}");
                    
                    try
                    {
                        // Try to parse the error to see if it's an API key issue
                        var errorJson = JsonSerializer.Deserialize<Dictionary<string, object>>(errorContent);
                        if (errorJson != null && errorJson.ContainsKey("needsApiKey") && errorJson.ContainsKey("message"))
                        {
                            bool needsApiKey = errorJson["needsApiKey"].ToString() == "True";
                            
                            if (needsApiKey)
                            {
                                result.NeedsApiKey = true;
                                result.ErrorMessage = errorJson["message"].ToString() ?? "API key required";
                                return result;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // If we can't parse the JSON, just use the raw error message
                    }
                    
                    result.Success = false;
                    result.ErrorMessage = $"Error: {response.StatusCode}. {errorContent}";
                    return result;
                }
                
                // If successful, refresh the posts list
                await GetPostsAsync(); // Refresh the cache
                
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error generating content: {ex.Message}");
                return new ContentGenerationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        public async Task<bool> DeleteAllContentAsync()
        {
            try
            {
                var response = await _httpClient.DeleteAsync("/api/delete-all-content");
                response.EnsureSuccessStatusCode();
                
                // Clear the cache
                _cachedPosts.Clear();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deleting content: {ex.Message}");
                return false;
            }
        }
    }
}
