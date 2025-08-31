namespace EcoFashionBackEnd.Entities
{
    /// <summary>
    /// Enum cho các loại giao dịch material inventory
    /// </summary>
    public enum MaterialTransactionType
    {
        // Nhập kho từ supplier (khi material được approve)       
        SupplierReceipt,
        
        // Bán material cho customer (khi order payment thành công)    
        CustomerSale,
        
        // Điều chỉnh thủ công bởi admin/supplier     
        ManualAdjustment,
        
        // Sử dụng để sản xuất (designer sử dụng material)       
        ProductionUse,
        
        // Chuyển kho vào       
        TransferIn,
        
        // Chuyển kho ra        
        TransferOut,
        
        // Trả hàng từ customer
        
        CustomerReturn,
        
        // Trả hàng cho supplier
        
        SupplierReturn,
        
        // Hỏng/mất mát
        Damaged,
        

        // Kiểm kê
        StockAudit
    }
    
    /// <summary>
    /// Extension methods cho MaterialTransactionType
    /// </summary>
    public static class MaterialTransactionTypeExtensions
    {
        /// <summary>
        /// Lấy mô tả cho loại transaction
        /// </summary>
        public static string GetDescription(this MaterialTransactionType type)
        {
            return type switch
            {
                MaterialTransactionType.SupplierReceipt => "Nhập kho từ nhà cung cấp",
                MaterialTransactionType.CustomerSale => "Bán cho khách hàng",
                MaterialTransactionType.ManualAdjustment => "Điều chỉnh thủ công",
                MaterialTransactionType.ProductionUse => "Sử dụng để sản xuất",
                MaterialTransactionType.TransferIn => "Chuyển kho vào",
                MaterialTransactionType.TransferOut => "Chuyển kho ra",
                MaterialTransactionType.CustomerReturn => "Trả hàng từ khách hàng",
                MaterialTransactionType.SupplierReturn => "Trả hàng cho nhà cung cấp",
                MaterialTransactionType.Damaged => "Hỏng/mất mát",
                MaterialTransactionType.StockAudit => "Kiểm kê kho",
                _ => "Không xác định"
            };
        }
        
        /// <summary>
        /// Kiểm tra xem transaction có làm tăng inventory không
        /// </summary>
        public static bool IsIncrease(this MaterialTransactionType type)
        {
            return type switch
            {
                MaterialTransactionType.SupplierReceipt => true,
                MaterialTransactionType.TransferIn => true,
                MaterialTransactionType.CustomerReturn => true,
                MaterialTransactionType.ManualAdjustment => false, // Có thể + hoặc -
                MaterialTransactionType.StockAudit => false, // Có thể + hoặc -
                _ => false // Tất cả loại khác đều làm giảm inventory
            };
        }
        
        /// <summary>
        /// Lấy màu hiển thị cho UI
        /// </summary>
        public static string GetDisplayColor(this MaterialTransactionType type)
        {
            return type switch
            {
                MaterialTransactionType.SupplierReceipt => "green",
                MaterialTransactionType.CustomerSale => "blue",
                MaterialTransactionType.CustomerReturn => "orange",
                MaterialTransactionType.TransferIn => "teal",
                MaterialTransactionType.TransferOut => "purple",
                MaterialTransactionType.ProductionUse => "indigo",
                MaterialTransactionType.ManualAdjustment => "yellow",
                MaterialTransactionType.SupplierReturn => "red",
                MaterialTransactionType.Damaged => "red",
                MaterialTransactionType.StockAudit => "gray",
                _ => "gray"
            };
        }
    }
}
