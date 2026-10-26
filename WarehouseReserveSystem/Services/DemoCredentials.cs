namespace WarehouseReserveSystem.Services;

public sealed record DemoStaffCredential(string FullName, string Email, string Login, string Password, string Role);

public static class DemoCredentials
{
    public static readonly IReadOnlyList<DemoStaffCredential> Staff =
    [
        new("Олександр Керівник", "admin@warehousereserve.local", "admin", "Admin#2026!WMS", "Admin"),
        new("Василь Комірник", "storekeeper@warehousereserve.local", "storekeeper", "Storekeeper#2026!WMS", "Storekeeper"),
        new("Олена Аудитор", "auditor@warehousereserve.local", "auditor", "Auditor#2026!WMS", "Auditor")
    ];

    public static string? GetPassword(string login)
    {
        return Staff.FirstOrDefault(s => s.Login.Equals(login, StringComparison.OrdinalIgnoreCase))?.Password;
    }
}
