namespace WebApplication4.Models;

    public class Review
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;

        public int Rating { get; set; }

        public DateTime Date { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int ProjectId { get; set; }

        public ApplicationUser? User { get; set; }

        public Project? Project { get; set; }
    }
