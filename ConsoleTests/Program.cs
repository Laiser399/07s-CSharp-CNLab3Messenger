using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.IO;
using System.Threading;

namespace ConsoleTests
{



    class Program
    {
        static Aes aes = Aes.Create();

        static void Main(string[] args)
        {
            // cannot write/read public after encrypted (only if read encrypted part in byte[] before)

            byte[] data;
            using (MemoryStream mStream = new MemoryStream())
            {
                int val1 = 1023;
                byte[] tm = BitConverter.GetBytes(val1);
                mStream.Write(tm, 0, tm.Length);

                tm = Encrypt(123);
                mStream.Write(tm, 0, tm.Length);

                int val3 = 12;
                tm = BitConverter.GetBytes(val3);
                mStream.Write(tm, 0, tm.Length);

                data = mStream.ToArray();
            }

            using (MemoryStream mStream = new MemoryStream(data))
            {
                byte[] buf = new byte[4];
                int hasRead = 0;
                do
                {
                    hasRead += mStream.Read(buf, hasRead, 4 - hasRead);
                } while (hasRead < 4);
                int val1 = BitConverter.ToInt32(buf, 0);
                Console.WriteLine(val1);

                using (CryptoStream cryptoStream = new CryptoStream(mStream, aes.CreateDecryptor(),
                    CryptoStreamMode.Read))
                {
                    hasRead = 0;
                    do
                    {
                        hasRead += cryptoStream.Read(buf, hasRead, 4 - hasRead);
                    } while (hasRead < 4);
                    int val2 = BitConverter.ToInt32(buf, 0);
                    Console.WriteLine(val2);

                    hasRead = 0;
                    do
                    {
                        hasRead += mStream.Read(buf, hasRead, 4 - hasRead);
                    } while (hasRead < 4);
                    int val3 = BitConverter.ToInt32(buf, 0);
                    Console.WriteLine(val3);
                }
            }









            Console.WriteLine("Press...");
            Console.ReadKey();
        }

        static byte[] Encrypt(int value)
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(mStream, aes.CreateEncryptor(),
                    CryptoStreamMode.Write))
                using (BinaryWriter crWriter = new BinaryWriter(cryptoStream))
                {
                    crWriter.Write(value);
                }
                return mStream.ToArray();
            }
        }

        static void Test0()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 60000);
            Task.Run(() =>
            {
                listener.Start();
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    OnClientAccepted(client);
                }
            });


            TcpClient client = new TcpClient();
            client.Connect(IPAddress.Loopback, 60000);
            NetworkStream stream = client.GetStream();

            using (CryptoStream writeCrStream = new CryptoStream(stream,
                aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (BinaryWriter crWriter = new BinaryWriter(writeCrStream))
            {
                int count = 5;
                Random random = new Random();

                crWriter.Write(count);
                for (int i = 0; i < count; ++i)
                {
                    crWriter.Write(random.Next());
                }

                crWriter.Flush();
                writeCrStream.Flush();
                writeCrStream.FlushFinalBlock();
                //writeCrStream.Dispose();

                //client.GetStream();
                Thread.Sleep(20000);
            }

            client.Close();
        }

        static void OnClientAccepted(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            using (CryptoStream readCrStream = new CryptoStream(stream,
                aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (BinaryReader crReader = new BinaryReader(readCrStream))
            {
                int count = crReader.ReadInt32();
                Console.WriteLine($"count = {count}");
                for (int i = 0; i < count; ++i)
                {
                    int value = crReader.ReadInt32();
                    Console.WriteLine($"i = {i}, value = {value}");
                }

                client.GetStream();
            }



            client.Close();
        }

        static void Test1()
        {
            using (MemoryStream mStream = new MemoryStream())
            using (CryptoStream crStream = new CryptoStream(mStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (BinaryWriter writer = new BinaryWriter(crStream))
            {
                writer.Write(1239);
                writer.Flush();
                crStream.Flush();
                crStream.FlushFinalBlock();
                byte[] data = mStream.ToArray();
            }
        }

    }
}
