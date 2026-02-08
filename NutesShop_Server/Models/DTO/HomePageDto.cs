namespace NutsShop_Server.Shop;

public record HomePageDto(
    string HeroTitle,
    string HeroSubtitle,
    string PromoText,
    string HeroImageUrl,
    ProductDto[] FeaturedProducts
);