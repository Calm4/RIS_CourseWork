using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp
{
    public class ClientInfo
    {
        public string ClientName { get; set; }
        public string Status { get; set; }
        public TcpClient TcpClient { get; set; }
    }
}
