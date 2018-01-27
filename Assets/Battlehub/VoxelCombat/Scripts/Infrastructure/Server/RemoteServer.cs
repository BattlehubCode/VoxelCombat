using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public abstract class RemoteServer : MonoBehaviour, IServer
    {
        public event ServerEventHandler<bool> ConnectionStateChanged;

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
            m_protocol = new LowProtocol<Socket>(ServerUrl);
            m_protocol.Enabled += OnEnabled;
            m_protocol.Disabled += OnDisabled;
            m_protocol.SocketError += OnSocketError;
            m_protocol.Message += OnMessage;
        }

        public void Connect()
        {
            m_protocol.Enable();
        }

        public void Disconnect()
        {
            if (m_protocol != null)
            {
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
                if (ConnectionStateChanged != null)
                {
                    ConnectionStateChanged(error, true);
                }
            });
        }

        protected virtual void OnDisabled(ILowProtocol sender)
        {
            if (ConnectionStateChanged != null)
            {
                ConnectionStateChanged(new Error(StatusCode.OK), false);
            }
        }

        protected virtual void OnSocketError(ILowProtocol sender, SocketErrorArgs args)
        {
            if (ConnectionStateChanged != null)
            {
                ConnectionStateChanged(new Error(StatusCode.UnhandledException) { Message = args.Message }, false);
            }
        }

        protected virtual void OnMessage(ILowProtocol sender, byte[] args)
        {
           
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

        public void CancelRequests()
        {
            m_protocol.CancelRequests();
        }

        protected void Call(RemoteCall rpc, Action<Error, RemoteResult> callback)
        {
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
                        error = new Error(StatusCode.UnhandledException) { Message = e.ToString() };
                    }
                }
                else
                {
                    result = new RemoteResult();
                    if (requestError == LowProtocol<Socket>.ErrorTimeout)
                    {
                        error = new Error(StatusCode.RequestTimeout);
                    }
                    else if (requestError == LowProtocol<Socket>.ErrorClosed)
                    {
                        error = new Error(StatusCode.ConnectionClosed);
                    }
                    else
                    {
                        throw new NotSupportedException("unknow requestError");
                    }
                }
                callback(error, result);
            },
            requestSent => { });
        }
    }

}
