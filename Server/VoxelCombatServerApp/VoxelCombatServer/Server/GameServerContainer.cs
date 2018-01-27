using System;
using System.Configuration;
using System.Web;

namespace Battlehub.VoxelCombat
{
    public class GameServerContainer : ServerContainer
    {
        private static GameServerContainer m_instance;
        public static GameServerContainer Instance
        {
            get { return m_instance; }
        }

        static GameServerContainer()
        {
            m_instance = new GameServerContainer();
        }

        private IGameServer m_gameServer;
        private string m_path;
        private string m_matchServerUrl;

        private GameServerContainer()
        {
            m_path = HttpContext.Current.Server.MapPath("/Data");
            m_matchServerUrl = ConfigurationManager.AppSettings["MatchServerUrl"];
        }

        protected override void OnBeforeRun()
        {
            base.OnBeforeRun();

            m_gameServer = new GameServerImpl(m_path, m_matchServerUrl);
            m_gameServer.LoggedIn += OnLoggedIn;
            m_gameServer.LoggedOff += OnLoggedOff;
            m_gameServer.RoomsListChanged += OnRoomsListChanged;
            m_gameServer.JoinedRoom += OnJoinRoom;
            m_gameServer.LeftRoom += OnLeftRoom;
            m_gameServer.RoomDestroyed += OnRoomDestoryed;
            m_gameServer.ReadyToLaunch += OnReadyToLaunch;
            m_gameServer.Launched += OnLaunched;
        }

        protected override void OnAfterStop()
        {
            base.OnAfterStop();

            ((ILoop)m_gameServer).Destroy();

            m_gameServer.LoggedIn -= OnLoggedIn;
            m_gameServer.LoggedOff -= OnLoggedOff;
            m_gameServer.RoomsListChanged -= OnRoomsListChanged;
            m_gameServer.JoinedRoom -= OnJoinRoom;
            m_gameServer.LeftRoom -= OnLeftRoom;
            m_gameServer.RoomDestroyed -= OnRoomDestoryed;
            m_gameServer.ReadyToLaunch -= OnReadyToLaunch;
            m_gameServer.Launched -= OnLaunched;
            m_gameServer = null;
        }

        protected override void OnMessage(ILowProtocol sender, byte[] message)
        {
            Log.Error("Unknow message");
        }

        private void OnLoggedIn(Error error, ServerEventArgs args)
        {
            Broadcast(RemoteEvent.Evt.LoggedIn, error, args);
        }

        private void OnLoggedOff(Error error, ServerEventArgs<Guid[]> args)
        {
            Broadcast(RemoteEvent.Evt.LoggedOff, error, args, RemoteArg.Create(args.Arg));
        }

        private void OnRoomsListChanged(Error error, ServerEventArgs args)
        {
            Broadcast(RemoteEvent.Evt.RoomsListChanged, error, args);
        }

        private void OnRoomDestoryed(Error error, ServerEventArgs args)
        {
            Broadcast(RemoteEvent.Evt.RoomDestroyed, error, args);
        }

        private void OnJoinRoom(Error error, ServerEventArgs<Guid[], Room> args)
        {
            Broadcast(RemoteEvent.Evt.JoinedRoom, error, args, RemoteArg.Create(args.Arg), RemoteArg.Create(args.Arg2));
        }

        private void OnLeftRoom(Error error, ServerEventArgs<Guid[], Room> args)
        {
            Broadcast(RemoteEvent.Evt.LeftRoom, error, args, RemoteArg.Create(args.Arg), RemoteArg.Create(args.Arg2));
        }

        private void OnReadyToLaunch(Error error, ServerEventArgs<Room> args)
        {
            Broadcast(RemoteEvent.Evt.ReadyToLaunch, error, args, RemoteArg.Create(args.Arg));
        }

        private void OnLaunched(Error error, ServerEventArgs<string> args)
        {
            Broadcast(RemoteEvent.Evt.Launched, error, args, RemoteArg.Create(args.Arg));
        }

