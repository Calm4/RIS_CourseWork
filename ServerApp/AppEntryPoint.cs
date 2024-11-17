using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
