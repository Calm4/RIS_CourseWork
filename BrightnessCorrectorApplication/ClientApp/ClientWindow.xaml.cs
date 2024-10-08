using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace DistributedImageProcessingClient
{
    public partial class ClientWindow : Window
    {
        private TcpClient _client;
        private bool _isConnected = false;

        public ClientWindow()
        {
            InitializeComponent();
        }

        // Обработчик кнопки "Connect to Server"
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConnected)
            {
                string ipAddress = IpAddressTextBox.Text;
                if (!int.TryParse(PortTextBox.Text, out int port))
                {
                    MessageBox.Show("Invalid port number.");
                    return;
                }

                try
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(ipAddress, port);
                    _isConnected = true;
                    StatusLabel.Text = "Connected to server.";

                    // Запуск задачи для получения данных от сервера
                    ReceiveDataFromServer();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error connecting to server: {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                MessageBox.Show("Already connected to server.");
            }
        }

        // Обработчик кнопки "Default IP"
        private void DefaultIpButton_Click(object sender, RoutedEventArgs e)
        {
            IpAddressTextBox.Text = "127.0.0.1";
            PortTextBox.Text = "5000";
        }

        // Получение данных от сервера и обработка изображения
        private async void ReceiveDataFromServer()
        {
            try
            {
                NetworkStream stream = _client.GetStream();
                while (true)
                {
                    if (stream.DataAvailable)
                    {
                        // Получение длины данных
                        byte[] lengthBuffer = new byte[4];
                        int bytesRead = await stream.ReadAsync(lengthBuffer, 0, 4);
                        if (bytesRead != 4)
                        {
                            throw new Exception("Failed to read the data length.");
                        }

                        int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

                        // Получение данных
                        byte[] dataBuffer = new byte[dataLength];
                        int totalBytesRead = 0;
                        while (totalBytesRead < dataLength)
                        {
                            int read = await stream.ReadAsync(dataBuffer, totalBytesRead, dataLength - totalBytesRead);
                            if (read == 0)
                            {
                                throw new Exception("Connection was closed prematurely.");
                            }
                            totalBytesRead += read;
                        }

                        // Обработка изображения
                        byte[] processedData = ProcessImage(dataBuffer);

                        // Отправка обработанных данных обратно на сервер
                        await SendDataToServer(processedData);

                        StatusLabel.Text = "Processing completed.";
                        _client.Close();
                        _isConnected = false;
                        break;
                    }

                    await Task.Delay(100); // Ожидание перед повторной проверкой
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Error receiving data: {ex.Message}\n{ex.StackTrace}"));
            }
        }

        // Обработка изображения (изменение яркости)
        private byte[] ProcessImage(byte[] imageData)
        {
            using (MemoryStream ms = new MemoryStream(imageData))
            {
                Bitmap bitmap = new Bitmap(ms);

                // Изменение яркости
                float brightness = 1.2f; // Коэффициент яркости
                System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(
                    new float[][]
                    {
                        new float[] { brightness, 0, 0, 0, 0 },
                        new float[] { 0, brightness, 0, 0, 0 },
                        new float[] { 0, 0, brightness, 0, 0 },
                        new float[] { 0, 0, 0, 1, 0 },
                        new float[] { 0, 0, 0, 0, 1 }
                    });

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    System.Drawing.Imaging.ImageAttributes attributes = new System.Drawing.Imaging.ImageAttributes();
                    attributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);
                }

                // Сохранение обработанного изображения в байты
                using (MemoryStream msResult = new MemoryStream())
                {
                    bitmap.Save(msResult, System.Drawing.Imaging.ImageFormat.Png);
                    return msResult.ToArray();
                }
            }
        }

        // Отправка данных обратно на сервер
        private async Task SendDataToServer(byte[] data)
        {
            try
            {
                NetworkStream stream = _client.GetStream();

                // Отправка длины данных
                byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
                await stream.WriteAsync(lengthBuffer, 0, 4);
                await stream.FlushAsync();

                // Отправка самих данных
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Error sending data: {ex.Message}\n{ex.StackTrace}"));
            }
        }
    }
}
