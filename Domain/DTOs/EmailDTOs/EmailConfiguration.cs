namespace Domain.DTOs.EmailDTOs;

public class EmailConfiguration
{
    public required string From { get; set; }
    public string? DisplayName { get; set; }
    public required string SmtpServer { get; set; }
    public required int Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public bool EnableSsl { get; set; } = true;
}