using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseReserveSystem.Data;

namespace WarehouseReserveSystem.Controllers;

[Authorize(Roles = "Admin,Auditor")]
public class AuditLogsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? actionFilter, string? staffFilter, int page = 1)
    {
        const int pageSize = 25;
        var query = context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action == actionFilter);
        }

        if (!string.IsNullOrWhiteSpace(staffFilter))
        {
            query = query.Where(a => a.StaffLogin == staffFilter);
        }

        var totalItems = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalItems = totalItems;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.ActionFilter = actionFilter;
        ViewBag.StaffFilter = staffFilter;

        return View(logs);
    }
}
