namespace MovieDatabase.Api.Core;

public static class Constants
{
    public static class Queries
    {
        public const int MaxPageSize = 20;
        public const int DefaultPageSize = 10;
    }

    public static class Blobs
    {
        public const long MaxBlobSizeInBytes = 5_000_000;
        public static readonly HashSet<string> AllowedContentTypes =
        [
            "image/png",
            "image/jpeg",
            "image/jpg",
            "image/webp",
        ];
    }
}