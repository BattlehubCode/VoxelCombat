using log4net;
using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Web.WebSockets;
using AsyncTask = System.Threading.Tasks.Task;
namespace Battlehub.VoxelCombat
{
    public class WebSocketHandler : ISocket
    {
        protected readonly ILog Log;

        public bool IsOpened
        {
            get { return true; }
        }

        public bool IsOpening
        {
            get { return false; }
        }

        public event SocketEvent Closed;
        public event SocketEvent<SocketErrorArgs> Error;
        public event SocketEvent<byte[]> Message;
        public event SocketEvent Opened;

        private bool m_wasClosed;
        private WebSocket m_webSocket;
        private LowProtocol<WebSocketHandler> m_protocol;
        private readonly MemoryStream m_memoryStream = new MemoryStream();
        protected readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);

        public WebSocketHandler()
        {
            Log = LogManager.GetLogger(GetType());
        }

        public void Open(string serverUrl)
        {
            throw new InvalidOperationException("Open method is not allowed on server");
        }

        public void Send(byte[] data, Action<bool> completed)
        {
            SendAsync(data, completed);
        }

        public async void Close()
        {
            await CloseAsync();
        }

        private async AsyncTask CloseAsync()
        {
            await m_semaphore.WaitAsync();
            try
            {
                if (!m_wasClosed)
                {
                    try
                    {
                        await m_webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    catch (WebSocketException exc)
                    {
                        Log.Error(exc.Message, exc);
                    }

                    m_memoryStream.SetLength(0);

                    m_wasClosed = true;

                    if (Closed != null)
                    {
                        Closed(this);
                    }

                    UnregisterConnection(m_protocol);
                    m_protocol.Dispose();
                }
            }
            catch(Exception e)
            {
                Log.Error(e.Message, e);

                if (Error != null)
                {
                    Error(this, new SocketErrorArgs(e.Message, e));
                }
            }
            finally
            {
                m_semaphore.Release();
            }
        }

        private async void SendAsync(byte[] data, Action<bool> completed)
        {
            await m_semaphore.WaitAsync();
            try
            {
                var cancellationToken = new CancellationToken();

                if (m_webSocket.State == WebSocketState.Open && (m_webSocket.CloseStatus == null || m_webSocket.CloseStatus == WebSocketCloseStatus.Empty))
                {
                    await m_webSocket.SendAsync(new ArraySegment<byte>(data),
                        WebSocketMessageType.Binary, true, cancellationToken);
                    if (completed != null)
                    {
                        completed(true);
                    }
                }
                else
                {
                    Log.Warn("Unable to SendAsync : " + m_webSocket.State + " " + m_webSocket.CloseStatus);
                    if (completed != null)
                    {
                        completed(false);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);

                if (Error != null)
                {
                    Error(this, new SocketErrorArgs(e.Message, e));
                }

                if (completed != null)
                {
                    completed(false);
                }
            }
            finally
            {
                m_semaphore.Release();
            }
        }

        protected virtual void RegisterConnection(ILowProtocol client) { }
        protected virtual void UnregisterConnection(ILowProtocol client) { }

        public async AsyncTask WebSocketRequestHandler(AspNetWebSocketContext webSocketContext)
        {
            m_webSocket = webSocketContext.WebSocket;
            try
            {
                await WebSocketRequestHandlerImpl(webSocketContext);
            }
            catch(Exception e)
            {
                if (Error != null)
                {
                    Error(this, new SocketErrorArgs(e.Message, e));
                }

                Log.Error("Unhandled exception " + e.ToString(), e);
            }
        }

        public async AsyncTask WebSocketRequestHandlerImpl(AspNetWebSocketContext webSocketContext)
        {
            m_protocol = new LowProtocol<WebSocketHandler>(this);

            RegisterConnection(m_protocol);

            const int maxMessageSize = 1024 * 10;

            //Buffer for received bits.
            ArraySegment<byte> receiveDataBuffer = new ArraySegment<byte>(new byte[maxMessageSize]);

            CancellationToken cancellationToken = CancellationToken.None;

            //Checks WebSocket state.
            while (m_webSocket.State == WebSocketState.Open)
            {
                //This operation will not block.The returned T: System.Threading.Tasks.Task`1 object will complete after the data has been received on the T:System.Net.WebSockets.WebSocket.
                //Exactly one send and one receive is supported on each T: System.Net.WebSockets.WebSocket object in parallel.
                //Reads data.
                WebSocketReceiveResult result =
                   await m_webSocket.ReceiveAsync(receiveDataBuffer, cancellationToken);

                //If input frame is cancelation frame, send close command.
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (m_webSocket.State == WebSocketState.CloseReceived)
                    {
                        await CloseAsync();
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    if (Message == null)
                    {
                        continue;
                    }

                    if (result.EndOfMessage)
                    {
                        if (m_memoryStream.Length == 0)
                        {
                            byte[] data = new byte[result.Count];
                            Array.Copy(receiveDataBuffer.Array, receiveDataBuffer.Offset, data, 0, result.Count);
                            Message(this, data);
                        }
                        else
                        {
                            byte[] data = m_memoryStream.ToArray();
                            m_memoryStream.SetLength(0);
                            Message(this, data);
                        }
                    }
                    else
                    {
                        m_memoryStream.Write(receiveDataBuffer.Array, receiveDataBuffer.Offset, result.Count);
                    }
                }
            }
        }
    }
}