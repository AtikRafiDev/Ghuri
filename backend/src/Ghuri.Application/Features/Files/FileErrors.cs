using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Files;

/// <summary>Every expected failure of the Files feature. Codes never change once shipped.</summary>
public static class FileErrors
{
    public static readonly Error NotAnImage =
        Error.Failure("image_invalid", "Upload a JPEG, PNG or WebP image.");
}
