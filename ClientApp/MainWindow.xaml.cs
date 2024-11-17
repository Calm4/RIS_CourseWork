using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageProcessingClient
{
    public partial class MainWindow : Window
    {
        private Socket _clientSocket;
        private byte[] _imageBytes; 
        private bool isMultithreaded = false;

        public MainWindow()
        {
            InitializeComponent();
            btnLinearMode.IsEnabled = false;
            btnMultithreadedMode.IsEnabled = false;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clientSocket == null || !_clientSocket.Connected)
            {
                string serverIp = ServerIpTextBox.Text;
                int port = 8888;

                try
                {
                    // Настраиваем сокет клиента
                    _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _clientSocket.Connect(new IPEndPoint(IPAddress.Parse(serverIp), port));

                    StatusLabel.Text = "Подключено к серверу.";
                    ConnectButton.Content = "Отключиться";
                    ConnectButton.Background = new SolidColorBrush(Color.FromRgb(255, 69, 0)); 
                    UploadButton.IsEnabled = true;
                    btnLinearMode.IsEnabled = true; 
                    btnMultithreadedMode.IsEnabled = true; 
                }
                catch (Exception ex)
                {
                    StatusLabel.Text = $"Не удалось подключиться: {ex.Message}";
                }
            }
            else
            {
                // Отключение от сервера
                _clientSocket.Close();
                _clientSocket = null;
                StatusLabel.Text = "Отключено от сервера.";
                ConnectButton.Content = "Подключиться";
                ConnectButton.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                UploadButton.IsEnabled = false;
                btnLinearMode.IsEnabled = false; 
                btnMultithreadedMode.IsEnabled = false;
            }
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg)|*.png;*.jpeg";

            if (openFileDialog.ShowDialog() == true)
            {
                string imagePath = openFileDialog.FileName;
                _imageBytes = System.IO.File.ReadAllBytes(imagePath); 

                // Отображаем выбранное изображение
                OriginalImage.Source = new BitmapImage(new Uri(imagePath));

                StatusLabel.Text = "Изображение загружено. Выберите режим обработки.";
            }
        }

        private void btnLinearMode_Click(object sender, RoutedEventArgs e)
        {
            isMultithreaded = false;
            StatusLabel.Text = "Выбран режим: Линейный";
            ProcessImage(); 
        }

        private void btnMultithreadedMode_Click(object sender, RoutedEventArgs e)
        {
            isMultithreaded = true;
            StatusLabel.Text = "Выбран режим: Многопоточный";
            ProcessImage(); 
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
                    Console.WriteLine($"Отправлен режим обработки: {processingMode}");

                    // Отправляем размер изображения
                    byte[] imageSize = BitConverter.GetBytes(_imageBytes.Length);
                    _clientSocket.Send(imageSize);
                    Console.WriteLine($"Отправлен размер изображения: {_imageBytes.Length}");

                    // Отправляем само изображение
                    _clientSocket.Send(_imageBytes);
                    Console.WriteLine("Отправлены байты изображения на сервер.");

                    StatusLabel.Text = "Изображение отправлено на сервер для обработки.";
              
                    byte[] processedImageBytes = ReceiveProcessedImage();

                    DisplayProcessedImage(processedImageBytes);

                    StatusLabel.Text = "Изображение обработано и отображено.";
                }
                else
                {
                    StatusLabel.Text = "Не подключено к серверу или изображение не загружено.";
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Не удалось отправить изображение: {ex.Message}";
                Console.WriteLine($"Ошибка при обработке: {ex.Message}");
            }
        }

        private byte[] ReceiveProcessedImage()
        {
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
                bitmap.Freeze(); 

                ProcessedImage.Source = bitmap;
            }
        }
    }
}
