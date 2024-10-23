using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp
{
    public class MultithreadedImageProcessor : IImageProcessor
    {
        private readonly int _numberOfThreads;

        public MultithreadedImageProcessor(int numberOfThreads)
        {
            _numberOfThreads = numberOfThreads;
        }

        public Bitmap ProcessImage(Bitmap originalImage)
        {
            return ProcessImageMultithreaded(originalImage, _numberOfThreads);
        }

        private Bitmap ProcessImageMultithreaded(Bitmap originalImage, int numberOfThreads)
        {
            Bitmap processedImage = new Bitmap(originalImage.Width, originalImage.Height);

            int imageHeight = originalImage.Height;
            int imageWidth = originalImage.Width;
            int rowsPerThread = imageHeight / numberOfThreads;
            int extraRows = imageHeight % numberOfThreads;

            int[,] filterMatrix = {
        { -1, -1, -1 },
        { -1,  8, -1 },
        { -1, -1, -1 }
    };

            BitmapData srcData = originalImage.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData destData = processedImage.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            byte[] srcBuffer = new byte[srcData.Stride * imageHeight];
            byte[] destBuffer = new byte[destData.Stride * imageHeight];
            Marshal.Copy(srcData.Scan0, srcBuffer, 0, srcBuffer.Length);
            Marshal.Copy(destData.Scan0, destBuffer, 0, destBuffer.Length);

            Parallel.For(0, numberOfThreads, i =>
            {
                int startY = i * rowsPerThread;
                int endY = (i == numberOfThreads - 1) ? startY + rowsPerThread + extraRows : startY + rowsPerThread; // Обработка остатка для последнего потока

                int bytesPerPixel = 3;
                int stride = srcData.Stride;
                int filterOffset = 1;

                for (int y = startY; y < endY; y++)
                {
                    for (int x = filterOffset; x < imageWidth - filterOffset; x++)
                    {
                        int blue = 0, green = 0, red = 0;
                        int byteOffset = y * stride + x * bytesPerPixel;

                        for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                        {
                            for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                            {
                                int calcOffset = byteOffset + (filterX * bytesPerPixel) + (filterY * stride);

                                if (calcOffset >= 0 && calcOffset < srcBuffer.Length) // Проверка на выход за пределы массива
                                {
                                    blue += srcBuffer[calcOffset] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                                    green += srcBuffer[calcOffset + 1] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                                    red += srcBuffer[calcOffset + 2] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                                }
                            }
                        }

                        destBuffer[byteOffset] = ClampToByte(blue);
                        destBuffer[byteOffset + 1] = ClampToByte(green);
                        destBuffer[byteOffset + 2] = ClampToByte(red);
                    }
                }
            });

            Marshal.Copy(destBuffer, 0, destData.Scan0, destBuffer.Length);
            originalImage.UnlockBits(srcData);
            processedImage.UnlockBits(destData);

            return processedImage;
        }


        public byte ClampToByte(int value)
        {
            return (byte)(value < 0 ? 0 : (value > 255 ? 255 : value));
        }
    }

}
