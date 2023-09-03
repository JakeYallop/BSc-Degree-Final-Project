namespace Web.Classification;

public interface IImageClassifierSettings
{
    int ImageHeight { get; }
    int ImageWidth { get; }
    string InputName { get; }
    string OutputName { get; }
    string ModelPath { get; }
    string[] Classes { get; }
}

