using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Repositories;
using Microsoft.EntityFrameworkCore;


namespace EcoFashionBackEnd.Services
{
    public class FlexibleCheckoutService
    {
        private readonly IRepository<CheckoutSession, Guid> _checkoutSessionRepository;
        private readonly IRepository<CheckoutSessionItem, int> _checkoutSessionItemRepository;
        private readonly IRepository<Order, int> _orderRepository;
        private readonly IRepository<OrderDetail, int> _orderDetailRepository;
        private readonly IRepository<OrderGroup, Guid> _orderGroupRepository;
        private readonly AppDbContext _dbContext;
        private readonly UserAddressService _userAddressService;


        public FlexibleCheckoutService(
            IRepository<CheckoutSession, Guid> checkoutSessionRepository,
            IRepository<CheckoutSessionItem, int> checkoutSessionItemRepository,
            IRepository<Order, int> orderRepository,
            IRepository<OrderDetail, int> orderDetailRepository,
            IRepository<OrderGroup, Guid> orderGroupRepository,
            AppDbContext dbContext,
            UserAddressService userAddressService)
        {
            _checkoutSessionRepository = checkoutSessionRepository;
            _checkoutSessionItemRepository = checkoutSessionItemRepository;
            _orderRepository = orderRepository;
            _orderDetailRepository = orderDetailRepository;
            _orderGroupRepository = orderGroupRepository;
            _dbContext = dbContext;
            _userAddressService = userAddressService;
        }


        public async Task<CheckoutSessionDto> CreateCheckoutSessionAsync(int userId, CreateCheckoutSessionRequest request)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(request.HoldMinutes <= 0 ? 30 : request.HoldMinutes);


            var checkoutSession = new CheckoutSession
            {
                CheckoutSessionId = Guid.NewGuid(),
                UserId = userId,
                Status = CheckoutSessionStatus.Active,
                ShippingAddress = request.ShippingAddress,
                AddressId = request.AddressId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };


            await _checkoutSessionRepository.AddAsync(checkoutSession);
            await _checkoutSessionRepository.Commit();


            // Process each item request
            foreach (var itemRequest in request.Items)
            {
                await AddItemToSessionAsync(checkoutSession.CheckoutSessionId, itemRequest);
            }


            // Update session summary
            await UpdateSessionSummaryAsync(checkoutSession.CheckoutSessionId);


            return await GetCheckoutSessionAsync(checkoutSession.CheckoutSessionId);
        }


        public async Task<CheckoutSessionDto> CreateCheckoutSessionFromCartAsync(int userId, string? shippingAddress = null, int? addressId = null)
        {
            var activeCart = await _dbContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);


            if (activeCart == null || !activeCart.Items.Any())
                throw new ArgumentException("Cart is empty or not found");


