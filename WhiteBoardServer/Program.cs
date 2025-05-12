using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WhiteBoardServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static List<TcpClient> clients = new List<TcpClient>();
        static TcpListener listener;
        static void Main()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            Console.WriteLine("Server started...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                lock (clients) clients.Add(client);

                BroadcastClientCount();

                Thread thread = new Thread(() => HandleClient(client));
                thread.Start();
            }
        }

        static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];

            while (true)
            {
                try
                {
                    int byteCount = stream.Read(buffer, 0, buffer.Length);
                    if (byteCount == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    BroadcastMessage(message, client);
                }
                catch
                {
                    break;
                }
            }

            lock (clients) clients.Remove(client);
            BroadcastClientCount();
            client.Close();
        }

        static void BroadcastMessage(string msg, TcpClient excludeClient)
        {
            lock (clients)
            {
                foreach (var c in clients)
                {
                    if (c != excludeClient && c.Connected)
                    {
                        NetworkStream ns = c.GetStream();
                        byte[] data = Encoding.UTF8.GetBytes(msg);
                        ns.Write(data, 0, data.Length);
                    }
                }
            }
        }

        static void BroadcastClientCount()
        {
            var data = JsonSerializer.Serialize(new { action = "count", value = clients.Count });
            BroadcastMessage(data, null);
        }
    }
}
