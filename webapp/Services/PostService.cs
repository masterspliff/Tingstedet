using webapp.Models;

namespace webapp.Services
{
    public interface IPostService
    {
        Task<List<Post>> GetPostsAsync();
        Task<Post> GetPostByIdAsync(int id);
        void UpvotePost(Post post);
        void DownvotePost(Post post);
        void UpvoteComment(Comment comment);
        void DownvoteComment(Comment comment);
        void AddComment(Post post, string commentText);
        void AddReply(Comment parentComment, string replyText);
    }

    public class PostService : IPostService
    {
        private readonly List<Post> _posts;
        private int _nextCommentId = 1;

        public PostService()
        {
            _posts = GenerateMockPosts();
        }

        public async Task<List<Post>> GetPostsAsync()
        {
            // Simulate API delay
            await Task.Delay(5000);
            return _posts;
        }

        public async Task<Post> GetPostByIdAsync(int id)
        {
            // Simulate API delay
            await Task.Delay(1000);
            return _posts.FirstOrDefault(p => p.Id == id) ?? new Post();
        }

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
                Author = "CurrentUser",
                Content = commentText,
                TimeAgo = "just now",
                Votes = 1,
                UserVote = 1
            };

            post.CommentsList.Add(comment);
            post.Comments = post.CommentsList.Count;
        }

        public void AddReply(Comment parentComment, string replyText)
        {
            var reply = new Comment
            {
                Id = _nextCommentId++,
                Author = "CurrentUser",
                Content = replyText,
                TimeAgo = "just now",
                Votes = 1,
                UserVote = 1
            };

            parentComment.Replies.Add(reply);
        }

        private List<Post> GenerateMockPosts()
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
                    ImageUrl = "https://images.unsplash.com/photo-1495616811223-4d98c6e9c869?ixlib=rb-1.2.1&auto=format&fit=crop&w=1000&q=80",
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
                },
                new Post
                {
                    Id = 4,
                    Title = "New coffee shop opening next week",
                    Content = "Just saw that 'Bean There' is opening next Tuesday on Main Street. They're offering free coffee on opening day. Has anyone heard anything about this place?",
                    Author = "CoffeeFanatic",
                    TimeAgo = "2 days ago",
                    Votes = 65,
                    UserVote = 0
                },
                new Post
                {
                    Id = 5,
                    Title = "Lost cat - please help!",
                    Content = "My orange tabby cat 'Whiskers' has been missing since yesterday evening. Last seen near Oak Street. He's wearing a blue collar with my contact info. Please message me if you see him!",
                    Author = "CatPerson",
                    TimeAgo = "5 hours ago",
                    Votes = 112,
                    ImageUrl = "https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?ixlib=rb-1.2.1&auto=format&fit=crop&w=1000&q=80",
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
                    Replies = new List<Comment>
                    {
                        new Comment
                        {
                            Id = _nextCommentId++,
                            Author = "NewNeighbor",
                            Content = "Thanks for the recommendation! I'll check it out this weekend.",
                            TimeAgo = "1 hour ago",
                            Votes = 3,
                            UserVote = 0
                        },
                        new Comment
                        {
                            Id = _nextCommentId++,
                            Author = "CoffeeAddict",
                            Content = "Their coffee is also amazing! Try the house blend.",
                            TimeAgo = "45 minutes ago",
                            Votes = 2,
                            UserVote = 0
                        }
                    }
                },
                new Comment
                {
                    Id = _nextCommentId++,
                    Author = "ParkRanger",
                    Content = "Riverside Park is beautiful this time of year. They have walking trails, picnic areas, and a dog park if you have a furry friend!",
                    TimeAgo = "2 hours ago",
                    Votes = 8,
                    UserVote = 0
                },
                new Comment
                {
                    Id = _nextCommentId++,
                    Author = "EventPlanner",
                    Content = "There's a farmers market every Saturday morning in the town square. Great place to meet locals and get fresh produce!",
                    TimeAgo = "1 hour ago",
                    Votes = 6,
                    UserVote = 0
                }
            };

            // Add comments to the second post
            posts[1].CommentsList = new List<Comment>
            {
                new Comment
                {
                    Id = _nextCommentId++,
                    Author = "PhotoEnthusiast",
                    Content = "Wow, that's a stunning shot! What camera did you use?",
                    TimeAgo = "7 hours ago",
                    Votes = 12,
                    UserVote = 0,
                    Replies = new List<Comment>
                    {
                        new Comment
                        {
                            Id = _nextCommentId++,
                            Author = "NatureLover",
                            Content = "Thanks! Just used my phone actually - Google Pixel 6.",
                            TimeAgo = "6 hours ago",
                            Votes = 5,
                            UserVote = 0
                        }
                    }
                },
                new Comment
                {
                    Id = _nextCommentId++,
                    Author = "SunsetChaser",
                    Content = "I love that spot! The sunsets there are always magical.",
                    TimeAgo = "5 hours ago",
                    Votes = 7,
                    UserVote = 0
                }
            };

            // Update comment counts
            foreach (var post in posts)
            {
                post.Comments = post.CommentsList.Count;
            }

            return posts;
        }
    }
}
