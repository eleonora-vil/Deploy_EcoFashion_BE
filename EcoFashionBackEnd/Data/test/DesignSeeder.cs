using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;

public static class DesignSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Designs.AnyAsync()) return;

        var designer = await context.Designers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.User.Email == "designer@example.com");

        if (designer == null) throw new Exception("Designer not found");

        var itemTypes = await context.ItemTypes.ToListAsync();
        if (itemTypes.Count == 0)
            throw new Exception("ItemTypes chưa seed hoặc không tồn tại");

        var random = new Random();
        int minPrice = 500_000;
        int maxPrice = 1_000_000;

        // 19 design: 6 Pant, 6 Shirt, 7 Skirt
        var designMapping = new Dictionary<int, string[]>
        {
            [1] = new[] // Áo (Shirt) - 6
            {
                "Eco Hoodie", "Organic T-Shirt", "Linen Blouse",
                "Bamboo Shirt", "Recycled Jacket", "Sustainable Polo"
            },
            [2] = new[] // Quần (Pant) - 6
            {
                "Sustainable Jeans", "RPET Shorts", "Eco Cargo Pants",
                "Recycled Chinos", "Vegan Joggers", "Organic Trousers"
            },
            [3] = new[] // Váy (Skirt) - 7
            {
                "Bio Cotton Skirt", "Linen Skirt", "Hemp Skirt",
                "Recycled Denim Skirt", "Eco Mini Skirt", "Bamboo Midi Skirt", "Organic Maxi Skirt"
            }
        };

        var designs = new List<Design>();
        var designFeatures = new List<DesignFeature>();

        foreach (var kv in designMapping)
        {
            int typeId = kv.Key;
            var designNames = kv.Value;

            foreach (var name in designNames)
            {
                var design = new Design
                {
                    Name = name,
                    Description = $"This is a sustainable design: {name}",
                    DesignerId = designer.DesignerId,
                    RecycledPercentage = random.Next(10, 100),
                    SalePrice = random.Next(minPrice, maxPrice + 1),
                    ProductScore = 5,
                    CreatedAt = DateTime.UtcNow,
                    CareInstruction = "Wash cold, hang dry",
                    ItemTypeId = typeId, // dùng Id thẳng
                    CarbonFootprint = (float)Math.Round(random.NextDouble() * 50, 2),
                    WaterUsage = (float)Math.Round(random.NextDouble() * 100, 2),
                    WasteDiverted = (float)Math.Round(random.NextDouble() * 20, 2),
                    LaborHours = (float)Math.Round(random.NextDouble() * 10, 2),
                    LaborCostPerHour = (decimal)Math.Round(random.NextDouble() * 50, 2)
                };

                designs.Add(design);

                designFeatures.Add(new DesignFeature
                {
                    Design = design,
                    ReduceWaste = random.Next(0, 2) == 1,
                    LowImpactDyes = random.Next(0, 2) == 1,
                    Durable = random.Next(0, 2) == 1,
                    EthicallyManufactured = random.Next(0, 2) == 1
                });
            }
        }

        await context.Designs.AddRangeAsync(designs);
        await context.DesignFeatures.AddRangeAsync(designFeatures);
        await context.SaveChangesAsync();
    }
}
