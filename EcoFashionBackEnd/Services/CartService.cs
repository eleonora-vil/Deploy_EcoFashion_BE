using EcoFashionBackEnd.Common.Payloads.Requests;
using EcoFashionBackEnd.Dtos.Cart;
using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EcoFashionBackEnd.Services
{
	public class CartService
	{
		private readonly AppDbContext _db;
		public CartService(AppDbContext db)
		{
			_db = db;
		}

		public async Task<CartDto> GetOrCreateActiveCartAsync(int userId)
		{
			var cart = await _db.Carts.Include(c => c.Items)
				.FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
			if (cart == null)
			{
				cart = new Cart { UserId = userId, IsActive = true };
				_db.Carts.Add(cart);
				await _db.SaveChangesAsync();
			}
			return await BuildCartDto(cart.CartId);
		}

		public async Task<CartDto> UpsertItemAsync(int userId, UpsertCartItemRequest request)
		{
			var cartId = await EnsureCartId(userId);
			
			if (request.ItemType == "material" && request.MaterialId.HasValue)
			{
				return await UpsertMaterialItemAsync(cartId, request.MaterialId.Value, request.Quantity);
			}
			else if (request.ItemType == "product" && request.ProductId.HasValue)
			{
				return await UpsertProductItemAsync(cartId, request.ProductId.Value, request.Quantity);
			}
			else
			{
				throw new ArgumentException("Invalid item type or missing ID");
			}
		}

		public async Task<CartDto> UpsertMaterialItemAsync(int userId, UpsertMaterialCartItemRequest request)
		{
			var cartId = await EnsureCartId(userId);
			return await UpsertMaterialItemAsync(cartId, request.MaterialId, request.Quantity);
		}

		public async Task<CartDto> UpsertProductItemAsync(int userId, UpsertProductCartItemRequest request)
		{
			var cartId = await EnsureCartId(userId);
			return await UpsertProductItemAsync(cartId, request.ProductId, request.Quantity);
		}

		private async Task<CartDto> UpsertMaterialItemAsync(int cartId, int materialId, int quantity)
		{
			var item = await _db.CartItems.FirstOrDefaultAsync(i => 
				i.CartId == cartId && 
				i.ItemType == "material" && 
				i.MaterialId == materialId);
				
			if (item == null)
			{
				var price = await _db.Materials.Where(m => m.MaterialId == materialId).Select(m => m.PricePerUnit).FirstOrDefaultAsync();
				item = new CartItem
				{
					CartId = cartId,
					ItemType = "material",
					MaterialId = materialId,
					Quantity = quantity,
					UnitPriceSnapshot = price,
				};
				_db.CartItems.Add(item);
			}
			else
			{
				item.Quantity = quantity;
				item.UpdatedAt = DateTime.UtcNow;
			}
			await _db.SaveChangesAsync();
			return await BuildCartDto(cartId);
		}

		private async Task<CartDto> UpsertProductItemAsync(int cartId, int productId, int quantity)
		{
			var item = await _db.CartItems.FirstOrDefaultAsync(i => 
				i.CartId == cartId && 
				i.ItemType == "product" && 
				i.ProductId == productId);
				
			if (item == null)
			{
				var price = await _db.Products.Where(p => p.ProductId == productId).Select(p => p.Price).FirstOrDefaultAsync();
				item = new CartItem
				{
					CartId = cartId,
					ItemType = "product",
					ProductId = productId,
					Quantity = quantity,
					UnitPriceSnapshot = price,
				};
				_db.CartItems.Add(item);
			}
			else
			{
				item.Quantity = quantity;
				item.UpdatedAt = DateTime.UtcNow;
			}
			await _db.SaveChangesAsync();
			return await BuildCartDto(cartId);
		}

		public async Task<CartDto> UpdateQuantityAsync(int userId, int cartItemId, int quantity)
		{
			var cartId = await EnsureCartId(userId);
			var item = await _db.CartItems.FirstOrDefaultAsync(i => i.CartItemId == cartItemId && i.CartId == cartId);
			if (item == null) throw new KeyNotFoundException("Cart item không tồn tại");
			item.Quantity = quantity;
			item.UpdatedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
			return await BuildCartDto(cartId);
		}

		public async Task<CartDto> RemoveItemAsync(int userId, int cartItemId)
		{
			var cartId = await EnsureCartId(userId);
			var item = await _db.CartItems.FirstOrDefaultAsync(i => i.CartItemId == cartItemId && i.CartId == cartId);
			if (item != null)
			{
				_db.CartItems.Remove(item);
				await _db.SaveChangesAsync();
			}
			return await BuildCartDto(cartId);
		}

		public async Task<CartDto> ClearAsync(int userId)
		{
			var cartId = await EnsureCartId(userId);
			var items = _db.CartItems.Where(i => i.CartId == cartId);
			_db.CartItems.RemoveRange(items);
			await _db.SaveChangesAsync();
			return await BuildCartDto(cartId);
		}

		private async Task<int> EnsureCartId(int userId)
		{
			var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
			if (cart == null)
			{
				cart = new Cart { UserId = userId, IsActive = true };
				_db.Carts.Add(cart);
				await _db.SaveChangesAsync();
			}
			return cart.CartId;
		}

		private async Task<CartDto> BuildCartDto(int cartId)
		{
			var cart = await _db.Carts.AsNoTracking().FirstAsync(c => c.CartId == cartId);
			var items = await _db.CartItems.AsNoTracking()
				.Where(i => i.CartId == cartId)
				.ToListAsync();

			// Get material data
			var materialIds = items.Where(i => i.ItemType == "material" && i.MaterialId.HasValue)
				.Select(i => i.MaterialId!.Value).Distinct().ToList();
			var materials = await _db.Materials
				.Where(m => materialIds.Contains(m.MaterialId))
				.Include(m => m.MaterialImages).ThenInclude(mi => mi.Image)
				.Include(m => m.Supplier)
				.ToListAsync();

			// Get product data
			var productIds = items.Where(i => i.ItemType == "product" && i.ProductId.HasValue)
				.Select(i => i.ProductId!.Value).Distinct().ToList();
			var products = await _db.Products
				.Where(p => productIds.Contains(p.ProductId))
				.Include(p => p.Design).ThenInclude(d => d.DesignerProfile)
				.Include(p => p.Design).ThenInclude(d => d.DesignImages).ThenInclude(di => di.Image)
				.Include(p => p.Size)
				.ToListAsync();

			var dto = new CartDto
			{
				CartId = cart.CartId,
				IsActive = cart.IsActive,
				UpdatedAt = cart.UpdatedAt,
				Items = items.Select(i =>
				{
					if (i.ItemType == "material" && i.MaterialId.HasValue)
					{
						var m = materials.First(x => x.MaterialId == i.MaterialId);
						return new CartItemDto
						{
							CartItemId = i.CartItemId,
							ItemType = "material",
							MaterialId = i.MaterialId,
							MaterialName = m.Name,
							Quantity = i.Quantity,
							UnitPriceSnapshot = i.UnitPriceSnapshot,
							CurrentPrice = m.PricePerUnit,
							ImageUrl = m.MaterialImages.FirstOrDefault()?.Image?.ImageUrl,
							UnitLabel = "mét",
							SupplierId = m.SupplierId,
							SupplierName = m.Supplier?.SupplierName
						};
					}
					else if (i.ItemType == "product" && i.ProductId.HasValue)
					{
						var p = products.First(x => x.ProductId == i.ProductId);
						return new CartItemDto
						{
							CartItemId = i.CartItemId,
							ItemType = "product",
							ProductId = i.ProductId,
							ProductName = $"{p.Design.Name} - {p.Size.SizeName} - {p.ColorCode}",
							SKU = p.SKU,
							ColorCode = p.ColorCode,
							SizeName = p.Size.SizeName,
							DesignId = p.DesignId,
							DesignName = p.Design.Name,
							DesignerId = p.Design.DesignerId,
							DesignerName = p.Design.DesignerProfile?.DesignerName,
							Quantity = i.Quantity,
							UnitPriceSnapshot = i.UnitPriceSnapshot,
							CurrentPrice = p.Price,
							ImageUrl = p.Design.DesignImages.FirstOrDefault()?.Image?.ImageUrl,
							UnitLabel = "cái"
						};
					}
					else
					{
						// Fallback for corrupted data
						return new CartItemDto
						{
							CartItemId = i.CartItemId,
							ItemType = i.ItemType,
							Quantity = i.Quantity,
							UnitPriceSnapshot = i.UnitPriceSnapshot,
							CurrentPrice = i.UnitPriceSnapshot,
							UnitLabel = "item"
						};
					}
				}).ToList()
			};
			return dto;
		}

		public async Task<bool> ClearCartAsync(int userId)
		{
			var cart = await _db.Carts
				.Include(c => c.Items)
				.FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
			
			if (cart == null) return false;

			// Mark cart as inactive instead of deleting to maintain history
			cart.IsActive = false;
			_db.Carts.Update(cart);
			await _db.SaveChangesAsync();
			
			return true;
		}
	}
}


