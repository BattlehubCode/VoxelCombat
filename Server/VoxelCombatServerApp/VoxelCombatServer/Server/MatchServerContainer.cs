using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Battlehub.VoxelCombat
{
    public interface IMatchServerClient
    {
        void CreateMatch(Room room,  Guid[] clientIds, Player[] players, ReplayData replay, ServerEventHandler callback);

        void GetReplay(ServerEventHandler<ReplayData, Room> callback);

        void Update(float time);
    }


    public class MatchServerClient : IMatchServerClient
    {
        private string m_url;
        private ILowProtocol m_protocol;
        private Action m_call;
        private Action<Error> m_response;

        public MatchServerClient(string url, Guid roomId)
        {
            m_url = string.Format("{0}?roomId={1}&identity={2}&cmd=", url, roomId, ServerContainer.ServerIdentity);
        }

        private void OnError(ILowProtocol sender, SocketErrorArgs args)
        {
            m_protocol.Enabled -= OnEnabled;
            m_protocol.SocketError -= OnError;
            m_protocol.Disabled -= OnDisabled;
            m_protocol.Dispose();

            if(m_response != null)
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

        public void CreateMatch(Room room, Guid[] clientIds, Player[] players, ReplayData replay, ServerEventHandler callback)
        {
            if(m_protocol != null)
            {
                throw new InvalidOperationException();
            }
 
            LowProtocol<ClientSocket> protocol = new LowProtocol<ClientSocket>(m_url + "create");
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
                RemoteCall rpc = new RemoteCall(RemoteCall.Proc.CreateMatch, ServerContainer.ServerIdentity, RemoteArg.Create(room), RemoteArg.Create(clientIds), RemoteArg.Create(players), RemoteArg.Create(replay));

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

            LowProtocol<ClientSocket> protocol = new LowProtocol<ClientSocket>(m_url);
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
            byte[] rpcSerialized = ProtobufSerializer.Serialize(rpc);
            m_protocol.BeginRequest(rpcSerialized, null, (requestError, response, userState) =>
            {
                RemoteResult result;
                Error error;
                if (requestError == LowProtocol<ClientSocket>.ErrorOK)
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

        public void Update(float time)
        {
            m_protocol.UpdateTime(time);    
        } 
    }

    public class MatchServerContainer : ServerContainer
    {
        private IMatchServer m_matchServer;
        private ILoop m_gameLoop;
        private string m_path;

        public MatchServerContainer()
        {
            m_path = HttpContext.Current.Server.MapPath("/Data");
        }

        protected override void OnRegisterClientSafe(ILowProtocol protocol, Guid clientId)
        {
            base.OnRegisterClientSafe(protocol, clientId);
            if(m_matchServer != null)
            {
                m_matchServer.RegisterClient(clientId, error =>
                {
                    if(m_matchServer.HasError(error))
                    {
                        Log.Error("m_matchServer.RegisterClient. This method should never fail but it does.. " + error.ToString());
                    };
                });
            }
        }

        protected override void OnUnregisterClientSafe(ILowProtocol protocol, Guid clientId)
        {
            base.OnUnregisterClientSafe(protocol, clientId);
            if(m_matchServer != null)
            {
                m_matchServer.UnregisterClient(clientId, error =>
                {
                    if (m_matchServer.HasError(error))
                    {
                        Log.Error("m_matchServer.UnregisterClient. This method should never fail but it does.. " + error.ToString());
                    };
                });
            }
        }

        protected override void OnBeforeRun()
        {
            base.OnBeforeRun();
        }

        private void OnPing(Error error, ServerEventArgs<RTTInfo> args)
        {
            Broadcast(RemoteEvent.Evt.Ping, error, args, RemoteArg.Create(args.Arg));
        }

        private void OnPaused(Error error, ServerEventArgs<bool> args)
        {
            Broadcast(RemoteEvent.Evt.Pause, error, args, RemoteArg.Create(args.Arg));
        }

        private void OnReadyToPlayAll(Error error, ServerEventArgs<Player[], Dictionary<Guid, Dictionary<Guid, Player>>, VoxelAbilitiesArray[], Room> args)
        {
            Dictionary<Guid, Dictionary<Guid, Player>> clientIdToPlayers = args.Arg2;
            foreach(KeyValuePair<Guid, Dictionary<Guid, Player>> kvp in clientIdToPlayers)
            {
                Guid clientId = kvp.Key;
                Dictionary<Guid, Player> players = kvp.Value;

                Send(RemoteEvent.Evt.ReadyToPlayAll, error, clientId,
                    RemoteArg.Create(args.Arg),
                    RemoteArg.Create(players.Keys.ToArray()),
                    RemoteArg.Create(args.Arg3),
                    RemoteArg.Create(args.Arg4));

            }
        }

        private RemoteEvent m_tickEvent = new RemoteEvent() { Event = RemoteEvent.Evt.Tick, Args = new[] { new RemoteArg<CommandsBundle>() } };
        private void OnTick(Error error,  ServerEventArgs<CommandsBundle> args)
        {
            m_tickEvent.Error = error;
            m_tickEvent.Args[0].Value = args.Arg;
            byte[] result = ProtobufSerializer.Serialize(m_tickEvent);
            BroadcastAll(result);
        }

        protected override void OnAfterStop()
        {
            base.OnAfterStop();

            if(m_matchServer != null)
            {
                m_matchServer.Tick -= OnTick;
                m_matchServer.ReadyToPlayAll -= OnReadyToPlayAll;
                m_matchServer.Paused -= OnPaused;
                m_matchServer.Ping -= OnPing;

                m_matchServer = null;
            }

            if(m_gameLoop != null)
            {
                m_gameLoop.Destroy();
                m_gameLoop = null;
            }
        }

        protected override void OnTick(TimeSpan elapsed)
        {
            base.OnTick(elapsed);
            if(m_gameLoop != null)
            {
                m_gameLoop.Update((float)elapsed.TotalSeconds);
            }
            else
            {
                if(m_matchServer != null)
                {
                    const int WaitSeconds = 5;
                    if (elapsed.TotalSeconds > WaitSeconds)
                    {
                        m_gameLoop = (ILoop)m_matchServer;
                        m_gameLoop.Start();
                    }
                }
            }
        }

        protected override void OnMessage(ILowProtocol sender, byte[] message)
        {
            
        }

        protected override void OnRequest(ILowProtocol sender, LowRequestArgs request)
        {
            RemoteCall rpc;
            try
            {
                rpc = ProtobufSerializer.Deserialize<RemoteCall>(request.Data);
            }
            catch (Exception e)
            {
                Log.Error("Invalid RemoteCall format ", e);

                #warning This code is not tested
                sender.Disable();

                throw;
            }

            switch (rpc.Procedure)
            {
                case RemoteCall.Proc.RegisterClient:
                    {
                        RegisterClient(sender, rpc.ClientId);
                        Return(sender, request, new Error(StatusCode.OK));
                    }
                    break;
                case RemoteCall.Proc.CreateMatch:
                    {
                        if (rpc.ClientId != ServerIdentity)
                        {
                            Return(sender, request, new Error(StatusCode.NotAuthorized));
                        }
                        else
                        {
                            Room room = rpc.Get<Room>(0);
                            Guid[] clientIds = rpc.Get<Guid[]>(1);
                            Player[] players = rpc.Get<Player[]>(2);
                            ReplayData replay = rpc.Get<ReplayData>(3);

                            if (m_matchServer == null) 
                            {
                                MatchServerImpl matchServer = new MatchServerImpl(m_path, room, clientIds, players, replay);
                                m_matchServer = matchServer;
                              
                                m_matchServer.Tick += OnTick;
                                m_matchServer.ReadyToPlayAll += OnReadyToPlayAll;
                                m_matchServer.Paused += OnPaused;
                                m_matchServer.Ping += OnPing;
                            }

                            Return(sender, request, new Error(StatusCode.OK));
                        }
                    }
                    break;
                case RemoteCall.Proc.GetReplay:
                 
                    m_matchServer.GetReplay(rpc.ClientId, (error, replayData, room) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(replayData), RemoteArg.Create(room));
                    });
                    break;
                case RemoteCall.Proc.DownloadMapData:
                    m_matchServer.DownloadMapData(rpc.ClientId, (Error error, byte[] data) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(data));
                    });
                    break;
                case RemoteCall.Proc.ReadyToPlay:
                    m_matchServer.ReadyToPlay(rpc.ClientId, error =>
                    {
                        Return(sender, request, error);
                    });
                    break;
                case RemoteCall.Proc.Submit:
                    m_matchServer.Submit(rpc.ClientId, rpc.Get<Guid>(0), rpc.Get<Cmd>(1), error =>
                    {
                        Return(sender, request, error);
                    });
                    break;
                case RemoteCall.Proc.Pong:
                    m_matchServer.Pong(rpc.ClientId, error =>
                    {
                        Return(sender, request, error);
                    });
                    break;
                case RemoteCall.Proc.Pause:
                    m_matchServer.Pause(rpc.ClientId, rpc.Get<bool>(0), error =>
                    {
                        Return(sender, request, error);
                    });
                    break;
            }
        }

       
    }
}
