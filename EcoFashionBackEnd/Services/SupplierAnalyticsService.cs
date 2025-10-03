using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EcoFashionBackEnd.Services
{
    public class SupplierAnalyticsService
    {
        private readonly AppDbContext _context;

        public SupplierAnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SupplierRevenueAnalyticsDto> GetSupplierRevenueAnalyticsAsync(int supplierUserId, SupplierRevenueRequestDto request)
        {
            // Set default date range if not provided (use date-only for consistent comparison)
            var endDate = request.EndDate?.Date ?? DateTime.Now.Date;
            var startDate = request.StartDate?.Date ?? endDate.AddDays(-30);

            // Convert to end of day for inclusive range
            var endDateTime = endDate.AddDays(1).AddTicks(-1); // End of endDate

            // Get supplier wallet
            var supplierWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == supplierUserId);

            if (supplierWallet == null)
            {
                return new SupplierRevenueAnalyticsDto
                {
                    RevenuePoints = new List<SupplierRevenuePointDto>(),
                    TotalRevenue = 0,
                    TotalOrders = 0,
                    Period = request.Period ?? "daily",
                    StartDate = startDate,
                    EndDate = endDate
                };
            }

            // Get revenue transactions (Transfer with positive amount = supplier receives money from admin after order delivered)
            var revenueTransactions = await _context.WalletTransactions
                .Where(t => t.WalletId == supplierWallet.WalletId
                           && t.Type == TransactionType.Transfer
                           && t.Amount > 0 // Positive amount means money received
                           && t.OrderId.HasValue // Only revenue from order settlements
                           && t.CreatedAt >= startDate
                           && t.CreatedAt <= endDateTime)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            var revenuePoints = new List<SupplierRevenuePointDto>();

            switch (request.Period.ToLower())
            {
                case "daily":
                    revenuePoints = revenueTransactions
                        .GroupBy(t => t.CreatedAt.Date)
                        .Select(g => new SupplierRevenuePointDto
                        {
                            Date = g.Key,
                            Revenue = (decimal)g.Sum(t => t.Amount),
                            OrderCount = g.Count()
                        })
                        .OrderBy(p => p.Date)
                        .ToList();
                    break;

                case "weekly":
                    revenuePoints = revenueTransactions
                        .GroupBy(t => GetWeekStart(t.CreatedAt))
                        .Select(g => new SupplierRevenuePointDto
                        {
                            Date = g.Key,
                            Revenue = (decimal)g.Sum(t => t.Amount),
                            OrderCount = g.Count()
                        })
                        .OrderBy(p => p.Date)
                        .ToList();
                    break;

                case "monthly":
                    revenuePoints = revenueTransactions
                        .GroupBy(t => new DateTime(t.CreatedAt.Year, t.CreatedAt.Month, 1))
                        .Select(g => new SupplierRevenuePointDto
                        {
                            Date = g.Key,
                            Revenue = (decimal)g.Sum(t => t.Amount),
                            OrderCount = g.Count()
                        })
                        .OrderBy(p => p.Date)
                        .ToList();
                    break;
            }

            // Fill missing periods with zero values
            revenuePoints = FillMissingPeriods(revenuePoints, startDate, endDate, request.Period ?? "daily");

            return new SupplierRevenueAnalyticsDto
            {
                RevenuePoints = revenuePoints,
                TotalRevenue = (decimal)revenueTransactions.Sum(t => t.Amount),
                TotalOrders = revenueTransactions.Count,
                Period = request.Period ?? "daily",
                StartDate = startDate,
                EndDate = endDate
            };
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private List<SupplierRevenuePointDto> FillMissingPeriods(List<SupplierRevenuePointDto> points, DateTime startDate, DateTime endDate, string period)
        {
            var filledPoints = new List<SupplierRevenuePointDto>();
            var currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                var periodStart = period.ToLower() switch
                {
                    "weekly" => GetWeekStart(currentDate),
                    "monthly" => new DateTime(currentDate.Year, currentDate.Month, 1),
                    _ => currentDate
                };

                var existingPoint = points.FirstOrDefault(p => p.Date == periodStart);
                if (existingPoint != null)
                {
                    filledPoints.Add(existingPoint);
                }
                else
                {
                    filledPoints.Add(new SupplierRevenuePointDto
                    {
                        Date = periodStart,
                        Revenue = 0,
                        OrderCount = 0
                    });
                }

                currentDate = period.ToLower() switch
                {
                    "weekly" => currentDate.AddDays(7),
                    "monthly" => currentDate.AddMonths(1),
                    _ => currentDate.AddDays(1)
                };
            }

            return filledPoints.DistinctBy(p => p.Date).OrderBy(p => p.Date).ToList();
        }
    }
}