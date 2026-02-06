using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;

namespace NutesShop_Server.Shop;

public sealed class ProductsService
{
    private readonly IMemoryCache _cache;
    private readonly PgStore _store;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _cfg;

    private static readonly TimeSpan MemoryTtl = TimeSpan.FromMinutes(2);

    public ProductsService(IMemoryCache cache, PgStore store, IHttpClientFactory httpFactory, IConfiguration cfg)
    {
        _cache = cache;
        _store = store;
        _httpFactory = httpFactory;
        _cfg = cfg;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue("products", out IReadOnlyList<ProductDto>? cached) && cached is not null)
            return cached;

        var fromDb = await _store.GetProductsAsync(ct);
        if (fromDb.Count > 0)
        {
            _cache.Set("products", fromDb, MemoryTtl);
            return fromDb;
        }

        var fresh = await FetchFromStrapiAsync(ct);
        await _store.UpsertProductsAsync(fresh, ct);
        _cache.Set("products", fresh, MemoryTtl);
        return fresh;
    }

    public async Task<ProductDto?> GetProductAsync(string slug, CancellationToken ct)
    {
        var products = await GetProductsAsync(ct);
        return products.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<HomePageDto> GetHomeAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue("home", out HomePageDto? cached) && cached is not null)
            return cached;

        var fromDb = await _store.GetHomeAsync(ct);
        if (fromDb is not null)
        {
            _cache.Set("home", fromDb, MemoryTtl);
            return fromDb;
        }

        var fresh = await FetchHomeFromStrapiAsync(ct);
        await _store.UpsertHomeAsync(fresh, ct);
        _cache.Set("home", fresh, MemoryTtl);
        return fresh;
    }

    public async Task SeedFromStrapiAsync(CancellationToken ct)
    {
        var products = await FetchWithRetriesAsync(FetchFromStrapiAsync, ct);
        if (products.Count > 0)
        {
            await _store.UpsertProductsAsync(products, ct);
        }

        var home = await FetchWithRetriesAsync(FetchHomeFromStrapiAsync, ct);
        await _store.UpsertHomeAsync(home, ct);
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

    private async Task<IReadOnlyList<ProductDto>> FetchFromStrapiAsync(CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("strapi");
        var resp = await http.GetAsync("/api/products?populate=*", ct);
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<StrapiResponse<List<StrapiEntry<ProductAttributes>>>>(cancellationToken: ct);
        if (dto is null) return Array.Empty<ProductDto>();
        var baseUrl = _cfg["STRAPI_BASE_URL"] ?? "";
        return StrapiMapper.MapProducts(dto, baseUrl);
    }

    private async Task<HomePageDto> FetchHomeFromStrapiAsync(CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("strapi");
        var resp = await http.GetAsync("/api/home-page?populate=*", ct);
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<StrapiResponse<StrapiEntry<HomePageAttributes>>>(cancellationToken: ct);
        if (dto is null) return new HomePageDto("", "", "", "", Array.Empty<ProductDto>());
        var baseUrl = _cfg["STRAPI_BASE_URL"] ?? "";
        return StrapiMapper.MapHome(dto, baseUrl);
    }

    public static void ConfigureHttpClient(IServiceCollection services, IConfiguration cfg)
    {
        services.AddHttpClient("strapi", client =>
        {
            var baseUrl = cfg["STRAPI_BASE_URL"] ?? "";
            if (!string.IsNullOrWhiteSpace(baseUrl))
                client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var token = cfg["STRAPI_API_TOKEN"];
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        });
    }
}
