using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication4.Models
{
    public class UserReport
    {
        public int Id { get; set; }

        public string ReporterId { get; set; } = string.Empty;
        [ForeignKey("ReporterId")]
        public ApplicationUser Reporter { get; set; } = null!;

        public string ReportedUserId { get; set; } = string.Empty;
        [ForeignKey("ReportedUserId")]
        public ApplicationUser ReportedUser { get; set; } = null!;

        public ReportType Type { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? AdditionalComment { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.Now;
        public bool IsResolved { get; set; }
        public string? AdminResponse { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public enum ReportType
    {
        Spam,
        Harassment,
        InappropriateContent,
        FakeProfile,
        Other
    }
}