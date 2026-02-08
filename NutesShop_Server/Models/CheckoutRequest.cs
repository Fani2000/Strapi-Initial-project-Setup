namespace NutsShop_Server.Shop;

public record CheckoutRequest(
    string CustomerName,
    string CustomerEmail,
    string FulfillmentType,
    DeliveryAddress? Delivery,
    PickupDetails? Pickup,
    CheckoutItem[] Items
);