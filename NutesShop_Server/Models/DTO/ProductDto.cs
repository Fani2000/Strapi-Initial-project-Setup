namespace NutsShop_Server.Shop;

public sealed class ProductDto
{
    public string Slug { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public int PriceCents { get; init; }
    public string Per { get; init; } = "each";
    public string ImageUrl { get; init; } = "";
    public bool InStock { get; init; }
    public bool Featured { get; init; }
    public string[] Badges { get; init; } = Array.Empty<string>();
}