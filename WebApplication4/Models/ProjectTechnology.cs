namespace WebApplication4.Models
{
    public class ProjectTechnology
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public int TechnologyId { get; set; }

        public Project? Project { get; set; }

        public Technology? Technology { get; set; }
    }
}