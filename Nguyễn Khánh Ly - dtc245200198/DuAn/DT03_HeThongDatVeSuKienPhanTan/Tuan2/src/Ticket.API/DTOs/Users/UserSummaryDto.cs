namespace Ticket.API.DTOs.Users;

public class UserSummaryDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
