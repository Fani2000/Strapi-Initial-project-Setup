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
        await store.UpsertHomeAsync(fresh, ct);
        cache.Set("home", fresh, MemoryTtl);
        return fresh;
    }

    public async Task SeedFromStrapiAsync(CancellationToken ct)
    {
        var products = await FetchWithRetriesAsync(strapiService.FetchFromStrapiAsync, ct);
        if (products.Count > 0)
        {
            await store.UpsertProductsAsync(products, ct);
        }

        var home = await FetchWithRetriesAsync(strapiService.FetchHomeFromStrapiAsync, ct);
        await store.UpsertHomeAsync(home, ct);
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

        return result is not null;
    }
    
}
