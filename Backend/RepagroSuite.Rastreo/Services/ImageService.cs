using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Rastreo.Api.Services;

public class ImageService
{
    private const int MaxWidth = 1600;
    private const long MaxBytes = 1024 * 1024; // 1MB

    public async Task<(byte[] bytes, string ext)> ComprimirAsync(Stream input, CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(input, ct);
        if (image.Width > MaxWidth)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(MaxWidth, 0),
                Mode = ResizeMode.Max
            }));
        }

        int quality = 80;
        byte[] bytes;
        while (true)
        {
            using var ms = new MemoryStream();
            await image.SaveAsync(ms, new JpegEncoder { Quality = quality }, ct);
            bytes = ms.ToArray();
            if (bytes.Length <= MaxBytes || quality <= 40) break;
            quality -= 10;
        }
        return (bytes, ".jpg");
    }
}
