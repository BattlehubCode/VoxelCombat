using WebSocketSharp;
using System;

namespace Battlehub.VoxelCombat
{
    public class Socket : ISocket
    {
        public event SocketEvent Opened;
        public event SocketEvent Closed;
        public event SocketEvent<SocketErrorArgs> Error;
        public event SocketEvent<byte[]> Message;

        private bool m_isOpening;
        private bool m_isOpened;
        public bool IsOpening
        {
            get { return m_isOpening; }
        }
        public bool IsOpened
        {
            get { return m_isOpened; }
        }
        private WebSocket m_ws;

        public void Open(string serverUrl)
        {
            if (IsOpening)
            {
                throw new InvalidOperationException("Socket is in Opening state");
            }

            if (IsOpened)
            {
                throw new InvalidOperationException("Close socket first");
            }

            m_ws = new WebSocket(serverUrl);
            m_ws.OnClose += OnClose;
            m_ws.OnError += OnError;
            m_ws.OnOpen += OnOpen;
            m_ws.OnMessage += OnMessage;

            m_isOpening = true;
            m_ws.ConnectAsync();
        }

  
        public void Close()
        {
            if (m_ws == null)
            {
                throw new InvalidOperationException("Socket is not opened. m_ws == null");
            }

            if(!IsOpened && !IsOpening)
            {
                return;
            }

            m_ws.CloseAsync();
        }

        private void OnOpen(object sender, System.EventArgs e)
        {
            Dispatcher.Dispatcher.Current.BeginInvoke(() =>
            {
                m_isOpening = false;
                m_isOpened = true;
                if (Opened != null)
                {
                    Opened(this);
                }
            });
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            Dispatcher.Dispatcher.Current.BeginInvoke(() =>
            {
                if (Message != null)
                {
                    Message(this, e.RawData);
                }
            });
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Dispatcher.Dispatcher.Current.BeginInvoke(() =>
            {
                if (Error != null)
                {
                    Error(this, new SocketErrorArgs(e.Message, e.Exception));
                }
            });
        }

        private void OnClose(object sender, CloseEventArgs e)
        {        
            Dispatcher.Dispatcher.Current.BeginInvoke(() =>
            {
                m_ws.OnClose -= OnClose;
                m_ws.OnError -= OnError;
                m_ws.OnOpen -= OnOpen;
                m_ws.OnMessage -= OnMessage;
                m_ws = null;
                m_isOpened = false;
                m_isOpening = false;

                if(!e.WasClean)
                {
                    UnityEngine.Debug.LogWarning("Socket Closed With Error Code: " + e.Code + " " + e.Reason);
                }
                
                if (Closed != null)
                {
                    Closed(this);
                }
            });
        }

        public void Send(byte[] data, Action<bool> completed)
        {
            if(m_ws == null)
            {
                completed(false);
                return;
            }
            m_ws.SendAsync(data, completed);
        }

        public void Update()
        {

        }
    }

}
