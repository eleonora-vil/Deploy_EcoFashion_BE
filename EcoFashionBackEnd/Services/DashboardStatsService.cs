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
        private readonly IRepository<Order, int> _orderRepository;
        private readonly IConfiguration _configuration;

        public DashboardStatsService(
            IRepository<User, int> userRepository,
            IRepository<Design, Guid> designRepository,
            IRepository<Material, Guid> materialRepository,
            IRepository<WalletTransaction, int> transactionRepository,
            IRepository<Wallet, int> walletRepository,
            IRepository<Order, int> orderRepository,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _designRepository = designRepository;
            _materialRepository = materialRepository;
            _transactionRepository = transactionRepository;
            _walletRepository = walletRepository;
            _orderRepository = orderRepository;
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

            // Calculate total revenue
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
                                       && t.Status == TransactionStatus.Success
                                       && (t.OrderId.HasValue || t.OrderGroupId.HasValue))
                    .ToListAsync();

                // Get all Transfer transactions (admin pays sellers)
                var transferTransactions = await _transactionRepository
                    .FindByCondition(t => t.WalletId == adminWallet.WalletId
                                       && t.Type == TransactionType.Transfer
                                       && t.Amount < 0 // Negative means money going out
                                       && t.Status == TransactionStatus.Success
                                       && t.OrderId.HasValue)
                    .ToListAsync();

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
                        totalRevenue += revenue;
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
