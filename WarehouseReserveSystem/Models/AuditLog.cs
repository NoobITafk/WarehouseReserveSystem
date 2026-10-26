namespace WarehouseReserveSystem.Models;

public class AuditLog
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty;       // e.g. "CreateProduct", "ExecuteReceipt", "LoginSuccess"
    public string EntityName { get; set; } = string.Empty;   // e.g. "StockMovement", "Product", "Account"
    public string? EntityId { get; set; }
    public string StaffLogin { get; set; } = "anonymous";
    public string? IpAddress { get; set; }
    public string Details { get; set; } = string.Empty;
}
