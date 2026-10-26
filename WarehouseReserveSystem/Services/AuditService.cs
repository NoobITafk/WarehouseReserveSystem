using WarehouseReserveSystem.Data;
using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Services;

public class AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task LogAsync(string action, string entityName, string? entityId, string details, string? staffLogin = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var ip = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var login = staffLogin 
            ?? httpContext?.User.Identity?.Name 
            ?? "anonymous";

        var log = new AuditLog
        {
            CreatedAt = DateTime.UtcNow,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            StaffLogin = login,
            IpAddress = ip,
            Details = details
        };

        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();
    }
}
