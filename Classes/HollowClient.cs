using Hollow_IM_Client.Classes.Models;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Hollow_IM_Client.Classes
{
    internal class HollowClient
    {
        
        private TcpClient? client;

        private SslStream? secured_stream;

        private Chat? chat;

        private CancellationTokenSource? chatSyncCts;

        public Chat? CurrentChat => chat;

        public HollowClient()
        {
            client = null;
            secured_stream = null;
            chat = null;
        }

        private async Task<Response?> readResponseAsync(byte[] prefixBuffer)
        {
            using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await secured_stream!.ReadExactlyAsync(prefixBuffer, 0, 4, cts1.Token);

                Int32 length = BitConverter.ToInt32(prefixBuffer, 0);
                byte[] responseBuffer = new byte[length];

                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await secured_stream.ReadExactlyAsync(responseBuffer, 0, length, cts2.Token);

                string responseJson = Encoding.UTF8.GetString(responseBuffer);

                return JsonSerializer.Deserialize<Response>(responseJson)!;

            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Read timed out after 5 seconds.");
                return null;
            }
            catch (EndOfStreamException)
            {
                Console.WriteLine("Stream ended before enough bytes were read.");
                return null;
            }

        }

        private static bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            // !!! Certificate check isn't implemented, because the certificate is self-signed
            return true;
        }

        public async Task ClientLoop()
        {
            byte[] prefixBuffer = new byte[4];

            // Sync loop
            chatSyncCts = new CancellationTokenSource();

            _ = Task.Run(async () => {
                while (!chatSyncCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        if (chat != null && secured_stream != null)
                        {
                            chat.RequestSyncChat(secured_stream);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine($"Unexpected sync loop error: {ex}"); 
                    }
                }
            }, chatSyncCts.Token);

            // Read loop
            try
            {
                while (true)
                {
                    Response? response = await readResponseAsync(prefixBuffer);
                    if (response == null)
                        break;

                    switch (response.Action)
                    {
                        case "JOIN_CHAT":
                            if (response.Status)
                            {
                                var data = response.Payload.Deserialize<ChatModel>()!;

                                chat = new Chat(data);
                            }
                            break;
                        case "SEND_MESSAGE":
                            if (response.Status && chat != null)
                            {
                                var message = response.Payload.Deserialize<MessageModel>()!;

                                chat.AddMessage(message);
                            }
                            break;
                        case "SYNC_CHAT":
                            if (response.Status && chat != null)
                            {
                                var data = response.Payload.Deserialize<SyncChatModel>()!;
                                chat.SyncChat(data);
                            }
                            break;
                    }
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Client disconnected unexpectedly.");
            }
            finally
            {
                chatSyncCts.Cancel();
                Disconnect();
            }

        }

        public async Task Connect(string address, Int32 port, string username)
        {
            try
            {
                client = new TcpClient(address, port);

                using var stream = client.GetStream();
                secured_stream = new SslStream(
                    stream, 
                    false, 
                    new RemoteCertificateValidationCallback(ValidateServerCertificate));

                await secured_stream.AuthenticateAsClientAsync(targetHost: "localhost", clientCertificates: null, enabledSslProtocols: SslProtocols.Tls13, checkCertificateRevocation: false);

                UserModel user = new UserModel { username = username };

                RequestManager.JoinChat(secured_stream, user);

                await ClientLoop();
            }
            catch (SocketException ex) {
                    Console.WriteLine($"Socket error: {ex.SocketErrorCode}");
            }
        }

        public void Disconnect() 
        {
            if (secured_stream != null)
            {
                secured_stream.Dispose();
                secured_stream = null;
            }

            if (client != null)
            {
                client.Dispose();
                client = null;
            }

            chat = null;
        }

        public void SendMessage(string message) 
        {
            if (chat == null) 
                return;

            chat.RequestSendMessage(secured_stream!, message);
        }

    }
}
