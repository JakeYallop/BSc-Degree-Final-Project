using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using ImageMagick;
using Web.Entities;

namespace Web;

public class VideoService
{
    public VideoService()
    {

    }

    public async Task<Stream> ExtractThumbnailAsync(Stream input, TimeSpan videoDuration)
    {
        //var outputPath = $"{Path.GetDirectoryName(inputPath)}{Path.PathSeparator}{Path.GetFileNameWithoutExtension(inputPath)}-thumbnail.jpg";
        return await ExtractFrameAtTimeAsync(input, videoDuration / 2);
    }

    public async Task<Stream> GetBestCroppedFrameAsync(Stream input, (BoundingBox BoundingBox, TimeSpan Timestamp)[] detections)
    {
        Debug.Assert(detections.Length > 0);

        var enumerable = detections
            .Select(x => (x.Timestamp, x.BoundingBox, x.BoundingBox.GetArea()))
            .OrderByDescending(x => x.Item3)
            .AsEnumerable();

        if (detections.Length > 5)
        {
            enumerable = enumerable.Skip((int)(detections.Length * 0.1));
        }

        var best = enumerable.First();
        var frame = await ExtractFrameAtTimeAsync(input, best.Timestamp);
        var image = new MagickImage(frame, MagickFormat.Jpeg);
        var newBox = best.BoundingBox.Grow(30, image.Width, image.Height);
        image.Crop(new MagickGeometry((int)newBox.X, (int)newBox.Y, (int)newBox.Width, (int)newBox.Height));
        image.Write($"{Path.GetTempPath()}{Path.DirectorySeparatorChar}{Guid.NewGuid()}.jpg");
        return new MemoryStream(image.ToByteArray());
    }

    private static async Task<Stream> ExtractFrameAtTimeAsync(Stream input, TimeSpan timestamp)
    {
        var inputPath = $"{Path.GetTempPath()}{Path.DirectorySeparatorChar}{Guid.NewGuid()}.mp4";
        var file = File.OpenWrite(inputPath);
        input.CopyTo(file);
        file.Close();

        var outputPath = $"{Path.GetTempPath()}{Path.DirectorySeparatorChar}{Guid.NewGuid()}.jpg";
        await FFMpegArguments
            .FromFileInput(inputPath, verifyExists: false, args =>
            {
                args.Seek(timestamp);
            })
            .OutputToFile(outputPath, overwrite: false, args =>
            {
                args
                    .WithFrameOutputCount(1)
                    .WithCustomArgument("-q:v 5");
            }).ProcessAsynchronously();
        return File.OpenRead(outputPath);
    }

   
}
