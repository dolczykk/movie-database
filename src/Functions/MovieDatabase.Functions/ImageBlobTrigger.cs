using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace MovieDatabase.Functions;

public static class ImageBlobTrigger
{
    [Function(nameof(ImageBlobTrigger))]
    [BlobOutput("thumbnails/{baseName}.jpg")]
    public static async Task<byte[]> Run(
        [BlobTrigger("images/{baseName}.{ext}")] ReadOnlyMemory<byte> blob,
        string baseName,
        string ext,
        FunctionContext context)
    {
        var logger = context.GetLogger(nameof(ImageBlobTrigger));

        var sourceName = $"{baseName}.{ext}";

        logger.LogInformation("Processing blob: {name}", sourceName);

        await using var blobStream = new MemoryStream(blob.ToArray(), writable: false);

        using var image = await Image.LoadAsync(blobStream);
        var encoder = new JpegEncoder
        {
            Quality = 70
        };
        
        using var memoryStream = new MemoryStream();
        
        await image.SaveAsync(memoryStream, encoder);

        logger.LogInformation("Thumbnail created for blob: {name}", sourceName);

        return memoryStream.ToArray();
    }
}