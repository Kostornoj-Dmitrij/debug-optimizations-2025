using System;
using System.Collections.Generic;

namespace JPEG.Images;
public readonly struct Pixel
{
    private readonly PixelFormat _format;
    private static readonly HashSet<PixelFormat> SupportedFormats = [PixelFormat.RGB, PixelFormat.YCbCr];
    private readonly double _r;
    private readonly double _g;
    private readonly double _b;
    private readonly double _y;
    private readonly double _cb;
    private readonly double _cr;

    public Pixel(double firstComponent, double secondComponent, double thirdComponent, PixelFormat pixelFormat)
    {
        if (!SupportedFormats.Contains(pixelFormat))
            throw new FormatException("Unknown pixel format: " + pixelFormat);
        _format = pixelFormat;
        if (pixelFormat == PixelFormat.RGB)
        {
            _r = firstComponent;
            _g = secondComponent;
            _b = thirdComponent;
            _y = _cb = _cr = 0;
        }
        else
        {
            _y = firstComponent;
            _cb = secondComponent;
            _cr = thirdComponent;
            _r = _g = _b = 0;
        }
    }

    public double R => _format == PixelFormat.RGB ? _r : (298.082 * _y + 408.583 * Cr) / 256.0 - 222.921;
    public double G => _format == PixelFormat.RGB ? _g : (298.082 * Y - 100.291 * Cb - 208.120 * Cr) / 256.0 + 135.576;
    public double B => _format == PixelFormat.RGB ? _b : (298.082 * Y + 516.412 * Cb) / 256.0 - 276.836;
    public double Y => _format == PixelFormat.YCbCr ? _y : 16.0 + (65.738 * R + 129.057 * G + 24.064 * B) / 256.0;
    public double Cb => _format == PixelFormat.YCbCr ? _cb : 128.0 + (-37.945 * R - 74.494 * G + 112.439 * B) / 256.0;
    public double Cr => _format == PixelFormat.YCbCr ? _cr : 128.0 + (112.439 * R - 94.154 * G - 18.285 * B) / 256.0;
}