namespace ServerApp
{
    internal class AppEntryPoint
    {
        public static void Main(string[] args)
        {
            ImageProcessingServer server = new ImageProcessingServer();
            server.StartServer();
        }
    }
}
