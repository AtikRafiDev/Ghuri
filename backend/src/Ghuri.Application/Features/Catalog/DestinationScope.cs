namespace Ghuri.Application.Features.Catalog;

/// <summary>Filter for destination lists: inside Bangladesh, or abroad.</summary>
public enum DestinationScope
{
    National = 1,
    International = 2
}

/// <summary>The agency's own country. "National" = destinations in it; everything else is international.</summary>
public static class HomeCountry
{
    public const string IsoCode = "BD";
}
