using System.Net.Http.Headers;

namespace NutsShop_Server.Shop;

public static class HttpExtension
{
    public static void ConfigureHttpClient(this IServiceCollection services, IConfiguration cfg)
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
