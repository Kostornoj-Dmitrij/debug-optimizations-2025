using System;
using System.Drawing;

namespace JPEG.Images;

class Matrix
{
    public readonly Pixel[] Pixels;
    public readonly int Height;
    public readonly int Width;

    public Matrix(int height, int width)
    {
        Height = height;
        Width = width;
        Pixels = new Pixel[height * width];  // Одномерный массив для ускорения работы с памятью
    }

    public Pixel this[int y, int x]
    {
        get => Pixels[y * Width + x];
        set => Pixels[y * Width + x] = value;
    }

    public static explicit operator Matrix(Bitmap bmp)
    {
        int height = bmp.Height - bmp.Height % 8;
        int width = bmp.Width - bmp.Width % 8;
        var matrix = new Matrix(height, width);
        
        var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), 
            System.Drawing.Imaging.ImageLockMode.ReadOnly, 
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        
        unsafe
        {
            byte* ptr = (byte*)bmpData.Scan0;
            int stride = bmpData.Stride;

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    byte b = ptr[j * stride + i * 3];
                    byte g = ptr[j * stride + i * 3 + 1];
                    byte r = ptr[j * stride + i * 3 + 2];
                    matrix[j, i] = new Pixel(r, g, b, false);
                }
            }
        }
        
        bmp.UnlockBits(bmpData);
        return matrix;
    }

    public static explicit operator Bitmap(Matrix matrix)
    {
        var bmp = new Bitmap(matrix.Width, matrix.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        var bmpData = bmp.LockBits(new Rectangle(0, 0, matrix.Width, matrix.Height), 
            System.Drawing.Imaging.ImageLockMode.WriteOnly, 
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);

        unsafe
        {
            byte* ptr = (byte*)bmpData.Scan0;
            int stride = bmpData.Stride;

            for (int j = 0; j < matrix.Height; j++)
            {
                for (int i = 0; i < matrix.Width; i++)
                {
                    var pixel = matrix[j, i];
                    ptr[j * stride + i * 3] = (byte)pixel.B;
                    ptr[j * stride + i * 3 + 1] = (byte)pixel.G;
                    ptr[j * stride + i * 3 + 2] = (byte)pixel.R;
                }
            }
        }

        bmp.UnlockBits(bmpData);
        return bmp;
    }
}
