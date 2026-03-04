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

    public async Task UpsertPagesAsync(SitePagesDto pages, CancellationToken ct)
    {
        using var conn = await OpenAsync(ct);
        var testimonialsJson = System.Text.Json.JsonSerializer.Serialize(pages.Testimonials);
        await conn.ExecuteAsync(new CommandDefinition("""
            select upsert_site_pages(
              @DeliveryTitle, @DeliveryContent, @AboutTitle, @AboutContent,
              @ContactTitle, @ContactContent, @Testimonials::jsonb
            );
        """, new
        {
            pages.DeliveryTitle,
            pages.DeliveryContent,
            pages.AboutTitle,
            pages.AboutContent,
            pages.ContactTitle,
            pages.ContactContent,
            Testimonials = testimonialsJson
        }, cancellationToken: ct));
    }

    public async Task<SitePagesDto?> GetPagesAsync(CancellationToken ct)
    {
        using var conn = await OpenAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync(new CommandDefinition("""
        select * from get_site_pages();
        """, cancellationToken: ct));

        if (row is null) return null;
        var testimonials = Array.Empty<TestimonialDto>();
        if (row.testimonials is not null)
        {
            string json = row.testimonials?.ToString() ?? "[]";
            var loaded = System.Text.Json.JsonSerializer.Deserialize<TestimonialDto[]>(json)
                         ?? Array.Empty<TestimonialDto>();
            var normalized = new List<TestimonialDto>(loaded.Length);
            foreach (var t in loaded)
            {
                normalized.Add(new TestimonialDto(
                    Name: t.Name ?? "",
                    Feedback: t.Feedback ?? "",
                    ImageUrl: t.ImageUrl ?? ""));
            }

            testimonials = normalized.ToArray();
        }

        return new SitePagesDto(
            DeliveryTitle: row.delivery_title ?? "",
            DeliveryContent: row.delivery_content ?? "",
            AboutTitle: row.about_title ?? "",
            AboutContent: row.about_content ?? "",
            ContactTitle: row.contact_title ?? "",
            ContactContent: row.contact_content ?? "",
            Testimonials: testimonials
        );
    }

    public async Task UpsertThemeAsync(ThemeDto theme, CancellationToken ct)
    {
        using var conn = await OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("""
            select upsert_theme(
              @Name, @Background, @BackgroundAccent, @Card, @CardSoft, @Text, @Muted,
              @Accent, @Accent2, @TopbarBg, @TopbarBorder,
              @HeroGradient1, @HeroGradient2, @HeroGradient3,
              @HeroOverlay1, @HeroOverlay2, @Glow, @Shadow
            );
        """, theme, cancellationToken: ct));
    }

    public async Task<ThemeDto?> GetThemeAsync(CancellationToken ct)
    {
        using var conn = await OpenAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync(new CommandDefinition("""
        select * from get_theme();
        """, cancellationToken: ct));

        if (row is null) return null;

        return new ThemeDto(
            Name: row.name,
            Background: row.background,
            BackgroundAccent: row.background_accent,
            Card: row.card,
            CardSoft: row.card_soft,
            Text: row.text,
            Muted: row.muted,
            Accent: row.accent,
            Accent2: row.accent_2,
            TopbarBg: row.topbar_bg,
            TopbarBorder: row.topbar_border,
            HeroGradient1: row.hero_gradient_1,
            HeroGradient2: row.hero_gradient_2,
            HeroGradient3: row.hero_gradient_3,
            HeroOverlay1: row.hero_overlay_1,
            HeroOverlay2: row.hero_overlay_2,
            Glow: row.glow,
            Shadow: row.shadow
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

    public async Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct)
    {
        using var conn = await OpenAsync(ct);

        var header = await conn.QuerySingleOrDefaultAsync<OrderHeaderRow>(new CommandDefinition("""
            select
              id as "Id",
              created_at as "CreatedAt",
              customer_name as "CustomerName",
              customer_email as "CustomerEmail",
              fulfillment_type as "FulfillmentType",
              city as "City",
              suburb as "Suburb",
              address_line1 as "AddressLine1",
              address_line2 as "AddressLine2",
              postal_code as "PostalCode",
              pickup_location as "PickupLocation",
              status as "Status",
              total_cents as "TotalCents"
            from orders
            where id = @OrderId;
        """, new { OrderId = orderId }, cancellationToken: ct));

        if (header is null) return null;

        var items = (await conn.QueryAsync<OrderItemDto>(new CommandDefinition("""
            select
              product_slug as "ProductSlug",
              product_name as "ProductName",
              unit_price_cents as "UnitPriceCents",
              quantity as "Quantity"
            from order_items
            where order_id = @OrderId
            order by product_name;
        """, new { OrderId = orderId }, cancellationToken: ct))).ToArray();

        return new OrderDto(
            header.Id,
            header.CreatedAt,
            header.CustomerName,
            header.CustomerEmail,
            header.FulfillmentType,
            header.City,
            header.Suburb,
            header.AddressLine1,
            header.AddressLine2,
            header.PostalCode,
            header.PickupLocation,
            header.Status,
            header.TotalCents,
            items
        );
    }

    private sealed record OrderHeaderRow(
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
        int TotalCents
    );
}
