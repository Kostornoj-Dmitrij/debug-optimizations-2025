using System;
using System.Threading.Tasks;
using JPEG.Utilities;

namespace JPEG;

public class DCT
{
    private static readonly double[,] CosTable = PrecomputeCosTable(8);

    private static double[,] PrecomputeCosTable(int size)
    {
        var table = new double[size, size];
        for (int u = 0; u < size; u++)
        for (int x = 0; x < size; x++)
            table[u, x] = Math.Cos((2 * x + 1) * u * Math.PI / (2 * size));
        return table;
    }

    public static double[,] DCT2D(double[,] input)
    {
        int height = input.GetLength(0);
        int width = input.GetLength(1);
        var coeffs = new double[width, height];

        Parallel.For(0, width, u =>
        {
            for (int v = 0; v < height; v++)
            {
                double sum = 0;
                for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    sum += input[x, y] * CosTable[u, x] * CosTable[v, y];

                coeffs[u, v] = sum * Beta(height, width) * Alpha(u) * Alpha(v);
            }
        });

        return coeffs;
    }
    public static double[,] IDCT2D(double[,] coeffs)
    {
        int height = coeffs.GetLength(0);
        int width = coeffs.GetLength(1);
        var output = new double[height, width];

        Parallel.For(0, width, x =>
        {
            for (int y = 0; y < height; y++)
            {
                double sum = 0;
                for (int u = 0; u < width; u++)
                for (int v = 0; v < height; v++)
                    sum += coeffs[u, v] * CosTable[u, x] * CosTable[v, y] * Alpha(u) * Alpha(v);

                output[x, y] = sum * Beta(height, width);
            }
        });

        return output;
    }

    public static double BasisFunction(double a, double u, double v, double x, double y, int height, int width)
    {
        var b = Math.Cos(((2d * x + 1d) * u * Math.PI) / (2 * width));
        var c = Math.Cos(((2d * y + 1d) * v * Math.PI) / (2 * height));

        return a * b * c;
    }
    private static double Alpha(int u) => u == 0 ? 1 / Math.Sqrt(2) : 1;
    private static double Beta(int height, int width) => 1d / width + 1d / height;
}