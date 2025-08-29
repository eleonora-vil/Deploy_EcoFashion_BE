using EcoFashionBackEnd.Dtos.Warehouse;
using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoFashionBackEnd.Services
{
    public class ProductInventoryAnalyticsService
    {
        private readonly AppDbContext _context;

        public ProductInventoryAnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProductSummaryDto> GetSummaryAsync(DateTime? from = null, DateTime? to = null, Guid? designerId = null)
        {
            var query = _context.ProductInventories
                .Include(pi => pi.Product)
                .ThenInclude(p => p.Design)
                .AsQueryable();

            if (designerId.HasValue)
            {
                query = query.Where(pi => pi.Product.Design.DesignerId == designerId.Value);
            }

            var inventories = await query.ToListAsync();

            // Calculate summary statistics
            var totalProducts = inventories.Select(pi => pi.ProductId).Distinct().Count();
            var totalCompleted = inventories.Sum(pi => pi.QuantityAvailable);
            
            // For demo purposes, assume 20% of products are "in production"
            var totalInProduction = (int)(totalCompleted * 0.2);
            
            // Calculate total inventory value (quantity * product price)
            var totalInventoryValue = await _context.ProductInventories
                .Include(pi => pi.Product)
                .Where(pi => designerId == null || pi.Product.Design.DesignerId == designerId.Value)
                .SumAsync(pi => pi.QuantityAvailable * pi.Product.Price);

            // Low stock threshold: products with less than 10 units
            var lowStockCount = inventories.Count(pi => pi.QuantityAvailable < 10);

            return new ProductSummaryDto
            {
                TotalProducts = totalProducts,
                TotalCompleted = totalCompleted,
                TotalInProduction = totalInProduction,
                TotalInventoryValue = totalInventoryValue,
                LowStockCount = lowStockCount
            };
        }

        public async Task<List<ProductLowStockItemDto>> GetLowStockItemsAsync(int limit = 20, Guid? designerId = null)
        {
            var query = _context.ProductInventories
                .Include(pi => pi.Product)
                .ThenInclude(p => p.Design)
                .Include(pi => pi.Product)
                .ThenInclude(p => p.Size)
                .Include(pi => pi.Warehouse)
                .Where(pi => pi.QuantityAvailable < 10) // Low stock threshold
                .AsQueryable();

            if (designerId.HasValue)
            {
                query = query.Where(pi => pi.Product.Design.DesignerId == designerId.Value);
            }

            var lowStockItems = await query
                .OrderBy(pi => pi.QuantityAvailable)
                .Take(limit)
                .ToListAsync();

            return lowStockItems.Select(pi => new ProductLowStockItemDto
            {
                ProductId = pi.ProductId,
                ProductName = $"{pi.Product.Design.Name} - {pi.Product.Size.SizeName} - {pi.Product.ColorCode}",
                SKU = pi.Product.SKU,
                WarehouseId = pi.WarehouseId,
                WarehouseName = pi.Warehouse.Name,
                QuantityAvailable = pi.QuantityAvailable,
                MinThreshold = 10, // Standard threshold
                Difference = pi.QuantityAvailable - 10,
                EstimatedValue = pi.QuantityAvailable * pi.Product.Price,
                LastUpdated = pi.LastUpdated,
                DesignName = pi.Product.Design.Name,
                SizeName = pi.Product.Size.SizeName,
                ColorCode = pi.Product.ColorCode
            }).ToList();
        }

        public async Task<List<ProductTransactionDto>> GetTransactionsAsync(DateTime? from = null, DateTime? to = null, Guid? designerId = null, int? productId = null, int limit = 50)
        {
            var query = _context.ProductInventoryTransactions
                .Include(pit => pit.ProductInventory)
                .ThenInclude(pi => pi.Product)
                .ThenInclude(p => p.Design)
                .Include(pit => pit.ProductInventory)
                .ThenInclude(pi => pi.Product)
                .ThenInclude(p => p.Size)
                .Include(pit => pit.ProductInventory)
                .ThenInclude(pi => pi.Warehouse)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(pit => pit.TransactionDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(pit => pit.TransactionDate <= to.Value);
            }

            if (designerId.HasValue)
            {
                query = query.Where(pit => pit.ProductInventory.Product.Design.DesignerId == designerId.Value);
            }

            if (productId.HasValue)
            {
                query = query.Where(pit => pit.ProductInventory.ProductId == productId.Value);
            }

            var transactions = await query
                .OrderByDescending(pit => pit.TransactionDate)
                .Take(limit)
                .ToListAsync();

            return transactions.Select(pit => new ProductTransactionDto
            {
                TransactionId = pit.TransactionId,
                ProductId = pit.ProductInventory.ProductId,
                WarehouseId = pit.ProductInventory.WarehouseId,
                TransactionType = pit.TransactionType,
                QuantityChanged = pit.QuantityChanged,
                TransactionDate = pit.TransactionDate,
                Notes = pit.Notes,
                ProductName = $"{pit.ProductInventory.Product.Design.Name} - {pit.ProductInventory.Product.Size.SizeName} - {pit.ProductInventory.Product.ColorCode}",
                SKU = pit.ProductInventory.Product.SKU,
                WarehouseName = pit.ProductInventory.Warehouse.Name,
                DesignName = pit.ProductInventory.Product.Design.Name,
                SizeName = pit.ProductInventory.Product.Size.SizeName,
                ColorCode = pit.ProductInventory.Product.ColorCode
            }).ToList();
        }

        public async Task<List<ProductActivityPointDto>> GetProductionActivityAsync(DateTime? from = null, DateTime? to = null, Guid? designerId = null)
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            var query = _context.ProductInventoryTransactions
                .Include(pit => pit.ProductInventory)
                .ThenInclude(pi => pi.Product)
                .ThenInclude(p => p.Design)
                .Where(pit => pit.TransactionDate >= fromDate && pit.TransactionDate <= toDate)
                .AsQueryable();

            if (designerId.HasValue)
            {
                query = query.Where(pit => pit.ProductInventory.Product.Design.DesignerId == designerId.Value);
            }

            var transactions = await query.ToListAsync();

            // Group by date and design, calculate production and sales
            var activityData = transactions
                .GroupBy(pit => new { 
                    Date = pit.TransactionDate.Date,
                    DesignName = pit.ProductInventory.Product.Design.Name
                })
                .Select(g => new ProductActivityPointDto
                {
                    Date = g.Key.Date,
                    DesignName = g.Key.DesignName,
                    Produced = g.Where(t => t.TransactionType == "Production" || t.TransactionType == "Restock")
                             .Sum(t => Math.Max(0, t.QuantityChanged)),
                    Sold = g.Where(t => t.TransactionType == "Sale")
                         .Sum(t => Math.Abs(Math.Min(0, t.QuantityChanged)))
                })
                .Where(a => a.Produced > 0 || a.Sold > 0)
                .OrderBy(a => a.Date)
                .ToList();

            return activityData;
        }

        public async Task<List<ProductActivityPointDto>> GetDesignPopularityAsync(DateTime? from = null, DateTime? to = null)
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            var salesData = await _context.ProductInventoryTransactions
                .Include(pit => pit.ProductInventory)
                .ThenInclude(pi => pi.Product)
                .ThenInclude(p => p.Design)
                .Where(pit => pit.TransactionDate >= fromDate && 
                            pit.TransactionDate <= toDate &&
                            pit.TransactionType == "Sale" &&
                            pit.QuantityChanged < 0)
                .GroupBy(pit => pit.ProductInventory.Product.Design.Name)
                .Select(g => new ProductActivityPointDto
                {
                    Date = DateTime.UtcNow, // For grouping purposes
                    DesignName = g.Key,
                    Sold = g.Sum(t => Math.Abs(t.QuantityChanged)),
                    Produced = 0
                })
                .OrderByDescending(a => a.Sold)
                .Take(10)
                .ToListAsync();

            return salesData;
        }
    }
}