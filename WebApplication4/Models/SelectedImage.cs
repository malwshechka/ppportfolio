namespace WebApplication4.Models;
using WebApplication4.Models; 

public class SelectedImage
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public ApplicationUser? User { get; set; }

    public Project? Project { get; set; }
}