using System;
using System.Collections.Generic;

namespace ContentManagementSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalPosts { get; set; }
        public int PublishedPosts { get; set; }
        public int DraftPosts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalComments { get; set; }
        public int TotalContactMessages { get; set; }
        public int TotalSubscribers { get; set; }

        // User role counts
        public int AdminCount { get; set; }
        public int QAManagerCount { get; set; }
        public int QACoordinatorCount { get; set; }
        public int CustomerCount { get; set; }

        // Chart 1: 6-month trends
        public List<string> Months { get; set; } = new();
        public List<int> MonthlyPostCounts { get; set; } = new();
        public List<int> MonthlyCommentCounts { get; set; } = new();

        // Chart 2: Categories distribution
        public List<string> CategoryNames { get; set; } = new();
        public List<int> CategoryPostCounts { get; set; } = new();

        // Chart 5: Nhận xét & Đánh giá (Testimonials / Rating)
        public int TotalTestimonials { get; set; } = 32;
        public double AverageRating { get; set; } = 4.8;
        public int Rating5Count { get; set; } = 20;
        public int Rating4Count { get; set; } = 8;
        public int Rating3Count { get; set; } = 3;
        public int Rating2Count { get; set; } = 1;
        public int Rating1Count { get; set; } = 0;

        // Chart 6: Thả icon cảm xúc (Emoji Reactions: Like, Love, Haha, Wow, Insight)
        public int TotalReactions { get; set; } = 156;
        public int LikeCount { get; set; } = 64;    // 👍 Thích
        public int LoveCount { get; set; } = 48;    // ❤️ Yêu thích
        public int HahaCount { get; set; } = 18;    // 😄 Vui vẻ / Haha
        public int WowCount { get; set; } = 12;     // 😮 Bất ngờ / Wow
        public int InsightCount { get; set; } = 14; // 💡 Hữu ích

        // Recent posts for Customer & reader view
        public List<DashboardRecentPost> RecentPosts { get; set; } = new();
    }

    public class DashboardRecentPost
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string CategoryName { get; set; } = "Tin tức";
        public string AuthorName { get; set; } = "Ban biên tập";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int CommentCount { get; set; } = 0;
    }
}

