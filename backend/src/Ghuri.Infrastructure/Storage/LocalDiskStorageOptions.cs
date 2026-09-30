namespace Ghuri.Infrastructure.Storage;

/// <summary>Settings from the "Storage" section of appsettings.json.</summary>
/// <remarks>
/// Public because the Api serves this same folder at PublicBaseUrl
/// (Program.cs) - the same reason JwtOptions is public.
/// </remarks>
public sealed class LocalDiskStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// The folder uploads are written to. A relative path is relative to the
    /// folder the API runs from (backend/src/Ghuri.Api when you use dotnet run
    /// or Visual Studio); after startup this always holds the full path.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>The URL prefix the folder is served under, e.g. "/files".</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
