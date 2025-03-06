using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using core.Models;

namespace webapp.Services
{
    public interface IPostService
    {
        Task<List<Post>> GetPostsAsync();
        Task<Post> GetPostByIdAsync(int id);
        Task<bool> LoadImageAsync(string imageUrl);
        Task<int> UpvotePostAsync(Post post, string username);
        Task<int> DownvotePostAsync(Post post, string username);
        Task<int> UpvoteCommentAsync(Comment comment, string username);
        Task<int> DownvoteCommentAsync(Comment comment, string username);
        Task<Comment> AddCommentAsync(Post post, string commentText, string author);
        Task<Post> CreatePostAsync(Post post);
        
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
            _cachedPosts = new List<Post>();
        }

        public async Task<List<Post>> GetPostsAsync()
        {
            try
            {
                var posts = await _httpClient.GetFromJsonAsync<List<Post>>("/api/posts", _jsonOptions);
                if (posts != null)
                {
                    _cachedPosts.Clear();
                    _cachedPosts.AddRange(posts);
                    return posts;
                }
            }
            catch (Exception)
            {
                // If API call fails, return cached posts or empty list
                if (_cachedPosts.Count == 0)
                {
                    return GenerateFallbackPosts();
                }
                return _cachedPosts;
            }
            
            return new List<Post>();
        }

