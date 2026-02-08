namespace NutsShop_Server.Shop;

public record DeliveryAddress(
    string City,
    string Suburb,
    string AddressLine1,
    string? AddressLine2,
    string PostalCode
);