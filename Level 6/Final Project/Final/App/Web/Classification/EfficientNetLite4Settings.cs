namespace Web.Classification;

public class EfficientNetLite4Settings : IImageClassifierSettings
{
    private EfficientNetLite4Settings() { }
    public static EfficientNetLite4Settings Create(string modelPath, string classesPath)
    {
        var settings = new EfficientNetLite4Settings();
        settings.ModelPath = modelPath;
        settings.Classes = File.ReadAllLines(classesPath);
        return settings;
    }

    public int ImageHeight { get; } = 224;
    public int ImageWidth { get; } = 224;
    public string InputName { get; } = "images:0";
    public string OutputName { get; } = "Softmax:0";
    public string ModelPath { get; private set; }
    public string[] Classes { get; private set; } = null!;
}

