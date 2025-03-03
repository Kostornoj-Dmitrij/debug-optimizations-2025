using System;
using System.Runtime.InteropServices;

namespace JPEG;

public unsafe class DCT
    {
        private readonly int _size;
        private readonly double* _cosineTable;
        private readonly double* _alphaTable;
        private readonly double _beta;

        public DCT(int dctSize)
        {
            _size = dctSize;

            _cosineTable = (double*)Marshal.AllocHGlobal(sizeof(double) * dctSize * dctSize);
            _alphaTable = (double*)Marshal.AllocHGlobal(sizeof(double) * dctSize);

            _beta = 2d / dctSize;

            for (var x = 0; x < dctSize; x++)
            {
                _alphaTable![x] = x == 0 ? 0.7071067811865475 : 1;

                for (var u = 0; u < dctSize; u++)
                {
                    *(_cosineTable + x * dctSize + u) = Math.Cos((x + 0.5) * u * Math.PI / dctSize);
                }
            }
        }

        public void DCT2D(double[,] input, double[,] coeffs)
        {
            fixed (double* inputPtr = &input[0, 0])
            fixed (double* coeffsPtr = &coeffs[0, 0])
            {
                var dct2DTemp = new double[_size, _size];

                for (var u = 0; u < _size; u++)
                {
                    for (var y = 0; y < _size; y++)
                    {
                        var sum = 0.0;
                        for (var x = 0; x < _size; x++)
                        {
                            sum += *(inputPtr + x * _size + y) * *(_cosineTable + x * _size + u);
                        }
                        dct2DTemp[y, u] = sum;
                    }
                }

                for (var u = 0; u < _size; u++)
                {
                    for (var v = 0; v < _size; v++)
                    {
                        var sum = 0.0;
                        for (var y = 0; y < _size; y++)
                        {
                            sum += dct2DTemp[y, u] * *(_cosineTable + y * _size + v);
                        }
                        *(coeffsPtr + u * _size + v) = sum * _beta * _alphaTable[u] * _alphaTable[v];
                    }
                }
            }
        }

        public void IDCT2D(double[,] coeffs, double[,] output)
        {
            fixed (double* coeffsPtr = &coeffs[0, 0])
            fixed (double* outputPtr = &output[0, 0])
            {
                var idct2DTemp = new double[_size, _size];

                for (var y = 0; y < _size; y++)
                {
                    for (var u = 0; u < _size; u++)
                    {
                        var sum = 0.0;
                        for (var v = 0; v < _size; v++)
                        {
                            sum += *(coeffsPtr + u * _size + v) * *(_cosineTable + y * _size + v) * _alphaTable[v];
                        }
                        idct2DTemp[y, u] = sum * _alphaTable[u];
                    }
                }

                for (var x = 0; x < _size; x++)
                {
                    for (var y = 0; y < _size; y++)
                    {
                        var sum = 0.0;
                        for (var u = 0; u < _size; u++)
                        {
                            sum += idct2DTemp[y, u] * *(_cosineTable + x * _size + u);
                        }
                        *(outputPtr + x * _size + y) = sum * _beta;
                    }
                }
            }
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal((IntPtr)_cosineTable);
            Marshal.FreeHGlobal((IntPtr)_alphaTable);
        }
    }