using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StorageZone> StorageZones => Set<StorageZone>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockMovementItem> StockMovementItems => Set<StockMovementItem>();
    public DbSet<WarehouseStaff> WarehouseStaffs => Set<WarehouseStaff>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Product constraints & indexes
        builder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.HasIndex(p => p.Barcode);
            entity.Property(p => p.CostPrice).HasPrecision(18, 2);
            entity.Property(p => p.SellingPrice).HasPrecision(18, 2);
            entity.Property(p => p.WeightKg).HasPrecision(10, 3);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Warehouse & Zone & Location
        builder.Entity<Warehouse>(entity =>
        {
            entity.HasIndex(w => w.Code).IsUnique();
            entity.Property(w => w.TotalAreaSqM).HasPrecision(12, 2);
        });

        builder.Entity<StorageZone>(entity =>
        {
            entity.HasOne(z => z.Warehouse)
                .WithMany(w => w.Zones)
                .HasForeignKey(z => z.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StorageLocation>(entity =>
        {
            entity.HasIndex(l => new { l.StorageZoneId, l.Code }).IsUnique();
            entity.Property(l => l.MaxWeightKg).HasPrecision(12, 2);
            entity.Property(l => l.MaxVolumeM3).HasPrecision(10, 2);

            entity.HasOne(l => l.StorageZone)
                .WithMany(z => z.Locations)
                .HasForeignKey(l => l.StorageZoneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StockItem
        builder.Entity<StockItem>(entity =>
        {
            entity.HasIndex(s => new { s.ProductId, s.StorageLocationId }).IsUnique();

            entity.HasOne(s => s.Product)
                .WithMany(p => p.StockItems)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.StorageLocation)
                .WithMany(l => l.StockItems)
                .HasForeignKey(s => s.StorageLocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StockMovement
        builder.Entity<StockMovement>(entity =>
        {
            entity.HasIndex(m => m.DocumentNumber).IsUnique();
            entity.HasIndex(m => m.CreatedAt);

            entity.HasOne(m => m.StaffUser)
                .WithMany(s => s.CreatedMovements)
                .HasForeignKey(m => m.StaffUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(m => m.Supplier)
                .WithMany(s => s.StockMovements)
                .HasForeignKey(m => m.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // StockMovementItem
        builder.Entity<StockMovementItem>(entity =>
        {
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(i => i.StockMovement)
                .WithMany(m => m.Items)
                .HasForeignKey(i => i.StockMovementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.SourceLocation)
                .WithMany()
                .HasForeignKey(i => i.SourceLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.DestinationLocation)
                .WithMany()
                .HasForeignKey(i => i.DestinationLocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WarehouseStaff
        builder.Entity<WarehouseStaff>(entity =>
        {
            entity.HasIndex(s => s.Login).IsUnique();
            entity.HasIndex(s => s.Email).IsUnique();
        });

        // AuditLog
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => a.CreatedAt);
            entity.HasIndex(a => a.Action);
        });
    }
}
