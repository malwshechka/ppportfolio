namespace WebApplication4.Models
{
    public class Image
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;      
        public string Url { get; set; } = string.Empty;       
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Привязка к проекту
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

    }
}