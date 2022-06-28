using Server.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            StartParameters startParameters = new StartParameters() 
            { 
                IPAddress = IPAddress.Parse(args[0]),
                Port = Int16.Parse(args[1]),
                FolderPath = args[2]
            };

            TcpListener server = new TcpListener(startParameters.IPAddress, startParameters.Port);
            server.Start();

            try
            {
                while (true)
                {
                    Console.WriteLine("Ожидание подключений...");

                    TcpClient tcpClient = server.AcceptTcpClient();

                    Console.WriteLine("Подключен клиент.");

                    string[] valuesSend = (await ReadTcp(tcpClient)).Split(':');

                    string newFileName = valuesSend[0];
                    short udpPort = Int16.Parse(valuesSend[1]);

                    UdpClient udpClient = new UdpClient(udpPort);
                    IPEndPoint endPoint = new IPEndPoint(startParameters.IPAddress, udpPort);

                    try
                    {
                        HashSet<Block> blocks = new HashSet<Block>();
                        string answerClient = null;

                        while (answerClient != "Передача завершена")
                        {
                            blocks.Add(ReadUdp(udpClient, tcpClient, endPoint));
                            answerClient = await ReadTcp(tcpClient);
                        }

                        byte[] bytes = null;

                        foreach (Block block in blocks)
                        {
                            Array.Copy(block.Data, bytes, bytes.Length + block.Data.Length);
                        }
                        
                        using (FileStream fileStream = new FileStream(startParameters.FolderPath + '\\' + newFileName, FileMode.OpenOrCreate))
                        {
                            fileStream.Write(bytes, 0, bytes.Length);
                        }
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        udpClient.Close();
                    }

                    tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (server != null)
                    server.Stop();
            }

            Console.ReadKey();
        }

        async static private Task<string> ReadTcp(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            StringBuilder builder = new StringBuilder();
            byte[] buffer = new byte[1024];

            do
            {
                int countBuffer = await stream.ReadAsync(buffer, 0, buffer.Length);

                builder.AppendFormat("{0}", Encoding.UTF8.GetString(buffer, 0, countBuffer));
            }
            while (stream.DataAvailable);

            stream.Close();

            return builder.ToString();
        }

        async static private void SendTcp(TcpClient client, string messege)
        {
            NetworkStream stream = client.GetStream();
            StringBuilder builder = new StringBuilder();
            
            byte[] buffer = Encoding.UTF8.GetBytes(messege);
            await stream.WriteAsync(buffer, 0, buffer.Length);

            stream.Close();
        }

        private static Block ReadUdp(UdpClient udpClient, TcpClient tcpClient, IPEndPoint endPoint)
        {
            Block block = new Block();
            byte[] bytes = null;

            try
            {
                while (bytes is null)
                {
                    bytes = udpClient.Receive(ref endPoint);
                }

                BinaryFormatter bf = new BinaryFormatter();

                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    block = (Block)bf.Deserialize(ms);
                }

                Console.WriteLine($"Получен блок id = {block.Id}");

                SendTcp(tcpClient, $"{block.Id}:принят");
            }
            catch (Exception)
            {
                Console.WriteLine($"Блок id = {block.Id} не загружен");
                SendTcp(tcpClient, $"{block.Id}:отклонён");
            }

            return block;
        }
    }
}
