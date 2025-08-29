using AutoMapper;
using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EcoFashionBackEnd.Services
{
    public class InventoryTransactionService
    {
        #region injection
        private readonly IRepository<ProductInventoryTransaction, int> _productTransactionRepository;
        private readonly IRepository<MaterialInventoryTransaction, int> _materialTransactionRepository;
        private readonly IRepository<Design, int> _designRepository;
        private readonly IRepository<Warehouse, int> _warehouseRepository;
        public InventoryTransactionService(
            IRepository<ProductInventoryTransaction, int> productTransactionRepository,
            IRepository<MaterialInventoryTransaction, int> materialTransactionRepository,
            IRepository<Design, int> designRepository,
            IRepository<Warehouse, int> warehouseRepository
            )
           
        {
            _productTransactionRepository = productTransactionRepository;
            _materialTransactionRepository = materialTransactionRepository;
            _designRepository = designRepository;
            _warehouseRepository = warehouseRepository;
            
        }
        #endregion


        //get all ProductInventoryTransaction   
        //get all ProductInventoryTransaction   
        public async Task<List<ProductInventoryTransaction>> GetAllProductTransactionsAsync()
        {
            return await _productTransactionRepository
                .GetAll()
                .Include(t => t.ProductInventory)
                .ThenInclude(pi => pi.Product)
                .Include(t => t.User)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<MaterialInventoryTransaction>> GetAllMaterialTransactionsAsync()
        {
            return await _materialTransactionRepository
                .GetAll()
                .Include(t => t.MaterialInventory)
                .ThenInclude (t=> t.Material)
                //.Include(t => t.User)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<InventoryTransactionDto>> GetAllTransactionsAsync()
        {
            var productTx = await _productTransactionRepository.GetAll()
                .Select(t => new InventoryTransactionDto
                {
                    TransactionId = t.TransactionId,
                    InventoryType = "Product",
                    InventoryId = t.InventoryId,
                    PerformedByUserId = t.PerformedByUserId,
                    QuantityChanged = t.QuantityChanged,
                    BeforeQty = t.BeforeQty,
                    AfterQty = t.AfterQty,
                    TransactionType = t.TransactionType,
                    Notes = t.Notes,
                    TransactionDate = t.TransactionDate
                })
                .ToListAsync();

            var materialTx = await _materialTransactionRepository.GetAll()
                .Select(t => new InventoryTransactionDto
                {
                    TransactionId = t.TransactionId,
                    InventoryType = "Material",
                    InventoryId = t.InventoryId,
                    PerformedByUserId = t.PerformedByUserId,
                    QuantityChanged = t.QuantityChanged,
                    BeforeQty = t.BeforeQty,
                    AfterQty = t.AfterQty,
                    TransactionType = t.TransactionType,
                    Notes = t.Notes,
                    TransactionDate = t.TransactionDate
                })
                .ToListAsync();

            return productTx.Concat(materialTx)
                            .OrderByDescending(t => t.TransactionDate)
                            .ToList();
        }

        public async Task<List<InventoryTransactionDto>> GetTransactionsByDesignerAsync(Guid designerId)
        {
            var productTx = await _productTransactionRepository.GetAll()
                .Where(t => t.ProductInventory.Warehouse.DesignerId == designerId)
                .Include(t => t.ProductInventory)
                    .ThenInclude(p => p.Product).ThenInclude(p => p.Design)
                .Include(t => t.User)
                .Select(t => new InventoryTransactionDto
                {
                    TransactionId = t.TransactionId,
                    InventoryType = "Product",
                    InventoryId = t.InventoryId,
                    DesignName = t.ProductInventory.Product.Design != null
                        ? t.ProductInventory.Product.Design.Name
                        : string.Empty,
                    ItemName = t.ProductInventory.Product.SKU ?? string.Empty,
                    PerformedByUserId = t.PerformedByUserId,
                    QuantityChanged = t.QuantityChanged,
                    BeforeQty = t.BeforeQty,
                    AfterQty = t.AfterQty,
                    TransactionType = t.TransactionType,
                    Notes = t.Notes,
                    TransactionDate = t.TransactionDate
                })
                .ToListAsync();

            var materialTx = await _materialTransactionRepository.GetAll()
                .Where(t => t.MaterialInventory.Warehouse.DesignerId == designerId)
                .Select(t => new InventoryTransactionDto
                {
                    TransactionId = t.TransactionId,
                    InventoryType = "Material",
                    InventoryId = t.InventoryId,
                    ItemName = t.MaterialInventory.Material.Name,
                    PerformedByUserId = t.PerformedByUserId,
                    QuantityChanged = t.QuantityChanged,
                    BeforeQty = t.BeforeQty,
                    AfterQty = t.AfterQty,
                    TransactionType = t.TransactionType,
                    Notes = t.Notes,
                    TransactionDate = t.TransactionDate
                })
                .ToListAsync();

            return productTx.Concat(materialTx)
                            .OrderByDescending(t => t.TransactionDate)
                            .ToList();
        }



    }
}