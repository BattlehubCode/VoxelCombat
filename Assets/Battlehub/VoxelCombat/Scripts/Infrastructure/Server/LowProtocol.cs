using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public delegate void SocketEvent(ISocket sender);
    public delegate void SocketEvent<T>(ISocket sender, T data);
    public class SocketErrorArgs
    {
        public string Message;
        public Exception Exception;

        public SocketErrorArgs(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }
    }

    public interface ISocket
    {
        event SocketEvent Opened;
        event SocketEvent Closed;
        event SocketEvent<SocketErrorArgs> Error;
        event SocketEvent<byte[]> Message;

        bool IsOpening
        {
            get;
        }

        bool IsOpened
        {
            get;
        }

        void Open(string serverUrl);

        void Close();

        void Send(byte[] data, Action<bool> completed);

        void Update();
    }

    public delegate void ProtocolEventHandler(ILowProtocol sender);
    public delegate void ProtocolEventHandler<TArgs>(ILowProtocol sender, TArgs args);

    public class LowRequestArgs
    {
        public int Id;
        public byte[] Data;

        public LowRequestArgs(int id, byte[] data)
        {
            Id = id;
            Data = data;
        }
    }

    public interface ILowProtocol
    {
        event ProtocolEventHandler Enabled;
        event ProtocolEventHandler Disabled;
        event ProtocolEventHandler<LowRequestArgs> Request;
        event ProtocolEventHandler<byte[]> Message;
        event ProtocolEventHandler<SocketErrorArgs> SocketError;

        bool IsEnabled
        {
            get;
        }


        int BeginRequest(byte[] data, object userState,
            Action<int, byte[], object> response, Action<bool> completed); /* userState, statusCode, data */

        void CancelRequest(int requestId);
   
        void CancelRequests();
       
        void Response(int id, byte[] data, Action<bool> completed);
    
        void Send(byte[] data, Action<bool> completed);
        
        void Dispose();
       
        void Enable();

        void Disable();
       
        void UpdateTime(float time);
    }

    public class LowProtocol<T> : ILowProtocol where T : ISocket, new()
    {
        public event ProtocolEventHandler Enabled;
        public event ProtocolEventHandler Disabled;
        public event ProtocolEventHandler<LowRequestArgs> Request;
        public event ProtocolEventHandler<byte[]> Message;
        public event ProtocolEventHandler<SocketErrorArgs> SocketError;

        private readonly float m_timeout;
        private readonly string m_serverUrl;
        private ISocket m_socket;
        private float m_time;
        private int m_currentRequestId;

        private const byte MessageHeader = 0x0;
        private const byte RequestHeader = 0x1;
        private const byte ResponseHeader = 0x2;

        public const int ErrorOK = 0;
        public const int ErrorTimeout = -1;
        public const int ErrorClosed = -2;

        private class RequestData
        {
            public object UserState;
            public Action<int, byte[], object> Callback;
            
            public RequestData(object userState, Action<int, byte[], object> callback)
            {
                UserState = userState;
                Callback = callback;
            }
        }

        private class RequestTimeout
        {
            public int RequestId;
            public float T;

            public RequestTimeout(int requestId, float t)
            {
                RequestId = requestId;
                T = t;
            }
        }

        private bool m_isEnabled;
        public bool IsEnabled
        {
            get { return m_isEnabled; }
            private set { m_isEnabled = value; }
        }

        private readonly Queue<RequestTimeout> m_timeoutQueue = new Queue<RequestTimeout>();
        private readonly Dictionary<int, RequestData> m_pendingRequests = new Dictionary<int, RequestData>();
     
        public LowProtocol(string serverUrl, float time, float timeout = 30)
        {
            m_time = time;
            m_timeout = timeout;
            if(m_timeout < 0)
            {
                m_timeout = float.MaxValue; //never
            }
            m_serverUrl = serverUrl;
            m_socket = new T();
            m_socket.Opened += OnSocketOpened;
            m_socket.Closed += OnSockedClosed;
            m_socket.Error += OnSocketError;
            m_socket.Message += OnSocketMessage;
        }

        public LowProtocol(T socket)
        {
            if(!socket.IsOpened)
            {
                throw new InvalidOperationException("opened socked is expected");
            }
            m_isEnabled = true;
            m_timeout = float.MaxValue;
            
            m_socket = socket;
            m_socket.Error += OnSocketError;
            m_socket.Message += OnSocketMessage;
        }

        public void Dispose()
        {
            m_timeoutQueue.Clear();
            m_pendingRequests.Clear();

            m_socket.Opened -= OnSocketOpened;
            m_socket.Closed -= OnSockedClosed;
            m_socket.Error -= OnSocketError;
            m_socket.Message -= OnSocketMessage;
            m_socket = null;
        }

        public void Enable()
        {
            m_socket.Open(m_serverUrl);
        }

        public void Disable()
        {
            if(m_socket.IsOpening || m_socket.IsOpened)
            {
                m_socket.Close();
            }
        }

        public void UpdateTime(float time)
        {
            m_socket.Update();
            m_time = time;
            while(m_timeoutQueue.Count > 0 && m_timeoutQueue.Peek().T <= time)
            {
                RequestTimeout timeout = m_timeoutQueue.Dequeue();
                RequestData requestData;
                if(m_pendingRequests.TryGetValue(timeout.RequestId, out requestData))
                {
                    m_pendingRequests.Remove(timeout.RequestId);
                    requestData.Callback(ErrorTimeout, null, requestData.UserState);
                }
            }
        }

        private void OnSocketOpened(ISocket sender)
        {
            IsEnabled = true;

            if(Enabled != null)
            {
                Enabled(this);
            }
        }

        private void OnSockedClosed(ISocket sender)
        {
            IsEnabled = false;

            foreach(RequestData data in m_pendingRequests.Values)
            {
                data.Callback(ErrorClosed, null, data.UserState);
            }
            m_pendingRequests.Clear();
            m_timeoutQueue.Clear();

            if(Disabled != null)
            {
                Disabled(this);
            }
        }

        private void OnSocketError(ISocket sender, SocketErrorArgs errorArgs)
        {
            if (SocketError != null)
            {
                SocketError(this, errorArgs);
            }
        }

        private void OnSocketMessage(ISocket sender, byte[] message)
        {
            byte header = message[message.Length - 1];

            switch (header)
            {
                case MessageHeader:
                    {
                        if (Message != null)
                        {
                            Array.Resize(ref message, message.Length - 1);
                            Message(this, message);
                        }
                    }
                    break;
                case RequestHeader:
                    {
                        if (Request != null)
                        {
                            int headerSize = sizeof(int) + 1;
                            int requestId = ToInt32(message, message.Length - headerSize);
                            Array.Resize(ref message, message.Length - headerSize);

                            Request(this, new LowRequestArgs(requestId, message));
                        }
                    }
                    break;
                case ResponseHeader:
                    {
                        int headerSize = sizeof(int) + 1;
                        int requestId = ToInt32(message, message.Length - headerSize);
                        Array.Resize(ref message, message.Length - headerSize);

                        RequestData request;
                        if(m_pendingRequests.TryGetValue(requestId, out request))
                        {
                            m_pendingRequests.Remove(requestId);
                            request.Callback(ErrorOK, message, request.UserState);
                        }                        
                    }
                    break;
            }
        }

        public int BeginRequest(byte[] data, object userState,
            Action<int, byte[], object> response, Action<bool> completed) /* userState, statusCode, data */
        {
            m_currentRequestId++;

            m_timeoutQueue.Enqueue(new RequestTimeout(m_currentRequestId, m_time + m_timeout));
            m_pendingRequests.Add(m_currentRequestId, new RequestData(userState, response));
            
            data = EncapsulateData(m_currentRequestId, data);
            data[data.Length - 1] = RequestHeader;
            m_socket.Send(data, completed);

            return m_currentRequestId;
        }

        public void CancelRequest(int requestId)
        {
            m_pendingRequests.Remove(requestId);
        }

        public void CancelRequests()
        {
            m_pendingRequests.Clear();
            m_timeoutQueue.Clear();
        }

        public void Response(int id, byte[] data, Action<bool> completed)
        {
            data = EncapsulateData(id, data);
            data[data.Length - 1] = ResponseHeader;

            if(m_socket != null)
            {
                m_socket.Send(data, completed);
            }
            else
            {
                completed(false);
            }   
        }

        private byte[] EncapsulateData(int id, byte[] data)
        {
            byte[] idBytes = ToBytes(id);
            int endOfData = data.Length;
            Array.Resize(ref data, endOfData + sizeof(int) + 1);
            Array.Copy(idBytes, 0, data, endOfData, idBytes.Length);
            return data;
        }

        public void Send(byte[] data, Action<bool> completed)
        {
            Array.Resize(ref data, data.Length + 1);
            data[data.Length - 1] = MessageHeader;
            if(m_socket != null)
            {
                m_socket.Send(data, completed);
            }
            else
            {
                completed(false);
            }
        }

        private byte[] ToBytes(int value)
        {
            byte[] intBytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(intBytes);
            }
            return intBytes;
        }

        private int ToInt32(byte[] bytes, int startIndex)
        {
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, startIndex + 0, sizeof(int));
            }
            return BitConverter.ToInt32(bytes, startIndex + 0);
        }
    }
}

