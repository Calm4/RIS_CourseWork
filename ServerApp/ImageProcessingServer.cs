using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace ServerApp
{
    public class ImageProcessingServer
    {
        private const int Port = 8888;
        private static int clientCounter = 0;

        public void StartServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();

            // Определяем текущий локальный IP-адрес
            string localIP = GetLocalIPAddress();
            Console.WriteLine($"Сервер запущен на {localIP}:{Port}, ожидает подключения...");


            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                int clientId = ++clientCounter;
                Console.WriteLine("===============================================");
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

                    Console.WriteLine($"Изображение получено от клиента #{clientId}.");

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    Bitmap processedImage = imageProcessor.ProcessImage(image);
                    stopwatch.Stop();

                    DisplayProcessingTime(stopwatch.ElapsedMilliseconds, clientId);

                    // Проверяем, что изображение не пустое
                    if (processedImage == null)
                    {
                        Console.WriteLine($"Ошибка: обработанное изображение пусто для клиента #{clientId}");
                        break;
                    }

                    try
                    {
                        SendImage(stream, processedImage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при отправке изображения клиенту #{clientId}: {ex.Message}");
                        break;
                    }

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
            Console.ForegroundColor = ConsoleColor.Green;
            if (elapsedMilliseconds > 1000)
            {
                Console.WriteLine($"Время обработки для клиента #{clientId}: {elapsedMilliseconds / 1000.0:F2} секунд");
            }
            else
            {
                Console.WriteLine($"Время обработки для клиента #{clientId}: {elapsedMilliseconds} мс");
            }
            Console.ResetColor();
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

            if (imageSize == 0)
            {
                throw new Exception("Получен нулевой размер изображения.");
            }

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

            Console.WriteLine($"Received {imageBytes.Length} bytes from client.");

            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                return new Bitmap(ms);
            }
        }

        private void SendImage(NetworkStream stream, Bitmap processedImage)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                processedImage.Save(ms, ImageFormat.Png);
                byte[] processedImageBytes = ms.ToArray();
                byte[] sizeBytes = BitConverter.GetBytes(processedImageBytes.Length);

                stream.Write(sizeBytes, 0, sizeBytes.Length);
                stream.Write(processedImageBytes, 0, processedImageBytes.Length);
            }
        }

        private string GetLocalIPAddress()
        {
            string localIP = "Не удалось определить IP-адрес";
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении локального IP: {ex.Message}");
            }
            return localIP;
        }

    }
}