        public async Task<Post> GetPostByIdAsync(int id)
        {
            try
            {
                var post = await _httpClient.GetFromJsonAsync<Post>($"/api/posts/{id}", _jsonOptions);
                return post ?? new Post();
            }
            catch (Exception)
            {
                // If API call fails, try to find in cached posts
                var cachedPost = _cachedPosts.FirstOrDefault(p => p.Id == id);
                if (cachedPost != null)
                {
                    return cachedPost;
                }
                
                // If not in cache, return fallback
                return new Post { Title = "Post not found", Author = "Unknown", TimeAgo = "unknown" };
            }
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
                var postDto = new PostCreateDto
                {
                    Title = post.Title,
                    Content = post.Content,
                    Url = post.Url,
                    Author = post.Author
                };
                
                var response = await _httpClient.PostAsJsonAsync("/api/posts", postDto);
                response.EnsureSuccessStatusCode();
                
                var createdPost = await response.Content.ReadFromJsonAsync<Post>(_jsonOptions);
                if (createdPost != null)
                {
                    // Add to cache
                    _cachedPosts.Add(createdPost);
                    return createdPost;
                }
                
                return post;
            }
            catch (Exception)
            {
                // Fallback for offline mode - assign a temporary ID
                post.Id = _cachedPosts.Count > 0 ? _cachedPosts.Max(p => p.Id) + 1 : 1;
                _cachedPosts.Add(post);
                return post;
            }
        }
        
        public async Task<int> UpvotePostAsync(Post post, string username)
        {
            try
            {
                var voteDto = new VoteDto { Value = 1, Username = username };
                var response = await _httpClient.PostAsJsonAsync($"/api/posts/{post.Id}/vote", voteDto);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>(_jsonOptions);
                    post.Votes = result.GetProperty("votes").GetInt32();
                    post.UserVote = 1;
                    return post.Votes;
                }
            }
            catch (Exception)
            {
                // Fallback to client-side voting
                UpvotePost(post);
            }
            
            return post.Votes;
        }
        
        public async Task<int> DownvotePostAsync(Post post, string username)
        {
            try
            {
                var voteDto = new VoteDto { Value = -1, Username = username };
                var response = await _httpClient.PostAsJsonAsync($"/api/posts/{post.Id}/vote", voteDto);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>(_jsonOptions);
                    post.Votes = result.GetProperty("votes").GetInt32();
                    post.UserVote = -1;
                    return post.Votes;
                }
            }
            catch (Exception)
            {
                // Fallback to client-side voting
                DownvotePost(post);
            }
            
            return post.Votes;
        }
        
        public async Task<int> UpvoteCommentAsync(Comment comment, string username)
        {
            try
            {
                var voteDto = new VoteDto { Value = 1, Username = username };
                var response = await _httpClient.PostAsJsonAsync($"/api/comments/{comment.Id}/vote", voteDto);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>(_jsonOptions);
                    comment.Votes = result.GetProperty("votes").GetInt32();
                    comment.UserVote = 1;
                    return comment.Votes;
                }
            }
            catch (Exception)
            {
                // Fallback to client-side voting
                UpvoteComment(comment);
            }
            
            return comment.Votes;
        }
        
        public async Task<int> DownvoteCommentAsync(Comment comment, string username)
        {
            try
            {
                var voteDto = new VoteDto { Value = -1, Username = username };
                var response = await _httpClient.PostAsJsonAsync($"/api/comments/{comment.Id}/vote", voteDto);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>(_jsonOptions);
                    comment.Votes = result.GetProperty("votes").GetInt32();
                    comment.UserVote = -1;
                    return comment.Votes;
                }
            }
            catch (Exception)
            {
                // Fallback to client-side voting
                DownvoteComment(comment);
            }
            
            return comment.Votes;
        }
        
        public async Task<Comment> AddCommentAsync(Post post, string commentText, string author)
        {
            try
            {
                var commentDto = new CommentCreateDto
                {
                    Content = commentText,
                    Author = author
                };
                
                var response = await _httpClient.PostAsJsonAsync($"/api/posts/{post.Id}/comments", commentDto);
                
                if (response.IsSuccessStatusCode)
                {
                    var comment = await response.Content.ReadFromJsonAsync<Comment>(_jsonOptions);
                    if (comment != null)
                    {
                        post.CommentsList.Add(comment);
                        return comment;
                    }
                }
            }
            catch (Exception)
            {
                // Fallback to client-side comment
                AddComment(post, commentText);
                return post.CommentsList.Last();
            }
            
            // If API call failed, fallback to client-side
            AddComment(post, commentText);
            return post.CommentsList.Last();
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
                UserVote = 1
            };

            parentComment.Replies.Add(reply);
        }

        private List<Post> GenerateFallbackPosts()
        {
            var posts = new List<Post>
            {
                new Post
                {
                    Id = 1,
                    Title = "Just moved to the neighborhood, any recommendations?",
                    Content = "Hi everyone! I just moved to the area and I'm looking for recommendations on local restaurants, parks, and community events. What are your favorite spots?",
                    Author = "NewNeighbor",
                    TimeAgo = "3 hours ago",
                    Votes = 42,
                    UserVote = 0
                },
                new Post
                {
                    Id = 2,
                    Title = "Beautiful sunset at the local park yesterday",
                    Content = "Caught this amazing view while walking my dog. Thought I'd share with the community!",
                    Author = "NatureLover",
                    TimeAgo = "8 hours ago",
                    Votes = 128,
                    Url = "https://images.unsplash.com/photo-1495616811223-4d98c6e9c869",
                    UserVote = 0
                },
                new Post
                {
                    Id = 3,
                    Title = "Community cleanup this weekend - volunteers needed!",
                    Content = "We're organizing a community cleanup this Saturday from 10am to 2pm. Meet at the central square. Gloves and bags will be provided. Hope to see many of you there!",
                    Author = "CommunityOrganizer",
                    TimeAgo = "1 day ago",
                    Votes = 89,
                    UserVote = 0
                }
            };

            // Add comments to the first post
            posts[0].CommentsList = new List<Comment>
            {
                new Comment
                {
                    Id = _nextCommentId++,
                    Author = "LocalFoodie",
                    Content = "You should definitely try 'The Green Table' on Oak Street. They have amazing farm-to-table options and great outdoor seating!",
                    TimeAgo = "2 hours ago",
                    Votes = 15,
                    UserVote = 0,
                },
                new Comment
                {
                    Id = _nextCommentId++,
                    Author = "ParkRanger",
                    Content = "Riverside Park is beautiful this time of year. They have walking trails, picnic areas, and a dog park if you have a furry friend!",
                    TimeAgo = "2 hours ago",
                    Votes = 8,
                    UserVote = 0
                }
            };

            return posts;
        }
    }
}
