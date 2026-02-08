namespace NutsShop_Server.Shop;

public record CreateOrderItem(string ProductSlug, string ProductName, int UnitPriceCents, int Quantity);