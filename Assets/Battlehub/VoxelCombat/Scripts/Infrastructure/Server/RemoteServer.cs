using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{

    public abstract class RemoteServer : MonoBehaviour, IServer
    {
        public event ServerEventHandler ConnectionStateChanging;
        public event ServerEventHandler<ValueChangedArgs<bool>> ConnectionStateChanged;
        public event ServerEventHandler<ChatMessage> ChatMessage;

        public bool IsConnectionStateChanging { get; private set; }

        private bool m_wasConnected;
        public bool IsConnected { get { return m_protocol != null && m_protocol.IsEnabled; } }

        private ILowProtocol m_protocol;
        protected IGlobalSettings m_settings;

        protected abstract string ServerUrl
        {
            get;
        }

        protected virtual void Awake()
        {  
            m_settings = Dependencies.Settings;

            m_wasConnected = false;

            m_protocol = new LowProtocol<Socket>(ServerUrl, Time.time);
            m_protocol.Enabled += OnEnabled;
            m_protocol.Disabled += OnDisabled;
            m_protocol.SocketError += OnSocketError;
            m_protocol.Message += OnMessage;
        }

        public void Connect()
        {
            m_wasConnected = IsConnected;

            IsConnectionStateChanging = true;

            if (ConnectionStateChanging != null)
            {
                ConnectionStateChanging(new Error(StatusCode.OK));
            }
            m_protocol.Enable();
        }

        public void Disconnect()
        {
            m_wasConnected = IsConnected;

            if (m_protocol != null)
            {
                IsConnectionStateChanging = true;

                if (ConnectionStateChanging != null)
                {
                    ConnectionStateChanging(new Error(StatusCode.OK));
                }
                m_protocol.Disable();
            }
        }

        protected virtual void OnEnable()
        {
            Connect();
        }

        protected virtual void OnDisable()
        {
            Disconnect();
        }

       
        protected virtual void OnDestroy()
        {
            if (m_protocol != null)
            {
                m_protocol.Dispose();
                m_protocol.Enabled -= OnEnabled;
                m_protocol.Disabled -= OnDisabled;
                m_protocol.SocketError -= OnSocketError;
                m_protocol.Message -= OnMessage;
            }

          
        }

        protected virtual void Update()
        {
            m_protocol.UpdateTime(Time.time);
        }

        protected virtual void OnEnabled(ILowProtocol sender)
        {
            RegisterClient(m_settings.ClientId, error =>
            {
                IsConnectionStateChanging = false;

                if (ConnectionStateChanged != null)
                {
                    ConnectionStateChanged(error, new ValueChangedArgs<bool>(m_wasConnected, true));
                }

                m_wasConnected = true;
            });
        }

        protected virtual void OnDisabled(ILowProtocol sender)
        {
            IsConnectionStateChanging = false;

            if (ConnectionStateChanged != null)
            {
                ConnectionStateChanged(new Error(StatusCode.OK), new ValueChangedArgs<bool>(m_wasConnected, false));
            }

            m_wasConnected = false;
        }

        protected virtual void OnSocketError(ILowProtocol sender, SocketErrorArgs args)
        {
            IsConnectionStateChanging = false;

            if (ConnectionStateChanged != null)
            {
                ConnectionStateChanged(new Error(StatusCode.ConnectionError) { Message = args.Message }, new ValueChangedArgs<bool>(m_wasConnected, false));
            }

            m_wasConnected = false;
        }

        protected virtual void OnMessage(ILowProtocol sender, byte[] args)
        {
            RemoteEvent evt = ProtobufSerializer.Deserialize<RemoteEvent>(args);
            OnRemoteEvent(evt);
        }

        protected virtual void OnRemoteEvent(RemoteEvent evt)
        {
            switch (evt.Event)
            {
                case RemoteEvent.Evt.ChatMessage:
                    if (ChatMessage != null)
                    {
                        ChatMessage(evt.Error, evt.Get<ChatMessage>(0));
                    }
                    break;
            }
        }

        public bool HasError(Error error)
        {
            return error.Code != StatusCode.OK;
        }

        public void RegisterClient(Guid clientId, ServerEventHandler callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.RegisterClient,
                clientId);

            Call(rpc, (error, result) => callback(error));
        }

        public void UnregisterClient(Guid clientId, ServerEventHandler callback)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(Guid clientId, ChatMessage message, ServerEventHandler<Guid> callback)
        {
            RemoteCall rpc = new RemoteCall(
                  RemoteCall.Proc.SendChatMessage,
                  clientId,
                  RemoteArg.Create(message));

            Call(rpc, (error, result) =>
            {
                Guid messageId = result.Get<Guid>(0);
                callback(error, messageId);
            });
        }

        public void CancelRequests()
        {
            m_protocol.CancelRequests();
        }

        protected void Call(RemoteCall rpc, Action<Error, RemoteResult> callback)
        {
            RemoteCall.Proc proc = rpc.Procedure;
            byte[] rpcSerialized = ProtobufSerializer.Serialize(rpc);
            m_protocol.BeginRequest(rpcSerialized, null, (requestError, response, userState) =>
            {
                RemoteResult result;
                Error error;
                if (requestError == LowProtocol<Socket>.ErrorOK)
                {
                    try
                    {
                        result = ProtobufSerializer.Deserialize<RemoteResult>(response);
                        error = result.Error;
                    }
                    catch (Exception e)
                    {
                        result = new RemoteResult();
                        error = new Error(StatusCode.UnhandledException) { Message = string.Format("Procedure {0}", proc) + " " + e.ToString() };
                    }
                }
                else
                {
                    result = new RemoteResult();
                    if (requestError == LowProtocol<Socket>.ErrorTimeout)
                    {
                        error = new Error(StatusCode.RequestTimeout);
                        error.Message = string.Format("Request Timeout - Procedure {0}", proc);
                    }
                    else if (requestError == LowProtocol<Socket>.ErrorClosed)
                    {
                        error = new Error(StatusCode.ConnectionClosed);
                        error.Message = string.Format("Request Error - Procedure {0}", proc);
                    }
                    else
                    {
                        throw new NotSupportedException("unknow requestError");
                    }
                }
                callback(error, result);
            },
            requestSent => { if (!requestSent) { callback(new Error(StatusCode.ConnectionClosed), new RemoteResult()); } });
        }

     
    }

}
