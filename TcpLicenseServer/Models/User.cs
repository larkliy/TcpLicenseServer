
namespace TcpLicenseServer.Models;

public class User
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public required string Role { get; set; }
    public string? Hwid { get; set; }

    public DateTime CreatedAt { get; set; }
}
