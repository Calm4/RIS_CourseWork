using ServerApp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TestClientApp
{
    public class ImageProcessingLoadTest
    {
        private readonly string _imagesDirectory = "Images/"; // Путь к изображениям
        private readonly string[] _imageFiles;
        private readonly Random _random = new Random();
        private const string ServerAddress = "127.0.0.1"; // Адрес сервера
        private const int ServerPort = 8888; // Порт сервера

        public ImageProcessingLoadTest()
        {
            _imageFiles = Directory.GetFiles(_imagesDirectory, "*.png");
            if (_imageFiles.Length != 3)
            {
                Console.WriteLine("В папке должно быть ровно 5 изображений формата .png для теста!");
                Environment.Exit(1);
            }
        }

        public async Task StartProcessing(int numberOfTests)
        {
            string processedImagesDirectory = "ProcessedImages";
            if (!Directory.Exists(processedImagesDirectory))
            {
                Directory.CreateDirectory(processedImagesDirectory); // Создаём папку, если её нет
            }

            for (int i = 0; i < numberOfTests; i++)
            {
                string imageFile = _imageFiles[i % _imageFiles.Length];
                try
                {
                    Bitmap image = new Bitmap(imageFile);
                    string imageSize = $"{image.Width}x{image.Height}";

                    // Выбор режима обработки
                    string processingMode = (_random.Next(2) == 0) ? "Линейный" : "Многопоточный";

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Тест #{i + 1}:");
                    Console.WriteLine($"  Изображение: {Path.GetFileName(imageFile)}");
                    Console.WriteLine($"  Размер: {imageSize}");
                    Console.WriteLine($"  Режим обработки: {processingMode}");

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    // Подключаемся к серверу и отправляем изображение
                    Bitmap processedImage = await ProcessImageOnServer(image, processingMode);

                    stopwatch.Stop();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  Время обработки: {stopwatch.ElapsedMilliseconds} мс");
                    Console.ResetColor();

                    // Сохранение обработанного изображения
                    string processedImagePath = Path.Combine(processedImagesDirectory, $"Processed_{Path.GetFileName(imageFile)}_{i + 1}.png");
                    processedImage.Save(processedImagePath, System.Drawing.Imaging.ImageFormat.Png);

                    // Подтверждение сохранения
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  Обработанное изображение сохранено в: {processedImagePath}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Ошибка при обработке {imageFile}: {ex.Message}");
                    Console.ResetColor();
                }

                Console.WriteLine(new string('-', 50));
            }
        }

        private async Task<Bitmap> ProcessImageOnServer(Bitmap image, string processingMode)
        {
            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync(ServerAddress, ServerPort);
                NetworkStream stream = client.GetStream();

                // Отправка режима обработки
                byte modeByte = processingMode == "Линейный" ? (byte)0 : (byte)1;
                stream.WriteByte(modeByte);

                // Преобразование изображения в байты и отправка
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] imageBytes = ms.ToArray();

                    Console.WriteLine($"Sending image with size: {imageBytes.Length} bytes");

                    byte[] sizeBytes = BitConverter.GetBytes(imageBytes.Length);
                    await stream.WriteAsync(sizeBytes, 0, sizeBytes.Length); // Отправка размера
                    await stream.WriteAsync(imageBytes, 0, imageBytes.Length); // Отправка изображения
                }

                // Получение обработанного изображения
                byte[] sizeBuffer = new byte[4];
                await stream.ReadAsync(sizeBuffer, 0, 4);
                int imageSize = BitConverter.ToInt32(sizeBuffer, 0);
                Console.WriteLine($"Expected processed image size: {imageSize} bytes");

                byte[] imageBuffer = new byte[imageSize];
                int totalReceived = 0;
                while (totalReceived < imageSize)
                {
                    int bytesRead = await stream.ReadAsync(imageBuffer, totalReceived, imageSize - totalReceived);
                    if (bytesRead == 0)
                    {
                        throw new Exception("Ошибка при получении изображения.");
                    }
                    totalReceived += bytesRead;
                }

                using (MemoryStream ms = new MemoryStream(imageBuffer))
                {
                    return new Bitmap(ms);
                }
            }
        }
    }
}
