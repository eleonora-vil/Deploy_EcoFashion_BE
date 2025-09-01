using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;

public static class ProductInventorySeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.ProductInventories.AnyAsync()) return;

        var products = await context.Products.ToListAsync();
        var warehouses = await context.Warehouses
            .Where(w => w.WarehouseType == "Product")
            .ToListAsync();

        if (!warehouses.Any()) throw new Exception("Không tìm thấy kho Product.");

        var defaultWarehouse = warehouses.First();
        var inventories = new List<ProductInventory>();
        var transactions = new List<ProductInventoryTransaction>();
        var random = new Random();

        foreach (var product in products)
        {
            var quantity = random.Next(5, 20);

            var inventory = new ProductInventory
            {
                ProductId = product.ProductId,
                WarehouseId = defaultWarehouse.WarehouseId,
                QuantityAvailable = quantity,
                LastUpdated = DateTime.UtcNow
            };

            inventories.Add(inventory);

            // log transaction
            transactions.Add(new ProductInventoryTransaction
            {
                ProductInventory = inventory,
                QuantityChanged = quantity,
                PerformedByUserId = 2,
                BeforeQty = 0,
                AfterQty = quantity,
                TransactionType = "Import",
                Notes = "Seed initial stock"
            });
        }

        await context.ProductInventories.AddRangeAsync(inventories);
        await context.ProductInventoryTransactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();
    }
}
