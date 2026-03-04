using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;

namespace NutsShop_Server.Shop;

public sealed class ProductsService(
    IMemoryCache cache,
    PgStore store,
    IHttpClientFactory httpFactory,
    IConfiguration cfg,
    StrapiService strapiService)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly IConfiguration _cfg = cfg;

    private static readonly TimeSpan MemoryTtl = TimeSpan.FromMinutes(2);

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct)
    {
        if (cache.TryGetValue("products", out IReadOnlyList<ProductDto>? cached) && cached is not null)
            return cached;

        var fromDb = await store.GetProductsAsync(ct);
        if (fromDb.Count > 0)
        {
            cache.Set("products", fromDb, MemoryTtl);
            return fromDb;
        }

        var fresh = await strapiService.FetchFromStrapiAsync(ct);
        await store.UpsertProductsAsync(fresh, ct);
        cache.Set("products", fresh, MemoryTtl);
        return fresh;
    }

    public async Task<ProductDto?> GetProductAsync(string slug, CancellationToken ct)
    {
        var products = await GetProductsAsync(ct);
        return products.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(IReadOnlyList<ProductDto> Items, int Total)> GetProductsPageAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var all = await GetProductsAsync(ct);
        var filtered = ApplyProductFilter(all, query);
        var total = filtered.Count;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return (items, total);
    }

    public async Task<IReadOnlyList<ProductDto>> SearchProductsAsync(
        string query,
        int limit,
        CancellationToken ct)
    {
        query = (query ?? "").Trim();
        if (query.Length < 2)
            return Array.Empty<ProductDto>();

        var all = await GetProductsAsync(ct);
        limit = Math.Clamp(limit, 1, 20);
        return ApplyProductSearch(all, query)
            .Take(limit)
            .ToArray();
    }

    public async Task<HomePageDto> GetHomeAsync(CancellationToken ct)
    {
        if (cache.TryGetValue("home", out HomePageDto? cached) && cached is not null)
            return cached;

        var fromDb = await store.GetHomeAsync(ct);
        if (fromDb is not null)
        {
            cache.Set("home", fromDb, MemoryTtl);
            return fromDb;
        }

        var fresh = await strapiService.FetchHomeFromStrapiAsync(ct);
        if (HasData(fresh))
        {
            await store.UpsertHomeAsync(fresh, ct);
        }
        cache.Set("home", fresh, MemoryTtl);
        return fresh;
    }

    public async Task<ThemeDto> GetThemeAsync(CancellationToken ct)
    {
        if (cache.TryGetValue("theme", out ThemeDto? cached) && cached is not null)
            return cached;

        var fromDb = await store.GetThemeAsync(ct);
        if (fromDb is not null)
        {
            cache.Set("theme", fromDb, MemoryTtl);
            return fromDb;
        }

        var fresh = await strapiService.FetchThemeFromStrapiAsync(ct);
        if (HasData(fresh))
        {
            await store.UpsertThemeAsync(fresh, ct);
        }
        cache.Set("theme", fresh, MemoryTtl);
        return fresh;
    }

    public async Task<SitePagesDto> GetPagesAsync(CancellationToken ct)
    {
        if (cache.TryGetValue("pages", out SitePagesDto? cached) && cached is not null)
            return cached;

        var fromDb = await store.GetPagesAsync(ct);
        if (fromDb is not null)
        {
            cache.Set("pages", fromDb, MemoryTtl);
            return fromDb;
        }

        var fresh = await strapiService.FetchPagesFromStrapiAsync(ct);
        if (HasData(fresh))
        {
            await store.UpsertPagesAsync(fresh, ct);
        }
        cache.Set("pages", fresh, MemoryTtl);
        return fresh;
    }

    public async Task EnsureInitialDataAsync(CancellationToken ct)
    {
        var existingProducts = await store.GetProductsAsync(ct);
        if (existingProducts.Count == 0)
        {
            var products = await FetchWithRetriesAsync(strapiService.FetchFromStrapiAsync, ct);
            if (products.Count > 0)
            {
                await store.UpsertProductsAsync(products, ct);
            }
        }

        var existingHome = await store.GetHomeAsync(ct);
        if (existingHome is null)
        {
            var home = await FetchWithRetriesAsync(strapiService.FetchHomeFromStrapiAsync, ct);
            if (HasData(home))
            {
                await store.UpsertHomeAsync(home, ct);
            }
        }

        var existingTheme = await store.GetThemeAsync(ct);
        if (existingTheme is null)
        {
            var theme = await FetchWithRetriesAsync(strapiService.FetchThemeFromStrapiAsync, ct);
            if (HasData(theme))
            {
                await store.UpsertThemeAsync(theme, ct);
            }
        }

        var existingPages = await store.GetPagesAsync(ct);
        if (existingPages is null)
        {
            var pages = await FetchWithRetriesAsync(strapiService.FetchPagesFromStrapiAsync, ct);
            if (HasData(pages))
            {
                await store.UpsertPagesAsync(pages, ct);
            }
        }
    }

    public async Task SeedFromStrapiAsync(CancellationToken ct)
    {
        var products = await FetchWithRetriesAsync(strapiService.FetchFromStrapiAsync, ct);
        if (products.Count > 0)
        {
            await store.UpsertProductsAsync(products, ct);
        }

        var home = await FetchWithRetriesAsync(strapiService.FetchHomeFromStrapiAsync, ct);
        if (HasData(home))
        {
            await store.UpsertHomeAsync(home, ct);
        }

        var theme = await FetchWithRetriesAsync(strapiService.FetchThemeFromStrapiAsync, ct);
        if (HasData(theme))
        {
            await store.UpsertThemeAsync(theme, ct);
        }

        var pages = await FetchWithRetriesAsync(strapiService.FetchPagesFromStrapiAsync, ct);
        if (HasData(pages))
        {
            await store.UpsertPagesAsync(pages, ct);
        }
    }

    public void InvalidateCache()
    {
        cache.Remove("products");
        cache.Remove("home");
        cache.Remove("theme");
        cache.Remove("pages");
    }

    private static async Task<T> FetchWithRetriesAsync<T>(
        Func<CancellationToken, Task<T>> fetch,
        CancellationToken ct)
    {
        const int maxAttempts = 3;
        var delay = TimeSpan.FromSeconds(1);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var result = await fetch(ct);
            if (HasData(result)) return result;

            if (attempt < maxAttempts)
                await Task.Delay(delay, ct);
        }

        return await fetch(ct);
    }

    private static bool HasData<T>(T result)
    {
        if (result is IReadOnlyList<ProductDto> list)
            return list.Count > 0;

        if (result is HomePageDto home)
            return !string.IsNullOrWhiteSpace(home.HeroTitle)
                   || !string.IsNullOrWhiteSpace(home.HeroSubtitle)
                   || !string.IsNullOrWhiteSpace(home.PromoText)
                   || (home.FeaturedProducts?.Length ?? 0) > 0;

        if (result is SitePagesDto pages)
            return !string.IsNullOrWhiteSpace(pages.DeliveryTitle)
                   || !string.IsNullOrWhiteSpace(pages.DeliveryContent)
                   || !string.IsNullOrWhiteSpace(pages.AboutTitle)
                   || !string.IsNullOrWhiteSpace(pages.AboutContent)
                   || !string.IsNullOrWhiteSpace(pages.ContactTitle)
                   || !string.IsNullOrWhiteSpace(pages.ContactContent)
                   || (pages.Testimonials?.Length ?? 0) > 0;

        if (result is ThemeDto theme)
            return !string.IsNullOrWhiteSpace(theme.Name);

        return result is not null;
    }

    private static IReadOnlyList<ProductDto> ApplyProductFilter(
        IReadOnlyList<ProductDto> products,
        string? query)
    {
        var ordered = products
            .OrderByDescending(p => p.Featured)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(query))
            return ordered.ToArray();

        var q = query.Trim();
        return ordered
            .Where(p =>
                p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Slug.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static IReadOnlyList<ProductDto> ApplyProductSearch(
        IReadOnlyList<ProductDto> products,
        string query)
    {
        var q = query.Trim();
        return products
            .Select(p => new { Product = p, Score = GetSearchScore(p, q) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Product.Featured)
            .ThenBy(x => x.Product.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Product)
            .ToArray();
    }

    private static int GetSearchScore(ProductDto product, string query)
    {
        var score = 0;
        if (product.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 120;
        else if (product.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 70;

        if (product.Slug.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 45;
        else if (product.Slug.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 25;

        if (product.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 12;
        if (product.Featured) score += 6;
        if (product.InStock) score += 4;

        return score;
    }
    
}
