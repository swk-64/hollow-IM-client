using Hollow_IM_Client.Classes;
using Hollow_IM_Client.Classes.Models;
using System;
using System.Collections.ObjectModel;
﻿using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Hollow_IM_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HollowClient _client;
        private readonly ObservableCollection<string> _messages;
        private readonly DispatcherTimer _uiUpdateTimer;

        public MainWindow()
        {
            InitializeComponent();

            _client = new HollowClient();
            _messages = new ObservableCollection<string>();
            MessagesItemsControl.ItemsSource = _messages;

            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            _uiUpdateTimer.Start();
        }

        private void UiUpdateTimer_Tick(object? sender, EventArgs e)
        {
            var chat = _client.CurrentChat;
            if (chat == null)
            {
                return;
            }


            _messages.Clear();
            foreach (MessageModel message in chat.messages)
            {
                string line = $"[{message.SentAt:HH:mm:ss}] {message.User.username}: {message.Content}";
                _messages.Add(line);
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string address = AddressTextBox.Text.Trim();
            string portText = PortTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                StatusTextBlock.Text = "Введите имя пользователя.";
                return;
            }

            if (!int.TryParse(portText, out int port))
            {
                StatusTextBlock.Text = "Некорректный порт.";
                return;
            }

            StatusTextBlock.Text = "Подключение...";
            ConnectButton.IsEnabled = false;

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                    _client.Connect(address, port, username));
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Ошибка подключения: " + ex.Message;
                ConnectButton.IsEnabled = true;
                return;
            }

            StatusTextBlock.Text = "Подключено к чату.";
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendCurrentMessage();
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendCurrentMessage();
                e.Handled = true;
            }
        }

        private void SendCurrentMessage()
        {
            string text = MessageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _client.SendMessage(text);
            MessageTextBox.Clear();
        }
    }
}