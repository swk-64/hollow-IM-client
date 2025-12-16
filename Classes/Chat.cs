using Hollow_IM_Client.Classes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Hollow_IM_Client.Classes
{
    internal class Chat
    {
        private UserModel me;

        private int messagesState;
        public List<MessageModel> messages { get; }

        private List<UserModel> users;

        public Chat(ChatModel chat)
        {
            this.me = chat.Me;

            messagesState = chat.MessagesState;
            messages = chat.Messages;

            users = chat.Users;
            return;
        }

        public void RequestSendMessage(SslStream stream, string content)
        {
            MessageModel message = new MessageModel
            {
                User = me,
                SentAt = DateTime.Now,
                Content = content
            };

            RequestManager.SendMessage(stream, message);
            return;
        }
        public void AddMessage(MessageModel message)
        {
            messages.Add(message);
            return;
        }

        public void RequestSyncChat(SslStream stream)
        {
            ClientChatState state = new ClientChatState { MessagesState = messagesState };

            RequestManager.SyncChat(stream, state);
            return;
        }

        public void SyncChat(SyncChatModel state)
        {
            users = state.Users;

            messages.AddRange(state.MessagesDelta);

            messagesState = state.LastMessagesState;
            return;
        }

    }
}
