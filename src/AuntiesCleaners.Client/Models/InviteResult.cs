namespace AuntiesCleaners.Client.Models;

public class InviteResult
{
    public bool Success { get; set; }
    public string? UserId { get; set; }
    public string? ProfileId { get; set; }
    public bool EmailSent { get; set; }
    public string? Error { get; set; }
    public string? Warning { get; set; }
}
