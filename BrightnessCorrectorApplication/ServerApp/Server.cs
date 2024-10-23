using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace ServerApp
{
    class ImageProcessingServer
    {
        private const int port = 8888;

        static void Main(string[] args)
        {
            StartServer();
        }

        public static void StartServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            listener.Start();
            Console.WriteLine($"Server started at 127.0.0.1:{port}, waiting for connections...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected.");
                Task.Run(() => HandleClient(client));
            }
        }

        public static void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                int processingMode = stream.ReadByte(); // 0 - линейная, 1 - многопоточная

                byte[] sizeBuffer = new byte[4];
                stream.Read(sizeBuffer, 0, 4);
                int imageSize = BitConverter.ToInt32(sizeBuffer, 0);

                byte[] imageBytes = new byte[imageSize];
                int totalReceived = 0;
                while (totalReceived < imageSize)
                {
                    int bytesRead = stream.Read(imageBytes, totalReceived, imageSize - totalReceived);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Connection closed by client before receiving full image.");
                        return;
                    }
                    totalReceived += bytesRead;
                }

                MemoryStream ms = new MemoryStream(imageBytes);
                Bitmap image = new Bitmap(ms);

                Console.WriteLine($"Processing client with Thread ID: {Task.CurrentId}");

                if (processingMode == 0)
                {
                    var stopwatch = Stopwatch.StartNew();
                    image = ApplyHighPassFilter(image);
                    stopwatch.Stop();
                    Console.WriteLine($"Linear image processing completed in {stopwatch.Elapsed.TotalSeconds} seconds");
                }
                else
                {
                    int numberOfThreads = 4; // Число потоков
                    var stopwatch = Stopwatch.StartNew();
                    image = ProcessImageMultithreaded(image, numberOfThreads);
                    stopwatch.Stop();
                    Console.WriteLine($"Multithreaded image processing completed in {stopwatch.Elapsed.TotalSeconds} seconds");
                }

                MemoryStream outputMs = new MemoryStream();
                image.Save(outputMs, ImageFormat.Png);
                byte[] responseImage = outputMs.ToArray();

                byte[] responseSize = BitConverter.GetBytes(responseImage.Length);
                stream.Write(responseSize, 0, responseSize.Length);

                stream.Write(responseImage, 0, responseImage.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        public static Bitmap ApplyHighPassFilter(Bitmap image)
        {
            int[,] filterMatrix = {
                { -1, -1, -1 },
                { -1,  8, -1 },
                { -1, -1, -1 }
            };

            Bitmap filteredImage = new Bitmap(image.Width, image.Height);
            BitmapData srcData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData destData = filteredImage.LockBits(new Rectangle(0, 0, filteredImage.Width, filteredImage.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            int bytesPerPixel = 3;
            int stride = srcData.Stride;
            IntPtr srcScan0 = srcData.Scan0;
            IntPtr destScan0 = destData.Scan0;

            byte[] srcBuffer = new byte[Math.Abs(stride) * image.Height];
            byte[] destBuffer = new byte[Math.Abs(stride) * filteredImage.Height];

            Marshal.Copy(srcScan0, srcBuffer, 0, srcBuffer.Length);
            Marshal.Copy(destScan0, destBuffer, 0, destBuffer.Length);

            int filterOffset = 1;
            for (int y = filterOffset; y < image.Height - filterOffset; y++)
            {
                for (int x = filterOffset; x < image.Width - filterOffset; x++)
                {
                    int blue = 0, green = 0, red = 0;
                    int byteOffset = y * stride + x * bytesPerPixel;

                    for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                        {
                            int calcOffset = byteOffset + (filterX * bytesPerPixel) + (filterY * stride);

                            blue += srcBuffer[calcOffset] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                            green += srcBuffer[calcOffset + 1] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                            red += srcBuffer[calcOffset + 2] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                        }
                    }

                    destBuffer[byteOffset] = ClampToByte(blue);
                    destBuffer[byteOffset + 1] = ClampToByte(green);
                    destBuffer[byteOffset + 2] = ClampToByte(red);
                }
            }

            Marshal.Copy(destBuffer, 0, destScan0, destBuffer.Length);
            image.UnlockBits(srcData);
            filteredImage.UnlockBits(destData);

            return filteredImage;
        }

        private static byte ClampToByte(int value)
        {
            return (byte)(value < 0 ? 0 : (value > 255 ? 255 : value));
        }

        public static Bitmap ProcessImageMultithreaded(Bitmap originalImage, int numberOfThreads)
        {
            Bitmap processedImage = new Bitmap(originalImage.Width, originalImage.Height);

            int imageHeight = originalImage.Height;
            int imageWidth = originalImage.Width;
            int rowsPerThread = imageHeight / numberOfThreads;

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
                int endY = (i == numberOfThreads - 1) ? imageHeight : startY + rowsPerThread;

                int bytesPerPixel = 3;
                int stride = srcData.Stride;

                for (int y = startY; y < endY; y++)
                {
                    for (int x = 0; x < imageWidth; x++)
                    {
                        if (y > 0 && y < imageHeight - 1 && x > 0 && x < imageWidth - 1)
                        {
                            int blue = 0, green = 0, red = 0;
                            int byteOffset = y * stride + x * bytesPerPixel;

                            for (int filterY = -1; filterY <= 1; filterY++)
                            {
                                for (int filterX = -1; filterX <= 1; filterX++)
                                {
                                    int calcY = y + filterY;
                                    int calcX = x + filterX;

                                    int calcOffset = calcY * stride + calcX * bytesPerPixel;

                                    blue += srcBuffer[calcOffset] * filterMatrix[filterY + 1, filterX + 1];
                                    green += srcBuffer[calcOffset + 1] * filterMatrix[filterY + 1, filterX + 1];
                                    red += srcBuffer[calcOffset + 2] * filterMatrix[filterY + 1, filterX + 1];
                                }
                            }

                            destBuffer[byteOffset] = ClampToByte(blue);
                            destBuffer[byteOffset + 1] = ClampToByte(green);
                            destBuffer[byteOffset + 2] = ClampToByte(red);
                        }
                    }
                }
            });

            Marshal.Copy(destBuffer, 0, destData.Scan0, destBuffer.Length);
            originalImage.UnlockBits(srcData);
            processedImage.UnlockBits(destData);

            return processedImage;
        }



    }
}
