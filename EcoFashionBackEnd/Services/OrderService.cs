using AutoMapper;
using EcoFashionBackEnd.Common.Payloads.Requests;
using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EcoFashionBackEnd.Services
{
    public class OrderService
    {
        private readonly IRepository<Order, int> _orderRepository;
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly WalletService _walletService;
        private readonly IConfiguration _configuration;

        public OrderService(
            IRepository<Order, int> repository, 
            AppDbContext dbContext, 
            IMapper mapper,
            WalletService walletService,
            IConfiguration configuration)
        {
            _orderRepository = repository;
            _dbContext = dbContext;
            _mapper = mapper;
            _walletService = walletService;
            _configuration = configuration;
        }

        private OrderModel MapOrderToModel(Order order)
        {
            // Get seller info from OrderDetails since orders can have mixed items
            var orderDetailsInfo = _dbContext.OrderDetails
                .Where(od => od.OrderId == order.OrderId)
                .Select(od => new { od.SupplierId, od.DesignerId })
                .ToList();

            // Determine primary seller - if mixed, show appropriate info
            var suppliers = orderDetailsInfo.Where(od => od.SupplierId.HasValue).Select(od => od.SupplierId!.Value).Distinct().ToList();
            var designers = orderDetailsInfo.Where(od => od.DesignerId.HasValue).Select(od => od.DesignerId!.Value).Distinct().ToList();

            string sellerName = "Multiple Sellers";
            string sellerType = "Mixed";
            string? sellerAvatarUrl = null;

            if (suppliers.Count == 1 && designers.Count == 0)
            {
                // Single supplier order
                var supplier = _dbContext.Suppliers
                    .Where(s => s.SupplierId == suppliers.First())
                    .Select(s => new { s.SupplierName, s.AvatarUrl })
                    .FirstOrDefault();
                if (supplier != null)
                {
                    sellerName = supplier.SupplierName;
                    sellerType = "Supplier";
                    sellerAvatarUrl = supplier.AvatarUrl;
                }
            }
            else if (designers.Count == 1 && suppliers.Count == 0)
            {
                // Single designer order
                var designer = _dbContext.Designers
                    .Where(d => d.DesignerId == designers.First())
                    .Select(d => new { d.DesignerName, d.AvatarUrl })
                    .FirstOrDefault();
                if (designer != null)
                {
                    sellerName = designer.DesignerName;
                    sellerType = "Designer";
                    sellerAvatarUrl = designer.AvatarUrl;
                }
            }
            else if (suppliers.Count > 0 && designers.Count > 0)
            {
                sellerName = "Mixed Sellers";
                sellerType = "Mixed";
            }
            else if (suppliers.Count > 1)
            {
                sellerName = "Multiple Suppliers";
                sellerType = "Supplier";
            }
            else if (designers.Count > 1)
            {
                sellerName = "Multiple Designers";
                sellerType = "Designer";
            }

            // Lấy SĐT từ địa chỉ mặc định của user nếu có
            string? personalPhone = null;
            try
            {
                var defaultAddr = _dbContext.UserAddresses
                    .AsNoTracking()
                    .FirstOrDefault(ua => ua.UserId == order.UserId && ua.IsDefault);
                if (defaultAddr != null)
                {
                    personalPhone = defaultAddr.PersonalPhoneNumber;
                }
            }
            catch { }

            return new OrderModel
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                UserName = order.User?.FullName ?? "Unknown User",
                ShippingAddress = order.ShippingAddress,
                PersonalPhoneNumber = personalPhone,
                TotalPrice = order.TotalPrice,
                OrderDate = order.OrderDate,
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                FulfillmentStatus = order.FulfillmentStatus.ToString(),
                SellerType = sellerType,
                SellerName = sellerName,
                SellerAvatarUrl = sellerAvatarUrl,
            };
        }

        public async Task<IEnumerable<OrderModel>> GetAllOrdersAsync()
        {
            var orders = await _dbContext.Orders
                .Include(o => o.User)
                .ToListAsync();
            
            return orders.Select(MapOrderToModel);
        }

        public async Task<IEnumerable<OrderModel>> GetOrdersByUserIdAsync(int userId)
        {
            var orders = await _dbContext.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.User)
                .ToListAsync();
            
            return orders.Select(MapOrderToModel);
        }

        public async Task<OrderModel?> GetOrderByIdAsync(int id)
        {
            var order = await _dbContext.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == id);
                
            if (order == null) return null;
            
            return MapOrderToModel(order);
        }

        // Copy all other methods from original OrderService with minimal changes
        public async Task<int> CreateOrderAsync(int userId, CreateOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ShippingAddress))
                throw new ArgumentException("Địa chỉ không được để trống.");
            if (request.TotalPrice < 0)
                throw new ArgumentException("Tổng giá không hợp lệ");
            var order = _mapper.Map<Order>(request);
            order.UserId = userId;
            order.Status = OrderStatus.pending;
            order.OrderDate = DateTime.UtcNow;
            await _orderRepository.AddAsync(order);
            await _dbContext.SaveChangesAsync();
            return order.OrderId;
        }

        public async Task<bool> UpdateOrderAsync(int orderId, UpdateOrderRequest request)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null) return false;

            if (!string.IsNullOrWhiteSpace(request.ShippingAddress))
                order.ShippingAddress = request.ShippingAddress;

            if (request.TotalPrice.HasValue)
                order.TotalPrice = request.TotalPrice.Value;

            if (request.Status.HasValue)
                order.Status = request.Status.Value;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var result = _orderRepository.Remove(id);
            await _dbContext.SaveChangesAsync();
            return result != null;
        }

        public async Task<bool> UpdateFulfillmentStatusAsync(int orderId, FulfillmentStatus fulfillmentStatus)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.FulfillmentStatus = fulfillmentStatus;
            
            // Update main status based on fulfillment status
            switch (fulfillmentStatus)
            {
                case FulfillmentStatus.Processing:
                    order.Status = OrderStatus.processing;
                    break;
                case FulfillmentStatus.Shipped:
                    order.Status = OrderStatus.shipped;
                    break;
                case FulfillmentStatus.Delivered:
                    order.Status = OrderStatus.delivered;
                    // Trigger settlement when delivered
                    await ProcessSettlementAsync(order);
                    break;
                case FulfillmentStatus.Canceled:
                    order.Status = OrderStatus.returned;
                    break;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }


        // Get orders by seller ID for shipment management
        public async Task<IEnumerable<OrderModel>> GetOrdersBySellerIdAsync(Guid sellerId)
        {
            // Since we removed sellerId, we need to find orders through OrderDetails
            var orderIds = await _dbContext.OrderDetails
                .Where(od => od.SupplierId == sellerId || od.DesignerId == sellerId)
                .Select(od => od.OrderId)
                .Distinct()
                .ToListAsync();

            var orders = await _dbContext.Orders
                .Where(o => orderIds.Contains(o.OrderId) && o.PaymentStatus == PaymentStatus.Paid)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(MapOrderToModel);
        }

        // Mark order as shipped
        public async Task<bool> MarkOrderShippedAsync(int orderId, ShipOrderRequest request)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null || order.PaymentStatus != PaymentStatus.Paid) return false;

            order.FulfillmentStatus = FulfillmentStatus.Shipped;
            order.Status = OrderStatus.shipped;
            
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Mark order as delivered and trigger settlement
        public async Task<bool> MarkOrderDeliveredAsync(int orderId)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null || order.PaymentStatus != PaymentStatus.Paid) return false;

            order.FulfillmentStatus = FulfillmentStatus.Delivered;
            order.Status = OrderStatus.delivered;
            
            // Process settlement (90% to seller, 10% platform commission)
            await ProcessSettlementAsync(order);
            
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Process settlement when order is delivered
        private async Task ProcessSettlementAsync(Order order)
        {
            try
            {
                // Calculate commission (10%) and net amount (90%)
                var commissionRate = 0.10m;
                var commissionAmount = order.TotalPrice * commissionRate;
                var netAmount = order.TotalPrice - commissionAmount;

                // Update order with settlement info
                order.CommissionRate = commissionRate;
                order.CommissionAmount = commissionAmount;
                order.NetAmount = netAmount;

                // Get admin wallet using configuration
                var adminUserId = _configuration.GetValue<int>("AdminUserId", 1);
                var adminWallet = await _walletService.GetWalletByUserIdAsync(adminUserId);

                if (adminWallet == null)
                {
                    // Create admin wallet if it doesn't exist
                    adminWallet = new Wallet
                    {
                        UserId = adminUserId,
                        Balance = 0,
                        CreatedAt = DateTime.UtcNow,
                        LastUpdatedAt = DateTime.UtcNow,
                        Status = WalletStatus.Active
                    };
                    _dbContext.Wallets.Add(adminWallet);
                    await _dbContext.SaveChangesAsync();
                }

                // Process settlements for each seller in the order
                var orderDetails = await _dbContext.OrderDetails
                    .Where(od => od.OrderId == order.OrderId)
                    .ToListAsync();

                var sellerSettlements = new Dictionary<int, decimal>();

                foreach (var detail in orderDetails)
                {
                    var itemTotal = detail.Quantity * detail.UnitPrice;
                    var itemNetAmount = itemTotal * 0.9m; // 90% to seller

                    int? sellerUserId = null;
                    if (detail.SupplierId.HasValue)
                    {
                        var supplier = await _dbContext.Suppliers
                            .FirstOrDefaultAsync(s => s.SupplierId == detail.SupplierId);
                        sellerUserId = supplier?.UserId;
                    }
                    else if (detail.DesignerId.HasValue)
                    {
                        var designer = await _dbContext.Designers
                            .FirstOrDefaultAsync(d => d.DesignerId == detail.DesignerId);
                        sellerUserId = designer?.UserId;
                    }

                    if (sellerUserId.HasValue)
                    {
                        if (sellerSettlements.ContainsKey(sellerUserId.Value))
                            sellerSettlements[sellerUserId.Value] += itemNetAmount;
                        else
                            sellerSettlements[sellerUserId.Value] = itemNetAmount;
                    }
                }

                // Transfer money to each seller
                foreach (var settlement in sellerSettlements)
                {
                    var sellerUserId = settlement.Key;
                    var amount = settlement.Value;

                    var sellerWallet = await _walletService.GetWalletByUserIdAsync(sellerUserId);

                    // Create seller wallet if it doesn't exist
                    if (sellerWallet == null)
                    {
                        sellerWallet = new Wallet
                        {
                            UserId = sellerUserId,
                            Balance = 0,
                            CreatedAt = DateTime.UtcNow,
                            LastUpdatedAt = DateTime.UtcNow,
                            Status = WalletStatus.Active
                        };
                        _dbContext.Wallets.Add(sellerWallet);
                        await _dbContext.SaveChangesAsync();
                    }

                    var amountDouble = (double)amount;

                    // Check if admin wallet has sufficient balance
                    if (adminWallet.Balance >= amountDouble)
                    {
                        var adminBalanceBefore = adminWallet.Balance;
                        var sellerBalanceBefore = sellerWallet.Balance;

                        // Deduct from admin wallet
                        adminWallet.Balance -= amountDouble;
                        adminWallet.LastUpdatedAt = DateTime.UtcNow;
                        
                        // Add to seller wallet
                        sellerWallet.Balance += amountDouble;
                        sellerWallet.LastUpdatedAt = DateTime.UtcNow;

                        // Create wallet transactions
                        var adminTransaction = new WalletTransaction
                        {
                            WalletId = adminWallet.WalletId,
                            Amount = -amountDouble,
                            BalanceBefore = adminBalanceBefore,
                            BalanceAfter = adminWallet.Balance,
                            Type = TransactionType.Transfer,
                            Description = $"Settlement for order #{order.OrderId}",
                            Status = TransactionStatus.Success,
                            OrderId = order.OrderId,
                            CreatedAt = DateTime.UtcNow
                        };

                        var sellerTransaction = new WalletTransaction
                        {
                            WalletId = sellerWallet.WalletId,
                            Amount = amountDouble,
                            BalanceBefore = sellerBalanceBefore,
                            BalanceAfter = sellerWallet.Balance,
                            Type = TransactionType.Transfer,
                            Description = $"Payment received for order #{order.OrderId}",
                            Status = TransactionStatus.Success,
                            OrderId = order.OrderId,
                            CreatedAt = DateTime.UtcNow
                        };

                        _dbContext.WalletTransactions.Add(adminTransaction);
                        _dbContext.WalletTransactions.Add(sellerTransaction);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking the order completion
                Console.WriteLine($"Settlement error for order {order.OrderId}: {ex.Message}");
            }
        }

        // Sub-order feature removed: keeping OrderService focused on single-order flow per seller
    }
}