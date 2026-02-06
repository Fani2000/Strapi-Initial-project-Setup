using System.Text.Json;

namespace NutesShop_Server.Shop;

public static class StrapiMapper
{
    public static IReadOnlyList<ProductDto> MapProducts(string json, string strapiBaseUrl)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<ProductDto>();

        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var item in data.EnumerateArray())
        {
            if (!item.TryGetProperty("attributes", out var a)) continue;

            var slug = a.TryGetProperty("slug", out var s) ? s.GetString() : null;
            var name = a.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(name)) continue;

            var desc = a.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";

            var priceCents = 0;
            var per = "each";
            if (a.TryGetProperty("price", out var price))
            {
                if (price.TryGetProperty("amount", out var amount) && amount.ValueKind == JsonValueKind.Number)
                    priceCents = (int)Math.Round(amount.GetDecimal() * 100m);
                if (price.TryGetProperty("per", out var perEl))
                    per = perEl.GetString() ?? per;
            }

            var imageUrl = "";
            if (a.TryGetProperty("images", out var imgs) &&
                imgs.TryGetProperty("data", out var imgData) &&
                imgData.ValueKind == JsonValueKind.Array &&
                imgData.GetArrayLength() > 0)
            {
                var url = imgData[0].GetProperty("attributes").GetProperty("url").GetString();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    imageUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? url
                        : $"{strapiBaseUrl.TrimEnd('/')}{url}";
                }
            }

            var inStock = a.TryGetProperty("inStock", out var stock) && stock.ValueKind == JsonValueKind.True;
            var featured = a.TryGetProperty("featured", out var feat) && feat.ValueKind == JsonValueKind.True;

            var badges = Array.Empty<string>();
            if (a.TryGetProperty("badges", out var b) && b.ValueKind == JsonValueKind.Array)
            {
                badges = b.EnumerateArray()
                    .Select(x => x.TryGetProperty("label", out var l) ? l.GetString() : null)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .ToArray();
            }

            list.Add(new ProductDto(
                Slug: slug!,
                Name: name!,
                Description: desc,
                PriceCents: priceCents,
                Per: per,
                ImageUrl: imageUrl,
                InStock: inStock,
                Featured: featured,
                Badges: badges
            ));
        }

        return list;
    }

    public static HomePageDto MapHome(string json, string strapiBaseUrl)
    {
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        var a = data.GetProperty("attributes");

        var heroTitle = a.TryGetProperty("heroTitle", out var ht) ? ht.GetString() ?? "" : "";
        var heroSubtitle = a.TryGetProperty("heroSubtitle", out var hs) ? hs.GetString() ?? "" : "";
        var promoText = a.TryGetProperty("promoText", out var pt) ? pt.GetString() ?? "" : "";

        var heroImageUrl = "";
        if (a.TryGetProperty("heroImage", out var heroImg) &&
            heroImg.TryGetProperty("data", out var heroData) &&
            heroData.ValueKind != JsonValueKind.Null)
        {
            var url = heroData.GetProperty("attributes").GetProperty("url").GetString();
            if (!string.IsNullOrWhiteSpace(url))
            {
                heroImageUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? url
                    : $"{strapiBaseUrl.TrimEnd('/')}{url}";
            }
        }

        var featured = Array.Empty<ProductDto>();
        if (a.TryGetProperty("featuredProducts", out var fp) &&
            fp.TryGetProperty("data", out var fpData) &&
            fpData.ValueKind == JsonValueKind.Array)
        {
            var payload = new JsonObjectBuilder()
                .WithData(fpData)
                .Build();
            featured = MapProducts(payload, strapiBaseUrl).ToArray();
        }

        return new HomePageDto(
            HeroTitle: heroTitle,
            HeroSubtitle: heroSubtitle,
            PromoText: promoText,
            HeroImageUrl: heroImageUrl,
            FeaturedProducts: featured
        );
    }

    public static IReadOnlyList<ProductDto> MapProducts(
        StrapiResponse<List<StrapiEntry<ProductAttributes>>> resp,
        string strapiBaseUrl)
    {
        if (resp?.Data is null) return Array.Empty<ProductDto>();
        return resp.Data
            .Select(e => MapProduct(e.Attributes, strapiBaseUrl))
            .Where(p => p is not null)
            .Select(p => p!)
            .ToArray();
    }

    public static HomePageDto MapHome(
        StrapiResponse<StrapiEntry<HomePageAttributes>> resp,
        string strapiBaseUrl)
    {
        var a = resp?.Data?.Attributes;
        if (a is null)
        {
            return new HomePageDto("", "", "", "", Array.Empty<ProductDto>());
        }

        var heroImageUrl = ResolveUrl(a.HeroImage?.Data?.Attributes?.Url, strapiBaseUrl);
        var featured = a.FeaturedProducts?.Data?
            .Select(p => MapProduct(p.Attributes, strapiBaseUrl))
            .Where(p => p is not null)
            .Select(p => p!)
            .ToArray() ?? Array.Empty<ProductDto>();

        return new HomePageDto(
            HeroTitle: a.HeroTitle ?? "",
            HeroSubtitle: a.HeroSubtitle ?? "",
            PromoText: a.PromoText ?? "",
            HeroImageUrl: heroImageUrl ?? "",
            FeaturedProducts: featured
        );
    }

    private static ProductDto? MapProduct(ProductAttributes? a, string strapiBaseUrl)
    {
        if (a is null) return null;
        if (string.IsNullOrWhiteSpace(a.Slug) || string.IsNullOrWhiteSpace(a.Name))
            return null;

        var priceCents = 0;
        var per = "each";
        if (a.Price?.Amount is not null)
            priceCents = (int)Math.Round(a.Price.Amount.Value * 100m);
        if (!string.IsNullOrWhiteSpace(a.Price?.Per))
            per = a.Price!.Per!;

        var imageUrl = ResolveUrl(a.Images?.Data?.FirstOrDefault()?.Attributes?.Url, strapiBaseUrl) ?? "";
        var badges = a.Badges?
            .Select(b => b.Label)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l!)
            .ToArray() ?? Array.Empty<string>();

        return new ProductDto(
            Slug: a.Slug!,
            Name: a.Name!,
            Description: a.Description ?? "",
            PriceCents: priceCents,
            Per: per,
            ImageUrl: imageUrl,
            InStock: a.InStock ?? false,
            Featured: a.Featured ?? false,
            Badges: badges
        );
    }

    private static string? ResolveUrl(string? url, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        return url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"{baseUrl.TrimEnd('/')}{url}";
    }

    private sealed class JsonObjectBuilder
    {
        private JsonElement _data;

        public JsonObjectBuilder WithData(JsonElement data)
        {
            _data = data;
            return this;
        }

        public string Build()
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            writer.WriteStartObject();
            writer.WritePropertyName("data");
            _data.WriteTo(writer);
            writer.WriteEndObject();
            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
