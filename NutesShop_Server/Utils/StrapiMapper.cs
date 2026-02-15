using System.Text.Json;
using NutesShop_Server.Shop;

namespace NutsShop_Server.Shop;

public static class StrapiMapper
{
    public static ThemeDto DefaultTheme()
    {
        return new ThemeDto(
            Name: "Amber Bright",
            Background: "#fff6e6",
            BackgroundAccent: "#ffe3a3",
            Card: "#fff3d4",
            CardSoft: "rgba(255, 255, 255, 0.75)",
            Text: "#2a1a05",
            Muted: "rgba(42, 26, 5, 0.65)",
            Accent: "#f59e0b",
            Accent2: "#fbbf24",
            TopbarBg: "rgba(255, 248, 230, 0.9)",
            TopbarBorder: "rgba(245, 158, 11, 0.25)",
            HeroGradient1: "#fff3d4",
            HeroGradient2: "#ffe1a0",
            HeroGradient3: "#ffd57a",
            HeroOverlay1: "rgba(255, 255, 255, 0.15)",
            HeroOverlay2: "rgba(255, 255, 255, 0.0)",
            Glow: "rgba(245, 158, 11, 0.45)",
            Shadow: "0 18px 40px rgba(245, 158, 11, 0.18)"
        );
    }

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

    public static ThemeDto MapTheme(
        StrapiResponse<StrapiEntry<ThemeAttributes>> resp,
        string strapiBaseUrl)
    {
        _ = strapiBaseUrl;
        var defaults = DefaultTheme();
        var ext = resp?.Data?.ExtensionData;
        if (ext is not null && ext.Count > 0)
            return MapThemeFromFlat(ext, defaults);

        var a = resp?.Data?.Attributes;
        if (a is null) return defaults;

        return new ThemeDto(
            Name: a.Name ?? defaults.Name,
            Background: a.Background ?? defaults.Background,
            BackgroundAccent: a.BackgroundAccent ?? defaults.BackgroundAccent,
            Card: a.Card ?? defaults.Card,
            CardSoft: a.CardSoft ?? defaults.CardSoft,
            Text: a.Text ?? defaults.Text,
            Muted: a.Muted ?? defaults.Muted,
            Accent: a.Accent ?? defaults.Accent,
            Accent2: a.Accent2 ?? defaults.Accent2,
            TopbarBg: a.TopbarBg ?? defaults.TopbarBg,
            TopbarBorder: a.TopbarBorder ?? defaults.TopbarBorder,
            HeroGradient1: a.HeroGradient1 ?? defaults.HeroGradient1,
            HeroGradient2: a.HeroGradient2 ?? defaults.HeroGradient2,
            HeroGradient3: a.HeroGradient3 ?? defaults.HeroGradient3,
            HeroOverlay1: a.HeroOverlay1 ?? defaults.HeroOverlay1,
            HeroOverlay2: a.HeroOverlay2 ?? defaults.HeroOverlay2,
            Glow: a.Glow ?? defaults.Glow,
            Shadow: a.Shadow ?? defaults.Shadow
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

    private static ThemeDto MapThemeFromFlat(
        Dictionary<string, System.Text.Json.JsonElement> data,
        ThemeDto defaults)
    {
        return new ThemeDto(
            Name: GetString(data, "name") ?? defaults.Name,
            Background: GetString(data, "background") ?? defaults.Background,
            BackgroundAccent: GetString(data, "backgroundAccent") ?? defaults.BackgroundAccent,
            Card: GetString(data, "card") ?? defaults.Card,
            CardSoft: GetString(data, "cardSoft") ?? defaults.CardSoft,
            Text: GetString(data, "text") ?? defaults.Text,
            Muted: GetString(data, "muted") ?? defaults.Muted,
            Accent: GetString(data, "accent") ?? defaults.Accent,
            Accent2: GetString(data, "accent2") ?? defaults.Accent2,
            TopbarBg: GetString(data, "topbarBg") ?? defaults.TopbarBg,
            TopbarBorder: GetString(data, "topbarBorder") ?? defaults.TopbarBorder,
            HeroGradient1: GetString(data, "heroGradient1") ?? defaults.HeroGradient1,
            HeroGradient2: GetString(data, "heroGradient2") ?? defaults.HeroGradient2,
            HeroGradient3: GetString(data, "heroGradient3") ?? defaults.HeroGradient3,
            HeroOverlay1: GetString(data, "heroOverlay1") ?? defaults.HeroOverlay1,
            HeroOverlay2: GetString(data, "heroOverlay2") ?? defaults.HeroOverlay2,
            Glow: GetString(data, "glow") ?? defaults.Glow,
            Shadow: GetString(data, "shadow") ?? defaults.Shadow
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
