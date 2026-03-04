namespace NutsShop_Server.Shop;

public record OrderItemDto(
    string ProductSlug,
    string ProductName,
    int UnitPriceCents,
    int Quantity
);
