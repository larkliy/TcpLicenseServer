
namespace TcpLicenseServer.Models;

public class Config
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string JsonConfig { get; set; }

    public DateTime CreatedAt { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }
}
