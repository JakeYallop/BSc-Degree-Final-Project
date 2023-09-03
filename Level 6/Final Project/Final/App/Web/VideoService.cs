using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using FFMpegCore;
using Web.Entities;

namespace Web;

public class VideoService
{
    public VideoService()
    {

    }

    public async Task<string> ExtractThumbnailAsync(string inputPath, TimeSpan videoDuration)
    {
        var outputPath = $"{Path.GetDirectoryName(inputPath)}{Path.PathSeparator}{Path.GetFileNameWithoutExtension(inputPath)}-thumbnail.jpg";
        await ExtractFrameAtTimeAsync(inputPath, outputPath, videoDuration / 2);
        return outputPath;
    }

    public async Task<string> ExtractMostSignificantFrameAsync(string inputPath, (Vector4 BoundingBox, TimeSpan Timestamp)[] detections)
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

        var outputPath = $"{Path.GetTempPath()}{Path.PathSeparator}{Guid.NewGuid()}.jpg";
        await ExtractFrameAtTimeAsync(inputPath, outputPath, bestDetection.Timestamp);
        return outputPath;
    }

    private static async Task<string> ExtractFrameAtTimeAsync(string inputPath, string outputPath, TimeSpan timestamp)
    {
        await FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, overwrite: false, args =>
            {
                args
                    .Seek(timestamp)
                    .WithFrameOutputCount(1)
                    .WithCustomArgument("-q:v 5");
            }).ProcessAsynchronously();
        return outputPath;
    }
}


public static class ImageExtensions
{
    public static double GetAreaWH(this Vector4 vector)
    {
        return vector.W * vector.Z;
    }
}
