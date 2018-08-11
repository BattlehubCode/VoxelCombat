using System;

namespace Battlehub.VoxelCombat
{
    public interface IMatchServerClient
    {
        void CreateMatch(Guid creatorClientId, Room room, Guid[] clientIds, Player[] players, ReplayData replay, ServerEventHandler callback);

        void GetReplay(ServerEventHandler<ReplayData, Room> callback);

        void Update();
    }


    public class MatchServerClient : IMatchServerClient
    {
        private string m_url;
        private ILowProtocol m_protocol;
        private Action m_call;
        private Action<Error> m_response;
        private ITimeService m_time;
        private ProtobufSerializer m_serializer;

        public MatchServerClient(ITimeService time, string url, Guid roomId)
        {
            m_serializer = new ProtobufSerializer();
            m_time = time;
            m_url = string.Format("{0}?roomId={1}&identity={2}&cmd=", url, roomId, ServerContainer.ServerIdentity);
        }

        private void OnError(ILowProtocol sender, SocketErrorArgs args)
        {
            m_protocol.Enabled -= OnEnabled;
            m_protocol.SocketError -= OnError;
            m_protocol.Disabled -= OnDisabled;
            m_protocol.Dispose();

            if (m_response != null)
            {
                m_response(new Error(StatusCode.ConnectionError) { Message = args.Message });
            }

            m_protocol = null;
            m_call = null;
            m_response = null;
        }

        private void OnEnabled(ILowProtocol sender)
        {
            m_protocol.Enabled -= OnEnabled;

            m_call();
            m_call = null;
        }

        private void OnDisabled(ILowProtocol sender)
        {
            m_protocol.Enabled -= OnEnabled;
            m_protocol.SocketError -= OnError;
            m_protocol.Disabled -= OnDisabled;
            m_protocol.Dispose();

            if (m_response != null)
            {
                m_response(null);
            }

            m_protocol = null;
            m_call = null;
            m_response = null;
        }

        public void IsAlive(ServerEventHandler callback)
        {
            if (m_protocol != null)
            {
                throw new InvalidOperationException();
            }

            LowProtocol<ClientSocket> protocol = new LowProtocol<ClientSocket>(m_url, m_time.Time);
            m_protocol = protocol;
            m_protocol.Enabled += OnEnabled;
            m_protocol.SocketError += OnError;
            m_protocol.Disabled += OnDisabled;

            m_response = externalError =>
            {
                callback(externalError);
            };

            m_call = () =>
            {
                RemoteCall rpc = new RemoteCall(RemoteCall.Proc.IsAliveCheck, ServerContainer.ServerIdentity);

                Call(rpc, (error, remoteResult) =>
                {
                    m_response = externalError =>
                    {
                        if (externalError != null)
                        {
                            callback(externalError);
                        }
                        else
                        {
                            callback(error);
                        }
                    };

                    m_protocol.Disable();
                });
            };

            m_protocol.Enable();
        }

        public void CreateMatch(Guid creatorClientId, Room room, Guid[] clientIds, Player[] players, ReplayData replay, ServerEventHandler callback)
        {
            if (m_protocol != null)
            {
                throw new InvalidOperationException();
            }

            LowProtocol<ClientSocket> protocol = new LowProtocol<ClientSocket>(m_url + "create", m_time.Time);
            m_protocol = protocol;
            m_protocol.Enabled += OnEnabled;
            m_protocol.SocketError += OnError;
            m_protocol.Disabled += OnDisabled;

            m_response = externalError =>
            {
                callback(externalError);
            };

            m_call = () =>
            {
                RemoteCall rpc = new RemoteCall(RemoteCall.Proc.CreateMatch, ServerContainer.ServerIdentity, RemoteArg.Create(creatorClientId), RemoteArg.Create(room), RemoteArg.Create(clientIds), RemoteArg.Create(players), RemoteArg.Create(replay));

                Call(rpc, (error, remoteResult) =>
                {
                    m_response = externalError =>
                    {
                        if (externalError != null)
                        {
                            callback(externalError);
                        }
                        else
                        {
                            callback(error);
                        }
                    };

                    m_protocol.Disable();
                });
            };

            m_protocol.Enable();
        }

        public void GetReplay(ServerEventHandler<ReplayData, Room> callback)
        {
            if (m_protocol != null)
            {
                throw new InvalidOperationException();
            }

            LowProtocol<ClientSocket> protocol = new LowProtocol<ClientSocket>(m_url, m_time.Time);
            m_protocol = protocol;
            m_protocol.Enabled += OnEnabled;
            m_protocol.SocketError += OnError;
            m_protocol.Disabled += OnDisabled;

            m_response = externalError =>
            {
                callback(externalError, null, null);
            };

            m_call = () =>
            {
                RemoteCall rpc = new RemoteCall(RemoteCall.Proc.GetReplay, ServerContainer.ServerIdentity);

                Call(rpc, (error, remoteResult) =>
                {
                    m_response = externalError =>
                    {
                        if (externalError != null)
                        {
                            callback(externalError, null, null);
                        }
                        else
                        {
                            callback(error, remoteResult.Get<ReplayData>(0), remoteResult.Get<Room>(1));
                        }
                    };

                    m_protocol.Disable();
                });
            };

            m_protocol.Enable();
        }


        private void Call(RemoteCall rpc, Action<Error, RemoteResult> callback)
        {
            byte[] rpcSerialized = m_serializer.Serialize(rpc);
            m_protocol.BeginRequest(rpcSerialized, null, (requestError, response, userState) =>
            {
                RemoteResult result;
                Error error;
                if (requestError == LowProtocol<ClientSocket>.ErrorOK)
                {
                    try
                    {
                        result = m_serializer.Deserialize<RemoteResult>(response);
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
                    if (requestError == LowProtocol<ClientSocket>.ErrorTimeout)
                    {
                        error = new Error(StatusCode.RequestTimeout);
                    }
                    else if (requestError == LowProtocol<ClientSocket>.ErrorClosed)
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

        public void Update()
        {
            m_protocol.UpdateTime(m_time.Time);
        }
    }
}
