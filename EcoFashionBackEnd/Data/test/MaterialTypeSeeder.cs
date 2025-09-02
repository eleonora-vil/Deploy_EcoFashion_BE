using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoFashionBackEnd.Data.test
{
    public static class MaterialTypeSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (context.MaterialTypes.Any())
                return;

            var materialTypes = new List<MaterialType>
            {
                new MaterialType { TypeName = "Organic Cotton", Description = "High-quality organic cotton for sustainable fashion.", Category = "Natural Fiber", IsOrganic = true, IsRecycled = false, SustainabilityNotes = "GOTS", DisplayOrder = 1, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826927/organic-coton_b9zo4y.webp" },
                new MaterialType { TypeName = "Recycled Cotton", Description = "Cotton made from post-consumer waste.", Category = "Recycled Fiber", IsOrganic = false, IsRecycled = true, SustainabilityNotes = "", DisplayOrder = 2, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826928/recycled-cotton-fabric_okdkrf.jpg"  },
                new MaterialType { TypeName = "Hemp", Description = "Durable natural hemp fiber.", Category = "Natural Fiber", IsOrganic = false, IsRecycled = true, SustainabilityNotes = "", DisplayOrder = 3, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826928/hemp-fabric_gnsrs4.jpg" },
                new MaterialType { TypeName = "Recycled Polyester", Description = "Polyester made from recycled PET bottles.", Category = "Recycled Synthetic", IsOrganic = false, IsRecycled = true, SustainabilityNotes = "GRS", DisplayOrder = 4, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826928/Recycled_Polyester_skbssj.jpg" },
                new MaterialType { TypeName = "Bamboo Viscose", Description = "Soft viscose fiber from bamboo.", Category = "Regenerated Cellulose", IsOrganic = false, IsRecycled = true, SustainabilityNotes = "", DisplayOrder = 5, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826927/bamboo-viscose_c06sdo.jpg" },
                new MaterialType { TypeName = "Tencel (Lyocell)", Description = "Premium lyocell fiber from wood pulp.", Category = "Regenerated Cellulose", IsOrganic = false, IsRecycled = true, SustainabilityNotes = "GRS", DisplayOrder = 6, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826929/vai-tencel_tsyifl.jpg" },
                new MaterialType { TypeName = "Recycled Wool", Description = "Wool made from post-consumer textiles.", Category = "Recycled Animal Fiber", IsOrganic = false, IsRecycled = true, SustainabilityNotes = "", DisplayOrder = 7, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826930/recycled-wool_qog93g.webp" },
                new MaterialType { TypeName = "Organic Silk", Description = "Luxurious silk from organic mulberry farming.", Category = "Natural Animal Fiber", IsOrganic = true, IsRecycled = false, SustainabilityNotes = "GOTS", DisplayOrder = 8, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826932/organic-silk_jokpck.webp" },
                new MaterialType { TypeName = "Recycled Nylon", Description = "Nylon made from post-consumer waste.", Category = "Recycled Synthetic", IsOrganic = false, IsRecycled = true, SustainabilityNotes = "", DisplayOrder = 9, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826929/recycled-nylon_za4fkb.jpg" },
                new MaterialType { TypeName = "Organic Linen", Description = "Natural linen from organic flax farming.", Category = "Natural Fiber", IsOrganic = true, IsRecycled = false, SustainabilityNotes = "GOTS", DisplayOrder = 10, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826928/organic-linen_zgrpm3.jpg" },
                new MaterialType { TypeName = "Recycled Denim", Description = "Denim made from post-consumer jeans.", Category = "Recycled Fiber", IsOrganic = false, IsRecycled = true, SustainabilityNotes = "", DisplayOrder = 11, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826929/recycled-denim_pdpwf6.jpg" },
                new MaterialType { TypeName = "Organic Alpaca", Description = "Luxurious alpaca wool from organic farming.", Category = "Natural Animal Fiber", IsOrganic = true, IsRecycled = false, SustainabilityNotes = "", DisplayOrder = 12, IsActive = true, ImageUrl = "https://res.cloudinary.com/dguz8gz6s/image/upload/v1754826927/Alpaca-Pure-Wool-Soft_yy3ohh.jpg" }
            };

            await context.MaterialTypes.AddRangeAsync(materialTypes);
            await context.SaveChangesAsync();
        }
    }
}
