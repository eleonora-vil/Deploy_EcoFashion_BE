using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace EcoFashionBackEnd.Services
{
    public class CheckoutService
    {
        private readonly IRepository<Order, int> _orderRepository;
        private readonly IRepository<OrderDetail, int> _orderDetailRepository;
        private readonly IRepository<OrderGroup, Guid> _orderGroupRepository;
        private readonly IRepository<Material, int> _materialRepository;
        private readonly AppDbContext _dbContext;
        private readonly UserAddressService _userAddressService;

        public CheckoutService(
            IRepository<Order, int> orderRepository,
            IRepository<OrderDetail, int> orderDetailRepository,
            IRepository<OrderGroup, Guid> orderGroupRepository,
            IRepository<Material, int> materialRepository,
            AppDbContext dbContext,
            UserAddressService userAddressService
        )
        {
            _orderRepository = orderRepository;
            _orderDetailRepository = orderDetailRepository;
            _orderGroupRepository = orderGroupRepository;
            _materialRepository = materialRepository;
            _dbContext = dbContext;
            _userAddressService = userAddressService;
        }

        // Tạo OrderGroup + nhiều Order theo Seller, kèm OrderDetail
        public async Task<CreateSessionResponse> CreateSessionAsync(int userId, CreateSessionRequest request)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(request.HoldMinutes <= 0 ? 30 : request.HoldMinutes);

            // Idempotency: nếu FE gửi IdempotencyKey, tìm các Order pending hiện có để reuse
            Guid sessionKey;
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey) && Guid.TryParse(request.IdempotencyKey, out sessionKey))
            {
                // Đảm bảo tồn tại CheckoutSession tương ứng để tránh lỗi FK khi lưu vào Order.CheckoutSessionId
                var session = await _dbContext.CheckoutSessions.FirstOrDefaultAsync(s => s.CheckoutSessionId == sessionKey && s.UserId == userId);
                if (session == null)
                {
                    session = new CheckoutSession
                    {
                        CheckoutSessionId = sessionKey,
                        UserId = userId,
                        Status = CheckoutSessionStatus.Active,
                        ShippingAddress = request.ShippingAddress,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = expiresAt
                    };
                    _dbContext.CheckoutSessions.Add(session);
                    await _dbContext.SaveChangesAsync();
                }

                var existingOrders = await _dbContext.Orders
                    .AsNoTracking()
                    .Where(o => o.UserId == userId
                        && o.PaymentStatus == PaymentStatus.Pending
                        && o.CheckoutSessionId == sessionKey
                        && (o.ExpiresAt == null || o.ExpiresAt > DateTime.UtcNow))
                    .OrderBy(o => o.OrderId)
                    .ToListAsync();

                if (existingOrders.Any())
                {
                    var existingGroupId = existingOrders.First().OrderGroupId ?? Guid.Empty;
                    var existingResponse = new CreateSessionResponse
                    {
                        OrderGroupId = existingGroupId,
                        ExpiresAt = existingOrders.Max(o => o.ExpiresAt) ?? DateTime.UtcNow.AddMinutes(30),
                        Orders = existingOrders.Select(o => new CheckoutOrderDto
                        {
                            OrderId = o.OrderId,
                            Subtotal = o.Subtotal,
                            ShippingFee = o.ShippingFee,
                            Discount = o.Discount,
                            TotalAmount = o.TotalPrice,
                            PaymentStatus = o.PaymentStatus.ToString()
                        }).ToList(),
                        // Populate các field mới
                        OrderIds = existingOrders.Select(o => o.OrderId).ToList(),
                        TotalOrderCount = existingOrders.Count,
                        TotalAmount = existingOrders.Sum(o => o.TotalPrice)
                    };
                    return existingResponse;
                }
            }

            // Chuẩn hoá items và xác định seller cho từng item để pre-split
            var normalizedItems = new List<(string ItemType, int? MaterialId, int? DesignId, int? ProductId, int Quantity, decimal UnitPrice, Guid? SupplierId, Guid? DesignerId)>();
            foreach (var i in request.Items)
            {
                if (i.ItemType.Equals("material", StringComparison.OrdinalIgnoreCase) && i.MaterialId.HasValue)
                {
                    var material = await _dbContext.Materials.AsNoTracking().FirstOrDefaultAsync(m => m.MaterialId == i.MaterialId.Value);
                    if (material == null) throw new ArgumentException($"Material không tồn tại: {i.MaterialId}");
                    normalizedItems.Add(("material", i.MaterialId, null, null, i.Quantity, material.PricePerUnit, material.SupplierId, null));
                }
                else if (i.ItemType.Equals("design", StringComparison.OrdinalIgnoreCase) && i.DesignId.HasValue)
                {
                    var design = await _dbContext.Designs.AsNoTracking().FirstOrDefaultAsync(d => d.DesignId == i.DesignId.Value);
                    normalizedItems.Add(("design", null, i.DesignId, null, i.Quantity, i.UnitPrice, null, design?.DesignerId));
                }
                else if (i.ItemType.Equals("product", StringComparison.OrdinalIgnoreCase) && i.ProductId.HasValue)
                {
                    var product = await _dbContext.Products
                        .Include(p => p.Design)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProductId == i.ProductId.Value);
                    if (product == null) throw new ArgumentException($"Product không tồn tại: {i.ProductId}");
                    normalizedItems.Add(("product", null, null, i.ProductId, i.Quantity, product.Price, null, product.Design?.DesignerId));
                }
            }

            // Group theo seller (ưu tiên SupplierId nếu có, ngược lại dùng DesignerId)
            var sellerGroups = normalizedItems
                .GroupBy(x => new
                {
                    SellerId = x.SupplierId ?? x.DesignerId,
                    SellerType = x.SupplierId.HasValue ? "Supplier" : "Designer",
                    SupplierId = x.SupplierId,
                    DesignerId = x.DesignerId
                })
                .ToList();

            var orderGroup = new OrderGroup
            {
                OrderGroupId = Guid.NewGuid(),
                UserId = userId,
                Status = OrderGroupStatus.InProgress,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
            };
            await _orderGroupRepository.AddAsync(orderGroup);
            await _orderGroupRepository.Commit();

            var response = new CreateSessionResponse
            {
                OrderGroupId = orderGroup.OrderGroupId,
                ExpiresAt = expiresAt,
                OrderIds = new List<int>(), // Sẽ được populate sau khi tạo orders
                TotalOrderCount = 0, // Sẽ được update sau
                TotalAmount = 0 // Sẽ được tính toán sau
            };

            int totalOrders = 0;
            foreach (var grp in sellerGroups)
            {
                // Tính tổng theo nhóm seller
                var groupSubtotal = grp.Sum(i => i.UnitPrice * i.Quantity);
                var groupShipping = 0m; // có thể tính tuỳ logic
                var groupDiscount = 0m;
                var groupTotal = groupSubtotal + groupShipping - groupDiscount;

                var order = new Order
                {
                    UserId = userId,
                    OrderGroupId = orderGroup.OrderGroupId,
                    ShippingAddress = request.ShippingAddress,
                    Subtotal = groupSubtotal,
                    ShippingFee = groupShipping,
                    Discount = groupDiscount,
                    TotalPrice = groupTotal,
                    Status = OrderStatus.pending,
                    PaymentStatus = PaymentStatus.Pending,
                    FulfillmentStatus = FulfillmentStatus.None,
                    ExpiresAt = expiresAt,
                    OrderDate = DateTime.UtcNow,
                    CreateAt = DateTime.UtcNow,
                    SupplierId = grp.Key.SupplierId,
                    DesignerId = grp.Key.DesignerId,
                    ProviderType = grp.Key.SellerType,
                    // Lưu idempotency key vào CheckoutSessionId để lần sau reuse
                    CheckoutSessionId = (!string.IsNullOrWhiteSpace(request.IdempotencyKey) && Guid.TryParse(request.IdempotencyKey, out sessionKey)) ? sessionKey : null
                };

                await _orderRepository.AddAsync(order);
                await _orderRepository.Commit();
                totalOrders++;

                foreach (var item in grp)
                {
                    var detail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        MaterialId = item.ItemType == "material" ? item.MaterialId : null,
                        DesignId = item.ItemType == "design" ? item.DesignId : null,
                        ProductId = item.ItemType == "product" ? item.ProductId : null,
                        SupplierId = item.SupplierId,
                        DesignerId = item.DesignerId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Type = item.ItemType == "material" ? OrderDetailType.material :
                               item.ItemType == "product" ? OrderDetailType.product : OrderDetailType.design,
                        Status = OrderDetailStatus.pending
                    };
                    await _orderDetailRepository.AddAsync(detail);
                }
                await _orderDetailRepository.Commit();

                response.Orders.Add(new CheckoutOrderDto
                {
                    OrderId = order.OrderId,
                    Subtotal = order.Subtotal,
                    ShippingFee = order.ShippingFee,
                    Discount = order.Discount,
                    TotalAmount = order.TotalPrice,
                    PaymentStatus = order.PaymentStatus.ToString()
                });

                // Thêm OrderId vào danh sách
                response.OrderIds.Add(order.OrderId);
            }

            // Update counts on group
            orderGroup.TotalOrders = totalOrders;
            orderGroup.CompletedOrders = 0;
            _orderGroupRepository.Update(orderGroup);
            await _orderGroupRepository.Commit();

            // Cập nhật thông tin tổng hợp trong response
            response.TotalOrderCount = totalOrders;
            response.TotalAmount = response.Orders.Sum(o => o.TotalAmount);

            return response;
        }

        // Tạo session trực tiếp từ cart của user
        public async Task<CreateSessionResponse> CreateSessionFromCartAsync(int userId, string shippingAddress, int holdMinutes)
        {
            // Lấy cart active của user
            var cart = await _dbContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

            if (cart == null || !cart.Items.Any())
            {
                throw new ArgumentException("Giỏ hàng trống hoặc không tồn tại.");
            }

            // Chuyển CartItem thành CreateSessionRequest format
            var requestItems = new List<CartItemDto>();
            foreach (var cartItem in cart.Items)
            {
                if (cartItem.ItemType == "material" && cartItem.MaterialId.HasValue)
                {
                    var material = await _materialRepository.GetByIdAsync(cartItem.MaterialId.Value);
                    if (material == null)
                    {
                        throw new InvalidOperationException($"Material với ID {cartItem.MaterialId} không tồn tại.");
                    }

                    requestItems.Add(new CartItemDto
                    {
                        ItemType = "material",
                        MaterialId = cartItem.MaterialId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = material.PricePerUnit // Dùng giá hiện tại
                    });
                }
                else if (cartItem.ItemType == "product" && cartItem.ProductId.HasValue)
                {
                    var product = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == cartItem.ProductId.Value);
                    if (product == null)
                    {
                        throw new InvalidOperationException($"Product với ID {cartItem.ProductId} không tồn tại.");
                    }

                    requestItems.Add(new CartItemDto
                    {
                        ItemType = "product",
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = product.Price // Dùng giá hiện tại
                    });
                }
            }

            var request = new CreateSessionRequest
            {
                Items = requestItems,
                ShippingAddress = shippingAddress,
                HoldMinutes = holdMinutes
            };

            return await CreateSessionAsync(userId, request);
        }

        public async Task<bool> UpdateOrderAddressAsync(int orderId, int userId, int addressId)
        {
            try
            {
                var order = await _dbContext.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId && o.PaymentStatus == PaymentStatus.Pending);

                if (order == null) return false;

                var formattedAddress = await _userAddressService.GetFormattedAddressAsync(addressId, userId);
                if (string.IsNullOrEmpty(formattedAddress)) return false;

                order.ShippingAddress = formattedAddress;
                _orderRepository.Update(order);
                await _orderRepository.Commit();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateOrderGroupAddressAsync(Guid orderGroupId, int userId, int addressId)
        {
            try
            {
                var orders = await _dbContext.Orders
                    .Where(o => o.OrderGroupId == orderGroupId && o.UserId == userId && o.PaymentStatus == PaymentStatus.Pending)
                    .ToListAsync();

                if (!orders.Any()) return false;

                var formattedAddress = await _userAddressService.GetFormattedAddressAsync(addressId, userId);
                if (string.IsNullOrEmpty(formattedAddress)) return false;

                foreach (var order in orders)
                {
                    order.ShippingAddress = formattedAddress;
                    _orderRepository.Update(order);
                }

                await _orderRepository.Commit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateOrderAddressDirectAsync(int orderId, int userId, string shippingAddress)
        {
            try
            {
                var order = await _dbContext.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId && o.PaymentStatus == PaymentStatus.Pending);

                if (order == null) return false;

                order.ShippingAddress = shippingAddress;
                _orderRepository.Update(order);
                await _orderRepository.Commit();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}