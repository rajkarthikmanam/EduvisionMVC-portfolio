namespace EduvisionMvc.Models;

public class SystemLog
{
    public int Id { get; set; }
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
}