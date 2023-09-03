using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
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

    public async Task<Stream> ExtractMostSignificantFrameAsync(Stream input, (Vector4 BoundingBox, TimeSpan Timestamp)[] detections)
    {
        Debug.Assert(detections.Length > 0);
        var bestDetection = detections[0];
        var largestArea = 0d;
        foreach (var detection in detections)
        {
            var area = detection.BoundingBox.GetAreaWH();
            if (area > largestArea)
            {
                bestDetection = detection;
                largestArea = area;
            }
        }

        return await ExtractFrameAtTimeAsync(input, bestDetection.Timestamp);
    }

    private static async Task<Stream> ExtractFrameAtTimeAsync(Stream input, TimeSpan timestamp)
    {
        var inputPath = $"{Path.GetTempPath()}{Path.PathSeparator}{Guid.NewGuid()}.mp4";
        var file = File.OpenWrite(inputPath);
        input.CopyTo(file);
        file.Close();

        var outputPath = $"{Path.GetTempPath()}{Path.PathSeparator}{Guid.NewGuid()}.jpg";
        await FFMpegArguments
            .FromFileInput(inputPath, verifyExists: false, args =>
            {
                args.Seek(timestamp);
                    //.ForceFormat("h264")
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


public static class ImageExtensions
{
    public static double GetAreaWH(this Vector4 vector)
    {
        return vector.W * vector.Z;
    }
}
