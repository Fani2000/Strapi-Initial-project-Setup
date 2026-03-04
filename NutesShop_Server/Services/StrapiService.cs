using NutesShop_Server.Shop;

namespace NutsShop_Server.Shop;

public class StrapiService(IHttpClientFactory _httpFactory, IConfiguration _cfg)
{
    public async Task<IReadOnlyList<ProductDto>> FetchFromStrapiAsync(CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("strapi");
        try
        {
            var resp = await http.GetAsync("/api/products?populate=*", ct);
            resp.EnsureSuccessStatusCode();

            var dto = await resp.Content.ReadFromJsonAsync<StrapiResponse<List<StrapiEntry<ProductAttributes>>>>(cancellationToken: ct);
            if (dto is null) return Array.Empty<ProductDto>();
            var baseUrl = _cfg["STRAPI_BASE_URL"] ?? "";
            return StrapiMapper.MapProducts(dto, baseUrl);
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
}
