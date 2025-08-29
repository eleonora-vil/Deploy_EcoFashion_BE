using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EcoFashionBackEnd.Entities;

namespace EcoFashionBackEnd.Data.test
{
    public static class MaterialStockTransactionSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Skip if transactions already exist
            if (await context.MaterialStockTransactions.AnyAsync()) return;

            Console.WriteLine("Seeding MaterialStockTransactions...");

            // Get existing material stocks to create transactions for
            var materialStocks = await context.MaterialStocks
                .Include(ms => ms.Material)
                .Include(ms => ms.Warehouse)
                .ToListAsync();

            if (!materialStocks.Any())
            {
                Console.WriteLine("No MaterialStocks found. Run MaterialInventorySeeder first.");
                return;
            }

            var random = new Random();
            var transactions = new List<MaterialStockTransaction>();

            // Create realistic transaction history for the last 90 days
            var startDate = DateTime.UtcNow.AddDays(-90);
            var endDate = DateTime.UtcNow;

            foreach (var stock in materialStocks)
            {
                var currentQty = 0m; // Start with 0 and build up
                var currentDate = startDate;

                // Create initial stock receipt (SupplierReceipt)
                var initialQty = stock.QuantityOnHand * 0.3m; // Start with 30% of current stock
                transactions.Add(new MaterialStockTransaction
                {
                    MaterialId = stock.MaterialId,
                    WarehouseId = stock.WarehouseId,
                    TransactionType = MaterialTransactionType.SupplierReceipt,
                    QuantityChange = initialQty,
                    BeforeQty = currentQty,
                    AfterQty = currentQty + initialQty,
                    Unit = "mét",
                    ReferenceType = "PurchaseOrder",
                    ReferenceId = $"PO-{random.Next(1000, 9999)}",
                    Note = $"Nhập kho ban đầu từ nhà cung cấp",
                    CreatedAt = currentDate
                });
                currentQty += initialQty;

                // Create additional transactions over time
                var transactionCount = random.Next(3, 8); // 3-7 transactions per material
                for (int i = 0; i < transactionCount; i++)
                {
                    currentDate = currentDate.AddDays(random.Next(5, 15)); // 5-15 days between transactions
                    if (currentDate > endDate) break;

                    var transactionTypes = new[]
                    {
                        MaterialTransactionType.SupplierReceipt,
                        MaterialTransactionType.CustomerSale,
                        MaterialTransactionType.ProductionUse,
                        MaterialTransactionType.ManualAdjustment
                    };

                    var transactionType = transactionTypes[random.Next(transactionTypes.Length)];
                    var beforeQty = currentQty;
                    decimal quantityChange;
                    string note;
                    string referenceType;
                    string referenceId;

                    switch (transactionType)
                    {
                        case MaterialTransactionType.SupplierReceipt:
                            quantityChange = random.Next(50, 200);
                            note = "Nhập thêm hàng từ nhà cung cấp";
                            referenceType = "PurchaseOrder";
                            referenceId = $"PO-{random.Next(1000, 9999)}";
                            break;

                        case MaterialTransactionType.CustomerSale:
                            quantityChange = -Math.Min(currentQty * 0.3m, random.Next(10, 50));
                            note = "Bán hàng cho khách hàng";
                            referenceType = "SalesOrder";
                            referenceId = $"SO-{random.Next(1000, 9999)}";
                            break;

                        case MaterialTransactionType.ProductionUse:
                            quantityChange = -Math.Min(currentQty * 0.2m, random.Next(5, 30));
                            note = "Sử dụng cho sản xuất";
                            referenceType = "ProductionOrder";
                            referenceId = $"PROD-{random.Next(1000, 9999)}";
                            break;

                        case MaterialTransactionType.ManualAdjustment:
                            quantityChange = random.Next(-20, 20);
                            note = quantityChange > 0 ? "Điều chỉnh tăng kho" : "Điều chỉnh giảm kho";
                            referenceType = "Manual";
                            referenceId = $"ADJ-{random.Next(1000, 9999)}";
                            break;

                        default:
                            continue;
                    }

                    // Ensure we don't go below 0
                    if (currentQty + quantityChange < 0)
                    {
                        quantityChange = -currentQty;
                    }

                    var afterQty = currentQty + quantityChange;

                    transactions.Add(new MaterialStockTransaction
                    {
                        MaterialId = stock.MaterialId,
                        WarehouseId = stock.WarehouseId,
                        TransactionType = transactionType,
                        QuantityChange = quantityChange,
                        BeforeQty = beforeQty,
                        AfterQty = afterQty,
                        Unit = "mét",
                        ReferenceType = referenceType,
                        ReferenceId = referenceId,
                        Note = note,
                        CreatedAt = currentDate
                    });

                    currentQty = afterQty;
                }

                // Final adjustment to match current stock quantity
                if (currentQty != stock.QuantityOnHand)
                {
                    var finalAdjustment = stock.QuantityOnHand - currentQty;
                    transactions.Add(new MaterialStockTransaction
                    {
                        MaterialId = stock.MaterialId,
                        WarehouseId = stock.WarehouseId,
                        TransactionType = MaterialTransactionType.ManualAdjustment,
                        QuantityChange = finalAdjustment,
                        BeforeQty = currentQty,
                        AfterQty = stock.QuantityOnHand,
                        Unit = "mét",
                        ReferenceType = "Manual",
                        ReferenceId = $"ADJ-FINAL-{random.Next(1000, 9999)}",
                        Note = "Điều chỉnh cuối để khớp tồn kho hiện tại",
                        CreatedAt = endDate.AddDays(-1)
                    });
                }
            }

