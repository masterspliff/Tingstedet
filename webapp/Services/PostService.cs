using webapp.Models;

namespace webapp.Services
{
    public interface IPostService
    {
        Task<List<Post>> GetPostsAsync();
        void UpvotePost(Post post);
        void DownvotePost(Post post);
    }

    public class PostService : IPostService
    {
        public async Task<List<Post>> GetPostsAsync()
        {
            // Simulate API delay
            await Task.Delay(5000);
            
            // Generate mock data
            return new List<Post>
            {
                new Post
                {
                    Id = 1,
                    Title = "Just moved to the neighborhood, any recommendations?",
                    Content = "Hi everyone! I just moved to the area and I'm looking for recommendations on local restaurants, parks, and community events. What are your favorite spots?",
                    Author = "NewNeighbor",
                    TimeAgo = "3 hours ago",
                    Votes = 42,
                    Comments = 15,
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
                    Comments = 23,
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
                    Comments = 42,
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
                    Comments = 31,
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
                    Comments = 28,
                    ImageUrl = "https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?ixlib=rb-1.2.1&auto=format&fit=crop&w=1000&q=80",
                    UserVote = 0
                }
            };
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
    }
}
