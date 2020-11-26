using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace CNLab3_Messenger.Protocol
{
    class GovnoCryptoData
    {
        public byte[] IV { get; private set; }
        public byte[] Key { get; private set; }

        public GovnoCryptoData(byte[] iv, byte[] key)
        {
            IV = iv;
            Key = key;
        }
    }

    class NotConnectedException : Exception { }

    class GovnoServer
    {
        public static readonly int MaxFileSize = 314_572_800;
        public static readonly int MaxImageSize = 10_485_760;

        private static Encoding _defaultEncoding = Encoding.UTF8;

        public event EventHandler<ConnectedEventArgs> OnConnected;
        public event EventHandler<TextMsgEventArgs> OnMessageReceived;
        public event EventHandler<ImageMsgEventArgs> OnImageReceived;
        public event EventHandler<FileMsgEventArgs> OnFileReceived;
        public event EventHandler<FileEventArgs> OnStartFileSending;
        public event EventHandler<FileSendingProgressEventArgs> OnFileSendingProgressChanged;
        public event EventHandler<FileEventArgs> OnCanceledSending;
        public event EventHandler<FileEventArgs> OnErrorFileSending;

        private TcpListener _listener;
        private IPEndPoint _serverIPEndPoint;
        private bool _isStarted = false;
        private Dictionary<IPEndPoint, GovnoCryptoData> _connected = new Dictionary<IPEndPoint, GovnoCryptoData>();
        private Dictionary<string, string> _accessCodeToFile = new Dictionary<string, string>();
        private Dictionary<string, CancellationTokenSource> _cancellationTokens =
            new Dictionary<string, CancellationTokenSource>();

        public GovnoServer(IPAddress ipAddress, int port) : this(new IPEndPoint(ipAddress, port)) { }

        public GovnoServer(IPEndPoint serverIPEndPoint)
        {
            _serverIPEndPoint = serverIPEndPoint;
            _listener = new TcpListener(IPAddress.Any, serverIPEndPoint.Port);
        }

        public async void Start()
        {
            if (_isStarted)
                return;
            _isStarted = true;

            _listener.Start();
            
            while (_isStarted)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                OnClientAcceptedAsync(client);
            }
        }

        public void Stop()
        {
            if (!_isStarted)
                return;
            _isStarted = false;
        }

        private async void OnClientAcceptedAsync(TcpClient client)
        {
            try
            {
                using (client)
                {
                    NetworkStream stream = client.GetStream();

                    IPEndPoint senderIPPoint = await ReadIPPointAsync(stream);
                    bool isConnection = await ReadBooleanAsync(stream);

                    if (isConnection)
                    {
                        await OnConnectionAsync(senderIPPoint, stream);
                    }
                    else
                    {
                        await OnMessageAsync(senderIPPoint, stream);
                    }
                }
            }
            catch { }
        }

        private async Task OnConnectionAsync(IPEndPoint sender, NetworkStream stream)
        {
            byte[] exponent = await ReadBytesWithPrefixAsync(stream);
            byte[] modulus = await ReadBytesWithPrefixAsync(stream);

            // encrypt aes key
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(new RSAParameters()
            {
                Exponent = exponent,
                Modulus = modulus
            });
            Aes aes = Aes.Create();
            byte[] IVEncrypted = rsa.Encrypt(aes.IV, false);
            byte[] keyEncrypted = rsa.Encrypt(aes.Key, false);

            // send aes encrypted key
            await WriteWithPrefixAsync(stream, IVEncrypted);
            await WriteWithPrefixAsync(stream, keyEncrypted);

            OnConnected?.Invoke(this, new ConnectedEventArgs { SenderIPPoint = sender });
            if (_connected.ContainsKey(sender))
                _connected.Remove(sender);
            _connected.Add(sender, new GovnoCryptoData(aes.IV, aes.Key));
        }

        private async Task OnMessageAsync(IPEndPoint sender, NetworkStream stream)
        {
            if (!_connected.ContainsKey(sender))
                return;

            byte[] data = await ReadBytesWithPrefixAsync(stream);

            JObject obj = await DecryptJObjectAsync(data, _connected[sender]);

            switch (obj.Value<string>("type"))
            {
                case "message":
                    {
                        string text = obj.Value<string>("text");
                        OnMessageReceived?.Invoke(this, new TextMsgEventArgs
                        {
                            SenderIPPoint = sender,
                            Text = text
                        });
                        break;
                    }
                case "image":
                    {
                        int fileLength = obj.Value<int>("bytes_count");
                        string fileName = obj.Value<string>("file_name");

                        byte[] imageEncryptedData = await ReadBytesWithPrefixAsync(stream);
                        byte[] imageData = await DecryptBytesAsync(imageEncryptedData, fileLength, _connected[sender]);
                        
                        OnImageReceived?.Invoke(this, new ImageMsgEventArgs
                        {
                            SenderIPPoint = sender,
                            FileSize = fileLength,
                            FileName = fileName,
                            ImageData = imageData
                        });
                        break;
                    }
                case "file_link":
                    {
                        string fileName = obj.Value<string>("file_name");
                        int fileSize = obj.Value<int>("file_size");
                        string accessCode = obj.Value<string>("access_code");
                        OnFileReceived?.Invoke(this, new FileMsgEventArgs
                        {
                            SenderIPPoint = sender,
                            FileSize = fileSize,
                            FileName = fileName,
                            AccessCode = accessCode
                        });
                        break;
                    }
                case "file_request":
                    {
                        string accessCode = obj.Value<string>("access_code");
                        if (!_accessCodeToFile.ContainsKey(accessCode))
                            break;
                        string filePath = _accessCodeToFile[accessCode];
                        _accessCodeToFile.Remove(accessCode);

                        int fileLength = (int)new FileInfo(filePath).Length;

                        CancellationTokenSource cts = new CancellationTokenSource();
                        _cancellationTokens.Add(accessCode, cts);
                        try
                        {
                            using (FileStream fStream = File.OpenRead(filePath))
                            using (CryptoStream crStream = new SilentCryptoStream(stream,
                                CreateEncryptor(_connected[sender]), CryptoStreamMode.Write))
                            {
                                OnStartFileSending?.Invoke(this, new FileEventArgs { AccessCode = accessCode });
                                await fStream.CopyToAsync(crStream, 20_000, fileLength, cts.Token, progress =>
                                {
                                    OnFileSendingProgressChanged?.Invoke(this, new FileSendingProgressEventArgs
                                    {
                                        AccessCode = accessCode,
                                        Progress = progress
                                    });
                                });
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            OnCanceledSending?.Invoke(this, new FileEventArgs { AccessCode = accessCode });
                        }
                        catch
                        {
                            OnErrorFileSending?.Invoke(this, new FileEventArgs
                            {
                                AccessCode = accessCode
                            });
                        }
                        finally
                        {
                            _cancellationTokens.Remove(accessCode);
                        }
                        break;
                    }
            }
        }

        public async Task ConnectAsync(IPEndPoint receiver)
        {
            using (TcpClient client = new TcpClient(AddressFamily.InterNetwork))
            {
                await client.ConnectAsync(receiver.Address, receiver.Port);
                NetworkStream stream = client.GetStream();

                await WriteAsync(stream, _serverIPEndPoint);
                await WriteAsync(stream, true);

                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                RSAParameters publicParams = rsa.ExportParameters(false);
                await WriteWithPrefixAsync(stream, publicParams.Exponent);
                await WriteWithPrefixAsync(stream, publicParams.Modulus);

                byte[] IVEncrypted = await ReadBytesWithPrefixAsync(stream);
                byte[] keyEncrypted = await ReadBytesWithPrefixAsync(stream);

                byte[] IV = rsa.Decrypt(IVEncrypted, false);
                byte[] key = rsa.Decrypt(keyEncrypted, false);

                if (_connected.ContainsKey(receiver))
                    _connected.Remove(receiver);
                _connected.Add(receiver, new GovnoCryptoData(IV, key));
            }
        }

        public void Disconnect(IPEndPoint point)
        {
            _connected.Remove(point);
        }

        public async Task SendTextMessageAsync(IPEndPoint receiver, string message)
        {
            if (!_connected.ContainsKey(receiver))
                throw new NotConnectedException();

            using (TcpClient client = new TcpClient(AddressFamily.InterNetwork))
            {
                await client.ConnectAsync(receiver.Address, receiver.Port);
                NetworkStream stream = client.GetStream();

                await WriteAsync(stream, _serverIPEndPoint);
                await WriteAsync(stream, false);

                byte[] data = await EncryptAsync(new JObject(new object[]
                {
                    new JProperty("type", "message"),
                    new JProperty("text", message)
                }), _connected[receiver]);
                await WriteWithPrefixAsync(stream, data);
            }
        }

        public async Task SendImageAsync(IPEndPoint receiver, string fileName, byte[] imageData)
        {
            if (!_connected.ContainsKey(receiver))
                throw new NotConnectedException();

            using (TcpClient client = new TcpClient(AddressFamily.InterNetwork))
            {
                await client.ConnectAsync(receiver.Address, receiver.Port);
                NetworkStream stream = client.GetStream();

                await WriteAsync(stream, _serverIPEndPoint);
                await WriteAsync(stream, false);

                byte[] data = await EncryptAsync(new JObject(new object[]
                {
                    new JProperty("type", "image"),
                    new JProperty("file_name", fileName),
                    new JProperty("bytes_count", imageData.Length),
                }), _connected[receiver]);
                await WriteWithPrefixAsync(stream, data);

                byte[] encryptedImage = await EncryptAsync(imageData, _connected[receiver]);
                await WriteWithPrefixAsync(stream, encryptedImage);
            }
        }

        public async Task<string> SendFileAccessAsync(IPEndPoint receiver, string filePath, int fileSize)
        {
            if (!_connected.ContainsKey(receiver))
                throw new NotConnectedException();

            using (TcpClient client = new TcpClient(AddressFamily.InterNetwork))
            {
                await client.ConnectAsync(receiver.Address, receiver.Port);
                NetworkStream stream = client.GetStream();

                await WriteAsync(stream, _serverIPEndPoint);
                await WriteAsync(stream, false);

                string accessCode = GenerateFileAccessCode(40);
                _accessCodeToFile.Add(accessCode, filePath);
                byte[] data = await EncryptAsync(new JObject(new object[]
                {
                    new JProperty("type", "file_link"),
                    new JProperty("file_name", Path.GetFileName(filePath)),
                    new JProperty("file_size", fileSize),
                    new JProperty("access_code", accessCode)
                }), _connected[receiver]);
                await WriteWithPrefixAsync(stream, data);

                return accessCode;
            }
        }

        public void BlockFileAccess(string accessCode)
        {
            if (_accessCodeToFile.ContainsKey(accessCode))
                _accessCodeToFile.Remove(accessCode);
        }

        public void CancelFileSending(string accessCode)
        {
            if (_cancellationTokens.ContainsKey(accessCode))
            {
                _cancellationTokens[accessCode].Cancel();
                _cancellationTokens.Remove(accessCode);
            }
        }

        private string GenerateFileAccessCode(int minLength)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            for (int i = 0; i < minLength || _accessCodeToFile.ContainsKey(builder.ToString()); ++i)
            {
                int value = random.Next(0, 62);
                if (value < 26)
                    builder.Append((char)('a' + value));
                else if (value < 52)
                    builder.Append((char)('A' + value - 26));
                else
                    builder.Append((char)('0' + value - 52));
            }
            return builder.ToString();
        }

        public async Task ReceiveFileAsync(IPEndPoint fileSender, string saveFilePath, string accessCode, 
            int fileSize, CancellationToken token, Action<double> progressCallback = null)
        {
            if (!_connected.ContainsKey(fileSender))
                throw new NotConnectedException();

            using (TcpClient client = new TcpClient(AddressFamily.InterNetwork))
            {
                await client.ConnectAsync(fileSender.Address, fileSender.Port);
                NetworkStream stream = client.GetStream();
                stream.ReadTimeout = 1000;
                stream.WriteTimeout = 1000;

                await WriteAsync(stream, _serverIPEndPoint);
                await WriteAsync(stream, false);

                byte[] data = await EncryptAsync(new JObject(new object[]
                {
                    new JProperty("type", "file_request"),
                    new JProperty("access_code", accessCode)
                }), _connected[fileSender]);
                await WriteWithPrefixAsync(stream, data);

                using (CryptoStream crStream = new SilentCryptoStream(stream,
                        CreateDecryptor(_connected[fileSender]), CryptoStreamMode.Read))
                using (FileStream fStream = File.OpenWrite(saveFilePath))
                {
                    await crStream.CopyToAsync(fStream, 20_480, fileSize, token, progressCallback);
                }
            }
        }

        private async Task<byte[]> DecryptBytesAsync(byte[] data, int length, GovnoCryptoData cryptoData)
        {
            using (CryptoStream crStream = new CryptoStream(new MemoryStream(data),
                CreateDecryptor(cryptoData), CryptoStreamMode.Read))
            {
                return await ReadBytesAsync(crStream, length);
            }
        }

        private async Task<byte[]> EncryptAsync(byte[] data, GovnoCryptoData cryptoData)
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                using (CryptoStream crStream = new CryptoStream(mStream,
                    CreateEncryptor(cryptoData), CryptoStreamMode.Write))
                {
                    await WriteAsync(crStream, data);
                }

                return mStream.ToArray();
            }
        }

        private async Task<JObject> DecryptJObjectAsync(byte[] data, GovnoCryptoData cryptoData)
        {
            using (CryptoStream crStream = new CryptoStream(new MemoryStream(data),
                CreateDecryptor(cryptoData), CryptoStreamMode.Read))
            {
                return await ReadJObjectAsync(crStream);
            }
        }

        private async Task<byte[]> EncryptAsync(JToken token, GovnoCryptoData cryptoData)
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                using (CryptoStream crStream = new CryptoStream(mStream,
                    CreateEncryptor(cryptoData), CryptoStreamMode.Write))
                {
                    await WriteAsync(crStream, token);
                }

                return mStream.ToArray();
            }
        }

        // static methods
        private static ICryptoTransform CreateEncryptor(GovnoCryptoData cryptoData)
        {
            Aes aes = new AesCryptoServiceProvider()
            {
                IV = cryptoData.IV,
                Key = cryptoData.Key
            };
            return aes.CreateEncryptor();
        }
        
        private static ICryptoTransform CreateDecryptor(GovnoCryptoData cryptoData)
        {
            Aes aes = new AesCryptoServiceProvider()
            {
                IV = cryptoData.IV,
                Key = cryptoData.Key
            };
            return aes.CreateDecryptor();
        }

        private static async Task<IPEndPoint> ReadIPPointAsync(Stream stream)
        {
            byte[] data = await ReadBytesAsync(stream, 4);
            int port = await ReadInt32Async(stream);
            return new IPEndPoint(new IPAddress(data), port);
        }

        private static IPEndPoint ReadIPPoint(Stream stream)
        {
            byte[] data = ReadBytes(stream, 4);
            int port = ReadInt32(stream);
            return new IPEndPoint(new IPAddress(data), port);
        }

        private static async Task WriteAsync(Stream stream, IPEndPoint point)
        {
            await WriteAsync(stream, point.Address.GetAddressBytes());
            await WriteAsync(stream, point.Port);
        }

        private static void Write(Stream stream, IPEndPoint point)
        {
            Write(stream, point.Address.GetAddressBytes());
            Write(stream, point.Port);
        }

        private static async Task<JObject> ReadJObjectAsync(Stream stream)
        {
            byte[] jsonBinary = await ReadBytesWithPrefixAsync(stream);
            string jsonString = _defaultEncoding.GetString(jsonBinary);
            return JObject.Parse(jsonString);
        }

        private static JObject ReadJObject(Stream stream)
        {
            byte[] jsonBinary = ReadBytesWithPrefix(stream);
            string jsonString = _defaultEncoding.GetString(jsonBinary);
            return JObject.Parse(jsonString);
        }

        private static async Task WriteAsync(Stream stream, JToken token)
        {
            string jsonString = token.ToString(Newtonsoft.Json.Formatting.None);
            byte[] jsonBinary = _defaultEncoding.GetBytes(jsonString);
            await WriteWithPrefixAsync(stream, jsonBinary);
        }

        private static void Write(Stream stream, JToken token)
        {
            string jsonString = token.ToString(Newtonsoft.Json.Formatting.None);
            byte[] jsonBinary = _defaultEncoding.GetBytes(jsonString);
            WriteWithPrefix(stream, jsonBinary);
        }

        private static async Task<byte[]> ReadBytesWithPrefixAsync(Stream stream)
        {
            int length = await ReadInt32Async(stream);
            return await ReadBytesAsync(stream, length);
        }

        private static byte[] ReadBytesWithPrefix(Stream stream)
        {
            int length = ReadInt32(stream);
            return ReadBytes(stream, length);
        }

        private static async Task WriteWithPrefixAsync(Stream stream, byte[] data)
        {
            await WriteAsync(stream, data.Length);
            await WriteAsync(stream, data);
        }

        private static void WriteWithPrefix(Stream stream, byte[] data)
        {
            Write(stream, data.Length);
            Write(stream, data);
        }

        private static async Task<int> ReadInt32Async(Stream stream)
        {
            byte[] data = await ReadBytesAsync(stream, 4);
            return BitConverter.ToInt32(data, 0);
        }

        private static int ReadInt32(Stream stream)
        {
            byte[] data = ReadBytes(stream, 4);
            return BitConverter.ToInt32(data, 0);
        }

        private static async Task WriteAsync(Stream stream, int value)
        {
            await WriteAsync(stream, BitConverter.GetBytes(value));
        }

        private static void Write(Stream stream, int value)
        {
            Write(stream, BitConverter.GetBytes(value));
        }

        private static async Task<bool> ReadBooleanAsync(Stream stream)
        {
            byte[] data = await ReadBytesAsync(stream, 1);
            return BitConverter.ToBoolean(data, 0);
        }

        private static bool ReadBoolean(Stream stream)
        {
            byte[] data = ReadBytes(stream, 1);
            return BitConverter.ToBoolean(data, 0);
        }

        private static async Task WriteAsync(Stream stream, bool value)
        {
            byte[] data = BitConverter.GetBytes(value);
            await WriteAsync(stream, data);
        }

        private static void Write(Stream stream, bool value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Write(stream, data);
        }

        private static async Task<byte[]> ReadBytesAsync(Stream stream, int length)
        {
            byte[] data = new byte[length];
            int hasRead = 0;
            do
            {
                hasRead += await stream.ReadAsync(data, hasRead, length - hasRead);
            } while (hasRead < length);
            return data;
        }

        private static byte[] ReadBytes(Stream stream, int length)
        {
            byte[] data = new byte[length];
            int hasRead = 0;
            do
            {
                hasRead += stream.Read(data, hasRead, length - hasRead);
            } while (hasRead < length);
            return data;
        }

        private static async Task WriteAsync(Stream stream, byte[] data)
        {
            await stream.WriteAsync(data, 0, data.Length);
        }

        private static void Write(Stream stream, byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }

    }
}
