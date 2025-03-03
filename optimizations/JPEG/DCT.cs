using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JPEG.Utilities;

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

            // Выделяем память для таблиц
            _cosineTable = (double*)Marshal.AllocHGlobal(sizeof(double) * dctSize * dctSize);
            _alphaTable = (double*)Marshal.AllocHGlobal(sizeof(double) * dctSize);

            _beta = 2d / dctSize;

            for (var x = 0; x < dctSize; x++)
            {
                _alphaTable[x] = x == 0 ? 0.7071067811865475 : 1;

                for (var u = 0; u < dctSize; u++)
                {
                    // Прямой доступ через указатели
                    *((double*)_cosineTable + x * dctSize + u) = Math.Cos((x + 0.5) * u * Math.PI / dctSize);
                }
            }
        }

        public void DCT2D(double[,] input, double[,] coeffs)
        {
            fixed (double* inputPtr = &input[0, 0])
            fixed (double* coeffsPtr = &coeffs[0, 0])
            {
                var dct2DTemp = new double[_size, _size];

                // Первый этап DCT
                for (var u = 0; u < _size; u++)
                {
                    for (var y = 0; y < _size; y++)
                    {
                        var sum = 0.0;
                        for (var x = 0; x < _size; x++)
                        {
                            sum += *((double*)inputPtr + x * _size + y) * *((double*)_cosineTable + x * _size + u);
                        }
                        dct2DTemp[y, u] = sum;
                    }
                }

                // Второй этап DCT
                for (var u = 0; u < _size; u++)
                {
                    for (var v = 0; v < _size; v++)
                    {
                        var sum = 0.0;
                        for (var y = 0; y < _size; y++)
                        {
                            sum += dct2DTemp[y, u] * *((double*)_cosineTable + y * _size + v);
                        }
                        *((double*)coeffsPtr + u * _size + v) = sum * _beta * _alphaTable[u] * _alphaTable[v];
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

                // Первый этап IDCT
                for (var y = 0; y < _size; y++)
                {
                    for (var u = 0; u < _size; u++)
                    {
                        var sum = 0.0;
                        for (var v = 0; v < _size; v++)
                        {
                            sum += *((double*)coeffsPtr + u * _size + v) * *((double*)_cosineTable + y * _size + v) * _alphaTable[v];
                        }
                        idct2DTemp[y, u] = sum * _alphaTable[u];
                    }
                }

                // Второй этап IDCT
                for (var x = 0; x < _size; x++)
                {
                    for (var y = 0; y < _size; y++)
                    {
                        var sum = 0.0;
                        for (var u = 0; u < _size; u++)
                        {
                            sum += idct2DTemp[y, u] * *((double*)_cosineTable + x * _size + u);
                        }
                        *((double*)outputPtr + x * _size + y) = sum * _beta;
                    }
                }
            }
        }

        // Освобождение памяти
        public void Dispose()
        {
            Marshal.FreeHGlobal((IntPtr)_cosineTable);
            Marshal.FreeHGlobal((IntPtr)_alphaTable);
        }
    }