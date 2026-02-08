namespace NutsShop_Server.Shop;

public record CreateOrderRequest(
    string CustomerName,
    string CustomerEmail,
    string FulfillmentType,
    string City,
    DeliveryAddress? Delivery,
    PickupDetails? Pickup,
    CreateOrderItem[] Items
);