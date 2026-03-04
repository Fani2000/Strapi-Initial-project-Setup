namespace NutsShop_Server.Shop;

public record OrderDto(
    Guid Id,
    DateTime CreatedAt,
    string CustomerName,
    string CustomerEmail,
    string FulfillmentType,
    string City,
    string? Suburb,
    string? AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    string? PickupLocation,
    string Status,
    int TotalCents,
    OrderItemDto[] Items
);
