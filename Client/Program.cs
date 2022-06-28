using Server.Structures;
using System;
using System.Collections.Generic;
using System.IO;
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

        public static async Task Main(string[] args)
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

                SendTcp(client, $"{fileInfo.Name}:{startParameters.PortSendUdp}");

                byte[] sendbuf = File.ReadAllBytes(fileInfo.FullName);
                HashSet<Block> blocks = new HashSet<Block>();

                for (int i = 0; i < sendbuf.Length/SizeBlock; i++)
                {
                    Block block = new Block() { Id = i };
                    for (int j = 0; j < SizeBlock; j++)
                    {
                        int index = i * SizeBlock + j;
                        block.Data[index] = sendbuf[index];
                    }

                    blocks.Add(block);
                }

                foreach (Block block in blocks)
                {
                    SendUdp(block, new IPEndPoint(startParameters.IPAddress, startParameters.PortSendUdp));
                    string[] answerServer = null;

                    while (answerServer.Length != 2)
                    { 
                        answerServer = (await ReadTcp(client)).Split(':');
                        await Task.Delay(startParameters.TimeoutConfirm);
                    }

                    Console.WriteLine($"Пакет id = {answerServer[0]} {answerServer[1]}");
                }

                SendTcp(client, "Передача завершена");

                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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

            //stream.Close();
        }

        private static void SendUdp(Block block, IPEndPoint iPEndPoint)
        {
            Socket soket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            BinaryFormatter bf = new BinaryFormatter();

            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, block);
                soket.SendTo(ms.ToArray(), iPEndPoint);
            }        
        }
    }
}