            var request = new CreateCheckoutSessionRequest
            {
                ShippingAddress = shippingAddress,
                AddressId = addressId,
                Items = activeCart.Items.Select(item => new CheckoutSessionItemRequest
                {
                    MaterialId = item.MaterialId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToList()
            };


            return await CreateCheckoutSessionAsync(userId, request);
        }


        private async Task AddItemToSessionAsync(Guid checkoutSessionId, CheckoutSessionItemRequest itemRequest)
        {
            CheckoutSessionItem sessionItem;


            if (itemRequest.MaterialId.HasValue)
            {
                var material = await _dbContext.Materials
                    .Include(m => m.Supplier)
                    .Include(m => m.MaterialImages).ThenInclude(mi => mi.Image)
                    .FirstOrDefaultAsync(m => m.MaterialId == itemRequest.MaterialId.Value);


                if (material == null) throw new ArgumentException($"Material not found: {itemRequest.MaterialId}");


                sessionItem = new CheckoutSessionItem
                {
                    CheckoutSessionId = checkoutSessionId,
                    MaterialId = material.MaterialId,
                    Type = OrderDetailType.material,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = itemRequest.UnitPrice ?? material.PricePerUnit,
                    SupplierId = material.SupplierId,
                    ProviderName = material.Supplier?.SupplierName ?? "Unknown Supplier",
                    ProviderType = "Supplier",
                    IsSelected = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else if (itemRequest.ProductId.HasValue)
            {
                var product = await _dbContext.Products
                    .Include(p => p.Design).ThenInclude(d => d.DesignerProfile)
                    .FirstOrDefaultAsync(p => p.ProductId == itemRequest.ProductId.Value);


                if (product == null) throw new ArgumentException($"Product not found: {itemRequest.ProductId}");


                sessionItem = new CheckoutSessionItem
                {
                    CheckoutSessionId = checkoutSessionId,
                    ProductId = product.ProductId,
                    Type = OrderDetailType.product,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = itemRequest.UnitPrice ?? product.Price,
                    DesignerId = product.Design?.DesignerId,
                    ProviderName = product.Design?.DesignerProfile?.DesignerName ?? "Unknown Designer",
                    ProviderType = "Designer",
                    IsSelected = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                throw new ArgumentException("Must specify MaterialId or ProductId");
            }


            await _checkoutSessionItemRepository.AddAsync(sessionItem);
            await _checkoutSessionItemRepository.Commit();
        }


        public async Task<CheckoutSessionDto> GetCheckoutSessionAsync(Guid checkoutSessionId)
        {
            var session = await _dbContext.CheckoutSessions
                .Include(cs => cs.Items)
                    .ThenInclude(csi => csi.Material).ThenInclude(m => m.MaterialImages).ThenInclude(mi => mi.Image)
                .Include(cs => cs.Items)
                    .ThenInclude(csi => csi.Product).ThenInclude(p => p.Design).ThenInclude(d => d.DesignImages).ThenInclude(di => di.Image)
                .Include(cs => cs.Items)
                    .ThenInclude(csi => csi.Supplier)
                .FirstOrDefaultAsync(cs => cs.CheckoutSessionId == checkoutSessionId);


            if (session == null) throw new ArgumentException("Checkout session not found");


            var items = session.Items.Select(item => new CheckoutSessionItemDto
            {
                CheckoutSessionItemId = item.CheckoutSessionItemId,
                CheckoutSessionId = item.CheckoutSessionId,
                MaterialId = item.MaterialId,
                ProductId = item.ProductId,
                ItemName = GetItemName(item),
                ItemImageUrl = GetItemImageUrl(item),
                Type = item.Type.ToString(),
                SupplierId = item.SupplierId,
                DesignerId = item.DesignerId,
                ProviderName = item.ProviderName ?? "Unknown",
                ProviderType = item.ProviderType ?? "Unknown",
                ProviderAvatarUrl = GetProviderAvatarUrl(item),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                IsSelected = item.IsSelected,
                CreatedAt = item.CreatedAt
            }).ToList();


            // Group items by provider
            var providerGroups = items
                .GroupBy(item => new { item.SupplierId, item.DesignerId, item.ProviderType })
                .Select(group => new ProviderGroupDto
                {
                    ProviderId = group.Key.SupplierId ?? group.Key.DesignerId,
                    ProviderName = group.First().ProviderName,
                    ProviderType = group.Key.ProviderType ?? "Unknown",
                    ProviderAvatarUrl = group.First().ProviderAvatarUrl,
                    Items = group.ToList(),
                    GroupSubtotal = group.Sum(item => item.TotalPrice),
                    GroupItemCount = group.Sum(item => item.Quantity),
                    CanCheckoutSeparately = true
                }).ToList();


            return new CheckoutSessionDto
            {
                CheckoutSessionId = session.CheckoutSessionId,
                UserId = session.UserId,
                Status = session.Status.ToString(),
                ShippingAddress = session.ShippingAddress,
                AddressId = session.AddressId,
                TotalAmount = session.TotalAmount,
                TotalItems = session.TotalItems,
                TotalProviders = session.TotalProviders,
                CreatedAt = session.CreatedAt,
                ExpiresAt = session.ExpiresAt,
                Items = items,
                ProviderGroups = providerGroups
            };
        }


        public async Task<bool> UpdateSelectionAsync(Guid checkoutSessionId, UpdateCheckoutSelectionRequest request)
        {
            var sessionItems = await _dbContext.CheckoutSessionItems
                .Where(csi => csi.CheckoutSessionId == checkoutSessionId)
                .ToListAsync();


            // First, deselect all items
            foreach (var item in sessionItems)
            {
                item.IsSelected = false;
            }


            // Then select the requested items
            var itemsToSelect = sessionItems
                .Where(item => request.SelectedItemIds.Contains(item.CheckoutSessionItemId))
                .ToList();


            // Apply provider filter if specified
            if (request.ProviderIdFilter.HasValue)
            {
                itemsToSelect = itemsToSelect
                    .Where(item => item.SupplierId == request.ProviderIdFilter || item.DesignerId == request.ProviderIdFilter)
                    .ToList();
            }


            if (!string.IsNullOrEmpty(request.ProviderTypeFilter))
            {
                itemsToSelect = itemsToSelect
                    .Where(item => item.ProviderType == request.ProviderTypeFilter)
                    .ToList();
            }


            foreach (var item in itemsToSelect)
            {
                item.IsSelected = true;
            }


            await _dbContext.SaveChangesAsync();
            await UpdateSessionSummaryAsync(checkoutSessionId);


            return true;
        }


        public async Task<CreateSessionResponse> ProcessFlexibleCheckoutAsync(int userId, FlexibleCheckoutRequest request)
        {
            var session = await _dbContext.CheckoutSessions
                .Include(cs => cs.Items)
                .FirstOrDefaultAsync(cs => cs.CheckoutSessionId == request.CheckoutSessionId && cs.UserId == userId);


            if (session == null) throw new ArgumentException("Checkout session not found");


            // Get items to checkout based on mode
            List<CheckoutSessionItem> itemsToCheckout;


            switch (request.CheckoutMode.ToLower())
            {
                case "selected":
                    itemsToCheckout = session.Items
                        .Where(item => request.SelectedItemIds.Contains(item.CheckoutSessionItemId))
                        .ToList();
                    break;


                case "byprovider":
                    if (!request.ProviderIdFilter.HasValue) throw new ArgumentException("ProviderIdFilter required for ByProvider mode");
                    itemsToCheckout = session.Items
                        .Where(item => item.SupplierId == request.ProviderIdFilter || item.DesignerId == request.ProviderIdFilter)
                        .ToList();
                    break;


                case "all":
                    itemsToCheckout = session.Items.ToList();
                    break;


                default:
                    throw new ArgumentException($"Unknown checkout mode: {request.CheckoutMode}");
            }


            if (!itemsToCheckout.Any()) throw new ArgumentException("No items selected for checkout");


            // Group items by provider to create separate orders
            var providerGroups = itemsToCheckout
                .GroupBy(item => new { SupplierId = item.SupplierId, DesignerId = item.DesignerId, ProviderType = item.ProviderType })
                .ToList();


            // Create OrderGroup
            var orderGroup = new OrderGroup
            {
                OrderGroupId = Guid.NewGuid(),
                UserId = userId,
                Status = OrderGroupStatus.InProgress,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };


            await _orderGroupRepository.AddAsync(orderGroup);
            await _orderGroupRepository.Commit();


            var response = new CreateSessionResponse
            {
                OrderGroupId = orderGroup.OrderGroupId,
                ExpiresAt = orderGroup.ExpiresAt ?? DateTime.UtcNow.AddMinutes(30),
                Orders = new List<CheckoutOrderDto>()
            };


            // Determine shipping address
            string shippingAddress = request.ShippingAddress ?? session.ShippingAddress ?? "Default address";
            if (request.AddressId.HasValue)
            {
                shippingAddress = await _userAddressService.GetFormattedAddressAsync(request.AddressId.Value, userId) ?? shippingAddress;
            }


            // Create one order per provider
            foreach (var providerGroup in providerGroups)
            {
                var groupItems = providerGroup.ToList();
                var subtotal = groupItems.Sum(item => item.UnitPrice * item.Quantity);


                var order = new Order
                {
                    UserId = userId,
                    OrderGroupId = orderGroup.OrderGroupId,
                    CheckoutSessionId = session.CheckoutSessionId,
                    SupplierId = providerGroup.Key.SupplierId,
                    DesignerId = providerGroup.Key.DesignerId,
                    ProviderName = groupItems.First().ProviderName,
                    ProviderType = providerGroup.Key.ProviderType,
                    ShippingAddress = shippingAddress,
                    Subtotal = subtotal,
                    ShippingFee = 0,
                    Discount = 0,
                    TotalPrice = subtotal,
                    Status = OrderStatus.pending,
                    PaymentStatus = PaymentStatus.Pending,
                    FulfillmentStatus = FulfillmentStatus.None,
                    ExpiresAt = orderGroup.ExpiresAt,
                    OrderDate = DateTime.UtcNow,
                    CreateAt = DateTime.UtcNow
                };


                await _orderRepository.AddAsync(order);
                await _orderRepository.Commit();


                // Create order details
                foreach (var sessionItem in groupItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        MaterialId = sessionItem.MaterialId,
                        ProductId = sessionItem.ProductId,
                        SupplierId = sessionItem.SupplierId,
                        DesignerId = sessionItem.DesignerId,
                        Quantity = sessionItem.Quantity,
                        UnitPrice = sessionItem.UnitPrice,
                        Type = sessionItem.Type,
                        Status = OrderDetailStatus.pending
                    };


                    await _orderDetailRepository.AddAsync(orderDetail);
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
            }


            // Update OrderGroup summary
            orderGroup.TotalOrders = response.Orders.Count;
            orderGroup.CompletedOrders = 0;
            _orderGroupRepository.Update(orderGroup);
            await _orderGroupRepository.Commit();


            // Mark session as completed or update status
            session.Status = CheckoutSessionStatus.Completed;
            _checkoutSessionRepository.Update(session);
            await _checkoutSessionRepository.Commit();


            return response;
        }


        private async Task UpdateSessionSummaryAsync(Guid checkoutSessionId)
        {
            var session = await _checkoutSessionRepository.GetByIdAsync(checkoutSessionId);
            if (session == null) return;


            var items = await _dbContext.CheckoutSessionItems
                .Where(csi => csi.CheckoutSessionId == checkoutSessionId)
                .ToListAsync();


            session.TotalAmount = items.Sum(item => item.TotalPrice);
            session.TotalItems = items.Sum(item => item.Quantity);
            session.TotalProviders = items
                .GroupBy(item => new { item.SupplierId, item.DesignerId })
                .Count();


            _checkoutSessionRepository.Update(session);
            await _checkoutSessionRepository.Commit();
        }


        private string GetItemName(CheckoutSessionItem item)
        {
            if (item.Material != null) return item.Material.Name ?? "Material";
            if (item.Product?.Design != null) return item.Product.Design.Name ?? "Product";
            return "Unknown Item";
        }


        private string? GetItemImageUrl(CheckoutSessionItem item)
        {
            if (item.Material != null)
                return item.Material.MaterialImages?.FirstOrDefault()?.Image?.ImageUrl;
            // TODO: Add Product and Design image URLs
            return null;
        }


        private string? GetProviderAvatarUrl(CheckoutSessionItem item)
        {
            if (item.Supplier != null) return item.Supplier.AvatarUrl;
            if (item.Product?.Design?.DesignerProfile != null) return item.Product.Design.DesignerProfile.AvatarUrl;
            return null;
        }
    }
}


