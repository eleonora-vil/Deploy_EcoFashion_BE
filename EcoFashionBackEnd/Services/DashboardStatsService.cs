using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EcoFashionBackEnd.Services
{
    public class DashboardStatsService
    {
        private readonly IRepository<User, int> _userRepository;
        private readonly IRepository<Design, Guid> _designRepository;
        private readonly IRepository<Material, Guid> _materialRepository;
        private readonly IRepository<WalletTransaction, int> _transactionRepository;
        private readonly IRepository<Wallet, int> _walletRepository;
        private readonly IConfiguration _configuration;

        public DashboardStatsService(
            IRepository<User, int> userRepository,
            IRepository<Design, Guid> designRepository,
            IRepository<Material, Guid> materialRepository,
            IRepository<WalletTransaction, int> transactionRepository,
            IRepository<Wallet, int> walletRepository,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _designRepository = designRepository;
            _materialRepository = materialRepository;
            _transactionRepository = transactionRepository;
            _walletRepository = walletRepository;
            _configuration = configuration;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            // Count total users
            var totalUsers = await _userRepository.GetAll().CountAsync();

            // Count total designs
            var totalDesigns = await _designRepository.GetAll().CountAsync();

            // Count total materials
            var totalMaterials = await _materialRepository.GetAll().CountAsync();

            // Calculate total revenue (using same logic as AdminAnalyticsService)
            var adminUserId = _configuration.GetValue<int>("AdminUserId", 1);
            var adminWallet = await _walletRepository
                .FindByCondition(w => w.UserId == adminUserId)
                .FirstOrDefaultAsync();

            decimal totalRevenue = 0;

            if (adminWallet != null)
            {
                // Get all PaymentReceived transactions
                var paymentReceivedTransactions = await _transactionRepository
                    .FindByCondition(t => t.WalletId == adminWallet.WalletId
                                       && t.Type == TransactionType.PaymentReceived
                                       && (t.OrderId.HasValue || t.OrderGroupId.HasValue))
                    .ToListAsync();

                // Get all Withdrawal transactions
                var withdrawalTransactions = await _transactionRepository
                    .FindByCondition(t => t.WalletId == adminWallet.WalletId
                                       && t.Type == TransactionType.Withdrawal
                                       && (t.OrderId.HasValue || t.OrderGroupId.HasValue))
                    .ToListAsync();

                // Calculate revenue from single orders
                var singleOrderPayments = paymentReceivedTransactions.Where(t => t.OrderId.HasValue).ToList();
                var singleOrderWithdrawals = withdrawalTransactions.Where(t => t.OrderId.HasValue).ToList();

                foreach (var payment in singleOrderPayments)
                {
                    var orderId = payment.OrderId!.Value;
                    var withdrawal = singleOrderWithdrawals
                        .Where(w => w.OrderId == orderId)
                        .Sum(w => Math.Abs(w.Amount));

                    var revenue = (decimal)(Math.Abs(payment.Amount) - withdrawal);
                    if (revenue > 0)
                    {
                        totalRevenue += revenue;
                    }
                }

                // Calculate revenue from order groups
                var groupPayments = paymentReceivedTransactions.Where(t => t.OrderGroupId.HasValue).ToList();
                var groupWithdrawals = withdrawalTransactions.Where(t => t.OrderGroupId.HasValue).ToList();

                foreach (var payment in groupPayments)
                {
                    var groupId = payment.OrderGroupId!.Value;
                    var withdrawal = groupWithdrawals
                        .Where(w => w.OrderGroupId == groupId)
                        .Sum(w => Math.Abs(w.Amount));

                    var revenue = (decimal)(Math.Abs(payment.Amount) - withdrawal);
                    if (revenue > 0)
                    {
                        totalRevenue += revenue;
                    }
                }
            }

            return new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                TotalDesigns = totalDesigns,
                TotalMaterials = totalMaterials,
                TotalRevenue = totalRevenue
            };
        }
    }
}
