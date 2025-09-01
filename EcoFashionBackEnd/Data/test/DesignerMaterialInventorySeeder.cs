using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;

public static class DesignerMaterialInventorySeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.DesignerMaterialInventories.AnyAsync()) return;

        var designer = await context.Designers.FirstOrDefaultAsync();
        var materials = await context.Materials.ToListAsync();
        var materialWarehouse = await context.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseType == "Material");

        if (designer == null || materials.Count == 0 || materialWarehouse == null) return;

        var random = new Random();
        var inventoryList = new List<DesignerMaterialInventory>();
        var transactionList = new List<MaterialInventoryTransaction>();

        foreach (var material in materials)
        {
            var quantity = random.Next(6000, 9000);
            var inventory = new DesignerMaterialInventory
            {
                WarehouseId = materialWarehouse.WarehouseId,
                MaterialId = material.MaterialId,
                Quantity = quantity,
                Cost = (decimal)material.PricePerUnit * quantity,
                LastBuyDate = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                Status = "In Stock"
            };

            inventoryList.Add(inventory);

            // Seed log transaction ngay khi tạo inventory
            transactionList.Add(new MaterialInventoryTransaction
            {
                MaterialInventory = inventory,
                PerformedByUserId = designer.UserId,
                QuantityChanged = quantity,
                BeforeQty = 0,
                AfterQty = quantity,
                TransactionType = "Initial Stock",
                Notes = $"Seeded {quantity} units for material {material.Name}",
                TransactionDate = DateTime.UtcNow
            });
        }

        await context.DesignerMaterialInventories.AddRangeAsync(inventoryList);
        await context.SaveChangesAsync();

        await context.MaterialInventoryTransactions.AddRangeAsync(transactionList);
        await context.SaveChangesAsync();
    }
}
