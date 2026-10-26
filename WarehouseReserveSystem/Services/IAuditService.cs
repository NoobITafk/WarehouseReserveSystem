namespace WarehouseReserveSystem.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? entityId, string details, string? staffLogin = null);
}
