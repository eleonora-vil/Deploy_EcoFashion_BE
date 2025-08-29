//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using EcoFashionBackEnd.Entities;

//namespace EcoFashionBackEnd.Data.test
//{
//    public static class ProductInventoryTransactionSeeder
//    {
//        public static async Task SeedAsync(AppDbContext context)
//        {
//            // Skip if transactions already exist
//            if (await context.ProductInventoryTransactions.AnyAsync()) return;

//            Console.WriteLine("Seeding ProductInventoryTransactions...");

//            // Get existing product inventories to create transactions for
//            var inventories = await context.ProductInventories
//                .Include(pi => pi.Product)
//                .ThenInclude(p => p.Design)
//                .Include(pi => pi.Product)
//                .ThenInclude(p => p.Size)
//                .Include(pi => pi.Warehouse)
//                .ToListAsync();

//            if (!inventories.Any())
//            {
//                Console.WriteLine("No ProductInventories found. Run ProductInventorySeeder first.");
//                return;
//            }

//            // Get a user to assign as transaction performer
//            var user = await context.Users.FirstOrDefaultAsync();
//            if (user == null)
//            {
//                Console.WriteLine("No Users found. Transactions will be created without user assignment.");
//                return;
//            }

//            var random = new Random();
//            var transactions = new List<ProductInventoryTransaction>();

//            // Create transaction history for the last 90 days
//            var startDate = DateTime.UtcNow.AddDays(-90);
//            var endDate = DateTime.UtcNow;

//            // Transaction types for products
//            var transactionTypes = new (string, string, bool?)[]
//            {
//                ("Production", "Sản xuất hoàn thành", true),      // +
//                ("Sale", "Bán hàng", false),                      // -
//                ("Return", "Trả hàng", true),                     // +
//                ("Damage", "Hỏng hóc", false),                   // -
//                ("Adjustment", "Điều chỉnh", null),              // +/-
//                ("Quality_Check", "Kiểm tra chất lượng", false), // -
//                ("Restock", "Bổ sung tồn kho", true)             // +
//            };

//            foreach (var inventory in inventories)
//            {
//                var currentStock = 0; // Start with 0 and build up to current quantity
//                var targetStock = inventory.QuantityAvailable;
//                var currentDate = startDate;

//                // Create initial production transaction
//                var initialProduction = Math.Max(targetStock / 2, 10); // Start with at least half the target stock
//                transactions.Add(new ProductInventoryTransaction
//                {
//                    InventoryId = inventory.InventoryId,
//                    PerformedByUserId = user.UserId,
//                    QuantityChanged = initialProduction,
//                    TransactionDate = currentDate,
//                    TransactionType = "Production",
//                    Notes = $"Sản xuất ban đầu cho {inventory.Product.Design.Name} - {inventory.Product.Size.SizeName}"
//                });
//                currentStock += initialProduction;

//                // Create 5-15 transactions over time for each inventory
//                var transactionCount = random.Next(5, 16);
//                for (int i = 0; i < transactionCount; i++)
//                {
//                    currentDate = currentDate.AddDays(random.Next(3, 10)); // 3-10 days between transactions
//                    if (currentDate > endDate) break;

//                    var (transactionType, description, isPositive) = transactionTypes[random.Next(transactionTypes.Length)];
//                    int quantityChange;

//                    if (isPositive == true) // Positive transaction (increase stock)
//                    {
//                        quantityChange = random.Next(5, 25); // Add 5-25 units
//                    }
//                    else if (isPositive == false) // Negative transaction (decrease stock)
//                    {
//                        var maxDecrease = Math.Min(currentStock, random.Next(1, 15)); // Remove 1-15 units but not more than current stock
//                        quantityChange = -maxDecrease;
//                    }
//                    else // Adjustment can be positive or negative
//                    {
//                        quantityChange = random.Next(-10, 11); // -10 to +10
//                        if (currentStock + quantityChange < 0)
//                        {
//                            quantityChange = -currentStock; // Don't go below 0
//                        }
//                    }

