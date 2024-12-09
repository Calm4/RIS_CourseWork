namespace TestClientApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var loadTest = new ImageProcessingLoadTest();

            await loadTest.StartProcessing(1000);
        }
    }
}
