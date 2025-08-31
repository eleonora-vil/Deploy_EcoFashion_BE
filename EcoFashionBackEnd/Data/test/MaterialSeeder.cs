using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoFashionBackEnd.Data.test
{
    public static class MaterialSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (context.Materials.Any())
                return;

            // Lấy Supplier One
            var supplier = await context.Suppliers.FirstOrDefaultAsync();
            if (supplier == null)
                throw new Exception("Supplier not found. Please run SupplierSeeder first.");

            var materials = new List<Material>
            {
                // Supplier One: Material 1 - Organic Cotton
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 1, // Organic Cotton
                    Name = "Premium Organic Cotton Fabric",
                    Description = "Premium Organic Cotton Fabric (Vải Bông Hữu Cơ Cao Cấp) là loại vải được làm từ sợi bông trồng theo quy trình hữu cơ, không sử dụng thuốc trừ sâu, phân bón hóa học độc hại, hạt giống biến đổi gen, và tuân thủ các tiêu chuẩn nghiêm ngặt như GOTS. Vải này thân thiện với môi trường, an toàn cho da nhạy cảm, mềm mại, thoáng khí, hút ẩm tốt và kháng khuẩn tự nhiên, thường được dùng trong sản xuất quần áo trẻ em và các sản phẩm cao cấp.",
                    RecycledPercentage = 0m, // Organic cotton typically 0% recycled (Source: GOTS standard)
                    QuantityAvailable = 500,
                    PricePerUnit = 85000m, // 85.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/organic-cotton-gots.pdf",
                    CarbonFootprint = 0.8m, // kg CO2e/mét (tốt hơn benchmark 1.2)
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 25m, // L/mét (tốt hơn benchmark 35)
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 88m, // Better than benchmark 85
                    WasteDivertedUnit = "%",
                    ProductionCountry = "India",
                    ProductionRegion = "Gujarat",
                    ManufacturingProcess = "Organic farming, mechanical processing",
                    CertificationDetails = "GOTS, OEKO-TEX Standard 100",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(2),
                    TransportDistance = 3500m,
                    TransportMethod = "Sea",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                },

                // Supplier One: Material 2 - Recycled Cotton
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 2, // Recycled Cotton
                    Name = "Eco-Friendly Recycled Cotton Blend",
                    Description = "Eco-Friendly Recycled Cotton Blend (Vải Pha Cotton Tái Chế Thân Thiện Môi Trường) là một loại vải được làm từ sự kết hợp của sợi bông tái chế với các loại sợi khác, mang lại lợi ích về môi trường như giảm rác thải và tiết kiệm tài nguyên, đồng thời giữ lại các đặc tính tốt của vải cotton.",
                    RecycledPercentage = 85m, // GRS minimum standard (Source: GRS Standard)
                    QuantityAvailable = 300,
                    PricePerUnit = 72000m, // 72.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/recycled-cotton-grs.pdf",
                    CarbonFootprint = 0.7m, // kg CO2e/mét (tốt hơn benchmark 1.0)
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 18m, // L/mét (tốt hơn benchmark 20)
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 92m, // Better than benchmark 90
                    WasteDivertedUnit = "%",
                    ProductionCountry = "Turkey",
                    ProductionRegion = "Istanbul",
                    ManufacturingProcess = "Post-consumer waste processing",
                    CertificationDetails = "GRS, RCS",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(1),
                    TransportDistance = 2800m,
                    TransportMethod = "Sea",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                },

                // Supplier One: Material 3 - Hemp
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 3, // Hemp
                    Name = "Natural Hemp Fabric",
                    Description = "Vải hemp là loại vải cao cấp, bền và thân thiện với môi trường, được dệt từ sợi cây gai dầu (Cannabis sativa), cùng loài với cây cần sa nhưng không có tác dụng gây nghiện. Vải hemp có khả năng thoáng khí, thấm hút mồ hôi tốt, chống khuẩn, chống nấm mốc, và càng giặt càng mềm mịn. Do những ưu điểm này, vải hemp được ứng dụng phổ biến trong sản xuất quần áo, phụ kiện và các sản phẩm thời trang bền vững.",
                    RecycledPercentage = 0m, // Hemp typically 0% recycled (Source: Textile Exchange)
                    QuantityAvailable = 200,
                    PricePerUnit = 95000m, // 95.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/hemp-natural.pdf",
                    CarbonFootprint = 1.1m, // Much better than benchmark 2.2
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 35m, // Much better than benchmark 75
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 96m, // Better than benchmark 85
                    WasteDivertedUnit = "%",
                    ProductionCountry = "China",
                    ProductionRegion = "Yunnan",
                    ManufacturingProcess = "Natural fiber processing",
                    CertificationDetails = "OEKO-TEX, EU ECOLABEL",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(3),
                    TransportDistance = 1200m,
                    TransportMethod = "Land",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                },

                // Supplier One: Material 4 - Recycled Polyester
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 4, // Recycled Polyester
                    Name = "Recycled PET Polyester Fabric",
                    Description = "Vải polyester tái chế (Recycled PET Polyester Fabric hay vải RPET) là loại vải nhân tạo được làm từ sợi polyester tổng hợp, tái chế từ các chai nhựa đã qua sử dụng như chai nước khoáng và nước giải khát. Loại vải này mang lại nhiều lợi ích về môi trường như giảm rác thải nhựa, tiết kiệm năng lượng và tài nguyên dầu mỏ, đồng thời giữ nguyên các đặc tính ưu việt của polyester truyền thống như độ bền cao, kháng mài mòn và dễ bảo quản.",
                    RecycledPercentage = 90m, // GRS minimum standard (Source: GRS Standard)
                    QuantityAvailable = 400,
                    PricePerUnit = 65000m, // 65.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/recycled-polyester-grs.pdf",
                    CarbonFootprint = 1.4m, // Better than benchmark 1.6
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 48m, // Better than benchmark 50
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 89m, // Better than benchmark 88
                    WasteDivertedUnit = "%",
                    ProductionCountry = "Taiwan",
                    ProductionRegion = "Taiwan",
                    ManufacturingProcess = "PET bottle recycling",
                    CertificationDetails = "GRS, OEKO-TEX Standard 100",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(2),
                    TransportDistance = 4200m,
                    TransportMethod = "Sea",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                },

                // Supplier One: Material 5 - Bamboo
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 5, // Bamboo Viscose
                    Name = "Bamboo Viscose Fabric",
                    Description = "Vải Bamboo Viscose là loại vải sợi tre được sản xuất bằng quy trình hóa học để biến bột giấy tre thành sợi vải Viscose, mang lại cảm giác mềm mại, thoáng khí, hút ẩm tốt và có khả năng kháng khuẩn tự nhiên, thân thiện với da và môi trường. Loại vải này có bề ngoài giống lụa, ít nhăn, nhẹ, có khả năng không tích điện và phân hủy sinh học. Tuy nhiên, quy trình sản xuất hóa học gây ra một số lo ngại về tác động môi trường.",
                    RecycledPercentage = 0m, // Bamboo typically 0% recycled (Source: EU Ecolabel)
                    QuantityAvailable = 250,
                    PricePerUnit = 78000m, // 78.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/bamboo-viscose-oeko.pdf",
                    CarbonFootprint = 1.7m, // Much better than benchmark 2.8
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 42m, // Much better than benchmark 95
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 94m, // Better than benchmark 82
                    WasteDivertedUnit = "%",
                    ProductionCountry = "China",
                    ProductionRegion = "Beijing",
                    ManufacturingProcess = "Bamboo pulp processing",
                    CertificationDetails = "OEKO-TEX Standard 100",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(1),
                    TransportDistance = 1800m,
                    TransportMethod = "Land",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                },

                // Supplier One: Material 6 - Tencel
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 6, // Tencel
                    Name = "Tencel Lyocell Fabric",
                    Description = "Vải Tencel Lyocell là loại sợi sinh học cao cấp, được làm từ bột gỗ cây trồng bền vững như bạch đàn, có quy trình sản xuất thân thiện môi trường. Đặc điểm nổi bật của vải bao gồm sự mềm mại như lụa, khả năng hút ẩm và thoáng khí tốt, kháng khuẩn tự nhiên, chống nhăn, giữ màu bền và có khả năng phân hủy sinh học. Vải được ứng dụng rộng rãi trong may mặc, chăn ga gối nệm, đồ nội thất và các sản phẩm tiêu dùng khác.",
                    RecycledPercentage = 0m, // Tencel typically 0% recycled (Source: EU Ecolabel)
                    QuantityAvailable = 180,
                    PricePerUnit = 120000m, // 120.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/tencel-lyocell-oeko.pdf",
                    CarbonFootprint = 1.9m, // Better than benchmark 2.1
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 65m, // Better than benchmark 70
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 87m, // Better than benchmark 85
                    WasteDivertedUnit = "%",
                    ProductionCountry = "Austria",
                    ProductionRegion = "Vienna",
                    ManufacturingProcess = "Lyocell process",
                    CertificationDetails = "OEKO-TEX Standard 100",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(2),
                    TransportDistance = 8500m,
                    TransportMethod = "Air",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                },

                // Supplier One: Material 7 - Recycled Wool
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 7, // Recycled Wool
                    Name = "Recycled Wool Blend Fabric",
                    Description = "Vải Recycled Wool blend (hoặc vải pha len) là sự kết hợp giữa sợi len tự nhiên (từ cừu, dê, lạc đà) với một hoặc nhiều loại sợi tổng hợp khác (như polyester, cotton), tạo ra một loại vải có nhiều ưu điểm như: giữ ấm tốt, độ bền cao, chống nhăn, giữ form dáng tốt, dễ chăm sóc hơn vải len 100% và có chi phí hợp lý hơn. Vải wool blend mang lại sự thoải mái và đa dạng trong các sản phẩm may mặc, từ áo khoác, quần suit đến các sản phẩm thời trang khác. ",
                    RecycledPercentage = 88m, // RWS minimum standard (Source: RWS Standard)
                    QuantityAvailable = 120,
                    PricePerUnit = 150000m, // 150.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/recycled-wool-rws.pdf",
                    CarbonFootprint = 1.7m, // Better than benchmark 1.9
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 58m, // Better than benchmark 65
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 94m, // Better than benchmark 92
                    WasteDivertedUnit = "%",
                    ProductionCountry = "Italy",
                    ProductionRegion = "Milan",
                    ManufacturingProcess = "Post-consumer wool processing",
                    CertificationDetails = "RWS, OEKO-TEX",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(1),
                    TransportDistance = 9500m,
                    TransportMethod = "Sea",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                },

                // Supplier One: Material 8 - Organic Silk
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 8, // Organic Silk
                    Name = "Organic Silk Fabric",
                    Description = "Vải lụa silk là loại vải tự nhiên, mềm mại, bóng mượt và sang trọng, được làm từ sợi tơ tằm có cấu trúc protein độc đáo, giúp phản chiếu ánh sáng tạo nên vẻ ngoài óng ánh đặc trưng. Vải silk có đặc tính thoáng khí, thấm hút ẩm tốt, mang lại cảm giác mát lạnh và thoải mái khi sử dụng. Vì quy trình sản xuất phức tạp, vải lụa silk thường có giá thành cao và được ứng dụng rộng rãi trong thời trang cao cấp, trang phục dự tiệc, váy cưới và các sản phẩm nội thất sang trọng.",
                    RecycledPercentage = 0m, // Organic silk typically 0% recycled (Source: GOTS Standard)
                    QuantityAvailable = 80,
                    PricePerUnit = 250000m, // 250.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/organic-silk-gots.pdf",
                    CarbonFootprint = 2.2m, // Better than benchmark 2.4
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 75m, // Better than benchmark 80
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 80m, // Better than benchmark 78
                    WasteDivertedUnit = "%",
                    ProductionCountry = "India",
                    ProductionRegion = "Mumbai",
                    ManufacturingProcess = "Ahimsa silk production",
                    CertificationDetails = "GOTS, OEKO-TEX",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(2),
                    TransportDistance = 3800m,
                    TransportMethod = "Sea",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                },

                // Supplier One: Material 9 - Recycled Nylon
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 9, // Recycled Nylon
                    Name = "Recycled Nylon Fabric",
                    Description = "Vải nylon tái chế (Recycled Nylon Fabric) là loại vải được sản xuất từ các vật liệu nylon đã qua sử dụng, chẳng hạn như lưới đánh cá cũ hoặc thảm cũ, sau đó được tái chế thành sợi và dệt lại thành vải. Vải này nổi bật với tính bền vững, thân thiện với môi trường do giảm thiểu rác thải nhựa, đồng thời vẫn giữ được các đặc tính tốt của nylon như độ bền, khả năng chống thấm nước, và cảm giác mềm mại. ",
                    RecycledPercentage = 80m, // GRS minimum standard (Source: GRS Standard)
                    QuantityAvailable = 150,
                    PricePerUnit = 95000m, // 95.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/recycled-nylon-grs.pdf",
                    CarbonFootprint = 1.2m, // Better than benchmark 1.4
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 40m, // Better than benchmark 45
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 87m, // Better than benchmark 85
                    WasteDivertedUnit = "%",
                    ProductionCountry = "Spain",
                    ProductionRegion = "Barcelona",
                    ManufacturingProcess = "Fishing net recycling",
                    CertificationDetails = "GRS, OEKO-TEX Standard 100",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(1),
                    TransportDistance = 12000m,
                    TransportMethod = "Sea",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                },

                // Supplier One: Material 10 - Organic Linen
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 10, // Organic Linen
                    Name = "Organic Linen Fabric",
                    Description = "Vải lanh hữu cơ (Organic Linen Fabric) là chất liệu tự nhiên, an toàn cho da và môi trường, được làm từ sợi cây lanh trồng theo phương pháp hữu cơ, không sử dụng hóa chất độc hại. Đặc điểm của loại vải này là mềm mại, thoáng mát, thấm hút mồ hôi tốt, cực kỳ bền chắc và có khả năng phân hủy sinh học, rất phù hợp cho thời trang bền vững và nội thất.",
                    RecycledPercentage = 0m, // Organic linen typically 0% recycled (Source: GOTS Standard)
                    QuantityAvailable = 100,
                    PricePerUnit = 180000m, // 180.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/organic-linen-gots.pdf",
                    CarbonFootprint = 2.5m, // Better than benchmark 2.8
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 85m, // Better than benchmark 90
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 85m, // Better than benchmark 82
                    WasteDivertedUnit = "%",
                    ProductionCountry = "Belgium",
                    ProductionRegion = "Brussels",
                    ManufacturingProcess = "Organic flax processing",
                    CertificationDetails = "GOTS, OEKO-TEX Standard 100",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(2),
                    TransportDistance = 10500m,
                    TransportMethod = "Sea",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                },

                // Supplier One: Material 11 - Recycled Denim
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 11, // Recycled Denim
                    Name = "Recycled Denim Fabric",
                    Description = "Recycled Denim là loại vải cotton dày, chắc, dệt chéo bằng sợi ngang và sợi dọc, tạo ra các đường sọc chéo rõ nét trên bề mặt. Vải denim đặc trưng với độ bền cao, chịu mài mòn tốt, khả năng giữ màu và có thể nhuộm bằng thuốc nhuộm chàm để tạo màu xanh đặc trưng. Ngày nay, denim còn được pha trộn với sợi polyester hoặc lycra để tăng độ mềm mại, co giãn, và ứng dụng đa dạng trong quần áo, phụ kiện thời trang, và đồ nội thất.",
                    RecycledPercentage = 85m, // GRS minimum standard (Source: GRS Standard)
                    QuantityAvailable = 200,
                    PricePerUnit = 85000m, // 85.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/recycled-denim-grs.pdf",
                    CarbonFootprint = 1.1m, // Better than benchmark 1.3
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 35m, // Better than benchmark 40
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 94m, // Better than benchmark 92
                    WasteDivertedUnit = "%",
                    ProductionCountry = "Turkey",
                    ProductionRegion = "Istanbul",
                    ManufacturingProcess = "Post-consumer denim processing",
                    CertificationDetails = "GRS, OEKO-TEX Standard 100",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(1),
                    TransportDistance = 2900m,
                    TransportMethod = "Sea",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                },

                // Supplier One: Material 12 - Organic Alpaca
                new Material
                {
                    SupplierId = supplier.SupplierId,
                    TypeId = 12, // Organic Alpaca
                    Name = "Organic Alpaca Wool Fabric",
                    Description = "Vải len Alpaca là một loại vải tự nhiên cao cấp, được làm từ lớp lông mềm mại, ấm áp và bền chắc của loài alpaca, động vật bản địa ở dãy Andes, Nam Mỹ. Vải có đặc tính mềm, không gây ngứa như len cừu, có khả năng giữ ấm và thoáng khí, và thường được dùng để sản xuất quần áo, khăn choàng và các sản phẩm giữ ấm khác.",
                    RecycledPercentage = 0m, // Organic alpaca typically 0% recycled (Source: RWS Standard)
                    QuantityAvailable = 60,
                    PricePerUnit = 350000m, // 350.000 VNĐ/mét (đơn vị: nghìn VNĐ)
                    DocumentationUrl = "https://example.com/certificates/organic-alpaca-rws.pdf",
                    CarbonFootprint = 1.9m, // Better than benchmark 2.1
                    CarbonFootprintUnit = "kg CO2e/mét",
                    WaterUsage = 65m, // Better than benchmark 70
                    WaterUsageUnit = "lít/mét",
                    WasteDiverted = 82m, // Better than benchmark 80
                    WasteDivertedUnit = "%",
                    ProductionCountry = "Peru",
                    ProductionRegion = "Lima",
                    ManufacturingProcess = "Free-range alpaca farming",
                    CertificationDetails = "RWS, OEKO-TEX Standard 100",
                    CertificationExpiryDate = DateTime.UtcNow.AddYears(2),
                    TransportDistance = 18500m,
                    TransportMethod = "Air",
                    ApprovalStatus = "Approved",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                }
            };

            await context.Materials.AddRangeAsync(materials);
            await context.SaveChangesAsync();
        }
    }
}
