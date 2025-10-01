namespace Domain.DTOs.User;

public class UserActivityDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public DateTime LastLoginTime { get; set; }
    public int LoginCount { get; set; }
    public List<LoginHistoryItem> RecentLogins { get; set; } = new List<LoginHistoryItem>();
    public int TotalActions { get; set; }
    public DateTime LastActivityTime { get; set; }
    public Dictionary<string, int> ActivityByCategory { get; set; } = new Dictionary<string, int>();
    public List<UserActionItem> RecentActions { get; set; } = new List<UserActionItem>();
    
    public class LoginHistoryItem
    {
        public DateTime LoginTime { get; set; }
        public string IpAddress { get; set; } = null!;
        public string UserAgent { get; set; } = null!;
        public bool IsSuccessful { get; set; }
    }
    
    public class UserActionItem
    {
        public DateTime Timestamp { get; set; }
        public string ActionType { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }
}
