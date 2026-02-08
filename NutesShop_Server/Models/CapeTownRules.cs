namespace NutsShop_Server.Shop;

public static class CapeTownRules
{
    public const string AllowedCity = "Cape Town";

    public static readonly Dictionary<string, string> PickupLocations = new()
    {
        ["CT_WATERFRONT"] = "V&A Waterfront Pickup Point",
        ["CT_CBD"] = "Cape Town CBD Pickup Point",
        ["CT_CLAREMONT"] = "Claremont Pickup Point"
    };

    public static bool IsCapeTown(string? city) =>
        string.Equals(city?.Trim(), AllowedCity, StringComparison.OrdinalIgnoreCase);
}
