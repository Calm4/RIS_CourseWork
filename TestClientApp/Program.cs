﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TestClientApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var loadTest = new ImageProcessingLoadTest();

            await loadTest.StartProcessing(25);
        }
    }
}
