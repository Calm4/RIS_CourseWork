using Microsoft.Win32;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Drawing; // System.Drawing.Common должен быть подключен через NuGet
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DistributedImageProcessingServer
{
    public partial class ServerWindow : Window
    {
        private List<ClientInfo> _clients = new List<ClientInfo>();
        private TcpListener _server;
        private int _expectedClients = 4;
        private int _connectedClients = 0;
        private BitmapImage _originalImage;

        public ServerWindow()
        {
            InitializeComponent();
            StartServer();
        }

        private void StartServer()
        {
            _server = new TcpListener(IPAddress.Any, 5000);
            _server.Start();
            StatusLabel.Text = "Server started. Waiting for clients...";

            Thread listenerThread = new Thread(ListenForClients);
            listenerThread.Start();
        }

        private void ListenForClients()
        {
            while (true)
            {
                TcpClient client = _server.AcceptTcpClient();
                Dispatcher.Invoke(() => AddClient(client));
            }
        }

        private void AddClient(TcpClient client)
        {
            _connectedClients++;
            var clientInfo = new ClientInfo { ClientName = $"Client {_connectedClients}", Status = "Connected", TcpClient = client };
            _clients.Add(clientInfo);

            ClientList.ItemsSource = null;
            ClientList.ItemsSource = _clients;

            if (_connectedClients == _expectedClients)
            {
                StatusLabel.Text = "All clients connected. Ready to load image.";
                LoadImageButton.IsEnabled = true;
            }
        }

        // Кнопка загрузки изображения
        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg)|*.png;*.jpeg";
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                StatusLabel.Text = "Image loaded. Sending to clients...";

                // Отображение исходного изображения в левом поле
                _originalImage = new BitmapImage(new Uri(filePath));
                OriginalImage.Source = _originalImage;

                // Отправка изображения клиентам
                SendImageToClients(filePath);
            }
        }

        // Отправка изображения всем подключенным клиентам
        private void SendImageToClients(string filePath)
        {
            byte[] imageBytes = File.ReadAllBytes(filePath);

            // Разделение изображения на части
            List<byte[]> imageParts = SplitImage(imageBytes, _expectedClients);

            for (int i = 0; i < _clients.Count; i++)
            {
                var clientInfo = _clients[i];
                clientInfo.Status = "Processing...";

                // Отправка части изображения клиенту
                SendDataToClient(clientInfo.TcpClient, imageParts[i]);
            }

            ClientList.ItemsSource = null;
            ClientList.ItemsSource = _clients;

            // Ожидание ответов от клиентов
            ReceiveDataFromClients();
        }

        private void SendDataToClient(TcpClient client, byte[] data)
        {
            try
            {
                NetworkStream stream = client.GetStream();

                // Отправка длины данных
                byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
                stream.Write(lengthBuffer, 0, 4);
                stream.Flush();

                // Отправка самих данных
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Error sending data: {ex.Message}\n{ex.StackTrace}"));
            }
        }

        private async void ReceiveDataFromClients()
        {
            List<byte[]> processedParts = new List<byte[]>(); // Исправлено на List<byte[]>
            int processedClients = 0;

            foreach (var clientInfo in _clients)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        NetworkStream stream = clientInfo.TcpClient.GetStream();

                        byte[] lengthBuffer = new byte[4];
                        int bytesRead = await stream.ReadAsync(lengthBuffer, 0, 4);
                        if (bytesRead != 4)
                        {
                            throw new Exception("Failed to read the data length.");
                        }

                        int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

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

                        // Сохранение обработанных данных
                        lock (processedParts)
                        {
                            processedParts.Add(dataBuffer); // Здесь добавляется byte[]
                            processedClients++;
                            clientInfo.Status = "Processed";
                        }

                        Dispatcher.Invoke(() =>
                        {
                            ClientList.ItemsSource = null;
                            ClientList.ItemsSource = _clients;
                        });

                        if (processedClients == _expectedClients)
                        {
                            StatusLabel.Text = "All clients processed the image. Combining results...";
                            CombineImageParts(processedParts);
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show($"Error receiving data from client: {ex.Message}\n{ex.StackTrace}"));
                    }
                });
            }
        }

        // Объединение частей изображения и отображение результата
        private void CombineImageParts(List<byte[]> processedParts)
        {
            // Логика для объединения обработанных частей изображения
            // Здесь вы можете использовать свой алгоритм для слияния частей в одно изображение

            // Пример: просто сохраняем первое изображение для демонстрации
            byte[] combinedImage = processedParts.FirstOrDefault();
            if (combinedImage != null)
            {
                using (MemoryStream ms = new MemoryStream(combinedImage))
                {
                    BitmapImage combinedBitmap = new BitmapImage();
                    combinedBitmap.BeginInit();
                    combinedBitmap.StreamSource = ms;
                    combinedBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    combinedBitmap.EndInit();

                    // Отображение объединённого изображения в правом поле
                    ProcessedImage.Source = combinedBitmap; // Изменено на ProcessedImage
                }
            }

            StatusLabel.Text = "Image processing completed.";
        }

        private List<byte[]> SplitImage(byte[] imageBytes, int parts)
        {
            int partSize = imageBytes.Length / parts;
            List<byte[]> imageParts = new List<byte[]>();

            for (int i = 0; i < parts; i++)
            {
                int size = (i == parts - 1) ? imageBytes.Length - (partSize * i) : partSize;
                byte[] part = new byte[size];
                Buffer.BlockCopy(imageBytes, partSize * i, part, 0, size);
                imageParts.Add(part);
            }

            return imageParts;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _server?.Stop();
            foreach (var client in _clients)
            {
                client.TcpClient.Close();
            }
        }
    }
}
