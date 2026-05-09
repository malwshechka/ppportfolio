namespace WebApplication4.Models
{

    public class Status
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<Project>? Projects { get; set; }
    }
}