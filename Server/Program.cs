using Server.Classes;
using Server.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        private const int SizeBlock = 1024;

        public static void Main(string[] args)
        {
            StartParameters startParameters = new StartParameters() 
            { 
                IPAddress = IPAddress.Parse("127.0.0.1"),//args[0]),
                Port = Int16.Parse("5555"),//args[1]),
                FolderPath = @"D:\Downloads\TaskNPFRateks\TaskOne\Server\bin\Debug\test"//args[2]
            };

            TcpListener server = new TcpListener(startParameters.IPAddress, startParameters.Port);
            server.Start();

            try
            {
                while (true)
                {
                    Console.WriteLine("Ожидание подключений...");

                    TcpClient tcpClient = server.AcceptTcpClient();
                    NetworkStream stream = tcpClient.GetStream();

                    Console.WriteLine("Подключен клиент.");

                    string[] valuesSend = ReadTcp(stream, tcpClient).Split(':');

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
                            blocks.Add(ReadUdp(udpClient, stream, endPoint));
                            answerClient = ReadTcp(stream, tcpClient);
                        }

                        byte[] bytes = new byte[SizeBlock * blocks.Count];

                        for (int i = 0; i < bytes.Length/SizeBlock; i++)
                        {
                            byte[] block = blocks.ElementAt(i).Data;
                            Array.Copy(block, 0, bytes, i * SizeBlock, block.Length);
                        }
                        
                        using (FileStream fileStream = new FileStream(startParameters.FolderPath + '\\' + newFileName, FileMode.OpenOrCreate))
                        {
                            fileStream.Write(bytes, 0, bytes.Length);
                        }
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        udpClient.Close();
                    }

                    stream.Close();
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
        }

        private static string ReadTcp(NetworkStream stream, TcpClient tcpClient)
        {
            stream = tcpClient.GetStream();
            StringBuilder builder = new StringBuilder();
            byte[] buffer = new byte[1024];

            do
            {
                int countBuffer = stream.Read(buffer, 0, buffer.Length);
                builder.AppendFormat("{0}", Encoding.UTF8.GetString(buffer, 0, countBuffer));
            } while (stream.DataAvailable);

            return builder.ToString();
        }

        private static void SendTcp(NetworkStream stream, string messege)
        {
            StringBuilder builder = new StringBuilder();
            
            byte[] buffer = Encoding.UTF8.GetBytes(messege);
            stream.Write(buffer, 0, buffer.Length);
        }

         private static Block ReadUdp(UdpClient udpClient, NetworkStream stream, IPEndPoint endPoint)
        {
            Block block = new Block();
            byte[] bytes = null;

            try
            {
                while (bytes is null)
                {
                    udpClient.Connect(endPoint);
                    bytes = udpClient.Receive(ref endPoint);
                    udpClient.Close();
                }

                BinaryFormatter bf = new BinaryFormatter();
                bf.Binder = new CustomBinder();

                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    block = (Block)bf.Deserialize(ms);
                }

                Console.WriteLine($"Получен блок id = {block.Id}");

                SendTcp(stream, $"{block.Id}:принят");
            }
            catch (Exception)
            {
                Console.WriteLine($"Блок id = {block.Id} не загружен");
                SendTcp(stream, $"{block.Id}:отклонён");
            }

            return block;
        }
    }
}
