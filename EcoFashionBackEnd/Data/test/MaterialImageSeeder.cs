
using EcoFashionBackEnd.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoFashionBackEnd.Data.test
{
    public static class MaterialImageSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.MaterialImages.AnyAsync()) return;

            var materials = await context.Materials.OrderBy(m => m.MaterialId).ToListAsync();
            var materialImages = new List<MaterialImage>();

            var imageUrlsByMaterial = new Dictionary<string, List<string>>
            {
                ["Premium Organic Cotton Fabric"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756631715/CottonFabricTexture_wjcvfx.jpg",
                    "https://images.unsplash.com/photo-1638724257411-5966f826be9d?q=80&w=1974&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://images.unsplash.com/photo-1638724256973-efb31bcac1a6?q=80&w=1074&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                },
                ["Eco-Friendly Recycled Cotton Blend"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756631709/RecycledCottonBlendTexture_yt7htv.jpg",
                    "https://images.unsplash.com/photo-1562530133-cc65eaf3fd19?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://plus.unsplash.com/premium_photo-1673481695586-51c3e9ad2184?q=80&w=687&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                },
                ["Natural Hemp Fabric"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756631709/NaturalHempFabricTexture_j9c873.jpg",
                    "https://images.unsplash.com/photo-1562530133-cc65eaf3fd19?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://plus.unsplash.com/premium_photo-1673481695586-51c3e9ad2184?q=80&w=687&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                },
                ["Recycled PET Polyester Fabric"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756631710/Recycled-Polyester-Yarn-Fabric_Recycled-PET-Bottles_01-1_fbwd7w.jpg",
                    "https://images.unsplash.com/photo-1694610882150-4de206edf95a?q=80&w=822&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://images.unsplash.com/photo-1606429437134-9d975fcc508f?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                },
                ["Bamboo Viscose Fabric"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756631707/BambooViscoseFabricTexture_crcd62.jpg",
                    "https://images.unsplash.com/photo-1621882844178-fa8129633ce4?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://images.unsplash.com/photo-1621882844178-fa8129633ce4?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                },
                ["Premium Tencel Lyocell Fabric"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756631709/PremiumTencelLyocellFabricTexture_n2cl9s.avif",
                    "https://plus.unsplash.com/premium_photo-1674617774833-8c3eda65bd9e?q=80&w=687&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://plus.unsplash.com/premium_photo-1674617774845-138c3e97b5b3?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                },
                ["Recycled Wool Blend Fabric"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756631710/RecycledWoolBlendFabricTexture_yn5ju8.jpg",
                    "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                },
                ["Organic Peace Silk Fabric"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756631708/OrganicPeaceSilkFabricTexture_exv9p5.avif",
                    "https://plus.unsplash.com/premium_photo-1674617774833-8c3eda65bd9e?q=80&w=687&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                },
                ["Recycled Nylon Performance Fabric"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756636183/RecycledNylonPerformanceFabricTexture_fj1zaz.jpg",
                    "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://plus.unsplash.com/premium_photo-1674617774845-138c3e97b5b3?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                },
                ["Organic Linen Natural Fabric"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756631708/Modern_Fabric_Textures_2_bqecsa.jpg",
                    "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                },
                ["Recycled Denim Sustainable Fabric"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756631712/DeminTexture_us3lvn.jpg",
                    "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                },
                ["Organic Alpaca Luxury Fabric"] = new List<string>
                {
                    "https://res.cloudinary.com/dguz8gz6s/image/upload/v1756631649/AlpacaFabricTexture_thbjta.avif",
                    "https://plus.unsplash.com/premium_photo-1674617774845-138c3e97b5b3?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                    "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                }
            };

            foreach (var material in materials)
            {
                var key = material.Name;
                if (!imageUrlsByMaterial.TryGetValue(key, out var urls))
                {
                    // Fallback to generic images if material name not found
                    urls = new List<string>
                    {
                        "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                        "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                        "https://plus.unsplash.com/premium_photo-1737073520175-b3303a6e1e76?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                    };
                }

                foreach (var url in urls)
                {
                    var image = new Image { ImageUrl = url };
                    await context.Images.AddAsync(image);
                    await context.SaveChangesAsync(); 

                    materialImages.Add(new MaterialImage
                    {
                        MaterialId = material.MaterialId,
                        ImageId = image.ImageId
                    });
                }
            }

            await context.MaterialImages.AddRangeAsync(materialImages);
            await context.SaveChangesAsync();
        }
    }
}
