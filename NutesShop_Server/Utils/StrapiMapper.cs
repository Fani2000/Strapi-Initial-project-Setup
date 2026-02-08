using System.Text.Json;
using NutesShop_Server.Shop;

namespace NutsShop_Server.Shop;

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

            list.Add(new ProductDto
            {
                Slug = slug!,
                Name = name!,
                Description = desc,
                PriceCents = priceCents,
                Per = per,
                ImageUrl = imageUrl,
                InStock = inStock,
                Featured = featured,
                Badges = badges
            });
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
            .Select(e => MapProductEntry(e, strapiBaseUrl))
            .Where(p => p is not null)
            .Select(p => p!)
            .ToArray();
    }

    public static HomePageDto MapHome(
        StrapiResponse<StrapiEntry<HomePageAttributes>> resp,
        string strapiBaseUrl)
    {
        var ext = resp?.Data?.ExtensionData;
        if (ext is not null && ext.Count > 0)
            return MapHomeFromFlat(ext, strapiBaseUrl);

        var a = resp?.Data?.Attributes;
        if (a is null)
            return new HomePageDto("", "", "", "", Array.Empty<ProductDto>());

        var heroImageUrl = ResolveUrl(a.HeroImage?.Data?.Attributes?.Url, strapiBaseUrl);
        var featured = a.FeaturedProducts?.Data?
            .Select(p => MapProductEntry(p, strapiBaseUrl))
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

    private static HomePageDto MapHomeFromFlat(
        Dictionary<string, System.Text.Json.JsonElement> data,
        string strapiBaseUrl)
    {
        var heroTitle = GetString(data, "heroTitle") ?? "";
        var heroSubtitle = GetString(data, "heroSubtitle") ?? "";
        var promoText = GetString(data, "promoText") ?? "";

        var heroImageUrl = "";
        if (data.TryGetValue("heroImage", out var heroEl))
        {
            if (heroEl.ValueKind == JsonValueKind.Object)
            {
                // v4 style: heroImage.data.attributes.url
                if (heroEl.TryGetProperty("data", out var heroData) &&
                    heroData.ValueKind != JsonValueKind.Null)
                {
                    var url = heroData.GetProperty("attributes").GetProperty("url").GetString();
                    heroImageUrl = ResolveUrl(url, strapiBaseUrl) ?? "";
                }
                // v5 flat: heroImage.url
                else if (heroEl.TryGetProperty("url", out var urlEl))
                {
                    heroImageUrl = ResolveUrl(urlEl.GetString(), strapiBaseUrl) ?? "";
                }
            }
            else if (heroEl.ValueKind == JsonValueKind.Array && heroEl.GetArrayLength() > 0)
            {
                var first = heroEl[0];
                if (first.TryGetProperty("url", out var urlEl))
                    heroImageUrl = ResolveUrl(urlEl.GetString(), strapiBaseUrl) ?? "";
            }
        }

        var featured = Array.Empty<ProductDto>();
        if (data.TryGetValue("featuredProducts", out var fp) && fp.ValueKind == JsonValueKind.Object)
        {
            if (fp.TryGetProperty("data", out var fpData) && fpData.ValueKind == JsonValueKind.Array)
            {
                var payload = new JsonObjectBuilder().WithData(fpData).Build();
                var parsed = System.Text.Json.JsonSerializer.Deserialize<StrapiResponse<List<StrapiEntry<ProductAttributes>>>>(payload);
                if (parsed is not null)
                    featured = MapProducts(parsed, strapiBaseUrl).ToArray();
            }
        }

        return new HomePageDto(
            HeroTitle: heroTitle,
            HeroSubtitle: heroSubtitle,
            PromoText: promoText,
            HeroImageUrl: heroImageUrl,
            FeaturedProducts: featured
        );
    }

    private static ProductDto? MapProductEntry(StrapiEntry<ProductAttributes> entry, string strapiBaseUrl)
    {
        if (entry.ExtensionData is not null && entry.ExtensionData.Count > 0)
            return MapProductFromFlat(entry.ExtensionData, strapiBaseUrl);

        if (entry.Attributes is not null)
            return MapProduct(entry.Attributes, strapiBaseUrl);

        return null;
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

        return new ProductDto
        {
            Slug = a.Slug!,
            Name = a.Name!,
            Description = a.Description ?? "",
            PriceCents = priceCents,
            Per = per,
            ImageUrl = imageUrl,
            InStock = a.InStock ?? false,
            Featured = a.Featured ?? false,
            Badges = badges
        };
    }

    private static ProductDto? MapProductFromFlat(
        Dictionary<string, System.Text.Json.JsonElement> data,
        string strapiBaseUrl)
    {
        var slug = GetString(data, "slug");
        var name = GetString(data, "name");
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(name))
            return null;

        var description = GetString(data, "description") ?? "";
        var inStock = GetBool(data, "inStock") ?? false;
        var featured = GetBool(data, "featured") ?? false;

        var priceCents = 0;
        var per = "each";
        if (data.TryGetValue("price", out var priceEl) && priceEl.ValueKind == JsonValueKind.Object)
        {
            if (priceEl.TryGetProperty("amount", out var amountEl) && amountEl.ValueKind == JsonValueKind.Number)
                priceCents = (int)Math.Round(amountEl.GetDecimal() * 100m);
            if (priceEl.TryGetProperty("per", out var perEl))
                per = perEl.GetString() ?? per;
        }

        var imageUrl = "";
        if (data.TryGetValue("images", out var imagesEl))
        {
            if (imagesEl.ValueKind == JsonValueKind.Object)
            {
                // v4 style: images.data[].attributes.url
                if (imagesEl.TryGetProperty("data", out var imgData) &&
                    imgData.ValueKind == JsonValueKind.Array &&
                    imgData.GetArrayLength() > 0)
                {
                    var url = imgData[0].GetProperty("attributes").GetProperty("url").GetString();
                    imageUrl = ResolveUrl(url, strapiBaseUrl) ?? "";
                }
            }
            else if (imagesEl.ValueKind == JsonValueKind.Array && imagesEl.GetArrayLength() > 0)
            {
                // v5 flat: images[] with url at root
                var first = imagesEl[0];
                if (first.TryGetProperty("url", out var urlEl))
                    imageUrl = ResolveUrl(urlEl.GetString(), strapiBaseUrl) ?? "";
            }
        }

        var badges = Array.Empty<string>();
        var badgeKey = data.ContainsKey("badges") ? "badges" : "bages";
        if (data.TryGetValue(badgeKey, out var badgesEl) && badgesEl.ValueKind == JsonValueKind.Array)
        {
            badges = badgesEl.EnumerateArray()
                .Select(x => x.TryGetProperty("label", out var l) ? l.GetString() : null)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToArray();
        }

        return new ProductDto
        {
            Slug = slug!,
            Name = name!,
            Description = description,
            PriceCents = priceCents,
            Per = per,
            ImageUrl = imageUrl,
            InStock = inStock,
            Featured = featured,
            Badges = badges
        };
    }

    private static string? GetString(Dictionary<string, System.Text.Json.JsonElement> data, string key)
    {
        return data.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
    }

    private static bool? GetBool(Dictionary<string, System.Text.Json.JsonElement> data, string key)
    {
        return data.TryGetValue(key, out var el) && el.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? el.GetBoolean()
            : null;
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
