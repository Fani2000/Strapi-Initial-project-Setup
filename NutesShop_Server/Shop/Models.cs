namespace NutsShop_Server.Shop;

public record ProductDto(
    string Slug,
    string Name,
    string Description,
    int PriceCents,
    string Per,
    string ImageUrl,
    bool InStock,
    bool Featured,
    string[] Badges
);

public record HomePageDto(
    string HeroTitle,
    string HeroSubtitle,
    string PromoText,
    string HeroImageUrl,
    ProductDto[] FeaturedProducts
);

public record CheckoutRequest(
    string CustomerName,
    string CustomerEmail,
    string FulfillmentType,
    DeliveryAddress? Delivery,
    PickupDetails? Pickup,
    CheckoutItem[] Items
);

public record DeliveryAddress(
    string City,
    string Suburb,
    string AddressLine1,
    string? AddressLine2,
    string PostalCode
);

public record PickupDetails(string LocationId);

public record CheckoutItem(string ProductSlug, int Quantity);

public record CreateOrderRequest(
    string CustomerName,
    string CustomerEmail,
    string FulfillmentType,
    string City,
    DeliveryAddress? Delivery,
    PickupDetails? Pickup,
    CreateOrderItem[] Items
);

public record CreateOrderItem(string ProductSlug, string ProductName, int UnitPriceCents, int Quantity);

public static class CapeTownRules
{
    public const string AllowedCity = "Cape Town";

    public static readonly Dictionary<string, string> PickupLocations = new()
    {
        ["CT_WATERFRONT"] = "V&A Waterfront Pickup Point",
        ["CT_CBD"] = "Cape Town CBD Pickup Point",
        ["CT_CLAREMONT"] = "Claremont Pickup Point"
    };

    public static bool IsCapeTown(string? city) =>
        string.Equals(city?.Trim(), AllowedCity, StringComparison.OrdinalIgnoreCase);
}
