using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TestClientApp
{
    internal class Program
    {
        private const string ServerIp = "127.0.0.1";
        private const int ServerPort = 8888;
        private const int TestIterations = 10000;
        private const string ImageFolderPath = "Images/"; // Папка с изображениями

        static async Task Main(string[] args)
        {
            Console.WriteLine("Начало тестирования...");

            long totalProcessingTime = 0;
            int successCount = 0;
            int errorCount = 0;

            // Загружаем список файлов изображений из папки
            string[] imageFiles = Directory.GetFiles(ImageFolderPath, "*.jpg");
            if (imageFiles.Length == 0)
            {
                Console.WriteLine("Нет изображений в папке 'Images'. Завершение работы.");
                return;
            }

            Random random = new Random();

            for (int i = 0; i < TestIterations; i++)
            {
                try
                {
                    using TcpClient client = new TcpClient(ServerIp, ServerPort);
                    using NetworkStream stream = client.GetStream();

                    // Выбираем режим обработки: 0 - линейный, 1 - многопоточный
                    byte processingMode = (byte)(i % 2);
                    stream.WriteByte(processingMode);

                    // Выбираем случайное изображение из папки
                    string imagePath = imageFiles[random.Next(imageFiles.Length)];
                    Bitmap testImage = new Bitmap(imagePath);

                    // Отправляем изображение и логируем его имя
                    SendImage(stream, testImage);
                    Console.WriteLine($"Отправлено изображение: {Path.GetFileName(imagePath)}");

                    DateTime startTime = DateTime.Now;
                    Bitmap processedImage = ReceiveImage(stream);
                    DateTime endTime = DateTime.Now;

                    long elapsedMilliseconds = (long)(endTime - startTime).TotalMilliseconds;
                    totalProcessingTime += elapsedMilliseconds;
                    successCount++;

                    Console.WriteLine($"Тест #{i+1}, picture: {Path.GetFileName(imagePath)}. Успешно, время обработки: {elapsedMilliseconds} мс (режим: {(processingMode == 0 ? "Линейный" : "Многопоточный")})");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"Ошибка при обработке изображения: {ex.Message}");
                }
            }

            Console.WriteLine("Тестирование завершено.");
            Console.WriteLine($"Успешных обработок: {successCount}, Ошибок: {errorCount}");
            Console.WriteLine($"Среднее время обработки: {totalProcessingTime / (double)successCount:F2} мс");
        }

        private static void SendImage(NetworkStream stream, Bitmap image)
        {
            using MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Jpeg);
            byte[] imageBytes = ms.ToArray();

            byte[] sizeBuffer = BitConverter.GetBytes(imageBytes.Length);
            stream.Write(sizeBuffer, 0, sizeBuffer.Length);
            stream.Write(imageBytes, 0, imageBytes.Length);
        }

        private static Bitmap ReceiveImage(NetworkStream stream)
        {
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
                    throw new Exception("Соединение закрыто сервером.");
                }
                totalReceived += bytesRead;
            }

            using MemoryStream ms = new MemoryStream(imageBytes);
            return new Bitmap(ms);
        }
    }
}
