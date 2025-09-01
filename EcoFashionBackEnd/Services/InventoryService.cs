using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcoFashionBackEnd.Services
{
    public class InventoryService
    {
        private readonly IRepository<Product, int> _productRepository;
        private readonly IRepository<Material, int> _materialRepository;
        private readonly IRepository<ProductInventory, int> _productInventoryRepository;
        private readonly IRepository<DesignerMaterialInventory, int> _designerMaterialInventory;
        private readonly IRepository<MaterialInventoryTransaction, int> _materialInventoryTransactionRepository;
        private readonly IRepository<ProductInventoryTransaction, int> _productInventoryTransactionRepository;

        private readonly IRepository<Warehouse, int> _warehouseRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public InventoryService(
          IRepository<Product, int> productRepository,
          IRepository<Material, int> materialRepository,
          IRepository<ProductInventory, int> productInventoryRepository,
          IRepository<DesignerMaterialInventory, int> designerMaterialInventory,
          IRepository<MaterialInventoryTransaction, int> materialInventoryTransactionRepository,
          IRepository<ProductInventoryTransaction, int> productInventoryTransactionRepository,

          IRepository<Warehouse, int> warehouseRepository,
        IHttpContextAccessor httpContextAccessor
          )
        {
            _productRepository = productRepository;
            _materialRepository = materialRepository;
            _productInventoryRepository = productInventoryRepository;
            _designerMaterialInventory = designerMaterialInventory;
            _materialInventoryTransactionRepository = materialInventoryTransactionRepository;
            _productInventoryTransactionRepository = productInventoryTransactionRepository;
            _warehouseRepository = warehouseRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task AddProductInventoriesAsync(List<(int productId, int warehouseId, int quantity)> changes)
        {
            
            var now = DateTime.UtcNow;
            // Lấy User ID từ HttpContext
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                // Xử lý trường hợp không tìm thấy UserId (ví dụ: yêu cầu không xác thực)
                throw new UnauthorizedAccessException("Người dùng không được xác thực.");
            }

            if (!int.TryParse(userIdString, out int userId))
            {
                // Xử lý trường hợp UserId không hợp lệ (ví dụ: không phải số nguyên)
                throw new InvalidOperationException("ID người dùng không hợp lệ.");
            }

            if (changes == null || !changes.Any())
                return;

            var groupedChanges = changes
                .GroupBy(c => new { c.productId, c.warehouseId })
                .Select(g => new
                {
                    ProductId = g.Key.productId,
                    WarehouseId = g.Key.warehouseId,
                    TotalQuantity = g.Sum(x => x.quantity)
                })
                .ToList();

            foreach (var change in groupedChanges)
            {
                // 🔎 Tìm inventory
                var inventory = await _productInventoryRepository
                      .FindByCondition(pi =>
                          pi.ProductId == change.ProductId &&
                          pi.WarehouseId == change.WarehouseId)
                      .FirstOrDefaultAsync();


                decimal beforeQty = inventory?.QuantityAvailable ?? 0;

                bool isNewInventory = inventory == null;

                if (isNewInventory)
                {
                    // ➕ Tạo mới
                    inventory = new ProductInventory
                    {
                        ProductId = change.ProductId,
                        WarehouseId = change.WarehouseId,
                        QuantityAvailable = change.TotalQuantity,
                        LastUpdated = DateTime.UtcNow
                    };

                    await _productInventoryRepository.AddAsync(inventory);
                    await _productInventoryRepository.Commit(); // 💡 để có InventoryId
                }
                else
                {
                    // 🔄 Cập nhật
                    inventory.QuantityAvailable += change.TotalQuantity;
                    inventory.LastUpdated = DateTime.UtcNow;
                    _productInventoryRepository.Update(inventory);
                    await _productInventoryRepository.Commit(); // 💡 commit để transaction chắc chắn thấy AfterQty
                }

                decimal afterQty = inventory.QuantityAvailable;

                // 📝 Log transaction
                var transaction = new ProductInventoryTransaction
                {
                    InventoryId = inventory.InventoryId,
                    QuantityChanged = change.TotalQuantity,
                    PerformedByUserId = userId,
                    BeforeQty = beforeQty,
                    AfterQty = afterQty,
                    TransactionType = change.TotalQuantity >= 0 ? "Import" : "Export",
                    TransactionDate = DateTime.Now,
                    Notes = isNewInventory
        ? "Tạo mới sản phẩm trong kho."
        : (change.TotalQuantity >= 0 ? "Nhập kho sản phẩm." : "Xuất kho sản phẩm.")
                };

                await _productInventoryTransactionRepository.AddAsync(transaction);
                await _productInventoryTransactionRepository.Commit();
            }
        }





        public async Task DeductMaterialsAsync(Guid designerId, Dictionary<int, decimal> usageMap)
        {
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                // Xử lý trường hợp không tìm thấy UserId (ví dụ: yêu cầu không xác thực)
                throw new UnauthorizedAccessException("Người dùng không được xác thực.");
            }

            if (!int.TryParse(userIdString, out int userId))
            {
                // Xử lý trường hợp UserId không hợp lệ (ví dụ: không phải số nguyên)
                throw new InvalidOperationException("ID người dùng không hợp lệ.");
            }
            var warehouse = await _warehouseRepository
             .FindByCondition(w => w.DesignerId == designerId && w.WarehouseType == "Material")
                .FirstOrDefaultAsync();
            if (warehouse == null)
            {
                throw new Exception($"Không tìm thấy kho vật liệu cho designerId={designerId}");
            }

            var materialIds = usageMap.Keys.ToList();


            var inventories = await _designerMaterialInventory.GetAll()
                .Where(i => i.WarehouseId == warehouse.WarehouseId && materialIds.Contains(i.MaterialId))
                .Include(i => i.Material)
                .ToDictionaryAsync(i => i.MaterialId);

            // Bước 2: Xử lý từng vật liệu cần trừ
            foreach (var materialId in usageMap.Keys)
            {
                var requiredQty = usageMap[materialId];


                if (!inventories.TryGetValue(materialId, out var inventory))
                {
                    throw new Exception($"Không tìm thấy kho vật liệu MaterialId={materialId} của designer");
                }


                if (inventory.Quantity < requiredQty)
                {
                    throw new Exception(
                                        $"Kho vật liệu không đủ cho '{inventory.Material.Name}' (MaterialId={materialId}). " +
                                        $"Yêu cầu: {requiredQty}m, Tồn kho: {inventory.Quantity}m" +
                                        $"Cần {requiredQty- inventory.Quantity}");
                                        }

                var originalQuantity = inventory.Quantity;
                inventory.Quantity -= requiredQty;

                var transaction = new MaterialInventoryTransaction
                {
                    InventoryId = inventory.InventoryId,
                    QuantityChanged = -requiredQty,
                    BeforeQty = originalQuantity,
                    AfterQty = originalQuantity-requiredQty,
                    PerformedByUserId = userId,
                    TransactionType = "Usage",
                    Notes = $"Trừ vật liệu cho sản phẩm",
                };
                await _materialInventoryTransactionRepository.AddAsync(transaction);

                _designerMaterialInventory.Update(inventory);
            }


            await _designerMaterialInventory.Commit();
            await _materialInventoryTransactionRepository.Commit();
        }

        //Cộng kho khi mua vật liệu
        public async Task AddDesignerMaterialsAsync(Guid designerId, Dictionary<int, decimal> addMap, int orderId)
        {
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                throw new UnauthorizedAccessException("Người dùng không được xác thực.");
            }

            if (!int.TryParse(userIdString, out int userId))
            {
                throw new InvalidOperationException("ID người dùng không hợp lệ.");
            }

            if (addMap == null || addMap.Count == 0)
            {
                return;
            }

            var warehouse = await _warehouseRepository
             .FindByCondition(w => w.DesignerId == designerId && w.WarehouseType == "Material")
                .FirstOrDefaultAsync();

            if (warehouse == null)
            {
                throw new Exception($"Không tìm thấy kho vật liệu cho designerId={designerId}");
            }

            var materialIds = addMap.Keys.ToList();

            var inventories = await _designerMaterialInventory.GetAll()
                .Where(i => i.WarehouseId == warehouse.WarehouseId && materialIds.Contains(i.MaterialId))
                .Include(i => i.Material)
                .ToDictionaryAsync(i => i.MaterialId);

            foreach (var materialId in addMap.Keys)
            {
                var addQty = addMap[materialId];

                if (addQty <= 0)
                {
                    continue;
                }

                if (!inventories.TryGetValue(materialId, out var inventory))
                {
                    var price = await _materialRepository
                         .GetAll()
                         .Where(m => m.MaterialId == materialId)
                         .Select(m => m.PricePerUnit)
                         .FirstOrDefaultAsync();
                    inventory = new DesignerMaterialInventory
                    {
                        WarehouseId = warehouse.WarehouseId,
                        MaterialId = materialId,
                        Quantity = addQty,
                        LastBuyDate = DateTime.UtcNow,
                        Status = addQty > 0 ? "In Stock" : "Out of Stock",
                        Cost = addQty * price,
                    };

                    await _designerMaterialInventory.AddAsync(inventory);
                    await _designerMaterialInventory.Commit();

                    var transactionNew = new MaterialInventoryTransaction
                    {
                        InventoryId = inventory.InventoryId,
                        QuantityChanged = addQty,
                        BeforeQty = 0,
                        AfterQty = addQty,
                        PerformedByUserId = userId,
                        TransactionType = "Import",
                        TransactionDate = DateTime.Now,
                        Notes = $"Nhập kho vật liệu cho Nhà Thiết Kế từ đơn hàng #{orderId}"
                    };

                    await _materialInventoryTransactionRepository.AddAsync(transactionNew);
                }
                else
                {
                    var beforeQty = inventory.Quantity ?? 0;
                    var afterQty = beforeQty + addQty;

                    inventory.Quantity = afterQty;
                    inventory.LastBuyDate = DateTime.UtcNow;
                    inventory.Status = afterQty > 0 ? "In Stock" : "Out of Stock";
                    inventory.Cost = afterQty * inventory.Material.PricePerUnit;

                    _designerMaterialInventory.Update(inventory);
                    await _designerMaterialInventory.Commit();

                    var transaction = new MaterialInventoryTransaction
                    {
                        InventoryId = inventory.InventoryId,
                        QuantityChanged = addQty,
                        BeforeQty = beforeQty,
                        AfterQty = afterQty,
                        PerformedByUserId = userId,
                        TransactionType = "Import",
                        TransactionDate = DateTime.Now,
                        Notes = $"Nhập kho vật liệu cho Nhà Thiết Kế từ đơn hàng #{orderId}"
                    };

                    await _materialInventoryTransactionRepository.AddAsync(transaction);
                }
            }

            await _materialInventoryTransactionRepository.Commit();
        }

    }
}
