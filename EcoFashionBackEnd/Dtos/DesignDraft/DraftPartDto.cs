using EcoFashionBackEnd.Entities;

namespace EcoFashionBackEnd.Dtos.DesignDraft
{
    public class DraftPartDto
    {
        public string Name { get; set; } = string.Empty;
        public float Length { get; set; }
        public float Width { get; set; }
        public int Quantity { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public string MaterialStatus { get; set; }
    }
}
