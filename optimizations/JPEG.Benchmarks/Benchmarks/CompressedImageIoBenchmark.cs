using BenchmarkDotNet.Attributes;

namespace JPEG.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class CompressedImageIoBenchmark
{
    private CompressedImage? _image;
    private string? _tempFilePath;

    [Params(64, 128, 256)]
    public int ImageSize;

    [Params(50, 70, 90)]
    public int Quality;

    [GlobalSetup]
    public void Setup()
    {
        _image = new CompressedImage
        {
            Width = ImageSize,
            Height = ImageSize,
            Quality = Quality,
            DecodeTable = new Dictionary<BitsWithLength, byte>
            {
                [new BitsWithLength { Bits = 0b110, BitsCount = 3 }] = (byte)'A',
                [new BitsWithLength { Bits = 0b111, BitsCount = 3 }] = (byte)'B',
            },
            BitsCount = 1024,
            CompressedBytes = new byte[ImageSize]
        };

        _tempFilePath = Path.Combine(Path.GetTempPath(), $"testimage_{ImageSize}_{Quality}.bin");
    }

    [Benchmark]
    public void SaveImage()
    {
        if (_image != null) _image.Save(_tempFilePath);
    }

    [Benchmark]
    public void LoadImage() => CompressedImage.Load(_tempFilePath);
}