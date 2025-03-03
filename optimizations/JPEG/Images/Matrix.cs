using System.Drawing;
using System.Drawing.Imaging;

namespace JPEG.Images;

class Matrix
{
    public readonly Pixel[,] Pixels;
    public readonly int Height;
    public readonly int Width;

    public Matrix(int height, int width)
    {
        Height = height;
        Width = width;

        Pixels = new Pixel[height, width];
        for (var i = 0; i < height; ++i)
        for (var j = 0; j < width; ++j)
            Pixels[i, j] = new Pixel(0, 0, 0, PixelFormat.RGB);
    }

    public static unsafe explicit operator Matrix(Bitmap bmp)
    {
        var height = bmp.Height - bmp.Height % 8;
        var width = bmp.Width - bmp.Width % 8;
        var matrix = new Matrix(height, width);
        var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        var ptr = (byte*)bmpData.Scan0;
        var stride = bmpData.Stride;

        for (var y = 0; y < height; y++)
        {
            var rowOffset = y * stride;
            for (var x = 0; x < width; x++)
            {
                var offset = ptr + rowOffset + x * 3;
                matrix.Pixels[y, x] = new Pixel(offset[2], offset[1], offset[0], PixelFormat.RGB);
            }
        }

        bmp.UnlockBits(bmpData);
        return matrix;
    }

    public static unsafe explicit operator Bitmap(Matrix matrix)
    {
        var width = matrix.Width;
        var height = matrix.Height;
        var bmp = new Bitmap(width, height);
        var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        var ptr = (byte*)bmpData.Scan0;
        var stride = bmpData.Stride;

        for (var y = 0; y < height; y++)
        {
            var rowOffset = y * stride;
            for (var x = 0; x < width; x++)
            {
                var pixel = matrix.Pixels[y, x];
                var offset = ptr + rowOffset + x * 3;
                offset[2] = ToByte(pixel.R);
                offset[1] = ToByte(pixel.G);
                offset[0] = ToByte(pixel.B);
            }
        }
        bmp.UnlockBits(bmpData);

        return bmp;
    }

    private static byte ToByte(double d)
    {
        return d switch
        {
            > byte.MaxValue => byte.MaxValue,
            < byte.MinValue => byte.MinValue,
            _ => (byte)d
        };
    }
}