        protected override void OnRequest(ILowProtocol sender, LowRequestArgs request)
        {
            RemoteCall rpc;
            try
            {
                rpc = ProtobufSerializer.Deserialize<RemoteCall>(request.Data);
            }
            catch(Exception e)
            {
                Log.Error("Invalid RemoteCall format ", e);

                //#warning Should force disconnect client
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
                case RemoteCall.Proc.GetPlayers:
                    m_gameServer.GetPlayers(rpc.ClientId, (error, players) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(players));
                    });
                    break;
                case RemoteCall.Proc.GetPlayersByRoomId:
                    m_gameServer.GetPlayers(rpc.ClientId, rpc.Get<Guid>(0), (error, players) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(players));
                    });
                    break;
                case RemoteCall.Proc.GetPlayer:
                    m_gameServer.GetPlayer(rpc.ClientId, rpc.Get<Guid>(0), (error, players) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(players));
                    });
                    break;
                case RemoteCall.Proc.Login:
                    m_gameServer.Login(rpc.Get<string>(0), rpc.Get<string>(1), rpc.ClientId, (error, playerId) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(playerId));
                    });
                    break;
                case RemoteCall.Proc.SignUp:
                    m_gameServer.SignUp(rpc.Get<string>(0), rpc.Get<string>(1), rpc.ClientId, (error, playerId) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(playerId));
                    });
                    break;
                case RemoteCall.Proc.Logoff:
                    m_gameServer.Logoff(rpc.ClientId, rpc.Get<Guid>(0), (error, guid) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(guid));
                    });
                    break;
                case RemoteCall.Proc.LogoffMultiple:
                    m_gameServer.Logoff(rpc.ClientId, rpc.Get<Guid[]>(0), (error, guids) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(guids));
                    });
                    break;
                case RemoteCall.Proc.JoinRoom:
                    m_gameServer.JoinRoom(rpc.ClientId, rpc.Get<Guid>(0), (error, room) =>
                    {
                        //Boradcast to room players
                        Return(sender, request, error, RemoteArg.Create(room));
                    });
                    break;
                case RemoteCall.Proc.LeaveRoom:
                    m_gameServer.LeaveRoom(rpc.ClientId, (error) =>
                    {
                        //Brodcast to room players
                        Return(sender, request, error);
                    });
                    break;
                case RemoteCall.Proc.GetRooms:
                    m_gameServer.GetRooms(rpc.ClientId, rpc.Get<int>(0), rpc.Get<int>(1), (error, rooms) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(rooms));
                    });
                    break;
                case RemoteCall.Proc.GetRoom:
                    m_gameServer.GetRoom(rpc.ClientId, (error, room) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(room));
                    });
                    break;
                case RemoteCall.Proc.GetRoomById:
                    m_gameServer.GetRoom(rpc.ClientId, rpc.Get<Guid>(0), (error, room) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(room));
                    });
                    break;
                case RemoteCall.Proc.CreateRoom:
                    m_gameServer.CreateRoom(rpc.ClientId, rpc.Get<Guid>(0), rpc.Get<GameMode>(1), (error, room) =>
                    {
                        //Do not broadcast 
                        //client will get rooms list using polling each 10 seconds
                        //or using refres button
                        Return(sender, request, error, RemoteArg.Create(room));
                    });
                    break;
                case RemoteCall.Proc.DestroyRoom:
                    m_gameServer.DestroyRoom(rpc.ClientId, rpc.Get<Guid>(0), (error, guid) =>
                    {
                        //Broadcast to room players
                        Return(sender, request, error, RemoteArg.Create(guid));
                    });
                    break;
                case RemoteCall.Proc.CreateBot:
                    m_gameServer.CreateBot(rpc.ClientId, rpc.Get<string>(0), rpc.Get<BotType>(1), (error, playerId, room) =>
                    {
                        //Broadcast to room players
                        Return(sender, request, error, RemoteArg.Create(playerId), RemoteArg.Create(room));
                    });
                    break;
                case RemoteCall.Proc.CreateBots:
                    m_gameServer.CreateBots(rpc.ClientId, rpc.Get<string[]>(0), rpc.Get<int[]>(1).ToEnum<BotType>(), (error, playerIds, room) =>
                    {
                        //Broadcast to room players
                        Return(sender, request, error, RemoteArg.Create(playerIds), RemoteArg.Create(room));
                    });
                    break;
                case RemoteCall.Proc.DestroyBot:
                    m_gameServer.DestroyBot(rpc.ClientId, rpc.Get<Guid>(0), (error, playerId, room) =>
                    {
                        //Broadcast to room players
                        Return(sender, request, error, RemoteArg.Create(playerId), RemoteArg.Create(room));
                    });
                    break;
                case RemoteCall.Proc.UploadMapData:
                    m_gameServer.UploadMap(rpc.ClientId, rpc.Get<MapInfo>(0), rpc.Get<byte[]>(1), (error) =>
                    {
                        Return(sender, request, error);
                    });
                    break;
                case RemoteCall.Proc.GetMaps:
                    m_gameServer.GetMaps(rpc.ClientId, (Error error, ByteArray[] mapsInfo) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(mapsInfo));
                    });
                    break;
                case RemoteCall.Proc.DownloadMapData:
                    m_gameServer.DownloadMapData(rpc.ClientId, rpc.Get<Guid>(0), (Error error, byte[] mapData) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(mapData));
                    });
                    break;

                case RemoteCall.Proc.GetReplays:
                    m_gameServer.GetReplays(rpc.ClientId, (Error error, ByteArray[] replaysInfo) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(replaysInfo));
                    });
                    break;

                case RemoteCall.Proc.SetReplay:
                    m_gameServer.SetReplay(rpc.ClientId, rpc.Get<Guid>(0),(error) =>
                    {
                        Return(sender, request, error);
                    });
                    break;
                case RemoteCall.Proc.SaveReplay:
                    m_gameServer.SaveReplay(rpc.ClientId, rpc.Get<string>(0), (error) =>
                    {
                        Return(sender, request, error);
                    });
                    break;
                case RemoteCall.Proc.GetStats:
                    m_gameServer.GetStats(rpc.ClientId, (error, serverStats) =>
                    {
                        Return(sender, request, error, RemoteArg.Create(serverStats));
                    });
                    break;
                case RemoteCall.Proc.SetReadyToLaunch:
                    m_gameServer.SetReadyToLaunch(rpc.ClientId, rpc.Get<bool>(0), (error, room) =>
                    {
                        //Broadcast to room players
                        Return(sender, request, error, RemoteArg.Create(room));
                    });
                    break;
                case RemoteCall.Proc.Launch:
                    m_gameServer.Launch(rpc.ClientId, (error, serverUrl) =>
                    {
                        //Broadcast to Match Server url to room players
                        Return(sender, request, error, RemoteArg.Create(serverUrl));
                    });
                    break;

            }
        }

        protected override void OnRegisterClientSafe(ILowProtocol protocol, Guid clientId)
        {
            base.OnRegisterClientSafe(protocol, clientId);

            //Sync method call no fail
            m_gameServer.RegisterClient(clientId, error => { });
        }

        protected override void OnUnregisterClientSafe(ILowProtocol protocol, Guid clientId)
        {
            base.OnUnregisterClientSafe(protocol, clientId);

            //Sync method call no fail
            m_gameServer.UnregisterClient(clientId, error => 
            {
                if(m_gameServer.HasError(error))
                {
                    Log.Error("Failed to Unregister Client: " + error.ToString());
                }
            });
        }

     
        protected override void OnTick(TimeSpan elapsed)
        {
            base.OnTick(elapsed);

            ILoop loop = (ILoop)m_gameServer;
            loop.Update((float)elapsed.TotalSeconds);
        }
    }

}
