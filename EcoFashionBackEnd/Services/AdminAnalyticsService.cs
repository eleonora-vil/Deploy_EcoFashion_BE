using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EcoFashionBackEnd.Services
{
    public class AdminAnalyticsService
    {
        private readonly IRepository<WalletTransaction, int> _transactionRepository;
        private readonly IRepository<Wallet, int> _walletRepository;
        private readonly IRepository<Order, int> _orderRepository;
        private readonly IConfiguration _configuration;

        public AdminAnalyticsService(
            IRepository<WalletTransaction, int> transactionRepository,
            IRepository<Wallet, int> walletRepository,
            IRepository<Order, int> orderRepository,
            IConfiguration configuration)
        {
            _transactionRepository = transactionRepository;
            _walletRepository = walletRepository;
            _orderRepository = orderRepository;
            _configuration = configuration;
        }

        public async Task<AdminRevenueAnalyticsDto> GetAdminRevenueAnalyticsAsync(AdminRevenueRequestDto request)
        {
            // Set default date range if not provided
            var endDate = request.EndDate?.Date ?? DateTime.Now.Date;
            var startDate = request.StartDate?.Date ?? endDate.AddDays(-30);

            // Convert to end of day for inclusive range
            var endDateTime = endDate.AddDays(1).AddTicks(-1);

            // Get admin wallet
            var adminUserId = _configuration.GetValue<int>("AdminUserId", 1);
            var adminWallet = await _walletRepository
                .FindByCondition(w => w.UserId == adminUserId)
                .FirstOrDefaultAsync();

            if (adminWallet == null)
            {
                return new AdminRevenueAnalyticsDto
                {
                    RevenuePoints = new List<AdminRevenuePointDto>(),
                    TotalRevenue = 0,
                    TotalOrders = 0,
                    Period = request.Period ?? "daily",
                    StartDate = startDate,
                    EndDate = endDate
                };
            }

            // Get all PaymentReceived transactions in date range
            var paymentReceivedTransactions = await _transactionRepository
                .FindByCondition(t => t.WalletId == adminWallet.WalletId
                                   && t.Type == TransactionType.PaymentReceived
                                   && t.Status == TransactionStatus.Success
                                   && (t.OrderId.HasValue || t.OrderGroupId.HasValue)
                                   && t.CreatedAt >= startDate
                                   && t.CreatedAt <= endDateTime)
                .ToListAsync();

            // Get all Transfer transactions (admin pays sellers) in date range
            var transferTransactions = await _transactionRepository
                .FindByCondition(t => t.WalletId == adminWallet.WalletId
                                   && t.Type == TransactionType.Transfer
                                   && t.Amount < 0 // Negative means money going out
                                   && t.Status == TransactionStatus.Success
                                   && t.OrderId.HasValue
                                   && t.CreatedAt >= startDate
                                   && t.CreatedAt <= endDateTime)
                .ToListAsync();

            var revenueData = new List<dynamic>();

            // Process order groups
            var groupPayments = paymentReceivedTransactions.Where(t => t.OrderGroupId.HasValue).ToList();
            foreach (var payment in groupPayments)
            {
                var groupId = payment.OrderGroupId!.Value;

                // Get all OrderIds in this group
                var orderIdsInGroup = await _orderRepository
                    .FindByCondition(o => o.OrderGroupId == groupId)
                    .Select(o => o.OrderId)
                    .ToListAsync();

                // Sum transfers (negative amounts) for all orders in this group
                var totalTransfer = transferTransactions
                    .Where(w => orderIdsInGroup.Contains(w.OrderId!.Value))
                    .Sum(w => Math.Abs(w.Amount)); // Use Abs to get positive value

                var revenue = (decimal)(Math.Abs(payment.Amount) - totalTransfer);

                if (revenue > 0)
                {
                    revenueData.Add(new
                    {
                        Date = payment.CreatedAt.Date,
                        OrderId = (int?)null,
                        Revenue = revenue
                    });
                }
            }

            // Process single orders (not in group)
            var singleOrderPayments = paymentReceivedTransactions.Where(t => t.OrderId.HasValue && !t.OrderGroupId.HasValue).ToList();
            foreach (var payment in singleOrderPayments)
            {
                var orderId = payment.OrderId!.Value;
                var transfer = transferTransactions
                    .Where(w => w.OrderId == orderId)
                    .Sum(w => Math.Abs(w.Amount)); // Use Abs to get positive value

                var revenue = (decimal)(Math.Abs(payment.Amount) - transfer);

                if (revenue > 0)
                {
                    revenueData.Add(new
                    {
                        Date = payment.CreatedAt.Date,
                        OrderId = (int?)orderId,
                        Revenue = revenue
                    });
                }
            }

            if (!revenueData.Any())
            {
                return new AdminRevenueAnalyticsDto
                {
                    RevenuePoints = new List<AdminRevenuePointDto>(),
                    TotalRevenue = 0,
                    TotalOrders = 0,
                    Period = request.Period ?? "daily",
                    StartDate = startDate,
                    EndDate = endDate
                };
            }

            var revenuePoints = new List<AdminRevenuePointDto>();

            switch (request.Period?.ToLower())
            {
                case "daily":
                    revenuePoints = revenueData
                        .GroupBy(d => d.Date)
                        .Select(g => new AdminRevenuePointDto
                        {
                            Date = g.Key,
                            Revenue = g.Sum(d => (decimal)d.Revenue),
                            OrderCount = g.Count() // Count transactions (orders/groups)
                        })
                        .OrderBy(p => p.Date)
                        .ToList();
                    break;

                case "weekly":
                    revenuePoints = revenueData
                        .GroupBy(d => GetWeekStart(d.Date))
                        .Select(g => new AdminRevenuePointDto
                        {
                            Date = g.Key,
                            Revenue = g.Sum(d => (decimal)d.Revenue),
                            OrderCount = g.Count()
                        })
                        .OrderBy(p => p.Date)
                        .ToList();
                    break;

                case "monthly":
                    revenuePoints = revenueData
                        .GroupBy(d => new DateTime(d.Date.Year, d.Date.Month, 1))
                        .Select(g => new AdminRevenuePointDto
                        {
                            Date = g.Key,
                            Revenue = g.Sum(d => (decimal)d.Revenue),
                            OrderCount = g.Count()
                        })
                        .OrderBy(p => p.Date)
                        .ToList();
                    break;

                default:
                    revenuePoints = revenueData
                        .GroupBy(d => d.Date)
                        .Select(g => new AdminRevenuePointDto
                        {
                            Date = g.Key,
                            Revenue = g.Sum(d => (decimal)d.Revenue),
                            OrderCount = g.Count()
                        })
                        .OrderBy(p => p.Date)
                        .ToList();
                    break;
            }

            // Fill missing periods with zero values
            revenuePoints = FillMissingPeriods(revenuePoints, startDate, endDate, request.Period ?? "daily");

            return new AdminRevenueAnalyticsDto
            {
                RevenuePoints = revenuePoints,
                TotalRevenue = revenueData.Sum(d => (decimal)d.Revenue),
                TotalOrders = revenueData.Count, // Total number of revenue transactions
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

        private List<AdminRevenuePointDto> FillMissingPeriods(List<AdminRevenuePointDto> points, DateTime startDate, DateTime endDate, string period)
        {
            var filledPoints = new List<AdminRevenuePointDto>();
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
                    filledPoints.Add(new AdminRevenuePointDto
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
