using System.Drawing;
using Microsoft.ML;
using Microsoft.ML.Transforms.Onnx;
using Microsoft.ML.Transforms.Image;
using System.Diagnostics.CodeAnalysis;
using Microsoft.ML.Data;

namespace Web.Classification;

public class ImageClassificationService
{
    private const string UnknownClass = "Unknown";
    private readonly MLContext _context = new();
    private readonly PredictionEngine<ImageInput, Output> _engine;
    private readonly ILogger<ImageClassificationService> _logger;
    private readonly IImageClassifierSettings _settings;

    public ImageClassificationService(ILogger<ImageClassificationService> logger, IImageClassifierSettings settings)
    {
        var pipeline = _context.Transforms
            .ResizeImages(outputColumnName: settings.InputName, settings.ImageWidth, settings.ImageHeight, inputColumnName: nameof(ImageInput.Image))
            //convert to tensor
            .Append(_context.Transforms.ExtractPixels(outputColumnName: settings.InputName))
            .Append(_context.Transforms.ApplyOnnxModel(modelFile: settings.ModelPath, inputColumnName: settings.InputName, outputColumnName: settings.OutputName))
            .Append(_context.Transforms.CopyColumns(nameof(Output.Score), settings.OutputName));

        var input = _context.Data.LoadFromEnumerable(Array.Empty<ImageInput>());
        var model = pipeline.Fit(input);

        _engine = _context.Model.CreatePredictionEngine<ImageInput, Output>(model);
        _logger = logger;
        _settings = settings;
    }

    public async Task<(string Class, float Confidence)[]> ClassifyAsync(byte[] image, int numClasses = 10)
    {
        await Task.Yield();
        var ms = new MemoryStream(image);
        var input = new ImageInput { Image = MLImage.CreateFromStream(ms) };
        var output = _engine.Predict(input);
        var scores = output.Score;
        var topScores = scores
            .Select((score, index) => (Score: score, Index: index))
            .OrderByDescending(x => x.Score)
            .Take(numClasses)
            .ToArray();

        return topScores.Select(x =>
        {
            try
            {
                return (_settings.Classes[x.Index], x.Score);
            }
            catch (IndexOutOfRangeException ex)
            {
                _logger.LogError(ex, "Index out of range extracting class for image. Index: {Index}, Score: {Score}", x.Index, x.Score);
                return (UnknownClass, x.Score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error extracting class for image. Index: {Index}, Score: {Score}", x.Index, x.Score);
                return (UnknownClass, x.Score);
            }
        }).ToArray();
    }

    private class ImageInput
    {
        [ImageType(224, 224)]
        public MLImage Image { get; init; }
    }

    private class Output
    {
        public float[] Score { get; init; }
    }
}