            // Add recent transactions (last 7 days) for better demo
            var recentTransactions = new List<MaterialStockTransaction>();
            var recentStocks = materialStocks.Take(5).ToList(); // Use first 5 materials for recent activity

            for (int day = 7; day >= 1; day--)
            {
                var transactionDate = DateTime.UtcNow.AddDays(-day);
                
                foreach (var stock in recentStocks.Take(random.Next(1, 3))) // 1-2 materials per day
                {
                    var transactionType = day % 2 == 0 
                        ? MaterialTransactionType.SupplierReceipt 
                        : MaterialTransactionType.CustomerSale;

                    var quantityChange = transactionType == MaterialTransactionType.SupplierReceipt
                        ? random.Next(20, 100)
                        : -random.Next(10, 50);

                    var currentStock = await context.MaterialStocks
                        .FirstOrDefaultAsync(ms => ms.MaterialId == stock.MaterialId && ms.WarehouseId == stock.WarehouseId);

                    if (currentStock != null)
                    {
                        var beforeQty = (int)currentStock.QuantityOnHand;
                        var afterQty = beforeQty + quantityChange;

                        // Ensure we don't go below 0
                        if (afterQty < 0)
                        {
                            quantityChange = -beforeQty;
                            afterQty = 0;
                        }

                        recentTransactions.Add(new MaterialStockTransaction
                        {
                            MaterialId = stock.MaterialId,
                            WarehouseId = stock.WarehouseId,
                            TransactionType = transactionType,
                            QuantityChange = quantityChange,
                            BeforeQty = beforeQty,
                            AfterQty = afterQty,
                            Unit = "mét",
                            ReferenceType = transactionType == MaterialTransactionType.SupplierReceipt ? "PurchaseOrder" : "SalesOrder",
                            ReferenceId = $"{(transactionType == MaterialTransactionType.SupplierReceipt ? "PO" : "SO")}-{random.Next(1000, 9999)}",
                            Note = transactionType == MaterialTransactionType.SupplierReceipt 
                                ? "Nhập kho gần đây" 
                                : "Bán hàng gần đây",
                            CreatedAt = transactionDate.AddHours(random.Next(8, 17)).AddMinutes(random.Next(0, 59))
                        });
                    }
                }
            }

            // Add all transactions
            transactions.AddRange(recentTransactions);

            await context.MaterialStockTransactions.AddRangeAsync(transactions);
            await context.SaveChangesAsync();

            Console.WriteLine($"Seeded {transactions.Count} MaterialStockTransactions successfully!");
        }
    }
}