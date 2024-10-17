using Microsoft.Win32;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageProcessingClient
{
    public partial class MainWindow : Window
    {
        private Socket _clientSocket;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string serverIp = ServerIpTextBox.Text;
            int port = 8888;

            try
            {
                // Настраиваем сокет клиента
                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _clientSocket.Connect(new IPEndPoint(IPAddress.Parse(serverIp), port));

                StatusLabel.Text = "Connected to server.";
                UploadButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Failed to connect: {ex.Message}";
            }
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg)|*.png;*.jpeg";

            if (openFileDialog.ShowDialog() == true)
            {
                string imagePath = openFileDialog.FileName;
                byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);

                // Отображаем выбранное изображение
                OriginalImage.Source = new BitmapImage(new Uri(imagePath));

                // Получаем яркость из поля ввода
                int brightness = 50; // значение по умолчанию
                if (int.TryParse(BrightnessTextBox.Text, out brightness))
                {
                    // Отправляем изображение с яркостью на сервер
                    SendImageToServer(imageBytes, brightness);
                }
                else
                {
                    StatusLabel.Text = "Invalid brightness value.";
                }
            }
        }

        private void SendImageToServer(byte[] imageBytes, int brightness)
        {
            try
            {
                if (_clientSocket != null && _clientSocket.Connected)
                {
                    // Отправляем длину данных
                    byte[] dataLength = BitConverter.GetBytes(imageBytes.Length);
                    _clientSocket.Send(dataLength);

                    // Отправляем само изображение
                    _clientSocket.Send(imageBytes);

                    // Отправляем значение яркости
                    byte[] brightnessBytes = BitConverter.GetBytes(brightness);
                    _clientSocket.Send(brightnessBytes);

                    StatusLabel.Text = "Image sent to server for processing.";

                    // Получаем обработанное изображение от сервера
                    byte[] sizeBuffer = new byte[4];
                    _clientSocket.Receive(sizeBuffer);
                    int processedImageSize = BitConverter.ToInt32(sizeBuffer, 0);

                    byte[] processedImageBytes = new byte[processedImageSize];
                    int totalReceived = 0;
                    while (totalReceived < processedImageSize)
                    {
                        int bytesReceived = _clientSocket.Receive(processedImageBytes, totalReceived, processedImageSize - totalReceived, SocketFlags.None);
                        totalReceived += bytesReceived;
                    }

                    // Отображаем обработанное изображение
                    using (var ms = new System.IO.MemoryStream(processedImageBytes))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        ProcessedImage.Source = bitmap;
                    }

                    StatusLabel.Text = "Image processed and displayed.";
                }
                else
                {
                    StatusLabel.Text = "Not connected to the server.";
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Failed to send image: {ex.Message}";
            }
        }


    }
}
