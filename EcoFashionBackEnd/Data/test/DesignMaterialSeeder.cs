using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoFashionBackEnd.Data.Seeding
{
    public static class DesignMaterialSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.DesignsMaterials.AnyAsync()) return;

            var designs = await context.Designs.ToListAsync();
            var materials = await context.Materials.ToListAsync();

            if (!designs.Any() || !materials.Any()) return;

            var designMaterials = new List<DesignsMaterial>();
            var random = new Random();

            foreach (var design in designs)
            {
                // mỗi design có 2–3 material
                var numMaterials = random.Next(2, 4);
                var usedMaterials = materials
                    .OrderBy(x => random.Next())
                    .Take(numMaterials)
                    .ToList();

                // phân bổ % sử dụng
                float remaining = 100f;
                for (int i = 0; i < usedMaterials.Count; i++)
                {

                    designMaterials.Add(new DesignsMaterial
                    {
                        DesignId = design.DesignId,
                        MaterialId = usedMaterials[i].MaterialId,
                        MeterUsed = random.Next(1, 10)
                    });
                }
            }

            await context.DesignsMaterials.AddRangeAsync(designMaterials);
            await context.SaveChangesAsync();
        }
    }
}
