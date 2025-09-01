using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Helpers;
using Microsoft.EntityFrameworkCore;

public static class ProductSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        // Lấy tất cả 19 design đã seed
        var designs = await context.Designs
            .Include(d => d.ItemTypes)
            .OrderBy(d => d.DesignId)
            .ToListAsync();

        var sizes = await context.Sizes.ToListAsync();
        var variants = await context.DesignsVarients.ToListAsync();

        var products = new List<Product>();
        var random = new Random();

        foreach (var design in designs)
        {
            foreach (var size in sizes)
            {
                var variant = variants.FirstOrDefault(v => v.DesignId == design.DesignId && v.SizeId == size.SizeId);

                var colors = new[] { "#FF0000", "#000000", "#FFFFFF" };

                foreach (var color in colors)
                {
                    var basicColorName = ColorExchange.ClassifyColorAdvanced(color);
                    var sku = $"D{design.DesignId}-S{size.SizeId}-C{basicColorName.Replace(" ", "").ToUpper()}";
                    var price = (design.SalePrice ?? 500000m) + random.Next(0, 200000);

                    products.Add(new Product
                    {
                        DesignId = design.DesignId,
                        SizeId = size.SizeId,
                        ColorCode = color,
                        SKU = sku,
                        Price = price
                    });
                }
            }
        }

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
