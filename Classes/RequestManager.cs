using Hollow_IM_Client.Classes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Hollow_IM_Client.Classes
{
    internal class RequestManager
    {
        private static Byte[] BuildRequestPacket(Request request)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            string serialized = JsonSerializer.Serialize(request);
            Byte[] bytes = Encoding.UTF8.GetBytes(serialized);

            // Write Length Prefix (int32)
            writer.Write(bytes.Length);

            // Write Payload
            writer.Write(bytes);

            byte[] packet = ms.ToArray();
            return packet;
        }
        public static void JoinChat(SslStream stream, UserModel user)
        {
            JsonElement payload;

            string userStr = JsonSerializer.Serialize<UserModel>(user);
            using var userJson = JsonDocument.Parse(userStr);
            payload = userJson.RootElement.Clone();

            var request = new Models.Request { Action = "JOIN_CHAT", Payload = payload };

            var packet = BuildRequestPacket(request);
            stream.Write(packet, 0, packet.Length);

            return;
        }
        public static void SendMessage(SslStream stream, MessageModel message)
        {
            JsonElement payload;

            string messageStr = JsonSerializer.Serialize<MessageModel>(message);
            using var messageJson = JsonDocument.Parse(messageStr);
            payload = messageJson.RootElement.Clone();

            var request = new Models.Request { Action = "SEND_MESSAGE", Payload = payload };

            var packet = BuildRequestPacket(request);
            stream.Write(packet, 0, packet.Length);

            return;
        }
        public static void SyncChat(SslStream stream, ClientChatState state)
        {
            JsonElement payload;

            string stateStr = JsonSerializer.Serialize<ClientChatState>(state);
            using var stateJson = JsonDocument.Parse(stateStr);
            payload = stateJson.RootElement.Clone();

            var request = new Models.Request { Action = "SYNC_CHAT", Payload = payload };

            var packet = BuildRequestPacket(request);
            stream.Write(packet, 0, packet.Length);

            return;
        }
    }
}
