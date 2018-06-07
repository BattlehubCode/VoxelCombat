using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Battlehub.VoxelCombat
{
    public class MatchServerContainer : ServerContainer, IMatchServerContainerDiagnostics
    {
        private IMatchServer m_matchServer;
        private ILoop m_gameLoop;
        private string m_path;

        public MatchServerContainer()
        {
            m_path = HttpContext.Current.Server.MapPath("/Data");
            Log.Info("Match Server data path " +  m_path);
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

        private void OnChatMessage(Error error, ServerEventArgs<ChatMessage> args)
        {
            Broadcast(RemoteEvent.Evt.ChatMessage, error, args, RemoteArg.Create(args.Arg));
        }

        private void OnReadyToPlayAll(Error error, ServerEventArgs<Player[], Dictionary<Guid, Dictionary<Guid, Player>>, VoxelAbilitiesArray[], Room> args)
        {
            Room room = args.Arg4;
            if (room.Mode == GameMode.Replay)
            {
                Send(RemoteEvent.Evt.ReadyToPlayAll, error, room.CreatorClientId,
                        RemoteArg.Create(args.Arg),
                        RemoteArg.Create(new Guid[0]),
                        RemoteArg.Create(args.Arg3),
                        RemoteArg.Create(args.Arg4));
            }
            else
            {
                Dictionary<Guid, Dictionary<Guid, Player>> clientIdToPlayers = args.Arg2;
                foreach (KeyValuePair<Guid, Dictionary<Guid, Player>> kvp in clientIdToPlayers)
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
                m_matchServer.ChatMessage -= OnChatMessage;
                m_matchServer = null;
            }

            if(m_gameLoop != null)
            {
                m_gameLoop.Destroy();
                m_gameLoop = null;
            }
        }

        protected override void OnTick()
        {
            base.OnTick();

            if(m_gameLoop != null)
            {
                m_gameLoop.Update();
            }
            else
            {
                if(m_matchServer != null)
                {
                    const int WaitSeconds = 5;
                    if (Time > WaitSeconds)
                    {
                        m_gameLoop = (ILoop)m_matchServer;
                        if(!m_gameLoop.Start(this))
                        {
                            m_gameLoop = null;
                        }
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
                            Guid creatorClientId = rpc.Get<Guid>(0);
                            Room room = rpc.Get<Room>(1);
                            Guid[] clientIds = rpc.Get<Guid[]>(2);
                            Player[] players = rpc.Get<Player[]>(3);
                            ReplayData replay = rpc.Get<ReplayData>(4);

                            if (m_matchServer == null) 
                            {
                                MatchServerImpl matchServer = new MatchServerImpl(this, m_path, creatorClientId, room, clientIds, players, replay);
                                m_matchServer = matchServer;
                              
                                m_matchServer.Tick += OnTick;
                                m_matchServer.ReadyToPlayAll += OnReadyToPlayAll;
                                m_matchServer.Paused += OnPaused;
                                m_matchServer.Ping += OnPing;
                                m_matchServer.ChatMessage += OnChatMessage;
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
                    m_matchServer.Submit(rpc.ClientId, rpc.Get<int>(0), rpc.Get<Cmd>(1), (error, returnedCommand) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(returnedCommand));
                    });
                    break;
                case RemoteCall.Proc.SubmitResponse:
                    m_matchServer.SubmitResponse(rpc.ClientId, rpc.Get<ClientRequest>(0), (error, response) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(response));
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
                case RemoteCall.Proc.IsAliveCheck:
                    Return(sender, request, new Error(StatusCode.OK));
                    break;
                case RemoteCall.Proc.SendChatMessage:
                    m_matchServer.SendMessage(rpc.ClientId, rpc.Get<ChatMessage>(0), (error, messageId) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(messageId));
                    });
                    break;
            }
        }


        private MatchServerDiagInfo m_matchServerDiagInfo;

        protected override void UpdateDiagInfo()
        {
            base.UpdateDiagInfo();

            IMatchServerDiagnostics diag = (IMatchServerDiagnostics)m_matchServer;
            if(diag != null)
            {
                m_matchServerDiagInfo = diag.GetDiagInfo();
            }
        }

        public ContainerDiagInfo GetContainerDiagInfo()
        {
            return DiagInfo;
        }

        public MatchServerDiagInfo GetDiagInfo()
        {
            return m_matchServerDiagInfo;
        }
    }
}
