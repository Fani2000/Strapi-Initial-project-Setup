namespace NutsShop_Server.Shop;

public class StrapiService(IHttpClientFactory _httpFactory, IConfiguration _cfg)
{
    public async Task<IReadOnlyList<ProductDto>> FetchFromStrapiAsync(CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("strapi");
        var resp = await http.GetAsync("/api/products?populate=*", ct);
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<StrapiResponse<List<StrapiEntry<ProductAttributes>>>>(cancellationToken: ct);
        if (dto is null) return Array.Empty<ProductDto>();
        var baseUrl = _cfg["STRAPI_BASE_URL"] ?? "";
        return StrapiMapper.MapProducts(dto, baseUrl);
    }

    public async Task<HomePageDto> FetchHomeFromStrapiAsync(CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("strapi");
        var resp = await http.GetAsync("/api/home-page?populate=*", ct);
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<StrapiResponse<StrapiEntry<HomePageAttributes>>>(cancellationToken: ct);
        if (dto is null) return new HomePageDto("", "", "", "", Array.Empty<ProductDto>());
        var baseUrl = _cfg["STRAPI_BASE_URL"] ?? "";
        return StrapiMapper.MapHome(dto, baseUrl);
    }
}