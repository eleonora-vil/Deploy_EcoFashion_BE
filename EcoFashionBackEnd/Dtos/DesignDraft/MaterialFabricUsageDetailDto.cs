namespace EcoFashionBackEnd.Dtos.DesignDraft
{
    public class MaterialFabricUsageDetailDto
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public decimal BaseMeterUsed { get; set; }
        public decimal DesignerStock { get; set; }
        public decimal SupplierStock { get; set; }
    }
    public class FabricRequirementDto
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public decimal MetersPerProduct { get; set; }
        public decimal DesignerStock { get; set; }
        public decimal SupplierStock { get; set; }
    }

    public class FabricRequirement
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public decimal RequiredMeters { get; set; }

    }
    public class FabricUsageDto
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public decimal RequiredMeters { get; set; }
        public decimal? DesignerStock { get; set; }
        public decimal? SupplierStock { get; set; }
    }

}
