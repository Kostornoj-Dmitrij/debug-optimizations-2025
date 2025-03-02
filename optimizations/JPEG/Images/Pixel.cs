using System;

namespace JPEG.Images;

public readonly struct Pixel
{
    private readonly double c1, c2, c3;
    private readonly bool isYCbCr;

    public Pixel(double first, double second, double third, bool isYCbCr)
    {
        this.c1 = first;
        this.c2 = second;
        this.c3 = third;
        this.isYCbCr = isYCbCr;
    }

    public double R => isYCbCr ? (298.082 * c1 + 408.583 * c3) / 256.0 - 222.921 : c1;
    public double G => isYCbCr ? (298.082 * c1 - 100.291 * c2 - 208.120 * c3) / 256.0 + 135.576 : c2;
    public double B => isYCbCr ? (298.082 * c1 + 516.412 * c2) / 256.0 - 276.836 : c3;

    public double Y  => isYCbCr ? c1 : 16.0 + (65.738 * R + 129.057 * G + 24.064 * B) / 256.0;
    public double Cb => isYCbCr ? c2 : 128.0 + (-37.945 * R - 74.494 * G + 112.439 * B) / 256.0;
    public double Cr => isYCbCr ? c3 : 128.0 + (112.439 * R - 94.154 * G - 18.285 * B) / 256.0;
}