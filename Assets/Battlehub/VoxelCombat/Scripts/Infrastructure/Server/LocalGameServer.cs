using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class LocalGameServer : MonoBehaviour, IGameServer
    {
        public event ServerEventHandler<Guid> LoggedIn;
        public event ServerEventHandler<Guid[]> LoggedOff;
        public event ServerEventHandler<Guid[], Room> JoinedRoom;
        public event ServerEventHandler<Guid[], Room> LeftRoom;
        public event ServerEventHandler RoomDestroyed;
        public event ServerEventHandler RoomsListChanged;
        public event ServerEventHandler<Room> ReadyToLaunch;
        public event ServerEventHandler<string> Launched;
        public event ServerEventHandler<bool> ConnectionStateChanged;

        private void GetRidOfWarnings()
        {
            LoggedIn(new Error(), Guid.Empty);
            LoggedOff(new Error(), null);
            JoinedRoom(new Error(), null, null);
            LeftRoom(new Error(), null, null);
            RoomsListChanged(new Error());
            ReadyToLaunch(new Error(), null);
            Launched(new Error(), null);
            ConnectionStateChanged(new Error(), true);
        }

        private IMatchServer m_matchServer;
        private IJob m_job;
        private IGlobalState m_gState;
        private HashSet<Guid> m_loggedInPlayers;
        private Dictionary<Guid, Player> m_players;

        private ServerStats m_stats;
        // private Room m_room;

        private Room Room
        {
            get { return m_gState.GetValue<Room>("LocalGameServer.m_room"); }
            set { m_gState.SetValue("LocalGameServer.m_room", value); }
        }

        public bool IsConnected
        {
            get { return true; }
        }

        [SerializeField]
        private int Lag = 400;

        private string m_persistentDataPath;

        private void Awake()
        {
            //m_persistentDataPath = Application.persistentDataPath;
            m_persistentDataPath = Application.streamingAssetsPath;
            Debug.Log(m_persistentDataPath);

            m_gState = Dependencies.State;
            m_job = Dependencies.Job;
            m_matchServer = Dependencies.MatchServer;

            m_stats = m_gState.GetValue<ServerStats>("LocalGameServer.m_stats");
            if (m_stats == null)
            {
                m_stats = new ServerStats();
                m_gState.SetValue("LocalGameServer.m_stats", m_stats);
            }


            m_loggedInPlayers = m_gState.GetValue<HashSet<Guid>>("LocalGameServer.m_loggedInPlayers");
            if (m_loggedInPlayers == null)
            {
                m_loggedInPlayers = new HashSet<Guid>();
                m_gState.SetValue("LocalGameServer.m_loggedInPlayers", m_loggedInPlayers);
            }

            LoadPlayers();
            m_gState.SetValue("LocalGameServer.m_players", m_players);
        }

        public bool HasError(Error error)
        {
            return error.Code != StatusCode.OK;
        }

        public bool IsLocal(Guid clientId, Guid playerId)
        {
            return m_loggedInPlayers.Contains(playerId);
        }

        public void BecomeAdmin(Guid playerId, ServerEventHandler callback)
        {
            callback(new Error { Code = StatusCode.OK });
        }

        public void Login(string name, string password, Guid clientId, ServerEventHandler<Guid> callback)
        {
            Error error = new Error();
            Player player = m_players.Values.Where(p => p.Name == name).FirstOrDefault();
            Guid playerId = Guid.Empty;
            if (player == null)
            {
                error.Code = StatusCode.NotAuthenticated;
            }
            else if (m_loggedInPlayers.Count == GameConstants.MaxLocalPlayers)
            {
                error.Code = StatusCode.TooMuchLocalPlayers;
                playerId = player.Id;
            }
            else
            {
                error.Code = StatusCode.OK;
                playerId = player.Id;

                if (!m_loggedInPlayers.Contains(player.Id))
                {
                    m_loggedInPlayers.Add(playerId);
                    m_stats.PlayersCount++;
                }
            }

            if (Lag == 0)
            {
                callback(error, playerId);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, playerId));
            }
        }

        public void SignUp(string name, string password, Guid clientId, ServerEventHandler<Guid> callback)
        {
            Error error = new Error();
            Player player = m_players.Values.Where(p => p.Name == name).FirstOrDefault();
            if (player != null)
            {
                error.Code = StatusCode.AlreadyExists;

                if (Lag == 0)
                {
                    callback(error, player.Id);
                }
                else
                {
                    m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, player.Id));
                }
            }
            else if (m_loggedInPlayers.Count == GameConstants.MaxLocalPlayers)
            {
                error.Code = StatusCode.TooMuchLocalPlayers;
                if (Lag == 0)
                {
                    callback(error, Guid.Empty);
                }
                else
                {
                    m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, Guid.Empty));
                }
            }
            else
            {
                error.Code = StatusCode.OK;
                Guid playerId = Guid.NewGuid();

                player = new Player
                {
                    Id = playerId,
                    Name = name,
                };

                lock (m_players)
                {
                    m_players.Add(playerId, player);
                }

                if (!m_loggedInPlayers.Contains(playerId))
                {
                    m_loggedInPlayers.Add(playerId);
                    m_stats.PlayersCount++;
                }

                m_job.Submit(() =>
                {
                    lock (m_players)
                    {
                        SavePlayers();
                    }
                    return null;
                },
                result =>
                {
                    if (Lag == 0)
                    {
                        callback(error, playerId);
                    }
                    else
                    {
                        m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result2 => callback(error, playerId));
                    }
                });
            }
        }

        public void Logoff(Guid clientId, Guid playerId, ServerEventHandler<Guid> callback)
        {
            Error error = new Error();

            if (m_loggedInPlayers.Contains(playerId))
            {
                error.Code = StatusCode.OK;
                if (m_loggedInPlayers.Contains(playerId))
                {
                    m_loggedInPlayers.Remove(playerId);
                    m_stats.PlayersCount--;
                }
            }

            if (Lag == 0)
            {
                callback(error, playerId);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, playerId));
            }
        }

        public void Logoff(Guid clientId, Guid[] playerIds, ServerEventHandler<Guid[]> callback)
        {
            Error error = new Error();

            List<Guid> loggedOffPlayers = new List<Guid>();
            for(int i = 0; i < playerIds.Length; i++)
            {
                if (m_loggedInPlayers.Contains(playerIds[i]))
                {
                    error.Code = StatusCode.OK;
                    if (m_loggedInPlayers.Contains(playerIds[i]))
                    {
                        m_loggedInPlayers.Remove(playerIds[i]);
                        loggedOffPlayers.Add(playerIds[i]);
                        m_stats.PlayersCount--;
                    }
                }
            }

            if (Lag == 0)
            {
                callback(error, loggedOffPlayers.ToArray());
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, loggedOffPlayers.ToArray()));
            }
        }

        public void GetPlayer(Guid clientId, Guid playerId, ServerEventHandler<Player> callback)
        {
            Error error = new Error();
            Player player;
            if (!m_players.TryGetValue(playerId, out player) || !m_loggedInPlayers.Contains(playerId))
            {
                error.Code = StatusCode.NotAuthenticated;
            }
            else
            {
                error.Code = StatusCode.OK;
            }

            if (Lag == 0)
            {
                callback(error, player);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, player));
            }
        }

        public void GetPlayers(Guid clientId, Guid roomId, ServerEventHandler<Player[]> callback)
        {
            Error error = new Error();

            Player[] players = new Player[0];
            if (Room != null)
            {
                error.Code = StatusCode.OK;
                players = new Player[Room.Players.Count];
                for (int i = 0; i < Room.Players.Count; ++i)
                {
                    Guid playerGuid = Room.Players[i];
                    players[i] = m_players[playerGuid];
                }
            }
            else
            {
                error.Code = StatusCode.NotFound;
                error.Message = string.Format("Room {0} not found", roomId);
            }

            if (Lag == 0)
            {
                callback(error, players);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, players));
            }
        }

        public void GetPlayers(Guid clientId, ServerEventHandler<Player[]> callback)
        {
            Error error = new Error();
            error.Code = StatusCode.OK;

            List<Player> players = new List<Player>();
            foreach (Guid playerId in m_loggedInPlayers)
            {
                players.Add(m_players[playerId]);
            }

            if (Lag == 0)
            {
                callback(error, players.ToArray());
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, players.ToArray()));
            }
        }

        public void GetStats(Guid clientId, ServerEventHandler<ServerStats> callback)
        {
            Error error = new Error();
            error.Code = StatusCode.OK;

            if (Lag == 0)
            {
                callback(error, m_stats);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, m_stats));
            }
        }

        public void GetMaps(Guid clientId, ServerEventHandler<MapInfo[]> callback)
        {
            MapInfo[] mapsInfo = null;
            Error error = new Error();
            error.Code = StatusCode.OK;

            try
            {
                string dataPath = m_persistentDataPath + "/Maps/";

                if (Directory.Exists(dataPath))
                {
                    string[] filePath = Directory.GetFiles(dataPath, "*.info", SearchOption.TopDirectoryOnly);
                    mapsInfo = new MapInfo[filePath.Length];
                    for (int i = 0; i < filePath.Length; ++i)
                    {
                        byte[] mapInfoBytes = File.ReadAllBytes(filePath[i]);
                        mapsInfo[i] = ProtobufSerializer.Deserialize<MapInfo>(mapInfoBytes);
                    }
                }
                else
                {
                    mapsInfo = new MapInfo[0];
                }
            }
            catch (Exception e)
            {
                error.Code = StatusCode.UnhandledException;
                error.Message = e.Message;
            }

            if (Lag == 0)
            {
                callback(error, mapsInfo);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, mapsInfo));
            }
        }

        public void GetMaps(Guid clientId, ServerEventHandler<ByteArray[]> callback)
        {
            throw new NotSupportedException();
        }
        

        /// <summary>
        /// Only user who own admin rights could upload map to server
        /// </summary>
        public void UploadMap(Guid clientId, MapInfo mapInfo, MapData mapData, ServerEventHandler callback)
        {
            Error error = new Error();
            if (mapInfo.Id != mapData.Id)
            {
                error.Code = StatusCode.NotAllowed;
                error.Message = "mapInfo.Id != mapData.Id";
            }
            else
            {
                error.Code = StatusCode.OK;

                string dataPath = m_persistentDataPath + "/Maps/";
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }

                byte[] mapInfoBytes = ProtobufSerializer.Serialize(mapInfo);

                File.WriteAllBytes(dataPath + mapInfo.Id + ".info", mapInfoBytes);

                byte[] mapDataBytes = ProtobufSerializer.Serialize(mapData);

                File.WriteAllBytes(dataPath + mapData.Id + ".data", mapDataBytes);
            }

            if (Lag == 0)
            {
                callback(error);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error));
            }
        }

        public void UploadMap(Guid clientId, MapInfo mapInfo, byte[] mapData, ServerEventHandler callback)
        {
            throw new NotSupportedException();
        }

        public void DownloadMapData(Guid clientId, Guid mapId, ServerEventHandler<MapData> callback)
        {
            DownloadMapDataById(mapId, callback);
        }

        public void DownloadMapData(Guid cleintId, Guid mapId, ServerEventHandler<byte[]> callback)
        {
            throw new NotSupportedException();
        }

        private void DownloadMapDataById(Guid mapId, ServerEventHandler<MapData> callback)
        {
            MapData mapData = null;
            Error error = new Error();

            string dataPath = m_persistentDataPath + "/Maps/";
            string filePath = dataPath + mapId + ".data";
            if (!File.Exists(filePath))
            {
                error.Code = StatusCode.NotFound;

                if (Lag == 0)
                {
                    callback(error, mapData);
                }
                else
                {
                    m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, mapData));
                }
            }
            else
            {
                m_job.Submit(() =>
                {
                    error.Code = StatusCode.OK;
                    try
                    {
                        byte[] mapDataBytes = File.ReadAllBytes(filePath);
                        mapData = ProtobufSerializer.Deserialize<MapData>(mapDataBytes);
                    }
                    catch (Exception e)
                    {
                        error.Code = StatusCode.UnhandledException;
                        error.Message = e.Message;
                    }

                    return null;
                },
                result =>
                {
                    callback(error, mapData);
                });
            }
        }

        private MapInfo GetMapInfo(Guid mapId)
        {
            string dataPath = m_persistentDataPath + "/Maps/";
            string filePath = dataPath + mapId + ".info";
            if (!File.Exists(filePath))
            {
                return null;
            }

            byte[] mapInfoBytes = File.ReadAllBytes(filePath);
            return ProtobufSerializer.Deserialize<MapInfo>(mapInfoBytes);
        }


        public void CreateRoom(Guid clientId, Guid mapId, GameMode mode, ServerEventHandler<Room> callback)
        {
            Error error = new Error();
        
            MapInfo mapInfo = GetMapInfo(mapId);
            if (mapInfo == null)
            {
                error.Code = StatusCode.NotFound;
                error.Message = "Map is not found";
            }
            else if ((mapInfo.SupportedModes & mode) == 0)
            {
                error.Code = StatusCode.NotAllowed;
                error.Message = string.Format("Mode {0} is not supported by {1} map", mode, mapId);
            }
            else
            {
                error.Code = StatusCode.OK;

                Room room = new Room();
                room.CreatorClientId = clientId;
                room.CreatorPlayerId = m_loggedInPlayers.First();
                room.MapInfo = mapInfo;
                room.Mode = mode;
                room.Id = Guid.NewGuid();
                room.Players = new List<Guid>();
                room.ReadyToLaunchPlayers = new List<Guid>();

                if (mode != GameMode.Replay)
                {
                    for (int i = 0; i < Math.Min(m_loggedInPlayers.Count, mapInfo.MaxPlayers); ++i)
                    {
                        room.Players.Add(m_loggedInPlayers.ElementAt(i));
                    }
                }

                Room = room;
            }

            if (Lag == 0)
            {
                callback(error, Room);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, Room));
            }
        }

        public void DestroyRoom(Guid clientId, Guid roomId, ServerEventHandler<Guid> callback)
        {
            Error error = new Error();

            error.Code = StatusCode.OK;

            if (Room != null)
            {
                for (int i = 0; i < Room.Players.Count; ++i)
                {
                    Guid playerId = Room.Players[i];
                    Player player = m_players[playerId];
                    if (player.IsBot)
                    {
                        m_players.Remove(playerId);
                    }
                }
            }

            Room = null;

            if (Lag == 0)
            {
                callback(error, roomId);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, roomId));
            }
        }

        public void GetRoom(Guid clientId, ServerEventHandler<Room> callback)
        {
            Error error = new Error();

            Room room;
            if (Room != null)
            {
                error.Code = StatusCode.OK;
                room = Room;
            }
            else
            {
                error.Code = StatusCode.NotFound;
                room = null;
            }

            if (Lag == 0)
            {
                callback(error, room);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, room));
            }
        }

        public void GetRoom(Guid clientId, Guid roomId, ServerEventHandler<Room> callback)
        {
            Error error = new Error();

            Room room;
            if (Room != null && Room.Id == roomId)
            {
                error.Code = StatusCode.OK;
                room = Room;
            }
            else
            {
                error.Code = StatusCode.NotFound;
                room = null;
            }

            if (Lag == 0)
            {
                callback(error, room);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, room));
            }
        }

        public void GetRooms(Guid clientId, int page, int count, ServerEventHandler<Room[]> callback)
        {
            Error error = new Error();

            error.Code = StatusCode.OK;
            Room[] rooms;
            if (Room != null)
            {
                rooms = new[] { Room };
            }
            else
            {
                rooms = new Room[0];
            }

            if (Lag == 0)
            {
                callback(error, rooms);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, rooms));
            }
        }

        public void JoinRoom(Guid clientId, Guid roomId, ServerEventHandler<Room> callback)
        {
            Error error = new Error();
            Room room = null;

            if (Room != null && Room.Id == roomId)
            {
                error.Code = StatusCode.OK;
                room = Room;

                int expectedPlayersCount = m_loggedInPlayers.Count + room.Players.Count;
                if (Room.MapInfo == null)
                {
                    error.Code = StatusCode.NotFound;
                    error.Message = string.Format("MapInfo for room {0} was not found", roomId);
                }
                else
                {
                    if (expectedPlayersCount > room.MapInfo.MaxPlayers)
                    {
                        error.Code = StatusCode.TooMuchPlayersInRoom;
                    }
                    else
                    {
                        foreach (Guid playerId in m_loggedInPlayers)
                        {
                            room.Players.Add(playerId);
                        }
                    }
                }
            }
            else
            {
                error.Code = StatusCode.NotFound;
                error.Message = string.Format("Room {0} was not found", roomId);
            }


            if (Lag == 0)
            {
                callback(error, room);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, room));
            }
        }

        public void LeaveRoom(Guid clientId, ServerEventHandler callback)
        {
            Error error = new Error();

            error.Code = StatusCode.OK;

            if (Room != null)
            {
                foreach (Guid playerId in m_loggedInPlayers)
                {
                    Room.Players.Remove(playerId);
                    Room.ReadyToLaunchPlayers.Remove(playerId);
                }
            }

            if (Lag == 0)
            {
                callback(error);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error));
            }
        }


        public void CreateBot(Guid clientId, string botName, BotType botType, ServerEventHandler<Guid, Room> callback)
        {
            Error error = new Error();
            Room room = null;
            Guid botId = Guid.Empty;

            if (Room != null)
            {
                room = Room;

                int expectedPlayersCount = room.Players.Count + 1;
                if (Room.MapInfo == null)
                {
                    error.Code = StatusCode.NotFound;
                    error.Message = string.Format("MapInfo for room {0} was not found", room.Id);
                }
                else
                {
                    if (expectedPlayersCount > Room.MapInfo.MaxPlayers)
                    {
                        error.Code = StatusCode.TooMuchPlayersInRoom;
                    }
                    else
                    {
                        error.Code = StatusCode.OK;
                        botId = Guid.NewGuid();
                        Player bot = new Player
                        {
                            Id = botId,
                            Name = botName,
                            BotType = botType
                        };

                        Room.Players.Add(botId);
                        Room.ReadyToLaunchPlayers.Add(botId);
                        m_players.Add(botId, bot);
                    }
                }
            }
            else
            {
                error.Code = StatusCode.NotFound;
                error.Message = "Room  was not found";
            }

            if (Lag == 0)
            {
                callback(error, botId, room);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, botId, room));
            }

        }

        public void CreateBots(Guid clientId, string[] botNames, BotType[] botTypes, ServerEventHandler<Guid[], Room> callback) //bot guids and room with bots
        {
            Error error = new Error();
            Room room = null;
            Guid[] botIds = new Guid[botNames.Length];

            if (Room != null)
            {
                room = Room;

                int expectedPlayersCount = room.Players.Count + botNames.Length;
                if (Room.MapInfo == null)
                {
                    error.Code = StatusCode.NotFound;
                    error.Message = string.Format("MapInfo for room {0} was not found", room.Id);
                }
                else
                {
                    if (expectedPlayersCount > Room.MapInfo.MaxPlayers)
                    {
                        error.Code = StatusCode.TooMuchPlayersInRoom;
                    }
                    else
                    {
                        error.Code = StatusCode.OK;

                        for(int i = 0; i < botNames.Length; ++i)
                        {
                            Guid botId = Guid.NewGuid();
                            Player bot = new Player
                            {
                                Id = botId,
                                Name = botNames[i],
                                BotType = botTypes[i]
                            };

                            Room.Players.Add(botId);
                            Room.ReadyToLaunchPlayers.Add(botId);
                            m_players.Add(botId, bot);
                        }
                    }
                }
            }
            else
            {
                error.Code = StatusCode.NotFound;
                error.Message = "Room  was not found";
            }

            if (Lag == 0)
            {
                callback(error, botIds, room);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, botIds, room));
            }

        }

        public void DestroyBot(Guid clientId,  Guid botId, ServerEventHandler<Guid, Room> callback)
        {
            Error error = new Error();
            Room room = null;

            if (Room != null)
            {
                room = Room;
                error.Code = StatusCode.OK;
                Room.Players.Remove(botId);
                Room.ReadyToLaunchPlayers.Remove(botId);
                m_players.Remove(botId);
            }
            else
            {
                error.Code = StatusCode.NotFound;
                error.Message = "Room was not found";
            }

            if (Lag == 0)
            {
                callback(error, botId, room);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, botId, room));
            }
        }

        public void SetReadyToLaunch(Guid clientId, bool isReady, ServerEventHandler<Room> callback)
        {
            Error error = new Error();
            if(Room == null)
            {
                error.Code = StatusCode.NotFound;
            }
            else
            {
                error.Code = StatusCode.OK;
            }

            if (Room.IsLaunched)
            {
                error.Code = StatusCode.AlreadyLaunched;
                error.Message = "Already Lauched";
                callback(error, Room);
                return;
            }

            if (isReady)
            {
                foreach (Guid player in m_loggedInPlayers)
                {
                    if (!Room.ReadyToLaunchPlayers.Contains(player))
                    {
                        Room.ReadyToLaunchPlayers.Add(player);
                    }
                }
            }
            else
            {
                foreach (Guid player in m_loggedInPlayers)
                {
                    if (!Room.ReadyToLaunchPlayers.Contains(player))
                    {
                        Room.ReadyToLaunchPlayers.Remove(player);
                    }
                }
            }

            if (Lag == 0)
            {
                callback(error, Room);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result =>
                {
                    callback(error, Room);
                });
            }
        }

        public void Launch(Guid clientId, ServerEventHandler<string> callback)
        {
            Error error = new Error();
            if (Room == null)
            {
                error.Code = StatusCode.NotFound;
            }
            else
            {
                error.Code = StatusCode.OK;
            }

            if (Lag == 0)
            {
                callback(error, string.Empty);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, string.Empty));
            }
        }

        public void SavePlayersStats(ServerEventHandler callback)
        {
            SavePlayers();

            Error error = new Error();
            error.Code = StatusCode.OK;

            if (Lag == 0)
            {
                callback(error);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error));
            }
        }

        private void SavePlayers()
        {
            byte[] playersData = ProtobufSerializer.Serialize(m_players.Values.Where(p => p.BotType == BotType.None).ToArray());
            File.WriteAllBytes(m_persistentDataPath + "/players.dat", playersData);
        }

        private void LoadPlayers()
        {
            string path = m_persistentDataPath + "/players.dat";
            if (File.Exists(path))
            {
                byte[] playersData = File.ReadAllBytes(path);
                Player[] players = ProtobufSerializer.Deserialize<Player[]>(playersData);

                m_players = players.ToDictionary(p => p.Id);
            }
            else
            {
                m_players = new Dictionary<Guid, Player>();
            }
        }

        public void GetReplays(Guid clientId, ServerEventHandler<ByteArray[]> callback)
        {
            throw new NotSupportedException();
        }

        public void GetReplays(Guid clientId, ServerEventHandler<ReplayInfo[]> callback)
        {
            ReplayInfo[] replaysInfo = null;
            Error error = new Error();
            error.Code = StatusCode.OK;

            try
            {
                string dataPath = m_persistentDataPath + "/Replays/";

                if (Directory.Exists(dataPath))
                {
                    string[] filePath = Directory.GetFiles(dataPath, "*.info", SearchOption.TopDirectoryOnly);
                    replaysInfo = new ReplayInfo[filePath.Length];
                    for (int i = 0; i < filePath.Length; ++i)
                    {
                        byte[] replayInfoBytes = File.ReadAllBytes(filePath[i]);
                        replaysInfo[i] = ProtobufSerializer.Deserialize<ReplayInfo>(replayInfoBytes);
                    }
                }
                else
                {
                    replaysInfo = new ReplayInfo[0];
                }
            }
            catch (Exception e)
            {
                error.Code = StatusCode.UnhandledException;
                error.Message = e.Message;
            }

            if (Lag == 0)
            {
                callback(error, replaysInfo);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error, replaysInfo));
            }
        }

        private ReplayData GetReplayData(Guid replayId)
        {
            string dataPath = m_persistentDataPath + "/Replays/";
            string filePath = dataPath + replayId + ".data";
            if (!File.Exists(filePath))
            {
                return null;
            }

            byte[] replayDataBytes = File.ReadAllBytes(filePath);
            return ProtobufSerializer.Deserialize<ReplayData>(replayDataBytes);
        }

        public void SetReplay(Guid clientId, Guid id, ServerEventHandler callback)
        {
            Error error = new Error(StatusCode.OK);
            ReplayData replay = GetReplayData(id);

            if (replay == null)
            {
                error.Code = StatusCode.NotFound;   
            }
            else
            {
                m_gState.SetValue("LocalGameServer.m_replay", replay);
            }

            if (Lag == 0)
            {
                callback(error);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error));
            }
        }

        public void SaveReplay(Guid clientId, string name, ServerEventHandler callback)
        {
            m_matchServer.GetReplay(clientId, (error, replayData) =>
            {
                if (HasError(error))
                {
                    callback(error);
                    return;
                }

                ReplayInfo replayInfo = new ReplayInfo();
                replayInfo.DateTime = DateTime.UtcNow.Ticks;
                replayInfo.Name = name;
                replayInfo.Id = replayData.Id = Guid.NewGuid();
                replayInfo.MapId = Room.MapInfo.Id;
                replayInfo.PlayerNames = Room.Players.Skip(1).Select(r => m_players[r].Name).ToArray();

                error.Code = StatusCode.OK;

                string dataPath = m_persistentDataPath + "/Replays/";
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }

                byte[] replayInfoBytes = ProtobufSerializer.Serialize(replayInfo);

                File.WriteAllBytes(dataPath + replayInfo.Id + ".info", replayInfoBytes);

                byte[] replayDataBytes = ProtobufSerializer.Serialize(replayData);

                File.WriteAllBytes(dataPath + replayData.Id + ".data", replayDataBytes);

                if (Lag == 0)
                {
                    callback(error);
                }
                else
                {
                    m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error));
                }
            });
        }


        public void RegisterClient(Guid clientId, ServerEventHandler callback)
        {
            Error error = new Error();
            if (Lag == 0)
            {
                callback(error);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(Lag); return null; }, result => callback(error));
            }
        }

        public void UnregisterClient(Guid clientId, ServerEventHandler callback)
        {
            throw new NotImplementedException();
        }

        public void CancelRequests()
        {
            m_job.CancelAll();
        }
    }
}

