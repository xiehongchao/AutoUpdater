﻿using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace GeneralUpdate.ClientCore.Hubs
{
    public class VersionHub
    {
        private const string ClientNameflag = "GeneralUpdate.Client";
        private const string ReceiveMessageflag = "ReceiveMessage";
        private const string SendMessageflag = "SendMessage";
        private const string Onlineflag = "Online";
        private const string Loginflag = "Login";
        private const string SignOutflag = "SignOut";

        private HubConnection connection = null;
        private VersionHub _instance;
        private readonly object _lock = new object();

        public VersionHub Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new VersionHub();
                        }
                    }
                }
                return _instance;
            }
        }

        private VersionHub()
        { }

        /// <summary>
        /// Subscribe to the latest version.
        /// </summary>
        public void Subscribe(string url, Action<Exception> onException)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new Exception("url not set !");

            try
            {
                if (connection == null)
                {
                    connection = new HubConnectionBuilder()
                            .WithUrl(url)
                            .WithAutomaticReconnect(new RandomRetryPolicy())
                            .Build();
                    connection.On<string>(ReceiveMessageflag, OnReceiveMessageHandler);
                    connection.On<string>(Onlineflag, OnOnlineMessageHandler);
                    connection.Reconnected += OnReconnected;
                    connection.Closed += OnClosed;
                }
                connection.StartAsync();
            }
            catch (Exception ex)
            {
                onException(ex);
            }
        }

        /// <summary>
        /// Receives the message.
        /// </summary>
        /// <param name="message"></param>
        private void OnReceiveMessageHandler(string message)
        {
            //TODO:接收到新版本推送处理
            //1.解析base64加密字符串
            //2.json解析为版本对象
            //3.http请求最新版本信息
            //4.下载、更新
            //5.暴露出更新完成通知、更新过程事件通知
        }

        /// <summary>
        /// Online and offline notification.
        /// </summary>
        /// <param name="message"></param>
        private void OnOnlineMessageHandler(string message)
        {
            //TODO:暴露出离线、上线通知
        }

        /// <summary>
        /// Reconnection notice.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task OnReconnected(string arg)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Shut down.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task OnClosed(Exception arg)
        {
            try
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Send message to server.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task Send(string user, string msg)
        {
            try
            {
                await connection.InvokeAsync(SendMessageflag, user, msg);
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <returns></returns>
        public async Task Login()
        {
            try
            {
                await connection.InvokeAsync(Loginflag, ClientNameflag);
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Sign out
        /// </summary>
        /// <returns></returns>
        public async Task SignOut()
        {
            try
            {
                await connection.InvokeAsync(SignOutflag, ClientNameflag);
                await connection.StopAsync();
            }
            catch (Exception ex)
            {
            }
        }
    }
}