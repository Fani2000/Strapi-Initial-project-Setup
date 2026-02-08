using System.Data;
using Dapper;
using Npgsql;

namespace NutsShop_Server.Shop;

public sealed class PgStore
{
    private readonly string _connString;

    public PgStore(IConfiguration cfg)
    {
        _connString = cfg.GetConnectionString("apidb")
                      ?? throw new InvalidOperationException("Missing connection string 'apidb'.");
    }

    public NpgsqlConnection CreateConnection() => new NpgsqlConnection(_connString);

    public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var conn = CreateConnection();
        await conn.OpenAsync(ct);
        return conn;
    }

    public async Task UpsertProductsAsync(IEnumerable<ProductDto> products, CancellationToken ct)
    {
        using var conn = await OpenAsync(ct);
        foreach (var p in products)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "select upsert_product(@Slug, @Name, @Description, @PriceCents, @Per, @ImageUrl, @InStock, @Featured, @Badges);",
                new
                {
                    p.Slug,
                    p.Name,
                    p.Description,
                    p.PriceCents,
                    p.Per,
                    p.ImageUrl,
                    InStock = p.InStock,
                    Featured = p.Featured,
                    Badges = p.Badges
                },
                cancellationToken: ct));
        }
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct)
    {
        using var conn = await OpenAsync(ct);
        var rows = await conn.QueryAsync<ProductDto>(new CommandDefinition("""
            select
              slug as "Slug",
              name as "Name",
              description as "Description",
              price_cents as "PriceCents",
              per as "Per",
              image_url as "ImageUrl",
              in_stock as "InStock",
              featured as "Featured",
              badges as "Badges"
            from get_products();
        """, cancellationToken: ct));
        return rows.ToArray();
    }

    public async Task<ProductDto?> GetProductAsync(string slug, CancellationToken ct)
    {
        using var conn = await OpenAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ProductDto>(new CommandDefinition("""
            select
              slug as "Slug",
              name as "Name",
              description as "Description",
              price_cents as "PriceCents",
              per as "Per",
              image_url as "ImageUrl",
              in_stock as "InStock",
              featured as "Featured",
              badges as "Badges"
            from get_product(@Slug);
        """, new { Slug = slug }, cancellationToken: ct));
    }

    public async Task UpsertHomeAsync(HomePageDto home, CancellationToken ct)
    {
        using var conn = await OpenAsync(ct);
        var featuredJson = System.Text.Json.JsonSerializer.Serialize(home.FeaturedProducts);
        await conn.ExecuteAsync(new CommandDefinition("""
            select upsert_home(@HeroTitle, @HeroSubtitle, @PromoText, @HeroImageUrl, @FeaturedProducts::jsonb);
        """, new
        {
            home.HeroTitle,
            home.HeroSubtitle,
            home.PromoText,
            home.HeroImageUrl,
            FeaturedProducts = featuredJson
        }, cancellationToken: ct));
    }

    public async Task<HomePageDto?> GetHomeAsync(CancellationToken ct)
    {
        using var conn = await OpenAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync(new CommandDefinition("""
        select * from get_home();
        """, cancellationToken: ct));

        if (row is null) return null;
        var featured = Array.Empty<ProductDto>();
        if (row.featured_products is not null)
        {
            var json = row.featured_products.ToString();
            featured = System.Text.Json.JsonSerializer.Deserialize<ProductDto[]>(json)
                       ?? Array.Empty<ProductDto>();
        }

        return new HomePageDto(
            HeroTitle: row.hero_title,
            HeroSubtitle: row.hero_subtitle,
            PromoText: row.promo_text,
            HeroImageUrl: row.hero_image_url,
            FeaturedProducts: featured
        );
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderRequest req, CancellationToken ct)
    {
        using var conn = await OpenAsync(ct);
        var total = req.Items.Sum(i => i.UnitPriceCents * i.Quantity);

        var orderId = await conn.ExecuteScalarAsync<Guid>(new CommandDefinition("""
            select create_order(
              @CustomerName, @CustomerEmail, @FulfillmentType, @City,
              @Suburb, @AddressLine1, @AddressLine2, @PostalCode, @PickupLocation, @TotalCents
            );
        """, new
        {
            req.CustomerName,
            req.CustomerEmail,
            req.FulfillmentType,
            req.City,
            Suburb = req.Delivery?.Suburb,
            AddressLine1 = req.Delivery?.AddressLine1,
            AddressLine2 = req.Delivery?.AddressLine2,
            PostalCode = req.Delivery?.PostalCode,
            PickupLocation = req.Pickup?.LocationId,
            TotalCents = total
        }, cancellationToken: ct));

        foreach (var item in req.Items)
        {
            await conn.ExecuteAsync(new CommandDefinition("""
                select add_order_item(@OrderId, @ProductSlug, @ProductName, @UnitPriceCents, @Quantity);
            """, new
            {
                OrderId = orderId,
                item.ProductSlug,
                item.ProductName,
                item.UnitPriceCents,
                item.Quantity
            }, cancellationToken: ct));
        }

        return orderId;
    }
}
