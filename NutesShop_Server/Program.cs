using NutsShop_Server.Shop;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddSingleton<PgStore>();
builder.Services.AddSingleton<MigrationRunner>();
builder.Services.AddSingleton<ProductsService>();
builder.Services.AddSingleton<StrapiService>();

builder.Services.ConfigureHttpClient(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}
app.UseCors();

// apply migrations + seed from Strapi on startup
using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
    await migrator.ApplyAsync(CancellationToken.None);
    var products = scope.ServiceProvider.GetRequiredService<ProductsService>();
    await products.SeedFromStrapiAsync(CancellationToken.None);
}

app.MapGet("/api/shop/products", async (
    ProductsService products,
    CancellationToken ct) =>
{
    var items = await products.GetProductsAsync(ct);
    return Results.Ok(new { currency = "ZAR", products = items });
});

app.MapGet("/api/shop/products/{slug}", async (
    string slug,
    ProductsService products,
    CancellationToken ct) =>
{
    var product = await products.GetProductAsync(slug, ct);
    return product is null
        ? Results.NotFound()
        : Results.Ok(new { currency = "ZAR", product });
});

app.MapGet("/api/shop/home", async (
    ProductsService products,
    CancellationToken ct) =>
{
    var home = await products.GetHomeAsync(ct);
    return Results.Ok(new { currency = "ZAR", home });
});

app.MapPost("/api/shop/checkout", async (
    CheckoutRequest req,
    ProductsService products,
    PgStore store,
    CancellationToken ct) =>
{
    if (req.Items.Length == 0) return Results.BadRequest("No items.");
    if (req.FulfillmentType is not ("Delivery" or "Pickup"))
        return Results.BadRequest("FulfillmentType must be Delivery or Pickup.");

    if (req.FulfillmentType == "Delivery")
    {
        if (req.Delivery is null) return Results.BadRequest("Delivery details required.");
        if (!CapeTownRules.IsCapeTown(req.Delivery.City))
            return Results.BadRequest("Delivery is only available in Cape Town.");
    }
    else
    {
        if (req.Pickup is null) return Results.BadRequest("Pickup details required.");
        if (!CapeTownRules.PickupLocations.ContainsKey(req.Pickup.LocationId))
            return Results.BadRequest("Invalid pickup location.");
    }

    var catalog = (await products.GetProductsAsync(ct))
        .ToDictionary(p => p.Slug, StringComparer.OrdinalIgnoreCase);

    var items = new List<CreateOrderItem>();
    foreach (var item in req.Items)
    {
        if (!catalog.TryGetValue(item.ProductSlug, out var p))
            return Results.BadRequest($"Unknown product: {item.ProductSlug}");
        if (!p.InStock) return Results.BadRequest($"Out of stock: {p.Name}");
        if (item.Quantity <= 0) return Results.BadRequest("Quantity must be > 0.");

        items.Add(new CreateOrderItem(p.Slug, p.Name, p.PriceCents, item.Quantity));
    }

    var orderId = await store.CreateOrderAsync(new CreateOrderRequest(
        CustomerName: req.CustomerName,
        CustomerEmail: req.CustomerEmail,
        FulfillmentType: req.FulfillmentType,
        City: CapeTownRules.AllowedCity,
        Delivery: req.Delivery,
        Pickup: req.Pickup,
        Items: items.ToArray()
    ), ct);

    return Results.Ok(new { orderId, currency = "ZAR" });
});

app.MapGet("/api/shop/pickup-locations", () =>
{
    return Results.Ok(CapeTownRules.PickupLocations.Select(kv => new
    {
        id = kv.Key,
        name = kv.Value
    }));
});

app.MapPost("/api/webhooks/strapi", async (
    ProductsService products,
    CancellationToken ct) =>
{
    products.InvalidateCache();
    await products.SeedFromStrapiAsync(ct);
    return Results.Ok(new { ok = true });
});

app.Run();
