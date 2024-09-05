using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerChat
{
    internal class Server
    {
        public static List<ClientHandler> clients = new List<ClientHandler>();
        private static TcpListener server;

        public static void Main(string[] args)
        {
            Console.WriteLine("Enter Address: ");
            string serverAddress = Console.ReadLine();

            Console.WriteLine("Enter Port: ");
            int serverPort = Convert.ToInt32(Console.ReadLine());


            server = new TcpListener(IPAddress.Parse(serverAddress), serverPort);
            server.Start();

            Console.WriteLine($"Server started on port {serverPort}");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                ClientHandler handler = new ClientHandler(client);
                clients.Add(handler);
                Thread thread = new Thread(handler.Handle);
                thread.Start();
            }
        }

        public static void BroadcastMessage(string message, ClientHandler sender)
        {
            foreach (var client in clients)
            {
                if (client != sender)
                {
                    client.SendMessage(message);
                }
            }
        }

        public static void RemoveClient(ClientHandler client)
        {
            clients.Remove(client);
            BroadcastMessage($"Users: {string.Join(",", clients.Select(c => c.Username))}", null);
            BroadcastMessage($"{client.Username} left the chat", null);
        }
    }

    internal class ClientHandler
    {
        private TcpClient client;
        private NetworkStream stream;
        public string Username { get; private set; }

        public ClientHandler(TcpClient client)
        {
            this.client = client;
        }

        public void Handle()
        {
            stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;
                try
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                }
                catch
                {
                    Server.RemoveClient(this);
                    break;
                }

                if (bytesRead == 0)
                {
                    Server.RemoveClient(this);
                    break;
                }

                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                if (Username == null)
                {
                    Username = message;
                    Server.BroadcastMessage($"Users: {string.Join(",", Server.clients.Select(c => c.Username))}", this);
                    Server.BroadcastMessage($"{Username} joined the chat", this);
                }
                else
                {
                    Server.BroadcastMessage($"{Username}: {message}", this);
                }
            }

            client.Close();
        }

        public void SendMessage(string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}