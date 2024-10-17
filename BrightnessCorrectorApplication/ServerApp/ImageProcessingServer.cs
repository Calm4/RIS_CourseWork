using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerApp
{
    class ImageProcessingServer
    {
        private Socket _serverSocket;
        private int _maxThreads = 4; // Максимум 4 параллельных потока для обработки
        private int _activeThreads = 0; // Счётчик активных потоков
        private Queue<Socket> _waitingQueue = new Queue<Socket>(); // Очередь клиентов

        public void StartServer(string ipAddress, int port)
        {
            // Настраиваем серверный сокет
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ipAddress), port));
            _serverSocket.Listen(10); // Ожидание клиентов

            Console.WriteLine($"Server started at {ipAddress}:{port}, waiting for connections...");

            // Принимаем клиентов асинхронно
            while (true)
            {
                Socket clientSocket = _serverSocket.Accept();
                Console.WriteLine("Client connected.");

                // Проверяем, можем ли обработать сразу или нужно ставить в очередь
                lock (this)
                {
                    if (_activeThreads >= _maxThreads)
                    {
                        Console.WriteLine("All threads are busy. Client added to queue.");
                        _waitingQueue.Enqueue(clientSocket); // Клиент в очереди
                    }
                    else
                    {
                        // Обрабатываем клиента сразу
                        _activeThreads++;
                        Task.Run(() => ProcessClient(clientSocket));
                    }
                }
            }
        }

        private void ProcessClient(Socket clientSocket)
        {
            try
            {
                Console.WriteLine("Processing client...");
                while (clientSocket.Connected)  // Ожидаем новые запросы, пока клиент подключён
                {
                    // Получаем изображение и обрабатываем его
                    byte[] processedImage = HandleClientImage(clientSocket);

                    if (processedImage != null)
                    {
                        // Отправляем обратно обработанное изображение
                        byte[] dataLength = BitConverter.GetBytes(processedImage.Length);
                        clientSocket.Send(dataLength);
                        clientSocket.Send(processedImage);

                        Console.WriteLine("Processed image sent back to client.");
                    }
                    else
                    {
                        // Клиент завершил соединение
                        Console.WriteLine("Client disconnected.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while processing client: {ex.Message}");
            }
            finally
            {
                lock (this)
                {
                    _activeThreads--;
                    if (_waitingQueue.Count > 0)
                    {
                        // Есть клиенты в очереди, забираем следующего
                        Socket nextClient = _waitingQueue.Dequeue();
                        Console.WriteLine("Processing next client from queue.");
                        _activeThreads++;
                        Task.Run(() => ProcessClient(nextClient));
                    }
                }
            }
        }



        private byte[] HandleClientImage(Socket clientSocket)
        {
            // Получение изображения
            byte[] sizeBuffer = new byte[4];
            clientSocket.Receive(sizeBuffer, 0, sizeBuffer.Length, SocketFlags.None);
            int imageSize = BitConverter.ToInt32(sizeBuffer, 0);

            byte[] imageData = new byte[imageSize];
            int totalReceived = 0;
            while (totalReceived < imageSize)
            {
                int bytesReceived = clientSocket.Receive(imageData, totalReceived, imageSize - totalReceived, SocketFlags.None);
                if (bytesReceived == 0) break;
                totalReceived += bytesReceived;
            }

            // Получаем яркость из клиента
            byte[] brightnessBuffer = new byte[4];
            clientSocket.Receive(brightnessBuffer);
            int brightnessValue = BitConverter.ToInt32(brightnessBuffer, 0);

            // Загружаем изображение из массива байтов
            using (var ms = new MemoryStream(imageData))
            {
                Bitmap originalImage = new Bitmap(ms);

                // Изменяем яркость изображения
                Bitmap brightImage = ChangeBrightness(originalImage, brightnessValue); // Используем переданное значение яркости

                // Применяем высокочастотный фильтр
                Bitmap processedImage = ApplyHighPassFilter(brightImage);

                // Преобразуем обработанное изображение обратно в байты
                using (var processedStream = new MemoryStream())
                {
                    processedImage.Save(processedStream, ImageFormat.Png);
                    return processedStream.ToArray();
                }
            }
        }

        private Bitmap ChangeBrightness(Bitmap image, int brightnessValue)
        {
            Bitmap adjustedImage = new Bitmap(image.Width, image.Height);
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color originalColor = image.GetPixel(x, y);
                    int r = Clamp(originalColor.R + brightnessValue, 0, 255);
                    int g = Clamp(originalColor.G + brightnessValue, 0, 255);
                    int b = Clamp(originalColor.B + brightnessValue, 0, 255);
                    adjustedImage.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return adjustedImage;
        }
        private Bitmap ApplyHighPassFilter(Bitmap image)
        {
            Bitmap filteredImage = new Bitmap(image.Width, image.Height);
            int[,] laplacianKernel = {
        { -1, -1, -1 },
        { -1,  8, -1 },
        { -1, -1, -1 }
    };

            for (int y = 1; y < image.Height - 1; y++)
            {
                for (int x = 1; x < image.Width - 1; x++)
                {
                    int r = 0, g = 0, b = 0;
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            Color pixel = image.GetPixel(x + kx, y + ky);
                            r += pixel.R * laplacianKernel[ky + 1, kx + 1];
                            g += pixel.G * laplacianKernel[ky + 1, kx + 1];
                            b += pixel.B * laplacianKernel[ky + 1, kx + 1];
                        }
                    }
                    filteredImage.SetPixel(x, y, Color.FromArgb(Clamp(r, 0, 255), Clamp(g, 0, 255), Clamp(b, 0, 255)));
                }
            }
            return filteredImage;
        }

        // Вспомогательная функция для ограничения значений в диапазоне
        private int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        static void Main(string[] args)
        {
            ImageProcessingServer server = new ImageProcessingServer();
            server.StartServer("127.0.0.1", 8888);
        }
    }
}
