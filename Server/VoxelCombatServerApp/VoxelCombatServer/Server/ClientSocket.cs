using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;

namespace Battlehub.VoxelCombat
{
    public class ClientSocket : ISocket
    {
        private bool m_isOpened;
        public bool IsOpened
        {
            get { return m_isOpened; }
        }

        private bool m_isOpening;
        public bool IsOpening
        {
            get { return m_isOpening; }
        }

        public event SocketEvent Closed;
        public event SocketEvent<SocketErrorArgs> Error;
        public event SocketEvent<byte[]> Message;
        public event SocketEvent Opened;

        private readonly MemoryStream m_memoryStream = new MemoryStream();
        protected readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);
        private Thread m_socketThread;

        private ClientWebSocket m_clientSocket;
        public ClientSocket()
        {
            m_clientSocket = new ClientWebSocket();
        }

        public async void Close()
        {
            await CloseAsync();
        }

        private async System.Threading.Tasks.Task CloseAsync()
        {
            await m_semaphore.WaitAsync();
            try
            {
                if(!m_isOpening && !m_isOpened)
                {
                    return;
                }

                m_isOpening = false;
                m_isOpened = false;

                try
                {
                    await m_clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);

                    Dispatch(() =>
                    {
                        if (Closed != null)
                        {
                           Closed(this);
                        }
                    });
                }
                catch (WebSocketException exc)
                {
                    Dispatch(() =>
                    {
                        Error(this, new SocketErrorArgs(exc.ToString(), exc));
                    });
                }

                m_memoryStream.SetLength(0);

            }
            catch (Exception e)
            {
                Dispatch(() =>
                {
                    Error(this, new SocketErrorArgs(e.ToString(), e));
                });
            }
            finally
            {
                m_semaphore.Release();
            }
        }

        public async void Open(string serverUrl)
        {
            await m_semaphore.WaitAsync();
            if (m_isOpening || m_isOpened)
            {
                m_semaphore.Release();
                return;
            }

            m_isOpening = true;
            m_semaphore.Release();

            m_socketThread = new Thread(SocketThread);
            m_socketThread.Start(serverUrl);
        }

        private async void SocketThread(object param)
        {
            string serverUrl = (string)param;
            await m_semaphore.WaitAsync();
            try
            {
                if (!m_isOpening)
                {
                    return;
                }

                await m_clientSocket.ConnectAsync(new Uri(serverUrl), CancellationToken.None);
                m_isOpened = true;

                Dispatch(() =>
                {
                    if (Opened != null)
                    {
                        Opened(this);
                    }
                });

                m_isOpening = false;
            }
            catch (Exception e)
            {
                 m_isOpening = false;

                Dispatch(() =>
                {
                    if(Error != null)
                    {
                        Error(this, new SocketErrorArgs(e.ToString(), e));
                    }
                });
                

                return;
            }
            finally
            {
                m_semaphore.Release();
            }

            try
            {
                const int maxMessageSize = 1024 * 10;

                //Buffer for received bits.
                ArraySegment<byte> receiveDataBuffer = new ArraySegment<byte>(new byte[maxMessageSize]);

                CancellationToken cancellationToken = CancellationToken.None;

                //Checks WebSocket state.
                while (m_clientSocket.State == WebSocketState.Open)
                { 
                    //This operation will not block.The returned T: System.Threading.Tasks.Task`1 object will complete after the data has been received on the T:System.Net.WebSockets.WebSocket.
                    //Exactly one send and one receive is supported on each T: System.Net.WebSockets.WebSocket object in parallel.
                    //Reads data.
                    WebSocketReceiveResult result = await m_clientSocket.ReceiveAsync(receiveDataBuffer, cancellationToken);

                    //If input frame is cancelation frame, send close command.
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (m_clientSocket.State == WebSocketState.CloseReceived)
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
                                Dispatch(() => Message(this, data));
                            }
                            else
                            {
                                byte[] data = m_memoryStream.ToArray();
                                m_memoryStream.SetLength(0);

                                Dispatch(() => Message(this, data));
                            }
                        }
                        else
                        {
                            m_memoryStream.Write(receiveDataBuffer.Array, receiveDataBuffer.Offset, result.Count);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Dispatch(() =>
                {
                    if (Error != null)
                    {
                        Error(this, new SocketErrorArgs(e.ToString(), e));
                    }
                });
            }
        }


        public async void Send(byte[] data, Action<bool> completed)
        {
            await m_semaphore.WaitAsync();
            try
            {
                await m_clientSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
                completed(true);
            }
            catch(Exception e)
            {
                completed(false);
                Dispatch(() =>
                {
                    if (Error != null)
                    {
                        Error(this, new SocketErrorArgs(e.ToString(), e));
                    }
                });
            }
            finally
            {
                m_semaphore.Release();
            }
        }


        private readonly Queue<Action> m_dispatchedActions = new Queue<Action>();
        private void Dispatch(Action action)
        {
            lock(m_dispatchedActions)
            {
                m_dispatchedActions.Enqueue(action);
            }
        }

        public void Update()
        {
            lock(m_dispatchedActions)
            {
                while (m_dispatchedActions.Count > 0)
                {
                    Action action = m_dispatchedActions.Dequeue();
                    action();
                }
            }
        }
    }
}
