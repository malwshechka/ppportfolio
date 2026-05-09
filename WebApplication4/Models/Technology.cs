namespace WebApplication4.Models
{

    public class Technology
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<ProjectTechnology>? ProjectTechnologies { get; set; }
    }
}