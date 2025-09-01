using EcoFashionBackEnd.Data.test;
using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoFashionBackEnd.Data.Seeding
{
    public static class DesignImageSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.DesignImages.AnyAsync()) return;

            var designs = await context.Designs
                .OrderBy(d => d.DesignId)
                .ToListAsync();

            var random = new Random();
            var allImageEntities = new List<Image>();
            var allDesignImages = new List<DesignImage>();

            foreach (var design in designs)
            {
                string[] sourceLinks;

                switch (design.ItemTypeId)
                {
                    case 1: // Shirt
                        sourceLinks = SeedImageLinks.Shirt.Links;
                        break;
                    case 2: // Pant
                        sourceLinks = SeedImageLinks.Pant.Links;
                        break;
                    case 3: // Skirt
                        sourceLinks = SeedImageLinks.Skirt.Links;
                        break;
                    default: // fallback
                        sourceLinks = SeedImageLinks.Shirt.Links;
                        break;
                }

                var chosenLinks = sourceLinks
                    .OrderBy(x => random.Next())
                    .Take(3)
                    .ToList();

                foreach (var url in chosenLinks)
                {
                    var image = new Image { ImageUrl = url };
                    allImageEntities.Add(image);

                    allDesignImages.Add(new DesignImage
                    {
                        Design = design,
                        Image = image
                    });
                }
            }

            await context.Images.AddRangeAsync(allImageEntities);
            await context.SaveChangesAsync();

            await context.DesignImages.AddRangeAsync(allDesignImages);
            await context.SaveChangesAsync();
        }
    }
}
