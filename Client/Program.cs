using Client.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        private const int SizeBlock = 1024;

        async public static Task Main(string[] args)
        {
            TcpClient client = new TcpClient();
            FileInfo fileInfo = null;

            try
            {
               StartParameters startParameters = new StartParameters()
                {
                    IPAddress = IPAddress.Parse(args[0]),
                    PortConect = Int16.Parse(args[1]),
                    PortSendUdp = Int16.Parse(args[2]),
                   FilePath = new Uri(args[3], UriKind.RelativeOrAbsolute).LocalPath,
                    TimeoutConfirm = TimeSpan.FromMilliseconds(Int32.Parse(args[4]))
                };

                Console.WriteLine("Ожидание подключения...");

                while (!client.Connected)
                {
                    client.Connect(startParameters.IPAddress, startParameters.PortConect);
                }

                Console.WriteLine("Успешное подключение");

                fileInfo = new FileInfo(startParameters.FilePath);

                if (fileInfo.Length > 10e7)
                {
                    new Exception("указанный файл по размеру превашает 10Мб");
                }
                
                NetworkStream stream = client.GetStream();

                SendTcp(stream, $"{fileInfo.Name}:{startParameters.PortSendUdp}");

                byte[] sendbuf = File.ReadAllBytes(fileInfo.FullName);
                HashSet<Block> blocks = new HashSet<Block>();
                

                for (int i = 0; i < (double)sendbuf.Length/SizeBlock; i++)
                {
                    Block block = new Block() { Id = i+1 };
                    block.Data = sendbuf.Skip(i * SizeBlock).Take(SizeBlock).ToArray();

                    blocks.Add(block);
                }

                UdpClient udpClient = new UdpClient();
                udpClient.Connect(new IPEndPoint(startParameters.IPAddress, startParameters.PortSendUdp));

                foreach (Block block in blocks)
                {
                    SendUdp(block, udpClient);
                    string[] answerServer = null;

                    while (answerServer is null ? true : answerServer.Length != 2)
                    { 
                        answerServer = ReadTcp(stream, client).Split(':');
                        await Task.Delay(startParameters.TimeoutConfirm);
                    }

                    Console.WriteLine($"Пакет id = {answerServer[0]} {answerServer[1]}");
                }

                SendTcp(stream, "Передача завершена");
                Console.WriteLine("Передача завершена");

                stream.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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

        private static void SendUdp(Block block, UdpClient udpClient)
        {
            BinaryFormatter bf = new BinaryFormatter();

            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, block);
                byte[] bytes = ms.ToArray();

                udpClient.Send(bytes, bytes.Length);
            }        
        }
    }
}
