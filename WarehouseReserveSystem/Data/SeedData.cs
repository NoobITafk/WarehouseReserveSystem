using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseReserveSystem.Models;
using WarehouseReserveSystem.Services;

namespace WarehouseReserveSystem.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        ApplicationDbContext context, 
        IPasswordHasher<WarehouseStaff> passwordHasher,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // 1. Roles & Identity
        string[] roles = ["Admin", "Storekeeper", "Auditor"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Staff & Identity Users
        if (!await context.WarehouseStaffs.AnyAsync())
        {
            foreach (var demoStaff in DemoCredentials.Staff)
            {
                var staff = new WarehouseStaff
                {
                    FullName = demoStaff.FullName,
                    Email = demoStaff.Email,
                    Login = demoStaff.Login,
                    Role = demoStaff.Role,
                    Phone = "+380501234567",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                staff.PasswordHash = passwordHasher.HashPassword(staff, demoStaff.Password);
                context.WarehouseStaffs.Add(staff);

                // Sync with AspNetUsers
                var identityUser = await userManager.FindByNameAsync(demoStaff.Login);
                if (identityUser is null)
                {
                    identityUser = new IdentityUser
                    {
                        UserName = demoStaff.Login,
                        Email = demoStaff.Email,
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(identityUser, demoStaff.Password);
                    await userManager.AddToRoleAsync(identityUser, demoStaff.Role);
                }
            }

            await context.SaveChangesAsync();
        }

        // 3. Warehouses
        if (!await context.Warehouses.AnyAsync())
        {
            var whKbp = new Warehouse
            {
                Name = "Центральний логістичний комплекс (Київ)",
                Code = "WH-KBP",
                Address = "м. Київ, вул. Велика Кільцева, 45",
                ContactPhone = "+38 (044) 330-10-20",
                TotalAreaSqM = 4500m,
                IsActive = true
            };

            var whLwo = new Warehouse
            {
                Name = "Західний розподільчий хаб (Львів)",
                Code = "WH-LWO",
                Address = "м. Львів, вул. Городоцька, 280",
                ContactPhone = "+38 (032) 299-40-50",
                TotalAreaSqM = 2200m,
                IsActive = true
            };

            context.Warehouses.AddRange(whKbp, whLwo);
            await context.SaveChangesAsync();

            // 4. Storage Zones for WH-KBP
            var zoneA = new StorageZone
            {
                WarehouseId = whKbp.Id,
                Name = "Зона A (Палетне стелажне зберігання)",
                Code = "ZONE-A",
                ZoneType = "Standard",
                Description = "Важке технологічне обладнання та палетні місця"
            };

            var zoneB = new StorageZone
            {
                WarehouseId = whKbp.Id,
                Name = "Зона B (Дрібноштучні деталі та комплектація)",
                Code = "ZONE-B",
                ZoneType = "Standard",
                Description = "Коміркові стелажі для контролерів та електроніки"
            };

            var zoneC = new StorageZone
            {
                WarehouseId = whKbp.Id,
                Name = "Зона C (Технічні рідини та ПММ)",
                Code = "ZONE-C",
                ZoneType = "Hazardous",
                Description = "Ізольована зона для мастил та гідравлічних сумішей"
            };

            // Zones for WH-LWO
            var zoneL1 = new StorageZone
            {
                WarehouseId = whLwo.Id,
                Name = "Зона Л1 (Універсальне зберігання)",
                Code = "ZONE-L1",
                ZoneType = "Standard",
                Description = "Універсальні багатоярусні стелажі"
            };

            context.StorageZones.AddRange(zoneA, zoneB, zoneC, zoneL1);
            await context.SaveChangesAsync();

            // 5. Addressable Storage Locations
            List<StorageLocation> locations = [];
            
            // Zone A: Aisles 01-02, Racks 01-02, Shelves A-B
            string[] shelves = ["A", "B"];
            for (int aisle = 1; aisle <= 2; aisle++)
            {
                for (int rack = 1; rack <= 2; rack++)
                {
                    foreach (var sh in shelves)
                    {
                        locations.Add(new StorageLocation
                        {
                            StorageZoneId = zoneA.Id,
                            Code = $"A-{aisle:00}-{rack:00}-{sh}",
                            Aisle = $"Ряд {aisle}",
                            Rack = $"Стелаж {rack}",
                            Shelf = $"Полиця {sh}",
                            Bin = "1",
                            MaxWeightKg = 1500m,
                            MaxVolumeM3 = 3.5m,
                            IsActive = true
                        });
                    }
                }
            }

            // Zone B: 6 locations
            for (int rack = 1; rack <= 3; rack++)
            {
                for (int bin = 1; bin <= 2; bin++)
                {
                    locations.Add(new StorageLocation
                    {
                        StorageZoneId = zoneB.Id,
                        Code = $"B-01-{rack:00}-{bin}",
                        Aisle = "Ряд 1",
                        Rack = $"Стелаж {rack}",
                        Shelf = "Полиця 1",
                        Bin = $"Комірка {bin}",
                        MaxWeightKg = 250m,
                        MaxVolumeM3 = 0.8m,
                        IsActive = true
                    });
                }
            }

            // Zone C: 3 locations
            locations.Add(new StorageLocation
            {
                StorageZoneId = zoneC.Id,
                Code = "C-01-01-A",
                Aisle = "Ряд 1",
                Rack = "Стелаж 1",
                Shelf = "Полиця A",
                Bin = "1",
                MaxWeightKg = 800m,
                MaxVolumeM3 = 2.0m,
                IsActive = true
            });

            // Zone L1: 3 locations
            locations.Add(new StorageLocation
            {
                StorageZoneId = zoneL1.Id,
                Code = "L1-01-01-A",
                Aisle = "Ряд 1",
                Rack = "Стелаж 1",
                Shelf = "Полиця A",
                Bin = "1",
                MaxWeightKg = 1000m,
                MaxVolumeM3 = 2.5m,
                IsActive = true
            });

            context.StorageLocations.AddRange(locations);
            await context.SaveChangesAsync();
        }

        // 6. Categories
        if (!await context.Categories.AnyAsync())
        {
            var catAuto = new Category
            {
                Name = "Промислова автоматика та ПЛК",
                Code = "CAT-AUTO",
                ColorHex = "#3b82f6",
                Description = "Контролери, частотні перетворювачі, модулі вводу-виводу"
            };

            var catTools = new Category
            {
                Name = "Професійний інструмент",
                Code = "CAT-TOOLS",
                ColorHex = "#f59e0b",
                Description = "Електроінструмент, монтажне обладнання, вимірювальні прилади"
            };

            var catParts = new Category
            {
                Name = "Складська техніка та запчастини",
                Code = "CAT-PARTS",
                ColorHex = "#10b981",
                Description = "Гідравлічні візки, колеса, ролики, ремкомплекти"
            };

            var catCable = new Category
            {
                Name = "Кабельно-провідникова продукція",
                Code = "CAT-CABLE",
                ColorHex = "#8b5cf6",
                Description = "Силові, сигнальні та оптичні кабелі"
            };

            context.Categories.AddRange(catAuto, catTools, catParts, catCable);
            await context.SaveChangesAsync();
        }

        // 7. Suppliers
        if (!await context.Suppliers.AnyAsync())
        {
            var sup1 = new Supplier
            {
                Name = "ТОВ «ПромЕлектроПостач»",
                TaxNumber = "38472910",
                ContactPerson = "Віктор Сергієнко",
                Phone = "+38 (044) 234-56-78",
                Email = "supply@promelectro.ua",
                Address = "м. Київ, вул. Промислова, 14",
                IsActive = true
            };

            var sup2 = new Supplier
            {
                Name = "ПП «ЗахідСпецІнструмент»",
                TaxNumber = "39281745",
                ContactPerson = "Андрій Коваль",
                Phone = "+38 (032) 298-76-54",
                Email = "sales@zahid-tools.com",
                Address = "м. Львів, вул. Зелена, 149",
                IsActive = true
            };

            var sup3 = new Supplier
            {
                Name = "ТОВ «ЛогістикТехнолоджі»",
                TaxNumber = "40192837",
                ContactPerson = "Світлана Мороз",
                Phone = "+38 (050) 333-22-11",
                Email = "order@logtech.ua",
                Address = "м. Дніпро, вул. Запорізьке шосе, 26",
                IsActive = true
            };

            context.Suppliers.AddRange(sup1, sup2, sup3);
            await context.SaveChangesAsync();
        }

        // 8. Products
        if (!await context.Products.AnyAsync())
        {
            var catAuto = await context.Categories.FirstAsync(c => c.Code == "CAT-AUTO");
            var catTools = await context.Categories.FirstAsync(c => c.Code == "CAT-TOOLS");
            var catParts = await context.Categories.FirstAsync(c => c.Code == "CAT-PARTS");
            var catCable = await context.Categories.FirstAsync(c => c.Code == "CAT-CABLE");

            var sup1 = await context.Suppliers.FirstAsync(s => s.TaxNumber == "38472910");
            var sup2 = await context.Suppliers.FirstAsync(s => s.TaxNumber == "39281745");
            var sup3 = await context.Suppliers.FirstAsync(s => s.TaxNumber == "40192837");

            List<Product> products =
            [
                new Product
                {
                    Sku = "SKU-PLC-1200",
                    Name = "Контролер програмований Siemens S7-1200 CPU 1214C",
                    Barcode = "4047622019283",
                    CategoryId = catAuto.Id,
                    SupplierId = sup1.Id,
                    Unit = "шт",
                    CostPrice = 14500m,
                    SellingPrice = 18900m,
                    MinStockThreshold = 5,
                    WeightKg = 0.45m,
                    Description = "Компактний промисловий контролер з інтерфейсом PROFINET та вбудованими I/O"
                },
                new Product
                {
                    Sku = "SKU-VFD-2200",
                    Name = "Частотний перетворювач Schneider Electric Altivar 2.2kW",
                    Barcode = "3606480182741",
                    CategoryId = catAuto.Id,
                    SupplierId = sup1.Id,
                    Unit = "шт",
                    CostPrice = 8200m,
                    SellingPrice = 11200m,
                    MinStockThreshold = 4,
                    WeightKg = 1.3m,
                    Description = "Привід змінного струму для трифазних асинхронних двигунів з фільтром ЕМС"
                },
                new Product
                {
                    Sku = "SKU-BOSCH-18V",
                    Name = "Перфоратор акумуляторний Bosch Professional GBH 18V-26",
                    Barcode = "3165140889271",
                    CategoryId = catTools.Id,
                    SupplierId = sup2.Id,
                    Unit = "шт",
                    CostPrice = 6400m,
                    SellingPrice = 8900m,
                    MinStockThreshold = 6,
                    WeightKg = 3.2m,
                    Description = "Безщітковий двигун EC, сила удару 2.6 Дж, патрон SDS-plus"
                },
                new Product
                {
                    Sku = "SKU-ROKLA-2500",
                    Name = "Гідравлічний візок (рокла) DF 2500 кг поліуретан",
                    Barcode = "4820019283741",
                    CategoryId = catParts.Id,
                    SupplierId = sup3.Id,
                    Unit = "шт",
                    CostPrice = 12000m,
                    SellingPrice = 16500m,
                    MinStockThreshold = 3,
                    WeightKg = 72.0m,
                    Description = "Вантажопідйомність 2.5т, довжина вил 1150 мм, посилений гідровузол"
                },
                new Product
                {
                    Sku = "SKU-VVG-3X25",
                    Name = "Кабель силовий ВВГнг-LS 3х2.5 (бухта 100м)",
                    Barcode = "4823019283019",
                    CategoryId = catCable.Id,
                    SupplierId = sup1.Id,
                    Unit = "бухт",
                    CostPrice = 3200m,
                    SellingPrice = 4500m,
                    MinStockThreshold = 10,
                    WeightKg = 14.5m,
                    Description = "Мідний кабель з низьким димогазовиділенням за стандартом ДСТУ"
                },
                new Product
                {
                    Sku = "SKU-ABB-3P25",
                    Name = "Автоматичний вимикач ABB S203 C25 3-полюсний",
                    Barcode = "7612270303810",
                    CategoryId = catAuto.Id,
                    SupplierId = sup1.Id,
                    Unit = "шт",
                    CostPrice = 420m,
                    SellingPrice = 680m,
                    MinStockThreshold = 20,
                    WeightKg = 0.35m,
                    Description = "Модульний автоматичний вимикач 25А крива C, 6кА"
                },
                new Product
                {
                    Sku = "SKU-WHEEL-200P",
                    Name = "Колесо рульове поліуретан 200х50 для рокли",
                    Barcode = "4820091827364",
                    CategoryId = catParts.Id,
                    SupplierId = sup3.Id,
                    Unit = "шт",
                    CostPrice = 650m,
                    SellingPrice = 980m,
                    MinStockThreshold = 12,
                    WeightKg = 1.8m,
                    Description = "Поліуретановий контактний шар на чавунній основі з підшипником 204"
                },
                new Product
                {
                    Sku = "SKU-OIL-HLP46",
                    Name = "Масло гідравлічне Total Azolla ZS 46 (каністра 20л)",
                    Barcode = "4820192837465",
                    CategoryId = catParts.Id,
                    SupplierId = sup3.Id,
                    Unit = "кан",
                    CostPrice = 1750m,
                    SellingPrice = 2400m,
                    MinStockThreshold = 5,
                    WeightKg = 18.2m,
                    Description = "Високоякісна гідравлічна рідина з протизносними присадками"
                },
                // Intentionally Low Stock item to trigger Alert banner:
                new Product
                {
                    Sku = "SKU-LED-100W",
                    Name = "Світильник промисловий LED HighBay 100W IP65",
                    Barcode = "4829910293847",
                    CategoryId = catTools.Id,
                    SupplierId = sup2.Id,
                    Unit = "шт",
                    CostPrice = 950m,
                    SellingPrice = 1450m,
                    MinStockThreshold = 15, // threshold 15, but actual stock will be 2!
                    WeightKg = 1.9m,
                    Description = "Світловий потік 14000 Лм, кут розсіювання 90°, колірна температура 5000K"
                }
            ];

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        // 9. Stock Items (Inventory Balances in physical addresses)
        if (!await context.StockItems.AnyAsync())
        {
            var locs = await context.StorageLocations.ToListAsync();
            var products = await context.Products.ToListAsync();

            var locA1 = locs.First(l => l.Code == "A-01-01-A");
            var locA2 = locs.First(l => l.Code == "A-01-01-B");
            var locA3 = locs.First(l => l.Code == "A-01-02-A");
            var locB1 = locs.First(l => l.Code == "B-01-01-1");
            var locB2 = locs.First(l => l.Code == "B-01-01-2");
            var locB3 = locs.First(l => l.Code == "B-01-02-1");
            var locC1 = locs.First(l => l.Code == "C-01-01-A");

            var pPlc = products.First(p => p.Sku == "SKU-PLC-1200");
            var pVfd = products.First(p => p.Sku == "SKU-VFD-2200");
            var pBosch = products.First(p => p.Sku == "SKU-BOSCH-18V");
            var pRokla = products.First(p => p.Sku == "SKU-ROKLA-2500");
            var pVvg = products.First(p => p.Sku == "SKU-VVG-3X25");
            var pAbb = products.First(p => p.Sku == "SKU-ABB-3P25");
            var pWheel = products.First(p => p.Sku == "SKU-WHEEL-200P");
            var pOil = products.First(p => p.Sku == "SKU-OIL-HLP46");
            var pLed = products.First(p => p.Sku == "SKU-LED-100W");

            List<StockItem> stock =
            [
                new StockItem { ProductId = pPlc.Id, StorageLocationId = locB1.Id, Quantity = 14, BatchNumber = "B-2026-08" },
                new StockItem { ProductId = pVfd.Id, StorageLocationId = locB2.Id, Quantity = 8, BatchNumber = "B-2026-09" },
                new StockItem { ProductId = pBosch.Id, StorageLocationId = locB3.Id, Quantity = 12, BatchNumber = "BT-8842" },
                new StockItem { ProductId = pRokla.Id, StorageLocationId = locA1.Id, Quantity = 5, BatchNumber = "RK-012" },
                new StockItem { ProductId = pVvg.Id, StorageLocationId = locA2.Id, Quantity = 25, BatchNumber = "CBL-991" },
                new StockItem { ProductId = pAbb.Id, StorageLocationId = locB1.Id, Quantity = 60, BatchNumber = "ABB-441" },
                new StockItem { ProductId = pWheel.Id, StorageLocationId = locA3.Id, Quantity = 28, BatchNumber = "WH-102" },
                new StockItem { ProductId = pOil.Id, StorageLocationId = locC1.Id, Quantity = 16, BatchNumber = "AZ-46-1" },
                // Low stock alert item:
                new StockItem { ProductId = pLed.Id, StorageLocationId = locA3.Id, Quantity = 2, BatchNumber = "LED-09" }
            ];

            context.StockItems.AddRange(stock);
            await context.SaveChangesAsync();
        }

        // 10. Sample Stock Movement Documents
        if (!await context.StockMovements.AnyAsync())
        {
            var admin = await context.WarehouseStaffs.FirstAsync(s => s.Login == "admin");
            var sup1 = await context.Suppliers.FirstAsync(s => s.TaxNumber == "38472910");
            var products = await context.Products.ToListAsync();
            var locs = await context.StorageLocations.ToListAsync();

            var docReceipt = new StockMovement
            {
                DocumentNumber = "DOC-REC-2026-0001",
                Type = StockMovementType.Receipt,
                Status = StockMovementStatus.Completed,
                SupplierId = sup1.Id,
                StaffUserId = admin.Id,
                StaffLogin = admin.Login,
                Reason = "Планова поставка обладнання за договором №45-П",
                Notes = "Вантаж прийнято без зауважень, палети опломбовані",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                CompletedAt = DateTime.UtcNow.AddDays(-3)
            };

            var pPlc = products.First(p => p.Sku == "SKU-PLC-1200");
            var locB1 = locs.First(l => l.Code == "B-01-01-1");

            docReceipt.Items.Add(new StockMovementItem
            {
                ProductId = pPlc.Id,
                DestinationLocationId = locB1.Id,
                Quantity = 10,
                UnitPrice = 14500m,
                BatchNumber = "B-2026-08"
            });

            var docShipment = new StockMovement
            {
                DocumentNumber = "DOC-SHP-2026-0002",
                Type = StockMovementType.Shipment,
                Status = StockMovementStatus.Completed,
                CustomerName = "ТОВ «ТехноСервіс Захід» (Львів)",
                StaffUserId = admin.Id,
                StaffLogin = admin.Login,
                Reason = "Відвантаження за замовленням клієнта №108",
                Notes = "Доставка Новою Поштою ТТН 20450918273645",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow.AddDays(-1)
            };

            var pBosch = products.First(p => p.Sku == "SKU-BOSCH-18V");
            var locB3 = locs.First(l => l.Code == "B-01-02-1");

            docShipment.Items.Add(new StockMovementItem
            {
                ProductId = pBosch.Id,
                SourceLocationId = locB3.Id,
                Quantity = 2,
                UnitPrice = 8900m,
                BatchNumber = "BT-8842"
            });

            context.StockMovements.AddRange(docReceipt, docShipment);
            await context.SaveChangesAsync();
        }

        // 11. Initial Audit Logs
        if (!await context.AuditLogs.AnyAsync())
        {
            context.AuditLogs.AddRange(
                new AuditLog
                {
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Action = "SystemInitialize",
                    EntityName = "System",
                    StaffLogin = "system",
                    IpAddress = "127.0.0.1",
                    Details = "Ініціалізація бази даних складського комплексу та первинне завантаження довідників"
                },
                new AuditLog
                {
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Action = "StockReceiptCompleted",
                    EntityName = "StockMovement",
                    EntityId = "DOC-REC-2026-0001",
                    StaffLogin = "admin",
                    IpAddress = "127.0.0.1",
                    Details = "Проведено прибуткову накладну DOC-REC-2026-0001 на суму 145 000.00 грн"
                },
                new AuditLog
                {
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Action = "StockShipmentCompleted",
                    EntityName = "StockMovement",
                    EntityId = "DOC-SHP-2026-0002",
                    StaffLogin = "admin",
                    IpAddress = "127.0.0.1",
                    Details = "Проведено видаткову накладну DOC-SHP-2026-0002 для ТОВ «ТехноСервіс Захід»"
                }
            );

            await context.SaveChangesAsync();
        }
    }
}
