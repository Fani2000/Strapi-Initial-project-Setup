namespace NutesShop_Server.Shop;

public sealed record StrapiResponse<T>(T Data);

public sealed record StrapiEntry<T>(int Id, T Attributes);

public sealed record StrapiRelationCollection<T>(List<StrapiEntry<T>> Data);

public sealed record StrapiMediaAttributes(string Url);

public sealed record StrapiMediaCollection(List<StrapiEntry<StrapiMediaAttributes>> Data);

public sealed record StrapiMediaSingle(StrapiEntry<StrapiMediaAttributes>? Data);

public sealed record PriceComponent(decimal? Amount, string? Per);

public sealed record BadgeComponent(string? Label);

public sealed record ProductAttributes(
    string? Slug,
    string? Name,
    string? Description,
    PriceComponent? Price,
    StrapiMediaCollection? Images,
    bool? InStock,
    bool? Featured,
    List<BadgeComponent>? Badges
);

public sealed record HomePageAttributes(
    string? HeroTitle,
    string? HeroSubtitle,
    string? PromoText,
    StrapiMediaSingle? HeroImage,
    StrapiRelationCollection<ProductAttributes>? FeaturedProducts
);
