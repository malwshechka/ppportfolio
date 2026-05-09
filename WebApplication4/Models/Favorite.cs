using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication4.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project Project { get; set; } = null!;

        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}