//                    transactions.Add(new ProductInventoryTransaction
//                    {
//                        InventoryId = inventory.InventoryId,
//                        PerformedByUserId = user.UserId,
//                        QuantityChanged = quantityChange,
//                        TransactionDate = currentDate,
//                        TransactionType = transactionType,
//                        Notes = $"{description} - {inventory.Product.Design.Name} ({inventory.Product.SKU})"
//                    });

//                    currentStock += quantityChange;
//                }

//                // Final adjustment to match current inventory quantity
//                if (currentStock != targetStock)
//                {
//                    var finalAdjustment = targetStock - currentStock;
//                    transactions.Add(new ProductInventoryTransaction
//                    {
//                        InventoryId = inventory.InventoryId,
//                        PerformedByUserId = user.UserId,
//                        QuantityChanged = finalAdjustment,
//                        TransactionDate = endDate.AddDays(-1),
//                        TransactionType = "Adjustment",
//                        Notes = $"Điều chỉnh cuối để khớp tồn kho hiện tại - {inventory.Product.Design.Name}"
//                    });
//                }
//            }

//            // Add recent transactions (last 7 days) for better demo
//            var recentInventories = inventories.Take(10).ToList(); // Use first 10 inventories for recent activity
            
//            for (int day = 7; day >= 1; day--)
//            {
//                var transactionDate = DateTime.UtcNow.AddDays(-day);
                
//                // Create 1-3 transactions per day
//                var dailyTransactionCount = random.Next(1, 4);
//                for (int i = 0; i < dailyTransactionCount; i++)
//                {
//                    var inventory = recentInventories[random.Next(recentInventories.Count)];
//                    var (transactionType, description, isPositive) = transactionTypes[random.Next(transactionTypes.Length)];
                    
//                    int quantityChange = isPositive == true 
//                        ? random.Next(1, 10)  // Small positive change
//                        : isPositive == false 
//                        ? -random.Next(1, 5)  // Small negative change
//                        : random.Next(-3, 4); // Small adjustment

//                    // Ensure we don't create impossible negative stock
//                    if (quantityChange < 0 && Math.Abs(quantityChange) > inventory.QuantityAvailable)
//                    {
//                        quantityChange = -inventory.QuantityAvailable;
//                    }

//                    transactions.Add(new ProductInventoryTransaction
//                    {
//                        InventoryId = inventory.InventoryId,
//                        PerformedByUserId = user.UserId,
//                        QuantityChanged = quantityChange,
//                        TransactionDate = transactionDate.AddHours(random.Next(8, 18)).AddMinutes(random.Next(0, 60)),
//                        TransactionType = transactionType,
//                        Notes = $"Hoạt động gần đây - {description} - {inventory.Product.Design.Name}"
//                    });
//                }
//            }

//            await context.ProductInventoryTransactions.AddRangeAsync(transactions);
//            await context.SaveChangesAsync();

//            Console.WriteLine($"Seeded {transactions.Count} ProductInventoryTransactions successfully!");
            
//            // Log statistics
//            var productionCount = transactions.Count(t => t.TransactionType == "Production");
//            var salesCount = transactions.Count(t => t.TransactionType == "Sale");
//            var totalPositive = transactions.Where(t => t.QuantityChanged > 0).Sum(t => t.QuantityChanged);
//            var totalNegative = transactions.Where(t => t.QuantityChanged < 0).Sum(t => Math.Abs(t.QuantityChanged));
            
//            Console.WriteLine($"Product Transaction Statistics:");
//            Console.WriteLine($"- Total Transactions: {transactions.Count}");
//            Console.WriteLine($"- Production Transactions: {productionCount}");
//            Console.WriteLine($"- Sales Transactions: {salesCount}");
//            Console.WriteLine($"- Total Units Produced: {totalPositive}");
//            Console.WriteLine($"- Total Units Sold: {totalNegative}");
//        }
//    }
//}
