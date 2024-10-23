using Microsoft.Win32;
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageProcessingClient
{
    public partial class MainWindow : Window
    {
        private Socket _clientSocket;
        private byte[] _imageBytes; // Данные изображения хранятся здесь после загрузки
        private bool isMultithreaded = false; // По умолчанию линейный режим

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
                _imageBytes = System.IO.File.ReadAllBytes(imagePath); // Загружаем изображение в байтовый массив

                // Отображаем выбранное изображение
                OriginalImage.Source = new BitmapImage(new Uri(imagePath));

                StatusLabel.Text = "Image loaded. Select processing mode.";
            }
        }

        private void btnLinearMode_Click(object sender, RoutedEventArgs e)
        {
            isMultithreaded = false;
            StatusLabel.Text = "Selected mode: Linear";
            ProcessImage(); // Запуск обработки в линейном режиме
        }

        private void btnMultithreadedMode_Click(object sender, RoutedEventArgs e)
        {
            isMultithreaded = true;
            StatusLabel.Text = "Selected mode: Multithreaded";
            ProcessImage(); // Запуск обработки в многопоточном режиме
        }

        private void ProcessImage()
        {
            try
            {
                if (_clientSocket != null && _clientSocket.Connected && _imageBytes != null)
                {
                    // Отправляем режим обработки (0 - линейный, 1 - многопоточный)
                    byte processingMode = isMultithreaded ? (byte)1 : (byte)0;
                    _clientSocket.Send(new[] { processingMode });
                    Console.WriteLine($"Sent processing mode: {processingMode}");

                    // Отправляем размер изображения
                    byte[] imageSize = BitConverter.GetBytes(_imageBytes.Length);
                    _clientSocket.Send(imageSize);
                    Console.WriteLine($"Sent image size: {_imageBytes.Length}");

                    // Отправляем само изображение
                    _clientSocket.Send(_imageBytes);
                    Console.WriteLine("Sent image bytes to server.");

                    StatusLabel.Text = "Image sent to server for processing.";

                    // Получаем обработанное изображение от сервера
                    byte[] processedImageBytes = ReceiveProcessedImage();

                    // Отображаем обработанное изображение
                    DisplayProcessedImage(processedImageBytes);

                    StatusLabel.Text = "Image processed and displayed.";
                }
                else
                {
                    StatusLabel.Text = "Not connected to the server or image not loaded.";
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Failed to send image: {ex.Message}";
                Console.WriteLine($"Error during processing: {ex.Message}");
            }
        }

        private byte[] ReceiveProcessedImage()
        {
            // Получаем размер обработанного изображения
            byte[] sizeBuffer = new byte[4];
            _clientSocket.Receive(sizeBuffer);
            int processedImageSize = BitConverter.ToInt32(sizeBuffer, 0);

            // Получаем изображение
            byte[] processedImageBytes = new byte[processedImageSize];
            int totalReceived = 0;
            while (totalReceived < processedImageSize)
            {
                int bytesReceived = _clientSocket.Receive(processedImageBytes, totalReceived, processedImageSize - totalReceived, SocketFlags.None);
                totalReceived += bytesReceived;
            }

            return processedImageBytes;
        }

        private void DisplayProcessedImage(byte[] processedImageBytes)
        {
            using (var ms = new System.IO.MemoryStream(processedImageBytes))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Зафиксировать, чтобы избежать блокировок

                ProcessedImage.Source = bitmap; // Отобразить обработанное изображение
            }
        }
    }
}
