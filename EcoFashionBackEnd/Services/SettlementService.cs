using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EcoFashionBackEnd.Services
{
    public class SettlementService
    {
        private readonly AppDbContext _context;
        private readonly WalletService _walletService;
        private readonly IConfiguration _configuration;

        public SettlementService(AppDbContext context, WalletService walletService, IConfiguration configuration)
        {
            _context = context;
            _walletService = walletService;
            _configuration = configuration;
        }

        public async Task CreateSettlementsForOrderAsync(int orderId, decimal commissionRate = 0.1m)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return;

            var existingSettlements = await _context.OrderSellerSettlements
                .Where(s => s.OrderId == orderId)
                .ToListAsync();

            if (existingSettlements.Any()) return;

            var sellerGroups = new Dictionary<(int SellerUserId, string SellerType), decimal>();

            var details = await _context.OrderDetails
                .Where(d => d.OrderId == orderId)
                .Include(d => d.Material).ThenInclude(m => m.Supplier)
                .Include(d => d.Product)
                    .ThenInclude(p => p.Design)
                        .ThenInclude(des => des.DesignerProfile)
                .ToListAsync();

            foreach (var detail in details)
            {
                if (detail.Material?.Supplier != null)
                {
                    var sellerUserId = detail.Material.Supplier.UserId;
                    var key = (sellerUserId, "Supplier");
                    var amount = detail.Quantity * detail.UnitPrice;
                    sellerGroups.TryAdd(key, 0);
                    sellerGroups[key] += amount;
                }
                else if (detail.Product?.Design?.DesignerProfile != null)
                {
                    var sellerUserId = detail.Product.Design.DesignerProfile.UserId;
                    var key = (sellerUserId, "Designer");
                    var amount = detail.Quantity * detail.UnitPrice;
                    sellerGroups.TryAdd(key, 0);
                    sellerGroups[key] += amount;
                }
            }

            foreach (var ((sellerUserId, sellerType), grossAmount) in sellerGroups)
            {
                var commissionAmount = grossAmount * commissionRate;
                var netAmount = grossAmount - commissionAmount;

                var settlement = new OrderSellerSettlement
                {
                    OrderId = orderId,
                    SellerUserId = sellerUserId,
                    SellerType = sellerType,
                    GrossAmount = grossAmount,
                    CommissionRate = commissionRate,
                    CommissionAmount = commissionAmount,
                    NetAmount = netAmount,
                    Status = SettlementStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OrderSellerSettlements.Add(settlement);
            }

            await _context.SaveChangesAsync();
        }

        public async Task ReleasePayoutsForOrderAsync(int orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var settlements = await _context.OrderSellerSettlements
                    .Where(s => s.OrderId == orderId && s.Status == SettlementStatus.Pending)
                    .ToListAsync();

                if (!settlements.Any()) return;

                var adminUserId = _configuration.GetValue<int>("AdminUserId", 1);
                var adminWallet = await _walletService.GetWalletByUserIdAsync(adminUserId);
                if (adminWallet == null) return;

                foreach (var settlement in settlements)
                {
                    var sellerWallet = await _walletService.GetWalletByUserIdAsync(settlement.SellerUserId);
                    if (sellerWallet == null) continue;

                    if (adminWallet.Balance < (double)settlement.NetAmount) continue;

                    await _walletService.CreateTransactionAsync(adminWallet.WalletId, 
                        TransactionType.Withdrawal, -(double)settlement.NetAmount, 
                        orderId: orderId, settlementId: settlement.SettlementId);

                    await _walletService.CreateTransactionAsync(sellerWallet.WalletId, 
                        TransactionType.Deposit, (double)settlement.NetAmount, 
                        orderId: orderId, settlementId: settlement.SettlementId);

                    settlement.Status = SettlementStatus.Released;
                    settlement.ReleasedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ReleasePayoutsForGroupAsync(Guid orderGroupId)
        {
            var orderIds = await _context.Orders
                .Where(o => o.OrderGroupId == orderGroupId)
                .Select(o => o.OrderId)
                .ToListAsync();

            foreach (var orderId in orderIds)
            {
                await ReleasePayoutsForOrderAsync(orderId);
            }
        }

        public async Task<List<OrderSellerSettlement>> GetSettlementsForOrderAsync(int orderId)
        {
            return await _context.OrderSellerSettlements
                .Where(s => s.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<List<OrderSellerSettlement>> GetPendingSettlementsForSellerAsync(int sellerUserId)
        {
            return await _context.OrderSellerSettlements
                .Where(s => s.SellerUserId == sellerUserId && s.Status == SettlementStatus.Pending)
                .Include(s => s.Order)
                .ToListAsync();
        }
    }
}