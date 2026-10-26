using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseReserveSystem.Data;
using WarehouseReserveSystem.Models;
using WarehouseReserveSystem.ViewModels;

namespace WarehouseReserveSystem.Services;

public class DemoUserService(ApplicationDbContext context, IPasswordHasher<WarehouseStaff> passwordHasher)
{
    public async Task<WarehouseStaff?> ValidateUserAsync(string login, string password)
    {
        var normalizedLogin = login.Trim().ToLower();

        var staff = await context.WarehouseStaffs
            .FirstOrDefaultAsync(item =>
                item.Login.ToLower() == normalizedLogin ||
                item.Email.ToLower() == normalizedLogin);

        if (staff is null || !staff.IsActive)
        {
            return null;
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(staff, staff.PasswordHash, password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            staff.PasswordHash = passwordHasher.HashPassword(staff, password);
            await context.SaveChangesAsync();
        }

        return staff;
    }

    public async Task<IReadOnlyList<DemoUserViewModel>> GetDemoUsersAsync()
    {
        var demoLogins = DemoCredentials.Staff.Select(user => user.Login).ToArray();
        var staffList = await context.WarehouseStaffs
            .AsNoTracking()
            .Where(staff => demoLogins.Contains(staff.Login))
            .OrderBy(staff => staff.Id)
            .ToListAsync();

        return staffList
            .Select(staff => new DemoUserViewModel(
                staff.Login,
                DemoCredentials.GetPassword(staff.Login) ?? string.Empty,
                staff.FullName,
                staff.Role))
            .ToList();
    }
}
