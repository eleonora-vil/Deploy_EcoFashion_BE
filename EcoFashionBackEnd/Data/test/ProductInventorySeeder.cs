using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EcoFashionBackEnd.Entities;

namespace EcoFashionBackEnd.Data.test
{
    public static class ProductInventorySeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Skip if product inventories already exist
            if (await context.ProductInventories.AnyAsync()) return;

            Console.WriteLine("Seeding ProductInventories...");

            // Get existing products to create inventories for
            var products = await context.Products
                .Include(p => p.Design)
                .Include(p => p.Size)
                .ToListAsync();

            if (!products.Any())
            {
                Console.WriteLine("No Products found. Run ProductSeeder first.");
                return;
            }

            // Get designer warehouses (product warehouses)
            var productWarehouses = await context.Warehouses
                .Where(w => w.WarehouseType == "Product" && w.IsActive)
                .ToListAsync();

            if (!productWarehouses.Any())
            {
                Console.WriteLine("No Product warehouses found. Creating default warehouse...");
                
                // Create a default product warehouse
                var defaultWarehouse = new Warehouse
                {
                    Name = "Kho Sản Phẩm Mặc Định",
                    WarehouseType = "Product",
                    IsDefault = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                };
                context.Warehouses.Add(defaultWarehouse);
                await context.SaveChangesAsync();
                
                productWarehouses.Add(defaultWarehouse);
            }

            var random = new Random();
            var inventories = new List<ProductInventory>();

            foreach (var product in products)
            {
                // Create inventory for each product in available warehouses
                foreach (var warehouse in productWarehouses.Take(2)) // Use max 2 warehouses per product
                {
                    // Generate realistic stock levels based on product design popularity
                    var baseQuantity = random.Next(10, 100); // Base stock 10-100 units
                    
                    // Adjust quantity based on some factors to make it more realistic
                    var designPopularityFactor = (product.DesignId % 3) + 1; // 1-3 multiplier
                    var sizePopularityFactor = product.Size.SizeName switch
                    {
                        "S" => 0.8,
                        "M" => 1.2, // M size is most popular
                        "L" => 1.0,
                        "XL" => 0.7,
                        _ => 1.0
                    };
                    
                    var finalQuantity = (int)(baseQuantity * designPopularityFactor * sizePopularityFactor);
                    finalQuantity = Math.Max(5, finalQuantity); // Minimum 5 units

                    inventories.Add(new ProductInventory
                    {
                        ProductId = product.ProductId,
                        WarehouseId = warehouse.WarehouseId,
                        QuantityAvailable = finalQuantity,
                        LastUpdated = DateTime.UtcNow.AddDays(-random.Next(1, 30)) // Updated in last 30 days
                    });
                }
            }

            await context.ProductInventories.AddRangeAsync(inventories);
            await context.SaveChangesAsync();

            Console.WriteLine($"Seeded {inventories.Count} ProductInventories successfully!");
            
            // Log some statistics
            var totalProducts = inventories.Count;
            var totalStock = inventories.Sum(i => i.QuantityAvailable);
            var averageStock = totalStock / totalProducts;
            
            Console.WriteLine($"Product Inventory Statistics:");
            Console.WriteLine($"- Total Product Inventories: {totalProducts}");
            Console.WriteLine($"- Total Stock Units: {totalStock}");
            Console.WriteLine($"- Average Stock per Product: {averageStock}");
            Console.WriteLine($"- Warehouses Used: {productWarehouses.Count}");
        }
    }
}