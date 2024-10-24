using ServerApp;
using System.Drawing.Imaging;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

public class ImageProcessingServer
{
    private const int Port = 8888;
    private static int clientCounter = 0; // Счетчик клиентов

    public void StartServer()
    {
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
        listener.Start();
        Console.WriteLine($"Сервер запущен на 127.0.0.1:{Port}, ожидает подключения...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            int clientId = ++clientCounter; // Присваиваем клиенту уникальный ID
            Console.WriteLine($"Клиент #{clientId} подключен.");
            Task.Run(() => HandleClient(client, clientId));
        }
    }

    private void HandleClient(TcpClient client, int clientId)
    {
        try
        {
            NetworkStream stream = client.GetStream();

            while (true)
            {
                IImageProcessor imageProcessor = CreateImageProcessor(stream);
                Bitmap image = ReceiveImage(stream);

                // Лог: загрузка изображения
                Console.WriteLine($"Изображение получено от клиента #{clientId}.");

                Stopwatch stopwatch = Stopwatch.StartNew(); // Запуск таймера
                Bitmap processedImage = imageProcessor.ProcessImage(image);
                stopwatch.Stop();

                // Лог: время выполнения задачи
                DisplayProcessingTime(stopwatch.ElapsedMilliseconds, clientId);

                SendImage(stream, processedImage);

                // Лог: изображение отправлено
                Console.WriteLine($"Обработанное изображение отправлено клиенту #{clientId}.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка у клиента #{clientId}: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    private void DisplayProcessingTime(long elapsedMilliseconds, int clientId)
    {
        Console.ForegroundColor = ConsoleColor.Green; // Устанавливаем цвет текста
        if (elapsedMilliseconds > 1000)
        {
            Console.WriteLine($"Время обработки для клиента #{clientId}: {elapsedMilliseconds / 1000.0:F2} секунд");
        }
        else
        {
            Console.WriteLine($"Время обработки для клиента #{clientId}: {elapsedMilliseconds} мс");
        }
        Console.ResetColor(); // Сбрасываем цвет текста
    }

    private IImageProcessor CreateImageProcessor(NetworkStream stream)
    {
        int processingMode = stream.ReadByte(); // 0 - линейная, 1 - многопоточная
        if (processingMode == 0)
        {
            Console.WriteLine("Выбран режим обработки: Линейный");
            return new LinearImageProcessor();
        }
        else
        {
            int numberOfThreads = 4; // Задайте количество потоков
            Console.WriteLine($"Выбран режим обработки: Многопоточный с {numberOfThreads} потоками");
            return new MultithreadedImageProcessor(numberOfThreads);
        }
    }

    private Bitmap ReceiveImage(NetworkStream stream)
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
                throw new Exception("Соединение закрыто клиентом до получения полного изображения.");
            }
            totalReceived += bytesRead;
        }

        using (MemoryStream ms = new MemoryStream(imageBytes))
        {
            return new Bitmap(ms);
        }
    }

    private void SendImage(NetworkStream stream, Bitmap image)
    {
        using (MemoryStream outputMs = new MemoryStream())
        {
            image.Save(outputMs, ImageFormat.Png);
            byte[] responseImage = outputMs.ToArray();

            byte[] responseSize = BitConverter.GetBytes(responseImage.Length);
            stream.Write(responseSize, 0, responseSize.Length);
            stream.Write(responseImage, 0, responseImage.Length);
            stream.Flush();
        }
    }
}
