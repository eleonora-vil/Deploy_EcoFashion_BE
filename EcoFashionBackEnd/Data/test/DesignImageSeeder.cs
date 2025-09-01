using EcoFashionBackEnd.Data.test;
using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;

public static class DesignImageSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.DesignImages.AnyAsync()) return;

        var designs = await context.Designs
            .OrderBy(d => d.DesignId)
            .ToListAsync();

        var allImageEntities = new List<Image>();
        var allDesignImages = new List<DesignImage>();

        // Gộp tất cả pool hình lại
        var allLinks = SeedImageLinks.Shirt.Links
            .Concat(SeedImageLinks.Pant.Links)
            .Concat(SeedImageLinks.Skirt.Links)
            .ToList();

        // Shuffle
        var rnd = new Random();
        var shuffledLinks = allLinks.OrderBy(x => rnd.Next()).ToList();

        int linkIndex = 0;

        foreach (var design in designs)
        {
            var chosenLinks = new List<string>();

            for (int i = 0; i < 3 && linkIndex < shuffledLinks.Count; i++, linkIndex++)
            {
                chosenLinks.Add(shuffledLinks[linkIndex]);
            }

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
