using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class RemoteGameServer : RemoteServer, IGameServer
    {
        public event ServerEventHandler<Guid> LoggedIn;
        public event ServerEventHandler<Guid[]> LoggedOff;
        public event ServerEventHandler<Guid[], Room> JoinedRoom;
        public event ServerEventHandler<Guid[], Room> LeftRoom;
        public event ServerEventHandler RoomDestroyed;
        public event ServerEventHandler RoomsListChanged;
        public event ServerEventHandler<Room> ReadyToLaunch;
        public event ServerEventHandler<string> Launched;
        
        private HashSet<Guid> m_localPlayers;

        protected override string ServerUrl
        {
            get { return m_settings.GameServerUrl; }
        }

        private static RemoteGameServer m_instance;

        protected override void Awake()
        {
            if(m_instance == null)
            {
                m_instance = this;
                gameObject.DontDestroyOnLoad();

                m_localPlayers = new HashSet<Guid>();
                base.Awake();
            }
            else
            {
                if(m_instance != this)
                {
                    Destroy(gameObject);
                }
            }
        }

        protected override void OnDisabled(ILowProtocol sender)
        {
            base.OnDisabled(sender);
            m_localPlayers.Clear();
        }

        protected override void OnRemoteEvent(RemoteEvent evt)
        {
            switch(evt.Event)
            {
                case RemoteEvent.Evt.JoinedRoom:
                    if(JoinedRoom != null)
                    {
                        JoinedRoom(evt.Error, evt.Get<Guid[]>(0), evt.Get<Room>(1));
                    }
                    break;
                case RemoteEvent.Evt.Launched:
                    {
                        string matchServerUrl = evt.Get<string>(0);
                        Dependencies.State.SetValue("Battlehub.VoxelCombat.MatchServerUrl", matchServerUrl);
                        if (Launched != null)
                        {
                            Launched(evt.Error, matchServerUrl);
                        }
                    }
                    break;
                case RemoteEvent.Evt.LeftRoom:
                    if (LeftRoom != null)
                    {
                        LeftRoom(evt.Error, evt.Get<Guid[]>(0), evt.Get<Room>(1));
                    }
                    break;
                case RemoteEvent.Evt.LoggedIn:
                    if (LoggedIn != null)
                    {
                        LoggedIn(evt.Error, evt.Get<Guid>(0));
                    }
                    break;
                case RemoteEvent.Evt.LoggedOff:
                    if(LoggedOff != null)
                    {
                        LoggedOff(evt.Error, evt.Get<Guid[]>(0));
                    }
                    break;
                case RemoteEvent.Evt.ReadyToLaunch:
                    if(ReadyToLaunch != null)
                    {
                        ReadyToLaunch(evt.Error, evt.Get<Room>(0));
                    }
                    break;
                case RemoteEvent.Evt.RoomDestroyed:
                    if(RoomDestroyed != null)
                    {
                        RoomDestroyed(evt.Error);
                    }
                    break;
                case RemoteEvent.Evt.RoomsListChanged:
                    if(RoomsListChanged != null)
                    {
                        RoomsListChanged(evt.Error);
                    }
                    break;
                default:
                    base.OnRemoteEvent(evt);
                    break;
            }
        }

        public bool IsLocal(Guid clientId, Guid playerId)
        {
            return m_localPlayers.Contains(playerId);
        }

        public void BecomeAdmin(Guid playerId, ServerEventHandler callback)
        {
            throw new NotImplementedException();
        }


        public void CreateBot(Guid clientId, string botName, BotType botType, ServerEventHandler<Guid, Room> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.CreateBot,
                clientId,
                RemoteArg.Create(botName),
                RemoteArg.Create((int)botType));

            Call(rpc, (error, result) => callback(error, result.Get<Guid>(0), result.Get<Room>(1)));
        }

        public void CreateBots(Guid clientId, string[] botNames, BotType[] botTypes, ServerEventHandler<Guid[], Room> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.CreateBots,
                clientId,
                RemoteArg.Create(botNames),
                RemoteArg.Create(botTypes.ToIntArray()));

            Call(rpc, (error, result) => callback(error, result.Get<Guid[]>(0), result.Get<Room>(1)));
        }

        public void DestroyBot(Guid clientId, Guid botId, ServerEventHandler<Guid, Room> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.DestroyBot,
                clientId,
                RemoteArg.Create(botId));

            Call(rpc, (error, result) => callback(error, result.Get<Guid>(0), result.Get<Room>(1)));
        }

        public void CreateRoom(Guid clientId, Guid mapId, GameMode gameMode, ServerEventHandler<Room> callback)
        {
            RemoteCall rpc = new RemoteCall(
               RemoteCall.Proc.CreateRoom,
               clientId,
               RemoteArg.Create(mapId),
               RemoteArg.Create((int)gameMode));

            Call(rpc, (error, result) => callback(error, result.Get<Room>(0)));
        }

        public void DestroyRoom(Guid clientId, Guid roomId, ServerEventHandler<Guid> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.DestroyRoom,
                clientId,
                RemoteArg.Create(roomId));

            Call(rpc, (error, result) => callback(error, result.Get<Guid>(0)));
        }

        public void DownloadMapData(Guid clientId, Guid mapId, ServerEventHandler<MapData> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.DownloadMapData,
                clientId,
                RemoteArg.Create(mapId));

            Call(rpc, (error, result) =>
            {
                byte[] mapDataBin = result.Get<byte[]>(0);
                MapData mapData = null;

                if(mapDataBin != null && !HasError(error))
                {
                    mapData = ProtobufSerializer.Deserialize<MapData>(mapDataBin);
                }

                callback(error, mapData);
            });
        }

        public void DownloadMapData(Guid cleintId, Guid mapId, ServerEventHandler<byte[]> callback)
        {
            throw new NotSupportedException();
        }

        public void GetMaps(Guid clientId, ServerEventHandler<MapInfo[]> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.GetMaps,
                clientId);

            Call(rpc, (error, result) =>
            {
                ByteArray[] mapInfoBin = result.Get<ByteArray[]>(0);

                MapInfo[] mapsInfo = new MapInfo[0];
                if(mapInfoBin != null && !HasError(error))
                {
                    mapsInfo = new MapInfo[mapInfoBin.Length];
                    for (int i = 0; i < mapsInfo.Length; ++i)
                    {
                        mapsInfo[i] = ProtobufSerializer.Deserialize<MapInfo>(mapInfoBin[i]);
                    }
                }
               
                callback(error, mapsInfo);
            });
        }

        public void GetMaps(Guid clientId, ServerEventHandler<ByteArray[]> callback)
        {
            throw new NotSupportedException();
        }

        public void GetPlayer(Guid clientId, Guid playerId, ServerEventHandler<Player> callback)
        {
            RemoteCall rpc = new RemoteCall(
               RemoteCall.Proc.GetPlayer,
               clientId,
               RemoteArg.Create(playerId));

            Call(rpc, (error, result) => callback(error, result.Get<Player>(0)));
        }

        public void GetPlayers(Guid clientId, ServerEventHandler<Player[]> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.GetPlayers, 
                clientId);

            Call(rpc, (error, result) => callback(error, result.Get<Player[]>(0)));
        }

        public void GetPlayers(Guid clientId, Guid roomId, ServerEventHandler<Player[]> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.GetPlayersByRoomId,
                clientId,
                RemoteArg.Create(roomId));

            Call(rpc, (error, result) => callback(error, result.Get<Player[]>(0)));
        }

        public void GetReplays(Guid clientId, ServerEventHandler<ReplayInfo[]> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.GetReplays,
                clientId);

            Call(rpc, (error, result) =>
            {
                ByteArray[] replayInfoBin = result.Get<ByteArray[]>(0);
                ReplayInfo[] replayInfo = new ReplayInfo[0]; 
                if(replayInfo != null && !HasError(error))
                {
                    replayInfo = new ReplayInfo[replayInfoBin.Length];
                    for (int i = 0; i < replayInfo.Length; ++i)
                    {
                        replayInfo[i] = ProtobufSerializer.Deserialize<ReplayInfo>(replayInfoBin[i]);
                    }
                }
                callback(error, replayInfo);
            });
        }

        public void GetReplays(Guid clientId, ServerEventHandler<ByteArray[]> callback)
        {
            throw new NotSupportedException();
        }

        public void GetRoom(Guid clientId, ServerEventHandler<Room> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.GetRoom,
                clientId);

            Call(rpc, (error, result) => callback(error, result.Get<Room>(0)));
        }

        public void GetRoom(Guid clientId, Guid roomId, ServerEventHandler<Room> callback)
        {
            RemoteCall rpc = new RemoteCall(
               RemoteCall.Proc.GetRoomById,
               clientId,
               RemoteArg.Create(roomId));

            Call(rpc, (error, result) => callback(error, result.Get<Room>(0)));
        }

        public void GetRooms(Guid clientId, int page, int count, ServerEventHandler<Room[]> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.GetRooms,
                clientId,
                RemoteArg.Create(page),
                RemoteArg.Create(count));

            Call(rpc, (error, result) => callback(error, result.Get<Room[]>(0)));
        }

        public void GetStats(Guid clientId, ServerEventHandler<ServerStats> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.GetStats,
                clientId);

            Call(rpc, (error, result) => callback(error, result.Get<ServerStats>(0)));
        }  

        public void JoinRoom(Guid clientId, Guid roomId, ServerEventHandler<Room> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.JoinRoom,
                clientId,
                RemoteArg.Create(roomId));

            Call(rpc, (error, result) => callback(error, result.Get<Room>(0)));
        }

        public void Launch(Guid clientId, ServerEventHandler<string> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.Launch,
                clientId);

            Call(rpc, (error, result) => callback(error, result.Get<string>(0)));
        }

        public void LeaveRoom(Guid clientId, ServerEventHandler callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.LeaveRoom,
                clientId);

            Call(rpc, (error, result) => callback(error));
        }

        public void Login(string name, byte[] pwdHash, Guid clientId, ServerEventHandler<Guid> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.LoginHash,
                clientId,
                RemoteArg.Create(name),
                RemoteArg.Create(pwdHash));

            Call(rpc, (error, result) =>
            {
                Guid playerId = result.Get<Guid>(0);
                if (!HasError(error))
                {
                    m_localPlayers.Add(playerId);
                }

                callback(error, playerId);
            });
        }


        public void Login(string name, string password, Guid clientId, ServerEventHandler<Guid, byte[]> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.Login,
                clientId,
                RemoteArg.Create(name),
                RemoteArg.Create(password));

            Call(rpc, (error, result) =>
            {
                Guid playerId = result.Get<Guid>(0);
                byte[] pwdHash = result.Get<byte[]>(1);
                if(!HasError(error))
                {
                    m_localPlayers.Add(playerId);
                }

                callback(error, playerId, pwdHash);
            });
        }

        public void SignUp(string name, string password, Guid clientId, ServerEventHandler<Guid, byte[]> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.SignUp,
                clientId,
                RemoteArg.Create(name),
                RemoteArg.Create(password));

            Call(rpc, (error, result) =>
            {
                Guid playerId = result.Get<Guid>(0);
                byte[] pwdHash = result.Get<byte[]>(1);
                if (!HasError(error))
                {
                    m_localPlayers.Add(playerId);
                }

                callback(error, playerId, pwdHash);
            });
        }


        public void Logoff(Guid clientId, Guid[] playerIds, ServerEventHandler<Guid[]> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.LogoffMultiple,
                clientId,
                RemoteArg.Create(playerIds));

            Call(rpc, (error, result) =>
            {
                playerIds = result.Get<Guid[]>(0);

                if (!HasError(error))
                {
                    if (playerIds != null)
                    {
                        for (int i = 0; i < playerIds.Length; ++i)
                        {
                            m_localPlayers.Remove(playerIds[i]);
                        }
                    }
                }
              

                callback(error, playerIds);
            });
        }

        public void Logoff(Guid clientId, Guid playerId, ServerEventHandler<Guid> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.Logoff,
                clientId,
                RemoteArg.Create(playerId));

            Call(rpc, (error, result) =>
            {
                playerId = result.Get<Guid>(0);

                if (!HasError(error))
                {
                    m_localPlayers.Remove(playerId);
                }
                callback(error, playerId);
            });
        }

        public void SavePlayersStats(ServerEventHandler callback)
        {
            Debug.LogWarning("SavePlayersStats is not implemented");
        }

        public void SaveReplay(Guid clientId, string name, ServerEventHandler callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.SaveReplay,
                clientId,
                RemoteArg.Create(name));

            Call(rpc, (error, result) => callback(error));
        }

        public void SetReadyToLaunch(Guid clientId, bool isReady, ServerEventHandler<Room> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.SetReadyToLaunch,
                clientId,
                RemoteArg.Create(isReady));

            Call(rpc, (error, result) => callback(error, result.Get<Room>(0)));
        }

        public void SetReplay(Guid clientId, Guid id, ServerEventHandler callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.SetReplay,
                clientId,
                RemoteArg.Create(id));

            Call(rpc, (error, result) => callback(error));
        }

   
        public void UploadMap(Guid clientId, MapInfo mapInfo, MapData mapData, ServerEventHandler callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.UploadMapData,
                clientId,
                RemoteArg.Create(mapInfo),
                RemoteArg.Create(ProtobufSerializer.Serialize(mapData)));

            Call(rpc, (error, result) => callback(error));
        }

        public void UploadMap(Guid clientId, MapInfo mapInfo, byte[] mapData, ServerEventHandler callback)
        {
            throw new NotSupportedException();
        }
    }
}
