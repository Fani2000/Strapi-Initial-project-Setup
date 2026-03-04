namespace NutsShop_Server.Shop;

public record SitePagesDto(
    string DeliveryTitle,
    string DeliveryContent,
    string AboutTitle,
    string AboutContent,
    string ContactTitle,
    string ContactContent,
    TestimonialDto[] Testimonials
);

public record TestimonialDto(
    string Name,
    string Feedback,
    string ImageUrl
);
