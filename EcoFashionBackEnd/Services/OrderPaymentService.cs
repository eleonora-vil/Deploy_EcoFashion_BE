using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EcoFashionBackEnd.Services
{
    public class OrderPaymentService
    {
        private readonly AppDbContext _context;
        private readonly WalletService _walletService;
        private readonly IConfiguration _configuration;
        private readonly MaterialInventoryService _materialInventoryService;

        public OrderPaymentService(
            AppDbContext context, 
            WalletService walletService, 
            IConfiguration configuration,
            MaterialInventoryService materialInventoryService)
        {
            _context = context;
            _walletService = walletService;
            _configuration = configuration;
            _materialInventoryService = materialInventoryService;
        }

        public async Task<bool> PayWithWalletAsync(int orderId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

                if (order == null || order.PaymentStatus == PaymentStatus.Paid)
                    return false;

                var customerWallet = await _walletService.GetWalletByUserIdAsync(userId);
                if (customerWallet == null || customerWallet.Balance < (double)order.TotalPrice)
                    return false;

                var adminUserId = _configuration.GetValue<int>("AdminUserId", 1);
                var adminWallet = await _walletService.GetWalletByUserIdAsync(adminUserId);
                if (adminWallet == null)
                    return false;

                // Khách hàng trả tiền cho đơn hàng
                await _walletService.CreateTransactionAsync(customerWallet.WalletId, 
                    TransactionType.Payment, -(double)order.TotalPrice, orderId: orderId);

                // Admin nhận tiền từ đơn hàng (thay vì Deposit để phân biệt với nạp tiền VNPay)
                await _walletService.CreateTransactionAsync(adminWallet.WalletId, 
                    TransactionType.PaymentReceived, (double)order.TotalPrice, orderId: orderId);

                order.PaymentStatus = PaymentStatus.Paid;
                order.Status = OrderStatus.processing;

                // Lưu thay đổi trước khi trừ kho
                await _context.SaveChangesAsync();

                // TRỪ KHO NGAY SAU KHI CHUYỂN SANG PROCESSING
                await DeductInventoryForOrderAsync(orderId);

                await transaction.CommitAsync();
                
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> PayGroupWithWalletAsync(Guid orderGroupId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var orders = await _context.Orders
                    .Where(o => o.OrderGroupId == orderGroupId && o.UserId == userId && o.PaymentStatus != PaymentStatus.Paid)
                    .ToListAsync();

                if (!orders.Any())
                    return false;

                var totalAmount = orders.Sum(o => o.TotalPrice);

                var customerWallet = await _walletService.GetWalletByUserIdAsync(userId);
                if (customerWallet == null || customerWallet.Balance < (double)totalAmount)
                    return false;

                var adminUserId = _configuration.GetValue<int>("AdminUserId", 1);
                var adminWallet = await _walletService.GetWalletByUserIdAsync(adminUserId);
                if (adminWallet == null)
                    return false;

                // Tạo description chi tiết cho giao dịch nhóm đơn hàng
                var orderDetails = orders.Select(o => $"orderId: {o.OrderId}, số tiền: {o.TotalPrice:N0}").ToList();
                var groupDescription = $"Thanh toán cho {orders.Count} đơn hàng có {string.Join("; ", orderDetails)}";
                
                // Khách hàng trả tiền cho nhóm đơn hàng với description chi tiết
                await _walletService.CreateTransactionAsync(customerWallet.WalletId, 
                    TransactionType.Payment, -(double)totalAmount, orderGroupId: orderGroupId, description: groupDescription);

                // Admin nhận tiền từ nhóm đơn hàng (thay vì Deposit)
                await _walletService.CreateTransactionAsync(adminWallet.WalletId, 
                    TransactionType.PaymentReceived, (double)totalAmount, orderGroupId: orderGroupId, description: groupDescription);

                // Cập nhật status cho tất cả orders
                foreach (var order in orders)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.Status = OrderStatus.processing;
                }

                // Lưu thay đổi trước khi trừ kho
                await _context.SaveChangesAsync();

                // TRỪ KHO CHO TẤT CẢ ORDERS TRONG NHÓM
                foreach (var order in orders)
                {
                    await DeductInventoryForOrderAsync(order.OrderId);
                }

                await transaction.CommitAsync();
                
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Helper method: Trừ inventory cho tất cả materials trong order khi payment thành công
        /// </summary>
        /// <param name="orderId">ID của order đã thanh toán thành công</param>
        /// <returns>True nếu trừ kho thành công</returns>
        private async Task<bool> DeductInventoryForOrderAsync(int orderId)
        {
            try
            {
                // Kiểm tra xem đã trừ kho cho order này chưa (tránh trùng lập)
                var existingDeduction = await _context.MaterialStockTransactions
                    .AnyAsync(t => t.ReferenceId == orderId.ToString() && 
                                  t.TransactionType == MaterialTransactionType.CustomerSale &&
                                  (t.ReferenceType == "WalletPayment" || t.ReferenceType == "OrderPayment"));

                if (existingDeduction)
                {
                    Console.WriteLine($"⚠️ Inventory already deducted for OrderId {orderId}. Skipping duplicate deduction.");
                    return true;
                }

                // Lấy tất cả order details có MaterialId (loại material, không phải design)
                var materialOrderDetails = await _context.OrderDetails
                    .Include(od => od.Material)
                    .ThenInclude(m => m.Supplier)
                    .Where(od => od.OrderId == orderId && od.Type == OrderDetailType.material && od.MaterialId.HasValue)
                    .ToListAsync();

                if (!materialOrderDetails.Any())
                {
                    Console.WriteLine($"No material order details found for OrderId {orderId}. Skipping inventory deduction.");
                    return true; // Không có material nào cần trừ
                }

                foreach (var orderDetail in materialOrderDetails)
                {
                    var materialId = orderDetail.MaterialId!.Value;
                    var quantity = orderDetail.Quantity;
                    var supplierId = orderDetail.Material!.SupplierId;

                    // Tìm warehouse mặc định của supplier (đúng loại kho Material)
                    var warehouse = await _context.Warehouses
                        .FirstOrDefaultAsync(w => w.SupplierId == supplierId && w.IsDefault && w.IsActive && w.WarehouseType == "Material");

                    if (warehouse == null)
                    {
                        Console.WriteLine($"WARNING: No default warehouse found for SupplierId {supplierId}. Skipping deduction for MaterialId {materialId}");
                        continue;
                    }

                    // Trừ inventory sử dụng MaterialInventoryService
                    await _materialInventoryService.CreateTransactionAsync(
                        materialId: materialId,
                        warehouseId: warehouse.WarehouseId,
                        transactionType: MaterialTransactionType.CustomerSale,
                        quantityChange: -quantity, // Số âm cho việc bán
                        unit: "mét", // Default unit cho material
                        note: $"Trừ kho cho đơn hàng #{orderId} - Thanh toán ví thành công",
                        referenceType: "WalletPayment",
                        referenceId: orderId.ToString(),
                        userId: null // System operation
                    );

                    Console.WriteLine($"✅ Successfully deducted {quantity} units of MaterialId {materialId} from WarehouseId {warehouse.WarehouseId} for OrderId {orderId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR deducting inventory for OrderId {orderId}: {ex.Message}");
                throw; // Re-throw để transaction bị rollback
            }
        }
    }
}