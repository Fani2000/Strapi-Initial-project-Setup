using NutesShop_Server.Shop;
using System.Text.Json;

namespace NutsShop_Server.Shop;

public class StrapiService(IHttpClientFactory _httpFactory, IConfiguration _cfg)
{
    public async Task<IReadOnlyList<ProductDto>> FetchFromStrapiAsync(CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("strapi");
        try
        {
            var baseUrl = _cfg["STRAPI_BASE_URL"] ?? "";
            var page = 1;
            const int pageSize = 100;
            var all = new List<ProductDto>();

            while (true)
            {
                var path = $"/api/products?populate=*&pagination[page]={page}&pagination[pageSize]={pageSize}";
                var resp = await http.GetAsync(path, ct);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync(ct);
                var mapped = Array.Empty<ProductDto>();
                var dto = System.Text.Json.JsonSerializer.Deserialize<StrapiResponse<List<StrapiEntry<ProductAttributes>>>>(json);
                if (dto?.Data is not null)
                {
                    mapped = StrapiMapper.MapProducts(dto, baseUrl).ToArray();
                }
                else
                {
                    mapped = StrapiMapper.MapProducts(json, baseUrl).ToArray();
                }
                if (mapped.Length > 0)
                    all.AddRange(mapped);

                var pageCount = ParsePageCount(json);
                if (pageCount <= page || mapped.Length == 0)
                    break;

                page++;
            }

            return all
                .GroupBy(p => p.Slug, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToArray();
        }
        catch
        {
            return Array.Empty<ProductDto>();
        }
    }

    public async Task<HomePageDto> FetchHomeFromStrapiAsync(CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("strapi");
        try
        {
            var resp = await http.GetAsync("/api/home-page?populate=*", ct);
            resp.EnsureSuccessStatusCode();

            var dto = await resp.Content.ReadFromJsonAsync<StrapiResponse<StrapiEntry<HomePageAttributes>>>(cancellationToken: ct);
            if (dto is null) return new HomePageDto("", "", "", "", Array.Empty<ProductDto>());
            var baseUrl = _cfg["STRAPI_BASE_URL"] ?? "";
            return StrapiMapper.MapHome(dto, baseUrl);
        }
        catch
        {
            return new HomePageDto("", "", "", "", Array.Empty<ProductDto>());
        }
    }

    public async Task<SitePagesDto> FetchPagesFromStrapiAsync(CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("strapi");
        try
        {
            var resp = await http.GetAsync("/api/site-page?populate[testimonials][populate]=image", ct);
            resp.EnsureSuccessStatusCode();

            var dto = await resp.Content.ReadFromJsonAsync<StrapiResponse<StrapiEntry<SitePageAttributes>>>(cancellationToken: ct);
            if (dto is null) return new SitePagesDto("", "", "", "", "", "", Array.Empty<TestimonialDto>());
            var baseUrl = _cfg["STRAPI_BASE_URL"] ?? "";
            return StrapiMapper.MapPages(dto, baseUrl);
        }
        catch
        {
            return new SitePagesDto("", "", "", "", "", "", Array.Empty<TestimonialDto>());
        }
    }

    public async Task<ThemeDto> FetchThemeFromStrapiAsync(CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("strapi");
        try
        {
            var resp = await http.GetAsync("/api/theme", ct);
            resp.EnsureSuccessStatusCode();

            var dto = await resp.Content.ReadFromJsonAsync<StrapiResponse<StrapiEntry<ThemeAttributes>>>(cancellationToken: ct);
            if (dto is null) return StrapiMapper.DefaultTheme();
            var baseUrl = _cfg["STRAPI_BASE_URL"] ?? "";
            return StrapiMapper.MapTheme(dto, baseUrl);
        }
        catch
        {
            return StrapiMapper.DefaultTheme();
        }
    }

    private static int ParsePageCount(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("meta", out var meta)) return 1;
        if (!meta.TryGetProperty("pagination", out var pagination)) return 1;
        if (!pagination.TryGetProperty("pageCount", out var pageCountEl)) return 1;
        if (pageCountEl.ValueKind != JsonValueKind.Number) return 1;
        return pageCountEl.GetInt32();
    }
}
