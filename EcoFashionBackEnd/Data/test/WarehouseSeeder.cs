using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoFashionBackEnd.Data.test
{
    public static class WarehouseSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.Warehouses.AnyAsync()) return;

            Console.WriteLine("Seeding Warehouses...");

            var warehouses = new List<Warehouse>();

            // Get first supplier for supplier warehouses
            var supplier = await context.Suppliers.FirstOrDefaultAsync();
            if (supplier != null)
            {
                warehouses.AddRange(new[]
                {
                    new Warehouse
                    {
                        Name = "Kho Chính - Nguyên Liệu",
                        WarehouseType = "Material",
                        SupplierId = supplier.SupplierId,
                        IsDefault = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-60)
                    },
                    new Warehouse
                    {
                        Name = "Kho Phụ - Nguyên Liệu",
                        WarehouseType = "Material", 
                        SupplierId = supplier.SupplierId,
                        IsDefault = false,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-45)
                    }
                });
            }

            // Get first designer for designer warehouses
            var designer = await context.Designers.FirstOrDefaultAsync();
            if (designer != null)
            {
                warehouses.AddRange(new[]
                {
                    new Warehouse
                    {
                        Name = "Kho Designer - Nguyên Liệu",
                        WarehouseType = "Material",
                        DesignerId = designer.DesignerId,
                        IsDefault = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-30)
                    },
                    new Warehouse
                    {
                        Name = "Kho Designer - Sản Phẩm",
                        WarehouseType = "Product",
                        DesignerId = designer.DesignerId,
                        IsDefault = false,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    }
                });
            }

            // Add general warehouses
            warehouses.AddRange(new[]
            {
                new Warehouse
                {
                    Name = "Kho Trung Tâm",
                    WarehouseType = "Material",
                    IsDefault = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-90)
                },
                new Warehouse
                {
                    Name = "Kho Sản Phẩm Hoàn Thiện",
                    WarehouseType = "Product", 
                    IsDefault = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-75)
                }
            });

            await context.Warehouses.AddRangeAsync(warehouses);
            await context.SaveChangesAsync();

            Console.WriteLine($"Seeded {warehouses.Count} Warehouses successfully!");
        }
    }

}
