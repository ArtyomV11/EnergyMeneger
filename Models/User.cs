namespace EnergyMeneger.Models;

public sealed class User
{
    public int Id { get; set; }
    public int FactoryId { get; set; }
    public int? WorkshopId { get; set; }
    public string Login { get; set; } = "";
    public string NormalizedLogin { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }

    public static string Normalize(string login) =>
        Rules.Text(login, "Логин", 100).ToUpperInvariant();
}
