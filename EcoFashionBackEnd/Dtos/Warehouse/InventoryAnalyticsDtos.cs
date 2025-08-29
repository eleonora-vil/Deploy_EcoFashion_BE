using System;

namespace EcoFashionBackEnd.Dtos.Warehouse
{
    public class InventorySummaryDto
    {
        public int TotalDistinctMaterials { get; set; }
        public decimal TotalOnHand { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public int LowStockCount { get; set; }
        public int StockoutCount { get; set; }
    }

    public class MovementPointDto
    {
        public DateTime Date { get; set; }
        public decimal InQty { get; set; }
        public decimal OutQty { get; set; }
        public decimal NetQty { get; set; }
    }

    public class LowStockItemDto
    {
        public int MaterialId { get; set; }
        public string? MaterialName { get; set; }
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal MinThreshold { get; set; }
        public decimal Difference { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal EstimatedValue { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class SupplierReceiptPointDto
    {
        public DateTime Date { get; set; }
        public decimal Quantity { get; set; }
        public string? SupplierName { get; set; }
    }

    public class ProductSummaryDto
    {
        public int TotalProducts { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalInProduction { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public int LowStockCount { get; set; }
    }

    public class ProductLowStockItemDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? SKU { get; set; }
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int QuantityAvailable { get; set; }
        public int MinThreshold { get; set; }
        public int Difference { get; set; }
        public decimal EstimatedValue { get; set; }
        public DateTime LastUpdated { get; set; }
        public string? DesignName { get; set; }
        public string? SizeName { get; set; }
        public string? ColorCode { get; set; }
    }

    public class ProductActivityPointDto
    {
        public DateTime Date { get; set; }
        public int Produced { get; set; }
        public int Sold { get; set; }
        public string? DesignName { get; set; }
    }

    public class ProductTransactionDto
    {
        public int TransactionId { get; set; }
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public int QuantityChanged { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Notes { get; set; }
        public string? ProductName { get; set; }
        public string? SKU { get; set; }
        public string? WarehouseName { get; set; }
        public string? DesignName { get; set; }
        public string? SizeName { get; set; }
        public string? ColorCode { get; set; }
    }
}


