using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication4.Models
{
    public class UserSubscription
    {
        public int Id { get; set; }

        public string SubscriberId { get; set; } = string.Empty;
        [ForeignKey("SubscriberId")]
        public ApplicationUser Subscriber { get; set; } = null!;

        public string FollowedUserId { get; set; } = string.Empty;
        [ForeignKey("FollowedUserId")]
        public ApplicationUser FollowedUser { get; set; } = null!;

        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    }
}