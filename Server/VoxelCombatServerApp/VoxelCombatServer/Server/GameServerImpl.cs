using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Battlehub.VoxelCombat
{
    public class GameServerImpl : IGameServer,  ILoop, IGameServerDiagnostics
    {
        protected readonly ILog Log;

        private void GetRidOfWarnings()
        {
            ConnectionStateChanging(new Error());
            ConnectionStateChanged(new Error(), new ValueChangedArgs<bool>(false, false));
        }

        public event ServerEventHandler<ServerEventArgs<Guid>> LoggedIn;
        public event ServerEventHandler<ServerEventArgs<Guid[]>> LoggedOff;
        public event ServerEventHandler<ServerEventArgs<Guid[], Room>> JoinedRoom;
        public event ServerEventHandler<ServerEventArgs<Guid[], Room>> LeftRoom;
        public event ServerEventHandler<ServerEventArgs> RoomDestroyed;
        public event ServerEventHandler<ServerEventArgs> RoomsListChanged;
        public event ServerEventHandler<ServerEventArgs<Room>> ReadyToLaunch;
        public event ServerEventHandler<ServerEventArgs<string>> Launched;
        public event ServerEventHandler<ValueChangedArgs<bool>> ConnectionStateChanged;
        public event ServerEventHandler ConnectionStateChanging;
        public event ServerEventHandler<ServerEventArgs<ChatMessage>> ChatMessage;

        private readonly Dictionary<Guid, Guid> m_replaysByClientId = new Dictionary<Guid, Guid>();
        private readonly Dictionary<Guid, Room> m_roomsByClientId = new Dictionary<Guid, Room>();
        private readonly Dictionary<Guid, Room> m_roomsById = new Dictionary<Guid, Room>();

        private readonly Dictionary<Guid, Guid> m_playerToClientId = new Dictionary<Guid, Guid>();
        private readonly Dictionary<Guid, List<Player>> m_players = new Dictionary<Guid, List<Player>>();
        private readonly Dictionary<Guid, Player> m_bots = new Dictionary<Guid, Player>();
        private readonly ServerStats m_stats = new ServerStats();
        private readonly List<Guid> m_runningMatches = new List<Guid>();
        private readonly List<float> m_runningMatchesNextCheck = new List<float>();
        private const float RunningMatchesCheckInterval = 30;

        public bool IsConnectionStateChanging
        {
            get { throw new NotSupportedException(); }
        }

        public bool IsConnected
        {
            get { throw new NotSupportedException(); }
        }

        private readonly string m_persistentDataPath;
        private readonly string m_matchServerUrl;
        private IPlayerRepository m_playerRepository;
        private ITimeService m_time;

        public GameServerImpl(string persistentDataPath, string matchServerUrl)
        {
            m_matchServerUrl = matchServerUrl;
            m_persistentDataPath = persistentDataPath;
            Log = LogManager.GetLogger(GetType());
            m_playerRepository = new PlayerRepository();
        }

        public bool HasError(Error error)
        {
            return error.Code != StatusCode.OK;
        }

        public bool IsLocal(Guid clientId, Guid playerId)
        {
            throw new NotSupportedException();
        }

        public void BecomeAdmin(Guid playerId, ServerEventHandler callback)
        {
            callback(new Error { Code = StatusCode.OK });
        }

        //Must call callback synchroniously
        public void RegisterClient(Guid clientId, ServerEventHandler callback)
        {
            m_players.Add(clientId, new List<Player>());

            callback(new Error { Code = StatusCode.OK });
        }

        //Must call callback synchroniously
        public void UnregisterClient(Guid clientId, ServerEventHandler callback)
        {
            List<Player> loggedInPlayers;
            if (m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                Logoff(clientId, loggedInPlayers.Select(p => p.Id).ToArray(), (error, guids) =>
                {
                    m_players.Remove(clientId);
                    callback(error);
                });
            }
            else
            {
                callback(new Error { Code = StatusCode.OK });
            }
        }

        public void GetPlayer(string name, byte[] pwdHash, Action<Error, Player> callback)
        {
            m_playerRepository.GetPlayer(name, pwdHash, callback);
        }


        public void GetPlayer(string name, string password, Action<Error, Player, byte[]> callback)
        {
            m_playerRepository.GetPlayer(name, password, callback);
        }

        public void CreatePlayer(Guid guid, string name, string password, Action<Error, Player, byte[]> callback)
        {
            m_playerRepository.CreatePlayer(guid, name, password, callback);
        }

        public void GetPlayers(Guid[] guids, Action<Error, Player[]> callback)
        {
            Player[] result = new Player[guids.Length];
            for(int i = 0; i < guids.Length; ++i)
            {
                Player bot;
                if(m_bots.TryGetValue(guids[i], out bot))
                {
                    result[i] = bot;
                }
            }

            m_playerRepository.GetPlayers(guids, (error, players) =>
            {
                if(HasError(error))
                {
                    callback(error, new Player[0]);
                }
                else
                {
                    for(int i = 0; i < guids.Length; ++i)
                    {
                        Player player;
                        if (players.TryGetValue(guids[i], out player))
                        {
                            result[i] = player;
                        }
                    }

                    callback(new Error(StatusCode.OK), result);
                }
            });
        }

        public void Login(string name, byte[] pwdHash, Guid clientId, ServerEventHandler<Guid> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, Guid.Empty);
                return;
            }

            GetPlayer(name, pwdHash, (getPlayerError, player) =>
            {
                Guid playerId = Guid.Empty;
                if (HasError(getPlayerError))
                {
                    error = getPlayerError;
                }
                else if (player == null)
                {
                    error.Code = StatusCode.NotAuthenticated;
                }
                else if (loggedInPlayers.Count == GameConstants.MaxLocalPlayers)
                {
                    error.Code = StatusCode.TooMuchLocalPlayers;
                    playerId = player.Id;
                }
                else
                {
                    error.Code = StatusCode.OK;
                    playerId = player.Id;

                    if (!loggedInPlayers.Any(p => p.Id == playerId))
                    {
                        if(m_playerToClientId.ContainsKey(playerId))
                        {
                            Logoff(clientId, m_playerToClientId[playerId], playerId, (logoffError, guid) =>
                            {
                                if (HasError(logoffError))
                                {
                                    error = logoffError;
                                }
                                else
                                {
                                    FinishLogin(clientId, player, error, loggedInPlayers, playerId);
                                }
                                callback(error, playerId);
                            });
                            return;
                        }
                        else
                        {
                            FinishLogin(clientId, player, error, loggedInPlayers, playerId);
                        }

                    }
                }
                callback(error, playerId);
            });
        }

        public void Login(string name, string password, Guid clientId, ServerEventHandler<Guid, byte[]> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, Guid.Empty, new byte[0]);
                return;
            }

            GetPlayer(name, password, (getPlayerError, player, pwdHash) =>
            {
                Guid playerId = Guid.Empty;
                if (HasError(getPlayerError))
                {
                    error = getPlayerError;
                }
                else if (player == null)
                {
                    error.Code = StatusCode.NotAuthenticated;
                }
                else if (loggedInPlayers.Count == GameConstants.MaxLocalPlayers)
                {
                    error.Code = StatusCode.TooMuchLocalPlayers;
                    playerId = player.Id;
                }
                else
                {
                    error.Code = StatusCode.OK;
                    playerId = player.Id;

                    if (!loggedInPlayers.Any(p => p.Id == playerId))
                    {
                        if (m_playerToClientId.ContainsKey(playerId))
                        {
                            Logoff(clientId, m_playerToClientId[playerId], playerId, (logoffError, guid) =>
                            {
                                if (HasError(logoffError))
                                {
                                    error = logoffError;
                                }
                                else
                                {
                                    FinishLogin(clientId, player, error, loggedInPlayers, playerId);
                                }
                                callback(error, playerId, pwdHash);
                            });
                            return;
                        }
                        else
                        {
                            FinishLogin(clientId, player, error, loggedInPlayers, playerId);
                        }
                    }
                }
                callback(error, playerId, pwdHash);
            });
        }


        public void SignUp(string name, string password, Guid clientId, ServerEventHandler<Guid, byte[]> callback)
        {
            Error error = new Error();
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, Guid.Empty, new byte[0]);
                return;
            }

            GetPlayer(name, password, (getPlayerError, player, pwdHash) =>
            {
                Guid playerId = Guid.Empty;
                if (player == null)
                {
                    error.Code = StatusCode.OK;
                    playerId = Guid.NewGuid();

                    CreatePlayer(playerId, name, password, (createPlayerError, createdPlayer, createdPwdHash) =>
                    {
                        if (HasError(createPlayerError))
                        {
                            error = createPlayerError;
                        }
                        else
                        {
                            m_playerToClientId.Add(playerId, clientId);
                            loggedInPlayers.Add(createdPlayer);
                            m_stats.PlayersCount++;

                            if (LoggedIn != null)
                            {
                                LoggedIn(error, new ServerEventArgs<Guid>(playerId) { Except = clientId });
                            }
                        }

                        callback(error, playerId, createdPwdHash);
                    });

                    return;
                }
                else if (loggedInPlayers.Count == GameConstants.MaxLocalPlayers)
                {
                    error.Code = StatusCode.TooMuchLocalPlayers;
                    playerId = player.Id;
                }
                else
                {
                    playerId = player.Id;
                    if (!loggedInPlayers.Any(p => p.Id == playerId))
                    {
                        if (m_playerToClientId.ContainsKey(playerId))
                        {
                            Logoff(clientId, m_playerToClientId[playerId], playerId, (logoffError, guid) =>
                            {
                                if (HasError(logoffError))
                                {
                                    error = logoffError;
                                }
                                else
                                {
                                    FinishLogin(clientId, player, error, loggedInPlayers, playerId);
                                }
                                callback(error, playerId, pwdHash);
                            });
                            return;
                        }
                        else
                        {
                            FinishLogin(clientId, player, error, loggedInPlayers, playerId);
                        }
                    }
                }
                callback(error, playerId, pwdHash);
            });
        }

        private void FinishLogin(Guid clientId, Player player, Error error, List<Player> loggedInPlayers, Guid playerId)
        {
            m_playerToClientId.Add(playerId, clientId);
            loggedInPlayers.Add(player);
            m_stats.PlayersCount++;

            //DO NOT RAISE THIS EVENT FOR EVERY CLIENT!
            // if (LoggedIn != null)
            //{
            //    LoggedIn(error, new ServerEventArgs<Guid>(playerId) { Except = clientId });
            //}
        }


        public void Logoff(Guid clientId, Guid playerId, ServerEventHandler<Guid> callback)
        {
            Logoff(clientId, clientId, playerId, callback);
        }

        private void Logoff(Guid senderClientId, Guid clientId, Guid playerId,  ServerEventHandler<Guid> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, Guid.Empty);
                return;
            }

            Room room;
            if (m_roomsByClientId.TryGetValue(clientId, out room))
            {
                if (room.CreatorClientId == clientId && !room.IsLaunched)
                {
                    DestroyRoom(senderClientId, clientId, room.Id, (e, g) =>
                    {
                        if (HasError(e))
                        {
                            error.Code = StatusCode.Failed;
                            error.Message = "Failed to destroy room. See inner error";
                            error.InnerError = e;
                            callback(error, playerId);
                        }
                        else
                        {
                            FinishLogOff(senderClientId, clientId, playerId, callback, error, loggedInPlayers);
                        }
                    });
                }
                else
                {
                    LeaveRoom(senderClientId, clientId, e =>
                    {
                        if (HasError(e))
                        {
                            error.Code = StatusCode.Failed;
                            error.Message = "Failed to leave room. See inner error";
                            error.InnerError = e;
                            callback(error, playerId);
                        }
                        else
                        {
                            FinishLogOff(senderClientId, clientId, playerId, callback, error, loggedInPlayers);
                        }
                    });
                }
            }
            else
            {
                FinishLogOff(senderClientId, clientId, playerId, callback, error, loggedInPlayers);
            }
        }

        private void FinishLogOff(Guid senderClientId, Guid clientId, Guid playerId, ServerEventHandler<Guid> callback, Error error, List<Player> loggedInPlayers)
        {
            Player loggedInPlayer = loggedInPlayers.Where(p => p.Id == playerId).FirstOrDefault();
            if (loggedInPlayer != null)
            {
              
                m_playerToClientId.Remove(playerId);
                loggedInPlayers.Remove(loggedInPlayer);
                m_stats.PlayersCount--;

                if (LoggedOff != null)
                {
                    if(senderClientId != clientId)
                    {
                        LoggedOff(error, new ServerEventArgs<Guid[]>(new[] { playerId }) { Except = senderClientId, Targets = new[] { clientId } });
                    }
                    
                }
            }

            callback(error, playerId);
        }

        public void Logoff(Guid clientId, Guid[] playerIds, ServerEventHandler<Guid[]> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, new Guid[0]);
                return;
            }

            Room room;
            if (m_roomsByClientId.TryGetValue(clientId, out room))
            {
                if (room.CreatorClientId == clientId && !room.IsLaunched)
                {
                    DestroyRoom(clientId, room.Id, (e, g) =>
                    {
                        if (HasError(e))
                        {
                            error.Code = StatusCode.Failed;
                            error.Message = "Failed to destroy room. See inner error";
                            error.InnerError = e;
                            callback(error, new Guid[0]);
                        }
                        else
                        {
                            FinishLogOff(clientId, playerIds, callback, error, loggedInPlayers);
                        }
                    });
                }
                else
                {
                    LeaveRoom(clientId, e =>
                    {
                        if (HasError(e))
                        {
                            error.Code = StatusCode.Failed;
                            error.Message = "Failed to leave room. See inner error";
                            error.InnerError = e;
                            callback(error, new Guid[0]);
                        }
                        else
                        {
                            FinishLogOff(clientId, playerIds, callback, error, loggedInPlayers);
                        }
                    });
                }
            }
            else
            {
                FinishLogOff(clientId, playerIds, callback, error, loggedInPlayers);
            }
        }

        private void FinishLogOff(Guid clientId, Guid[] playerIds, ServerEventHandler<Guid[]> callback, Error error, List<Player> loggedInPlayers)
        {
            List<Guid> loggedOffPlayers = new List<Guid>();
            for (int i = 0; i < playerIds.Length; i++)
            {
                Player loggedInPlayer = loggedInPlayers.Where(p => p.Id == playerIds[i]).FirstOrDefault();
                if (loggedInPlayer != null)
                {
                    m_playerToClientId.Remove(loggedInPlayer.Id);
                    loggedInPlayers.Remove(loggedInPlayer);
                    loggedOffPlayers.Add(loggedInPlayer.Id);
                    m_stats.PlayersCount--;
                }
            }

            if(loggedOffPlayers.Count > 0)
            {
                if (LoggedOff != null)
                {
                    LoggedOff(error, new ServerEventArgs<Guid[]>(loggedOffPlayers.ToArray()) { Except = clientId });
                }
            }

            callback(error, loggedOffPlayers.ToArray());
        }

        public void GetPlayer(Guid clientId, Guid playerId, ServerEventHandler<Player> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, null);
                return;
            }

            Player loggedInPlayer = loggedInPlayers.Where(p => p.Id == playerId).FirstOrDefault();
            if (loggedInPlayer == null)
            {
                error.Code = StatusCode.NotAuthenticated;
            }

            callback(error, loggedInPlayer);
        }

        public void GetPlayers(Guid clientId, Guid roomId, ServerEventHandler<Player[]> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, null);
                return;
            }

            Room room;
            if(m_roomsById.TryGetValue(roomId, out room))
            {
                error.Code = StatusCode.OK;
                GetPlayers(room.Players.ToArray(), (getPlayersError, players) =>
                {
                    if(HasError(getPlayersError))
                    {
                        callback(getPlayersError, players);
                    }
                    else
                    {
                        callback(error, players);
                    }
                });
            }
            else
            {
                error.Code = StatusCode.NotFound;
                error.Message = string.Format("Room {0} not found", roomId);
                callback(error, new Player[0]);
            }
        }

        public void GetPlayers(Guid clientId, ServerEventHandler<Player[]> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, new Player[0]);
                return;
            }

            List<Player> players = loggedInPlayers.ToList();
            callback(error, players.ToArray());
        }

        public void GetStats(Guid clientId, ServerEventHandler<ServerStats> callback)
        {
            Error error = new Error(StatusCode.OK);
            if (!m_players.ContainsKey(clientId))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, null);
                return;
            }

            callback(error, m_stats);
        }

        public void GetMaps(Guid clientId, ServerEventHandler<ByteArray[]> callback)
        {   
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, null);
                return;
            }

            if(loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, null);
                return;
            }

            ByteArray[] mapsInfo = null;
            try
            {
                string dataPath = m_persistentDataPath + "/Maps/";

                if (Directory.Exists(dataPath))
                {
                    string[] filePath = Directory.GetFiles(dataPath, "*.info", SearchOption.TopDirectoryOnly);
                    mapsInfo = new ByteArray[filePath.Length];
                    for (int i = 0; i < filePath.Length; ++i)
                    {
                        byte[] mapInfoBytes = File.ReadAllBytes(filePath[i]);
                        mapsInfo[i] = mapInfoBytes;
                    }
                }
                else
                {
                    mapsInfo = new ByteArray[0];
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);

                error.Code = StatusCode.UnhandledException;
                error.Message = e.Message;
            }
            callback(error, mapsInfo);
        }

        public void GetMaps(Guid clientId, ServerEventHandler<MapInfo[]> callback)
        {
            throw new NotSupportedException();
        }

        public void UploadMap(Guid clientId, MapInfo mapInfo, byte[] mapData, ServerEventHandler callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error);
                return;
            }

            try
            {
                string dataPath = m_persistentDataPath + "/Maps/";
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }

                //Relace with async version
                byte[] mapInfoBytes = ProtobufSerializer.Serialize(mapInfo);
                File.WriteAllBytes(dataPath + mapInfo.Id + ".info", mapInfoBytes);
                File.WriteAllBytes(dataPath + mapInfo.Id + ".data", mapData);
            }
            catch(Exception e)
            {
                Log.Error(e.Message, e);

                error.Code = StatusCode.UnhandledException;
                error.Message = e.Message;
            }

            callback(error);
        }

        public void UploadMap(Guid clientId, MapInfo mapInfo, MapData mapData, ServerEventHandler callback)
        {
            throw new NotSupportedException();
        }

        public void DownloadMapData(Guid clientId, Guid mapId, ServerEventHandler<byte[]> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                Room room;
                if(m_roomsByClientId.TryGetValue(clientId, out room))
                {
                    if(room.CreatorClientId != clientId && room.Mode != GameMode.Replay)
                    {
                        error.Code = StatusCode.NotRegistered;
                        callback(error, null);
                        return;
                    }
                }
                else
                {
                    error.Code = StatusCode.NotRegistered;
                    callback(error, null);
                    return;
                }
            }


            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, null);
                return;
            }

            DownloadMapDataById(mapId, callback);
        }

        public void DownloadMapData(Guid clientId, Guid mapId, ServerEventHandler<MapData> callback)
        {
            throw new NotSupportedException();
        }


        private void DownloadMapDataById(Guid mapId, ServerEventHandler<byte[]> callback)
        {
            Error error = new Error(StatusCode.OK);
            string dataPath = m_persistentDataPath + "/Maps/";
            string filePath = dataPath + mapId + ".data";
            if (!File.Exists(filePath))
            {
                error.Code = StatusCode.NotFound;
                callback(error, new byte[0]);
            }
            else
            {
                //Relace with async version
                byte[] mapDataBytes = new byte[0];
                try
                {
                    mapDataBytes = File.ReadAllBytes(filePath);
                }
                catch (Exception e)
                {
                    Log.Error(e.Message, e);

                    error.Code = StatusCode.UnhandledException;
                    error.Message = e.Message;
                }

                callback(error, mapDataBytes);

            }
        }

        private void GetMapInfo(Guid mapId, ServerEventHandler<MapInfo> callback)
        {
            Error error = new Error(StatusCode.OK);
            try
            {
                string dataPath = m_persistentDataPath + "/Maps/";
                string filePath = dataPath + mapId + ".info";
                if (!File.Exists(filePath))
                {
                    error.Code = StatusCode.NotFound;
                    callback(error, null);
                }
                else
                {
                    byte[] mapInfoBytes = File.ReadAllBytes(filePath);
                    MapInfo info = ProtobufSerializer.Deserialize<MapInfo>(mapInfoBytes);
                    callback(error, info);
                }
                
            }
            catch(Exception e)
            {
                Log.Error(e.Message, e);

                error.Code = StatusCode.UnhandledException;
                error.Message = e.Message;
                callback(error, null);
            }
        }

        public void CreateRoom(Guid clientId, Guid mapId, GameMode mode, ServerEventHandler<Room> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, null);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, null);
                return;
            }

            if (m_roomsByClientId.ContainsKey(clientId))
            {
                error.Code = StatusCode.AlreadyExists;
                callback(error, m_roomsByClientId[clientId]);
                return;
            }

            GetMapInfo(mapId, (getMapInfoError, mapInfo) =>
            {
                if (HasError(getMapInfoError))
                {
                    callback(getMapInfoError, null);
                    return;
                }
                else
                {
                    Room room = null;
                    if ((mapInfo.SupportedModes & mode) == 0)
                    {
                        error.Code = StatusCode.NotAllowed;
                        error.Message = string.Format("Mode {0} is not supported by {1} map", mode, mapId);
                    }
                    else
                    {
                        room = new Room();
                        room.CreatorClientId = clientId;
                        room.CreatorPlayerId = loggedInPlayers.First().Id;
                        room.MapInfo = mapInfo;
                        room.Mode = mode;
                        room.Id = Guid.NewGuid();
                        room.Players = new List<Guid>();
                        room.ReadyToLaunchPlayers = new List<Guid>();

                        if (mode != GameMode.Replay)
                        {
                            for (int i = 0; i < Math.Min(loggedInPlayers.Count, mapInfo.MaxPlayers); ++i)
                            {
                                room.Players.Add(loggedInPlayers[i].Id);
                            }
                        }

                        m_roomsByClientId.Add(clientId, room);
                        m_roomsById.Add(room.Id, room);

                        m_stats.RoomsCount++;
                    }
                    callback(error, room);
                }
            });
        }

        public void DestroyRoom(Guid clientId, Guid roomId, ServerEventHandler<Guid> callback)
        {
            DestroyRoom(clientId, clientId, roomId, callback);
        }

        private void DestroyRoom(Guid senderClientId, Guid clientId, Guid roomId, ServerEventHandler<Guid> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, Guid.Empty);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, Guid.Empty);
                return;
            }

            Room room = null;
            if (m_roomsById.TryGetValue(roomId, out room))
            {
                if (room.CreatorClientId != clientId)
                {
                    error.Code = StatusCode.NotAuthorized;
                    callback(error, roomId);
                    return;
                }

                List<Guid> playersWillLeaveRoom = new List<Guid>();
                List<Guid> botsWillLeaveRoom = new List<Guid>();
                for (int i = 0; i < room.Players.Count; ++i)
                {
                    Guid playerId = room.Players[i];
                    if (m_bots.ContainsKey(playerId))
                    {
                        botsWillLeaveRoom.Add(playerId);
                    }
                    else
                    {
                        playersWillLeaveRoom.Add(playerId);
                    }
                }

                DestroyBots(clientId, botsWillLeaveRoom.ToArray(), false, (destroyBotError, g, r) =>
                {
                    if (HasError(destroyBotError))
                    {
                        error.Code = StatusCode.Failed;
                        error.Message = "Failed to destroy bot. See inner error";
                        error.InnerError = destroyBotError;

                        Log.Error("DestroyBots. This operation should never fail. But id does " + error.ToString());

                        callback(error, roomId);
                    }
                    else
                    {
                        KickPlayers(room, kickError =>
                        {
                            if (HasError(kickError))
                            {
                                error.Code = StatusCode.Failed;
                                error.Message = "Failed to leave room. See inner error";
                                error.InnerError = kickError;
                                Log.Error("KickPlayers. This operation should never fail. But id does " + error.ToString());
                            }
                            else
                            {
                                m_stats.RoomsCount--;
                                m_roomsById.Remove(roomId);

                                if (RoomDestroyed != null)
                                {
                                    RoomDestroyed(new Error(StatusCode.OK), new ServerEventArgs { Except = senderClientId, Targets = GetTargets(senderClientId, room) });
                                }

                                if (RoomsListChanged != null)
                                {
                                    RoomsListChanged(new Error(StatusCode.OK), new ServerEventArgs { Except = senderClientId });
                                }
                            }

                            callback(error, roomId);
                        });
                    }
                });
            }
            else
            {
                callback(error, roomId);
            }
        }

        public void GetRoom(Guid clientId, ServerEventHandler<Room> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, null);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, null);
                return;
            }

            Room room;
            if (!m_roomsByClientId.TryGetValue(clientId, out room))
            {
                error.Code = StatusCode.NotFound;
                room = null;
            }
            callback(error, room);
        }

        public void GetRoom(Guid clientId, Guid roomId, ServerEventHandler<Room> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, null);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, null);
                return;
            }
            Room room;
            if (!m_roomsById.TryGetValue(roomId, out room)) 
            {
                error.Code = StatusCode.NotFound;
            }
            callback(error, room);
        }

        public void GetRooms(Guid clientId, int page, int count, ServerEventHandler<Room[]> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, null);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, null);
                return;
            }

            Room[] rooms = m_roomsById.Values.Where(r => r.Mode != GameMode.Replay).Skip(page * count).Take(count).ToArray();
            callback(error, rooms);
        }

        public void JoinRoom(Guid clientId, Guid roomId, ServerEventHandler<Room> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, null);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, null);
                return;
            }
            Room room;
            if (m_roomsByClientId.TryGetValue(clientId, out room))
            {
                error.Code = StatusCode.AlreadyJoined;
                callback(error, room);
                return;
            }


            if (m_roomsById.TryGetValue(roomId, out room))
            {
                if (room.Mode == GameMode.Replay)
                {
                    error.Code = StatusCode.NotAllowed;
                    callback(error, room);
                    return;
                }

                int expectedPlayersCount = loggedInPlayers.Count + room.Players.Count;
                if (room.MapInfo == null)
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
                        if(room.IsLaunched)
                        {
                            error.Code = StatusCode.AlreadyLaunched;
                        }
                        else
                        {
                            foreach (Player player in loggedInPlayers)
                            {
                                room.Players.Add(player.Id);
                            }

                            m_roomsByClientId.Add(clientId, room);

                            if (JoinedRoom != null)
                            {
                                JoinedRoom(new Error(StatusCode.OK), new ServerEventArgs<Guid[], Room>(loggedInPlayers.Select(p => p.Id).ToArray(), room)
                                {
                                    Except = clientId,
                                    Targets = GetTargets(clientId, room)
                                });
                            }
                        }
                    }
                }
            }
            else
            {
                error.Code = StatusCode.NotFound;
                error.Message = string.Format("Room {0} was not found", roomId);
            }
            callback(error, room);
        }

        public void LeaveRoom(Guid clientId, ServerEventHandler callback)
        {
            LeaveRoom(clientId, clientId, callback);
        }

        private void LeaveRoom(Guid senderId, Guid clientId, ServerEventHandler callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error);
                return;
            }

            Room room;
            if (m_roomsByClientId.TryGetValue(clientId, out room))
            {
                if (LeftRoom != null)
                {
                    LeftRoom(new Error(StatusCode.OK), new ServerEventArgs<Guid[], Room>(loggedInPlayers.Select(p => p.Id).ToArray(), room)
                    {
                        Except = senderId,
                        Targets = GetTargets(senderId, room)
                    });
                }

                foreach (Player player in loggedInPlayers)
                {
                    room.Players.Remove(player.Id);
                    room.ReadyToLaunchPlayers.Remove(player.Id);
                }

                if (room.CreatorClientId == clientId && !room.IsLaunched || room.Players.Count == 0 || room.Players.All(p => m_bots.ContainsKey(p)))
                {
                    DestroyRoom(clientId, room.Id, (e2, g2) =>
                    {
                        if (HasError(e2))
                        {
                            error.Code = StatusCode.Failed;
                            error.Message = "Failed to destroy room. See inner error";
                            error.InnerError = e2;
                        }
                        m_roomsByClientId.Remove(clientId);
                        m_replaysByClientId.Remove(clientId);
                        callback(error);
                    });
                }
                else
                {
                    m_roomsByClientId.Remove(clientId);
                    m_replaysByClientId.Remove(clientId);
                    callback(error);
                }
            }
            else
            {
                m_replaysByClientId.Remove(clientId);
                callback(error);
            }
        }


        private void KickPlayers(Room room, ServerEventHandler callback)
        {
            HashSet<Guid> disconnectedClients = new HashSet<Guid>();
            for(int i = 0; i < room.Players.Count; ++i)
            {
                Guid playerId = room.Players[i];
                Guid playerClientId;
                if (m_playerToClientId.TryGetValue(playerId, out playerClientId))
                {
                    m_roomsByClientId.Remove(playerClientId);
                    m_replaysByClientId.Remove(playerClientId);

                    if(!disconnectedClients.Contains(playerClientId))
                    {
                        disconnectedClients.Add(playerClientId);
                    }
                }
            }

            m_roomsByClientId.Remove(room.CreatorClientId);
            m_replaysByClientId.Remove(room.CreatorClientId);
            room.Players.Clear();
            callback(new Error(StatusCode.OK));
        }

        public void CreateBot(Guid clientId, string botName, BotType botType, ServerEventHandler<Guid, Room> callback)
        {
            CreateBots(clientId, new[] { botName }, new[] { botType }, (error, guids, room) =>
            {
                if (guids != null && guids.Length > 0)
                {
                    callback(error, guids[0], room);
                }
                else
                {
                    callback(error, Guid.Empty, room);
                }
            });
        }

        public void CreateBots(Guid clientId, string[] botNames, BotType[] botTypes, ServerEventHandler<Guid[], Room> callback) //bot guids and room with bots
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, new Guid[0], null);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, new Guid[0], null);
                return;
            }

            Guid[] botIds = new Guid[botNames.Length];
            Room room = null;
            if (m_roomsByClientId.TryGetValue(clientId, out room))
            {
                if (room.CreatorClientId != clientId)
                {
                    error.Code = StatusCode.NotAuthorized;
                    error.Message = string.Format("Not autorized to create bots in this room");
                }
                else if (room.MapInfo == null)
                {
                    error.Code = StatusCode.NotFound;
                    error.Message = string.Format("MapInfo for room {0} was not found", room.Id);
                }
                else
                {
                    int expectedPlayersCount = room.Players.Count + botNames.Length;
                    if (expectedPlayersCount > room.MapInfo.MaxPlayers)
                    {
                        error.Code = StatusCode.TooMuchPlayersInRoom;
                    }
                    else
                    {
                        error.Code = StatusCode.OK;
                        for (int i = 0; i < botNames.Length; ++i)
                        {
                            Guid botId = Guid.NewGuid();
                            Player bot = new Player
                            {
                                Id = botId,
                                Name = botNames[i],
                                BotType = botTypes[i]
                            };
                            botIds[i] = botId;
                            room.Players.Add(botId);
                            room.ReadyToLaunchPlayers.Add(botId);
                            m_bots.Add(botId, bot);
                        }

                        if (JoinedRoom != null)
                        {
                            JoinedRoom(new Error(StatusCode.OK), new ServerEventArgs<Guid[], Room>(botIds, room)
                            {
                                Except = clientId,
                                Targets = GetTargets(clientId, room)
                            });
                        }
                    }
                }
            }
            else
            {
                error.Code = StatusCode.NotFound;
                error.Message = "Room  was not found";
            }

            callback(error, botIds, room);
        }

        public void DestroyBot(Guid clientId, Guid botId, ServerEventHandler<Guid, Room> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, Guid.Empty, null);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, Guid.Empty, null);
                return;
            }


            DestroyBots(clientId, new[] { botId }, true, (e, guids, room) =>
            {
                if (guids != null && guids.Length > 0)
                {
                    callback(e, guids[0], room);
                }
                else
                {
                    callback(e, Guid.Empty, room);
                }
            });
        }

        private void DestroyBots(Guid clientId, Guid[] botIds, bool raiseGlobalEvent, ServerEventHandler<Guid[], Room> callback)
        {
            DestroyBots(clientId, clientId, botIds, raiseGlobalEvent, callback);
        }

        private void DestroyBots(Guid senderClientId, Guid clientId, Guid[] botIds, bool raiseGlobalEvent, ServerEventHandler<Guid[], Room> callback)
        {
            Error error = new Error(StatusCode.OK);
            Room room = null;
            if (m_roomsByClientId.TryGetValue(clientId, out room))
            {
                if (raiseGlobalEvent)
                {
                    if (LeftRoom != null)
                    {
                        LeftRoom(new Error(StatusCode.OK), new ServerEventArgs<Guid[], Room>(botIds, room)
                        {
                            Except = senderClientId,
                            Targets = GetTargets(senderClientId, room)
                        });
                    }
                }

                for (int i = 0; i < botIds.Length; ++i)
                {
                    Guid botId = botIds[i];
                    room.Players.Remove(botId);
                    room.ReadyToLaunchPlayers.Remove(botId);
                    m_bots.Remove(botId);
                }
            }
            else
            {
                error.Code = StatusCode.NotFound;
                error.Message = "Room was not found";
            }

            callback(error, botIds, room);
        }

        public void SetReadyToLaunch(Guid clientId, bool isReady, ServerEventHandler<Room> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, null);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, null);
                return;
            }


            Room room;
            if (!m_roomsByClientId.TryGetValue(clientId, out room))
            {
                error.Code = StatusCode.NotFound;
            }
            else
            {
                if(room.IsLaunched)
                {
                    error.Code = StatusCode.AlreadyLaunched;
                    error.Message = "Already Launched";
                    callback(error, room);
                    return;
                }

                if(isReady)
                {
                    for(int i = 0; i < loggedInPlayers.Count; ++i)
                    {
                        Player player = loggedInPlayers[i];
                        if(!room.ReadyToLaunchPlayers.Contains(player.Id))
                        {
                            room.ReadyToLaunchPlayers.Add(player.Id);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < loggedInPlayers.Count; ++i)
                    {
                        Player player = loggedInPlayers[i];
                        room.ReadyToLaunchPlayers.Remove(player.Id);
                    }
                }

                if(ReadyToLaunch != null)
                {
                    ReadyToLaunch(error, new ServerEventArgs<Room>(room) { Except = clientId, Targets = GetTargets(clientId, room) });
                }
            }

            callback(error, room);
        }

        public void Launch(Guid clientId, ServerEventHandler<string> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, string.Empty);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, string.Empty);
                return;
            }

            Room room;
            if (!m_roomsByClientId.TryGetValue(clientId, out room))
            {
                error.Code = StatusCode.NotFound;
                callback(error, string.Empty);
            }
            else
            {
                if(!room.IsReadyToLauch)
                {
                    error.Code = StatusCode.NotReady;
                    error.Message = "Room is not ready to launch";
                    callback(error, string.Empty);
                    return;
                }

                MatchServerClient matchServerClient = new MatchServerClient(m_time, m_matchServerUrl, room.Id);
                Guid roomCreatorClientId = room.CreatorClientId;
                room = ProtobufSerializer.DeepClone(room);
                room.CreatorClientId = roomCreatorClientId;
                GetPlayers(room.Players.ToArray(), (getPlayersError, players) =>
                {
                    if (HasError(getPlayersError))
                    {
                        callback(getPlayersError, string.Empty);
                    }
                    else
                    {
                        ReplayData replay = null;
                        Guid replayId;
                        if (m_replaysByClientId.TryGetValue(clientId, out replayId))
                        {
                            replay = GetReplayData(replayId);
                            m_replaysByClientId.Remove(clientId);
                        }

                        List<Guid> clientIds = new List<Guid>();
                        for (int i = 0; i < players.Length; ++i)
                        {
                            Guid guid;
                            if (m_playerToClientId.TryGetValue(players[i].Id, out guid))
                            {
                                clientIds.Add(guid);
                            }
                            else
                            {
                                clientIds.Add(Guid.Empty);
                            }
                        }

                        m_matchServerClients.Add(matchServerClient);
                        matchServerClient.CreateMatch(room.CreatorClientId, room, clientIds.ToArray(), players, replay, createMatchError =>
                        {
                            m_matchServerClients.Remove(matchServerClient);

                            string matchServerUrl = string.Format("{0}?roomId={1}", m_matchServerUrl, room.Id);

                            if (!HasError(createMatchError))
                            {
                                room.IsLaunched = true;
                                m_stats.MatchesCount++;
                                m_runningMatches.Add(room.Id);
                                m_runningMatchesNextCheck.Add(m_time.Time + RunningMatchesCheckInterval);
                                if (Launched != null)
                                {
                                    Launched(error, new ServerEventArgs<string>(matchServerUrl)
                                    {
                                        Except = clientId,
                                        Targets = GetTargets(clientId, room)
                                    });
                                }
                            }

                            callback(createMatchError, matchServerUrl);
                        });
                    }
                });
            }
        }

        private Guid[] GetTargets(Guid clientId, Room room) 
        {
            return room.Players
                .Where(p => m_playerToClientId.ContainsKey(p))
                .Select(p => m_playerToClientId[p])
                .Where(cid => cid != clientId)
                .Distinct()
                .ToArray();
        }


        public void SavePlayersStats(ServerEventHandler callback)
        {
            
        }

        public void GetReplays(Guid clientId, ServerEventHandler<ReplayInfo[]> callback)
        {
            throw new NotSupportedException();
        }

        public void GetReplays(Guid clientId, ServerEventHandler<ByteArray[]> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, new ByteArray[0]);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, new ByteArray[0]);
                return;
            }

            ByteArray[] replaysInfo = null;
            error.Code = StatusCode.OK;
            try
            {
                string dataPath = m_persistentDataPath + "/Replays/";
                if (Directory.Exists(dataPath))
                {
                    string[] filePath = Directory.GetFiles(dataPath, "*.info", SearchOption.TopDirectoryOnly);
                    replaysInfo = new ByteArray[filePath.Length];
                    for (int i = 0; i < filePath.Length; ++i)
                    {
                        byte[] replayInfoBytes = File.ReadAllBytes(filePath[i]);
                        replaysInfo[i] = replayInfoBytes;
                    }
                }
                else
                {
                    replaysInfo = new ByteArray[0];
                }
            }
            catch (Exception e)
            {
                error.Code = StatusCode.UnhandledException;
                error.Message = e.Message;
            }

            callback(error, replaysInfo);
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

        private bool ReplayExists(Guid replayId)
        {
            string dataPath = m_persistentDataPath + "/Replays/";
            string filePath = dataPath + replayId + ".data";
            return File.Exists(filePath);
        }

        public void SetReplay(Guid clientId, Guid id, ServerEventHandler callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error);
                return;
            }

            if (ReplayExists(id))
            {
                m_replaysByClientId.Remove(clientId);
                m_replaysByClientId.Add(clientId, id);
            }
            else
            {
                error.Code = StatusCode.NotFound;
            }

            callback(error);
        }

        public void SaveReplay(Guid clientId, string name, ServerEventHandler callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error);
                return;
            }

            Room room;
            if (!m_roomsByClientId.TryGetValue(clientId, out room))
            {
                error.Code = StatusCode.NotFound;
                callback(error);
                return;
            }

            if (!room.IsLaunched)
            {
                error.Code = StatusCode.NotAllowed;
                error.Message = "Unable save replay. Room was not launched";
                callback(error);
                return;
            }

            MatchServerClient matchServerClient = new MatchServerClient(m_time, m_matchServerUrl, room.Id);
            m_matchServerClients.Add(matchServerClient);
            matchServerClient.GetReplay((getReplayError, replayData, originalRoom) =>
            {
                m_matchServerClients.Remove(matchServerClient);

                if (HasError(getReplayError))
                {
                    callback(getReplayError);
                    return;
                }

                ReplayInfo replayInfo = new ReplayInfo();
                replayInfo.DateTime = DateTime.UtcNow.Ticks;
                replayInfo.Name = name;
                replayInfo.Id = replayData.Id = Guid.NewGuid();
                replayInfo.MapId = room.MapInfo.Id;

                GetPlayers(originalRoom.Players.Skip(1).ToArray(), (getPlayersError, players) =>
                {
                    if (HasError(getPlayersError))
                    {
                        callback(getPlayersError);
                        return;
                    }

                    replayInfo.PlayerNames = players.Select(p => p.Name).ToArray();

                    string dataPath = m_persistentDataPath + "/Replays/";
                    if (!Directory.Exists(dataPath))
                    {
                        Directory.CreateDirectory(dataPath);
                    }

                    byte[] replayInfoBytes = ProtobufSerializer.Serialize(replayInfo);

                    File.WriteAllBytes(dataPath + replayInfo.Id + ".info", replayInfoBytes);

                    byte[] replayDataBytes = ProtobufSerializer.Serialize(replayData);

                    File.WriteAllBytes(dataPath + replayData.Id + ".data", replayDataBytes);

                    callback(error);
                });
            });
        }

        public void SendMessage(Guid clientId, ChatMessage message, ServerEventHandler<Guid> callback)
        {
            Error error = new Error(StatusCode.OK);
            List<Player> loggedInPlayers;
            if (!m_players.TryGetValue(clientId, out loggedInPlayers))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, message.MessageId);
                return;
            }

            if (loggedInPlayers.Count == 0)
            {
                error.Code = StatusCode.NotAuthenticated;
                callback(error, message.MessageId);
                return;
            }

            if (message.ReceiverIds == null || message.ReceiverIds.Length == 0)
            {
                Room room;
                if (!m_roomsByClientId.TryGetValue(clientId, out room))
                {
                    error.Code = StatusCode.NotFound;
                    callback(error, message.MessageId);
                }
                else
                {
                    if (ChatMessage != null)
                    {
                        ChatMessage(error, new ServerEventArgs<ChatMessage>(message)
                        {
                            Targets = GetTargets(clientId, room)
                        });
                    }
                    callback(error, message.MessageId);
                }
            }
            else
            {
                List<Guid> receivers = new List<Guid>();
                for(int i = 0; i < message.ReceiverIds.Length; ++i)
                {
                    Guid receiver = message.ReceiverIds[i];
                    Guid receiverClientId;
                    if(m_playerToClientId.TryGetValue(receiver, out receiverClientId))
                    {
                        if(!receivers.Contains(receiverClientId))
                        {
                            receivers.Add(receiverClientId);
                        }
                    }

                    if(!receivers.Contains(receiverClientId))
                    {
                        receivers.Add(clientId);
                    }
                }

                if(receivers.Count > 0)
                {
                    if (ChatMessage != null)
                    {
                        ChatMessage(error, new ServerEventArgs<ChatMessage>(message)
                        {
                            Targets = receivers.ToArray(),
                        });
                    }
                }
              
                callback(error, message.MessageId);
            }
        }

        public void CancelRequests()
        {
            throw new NotSupportedException();
        }


        private List<IMatchServerClient> m_matchServerClients = new List<IMatchServerClient>();
 
        public bool Start(ITimeService time)
        {
            m_time = time;
            return true;
        }

        public void Update()
        {
            for (int i = 0; i < m_matchServerClients.Count; ++i)
            {
                m_matchServerClients[i].Update();
            }

            for(int i = m_runningMatches.Count - 1; i >= 0; i--)
            {
                Guid runningMatchId = m_runningMatches[i];
                float nextCheckTime = m_runningMatchesNextCheck[i];
                if (nextCheckTime <= m_time.Time)
                {
                    m_runningMatchesNextCheck[i] = float.MaxValue;

                    MatchServerClient matchServerClient = new MatchServerClient(m_time, m_matchServerUrl, runningMatchId);
                    m_matchServerClients.Add(matchServerClient);
                    matchServerClient.IsAlive(error =>
                    {
                        m_matchServerClients.Remove(matchServerClient);
                        int index = m_runningMatches.IndexOf(runningMatchId);
                        if (HasError(error))
                        {
                            m_stats.MatchesCount--;
                            m_runningMatches.RemoveAt(index);
                            m_runningMatchesNextCheck.RemoveAt(index);
                        }
                        else
                        {
                            m_runningMatchesNextCheck[index] = m_time.Time + RunningMatchesCheckInterval;
                        }
                    });
                }
            }
           
        }

        public void Destroy()
        {
            
        }

        public GameServerDiagInfo GetDiagInfo()
        {
            return new GameServerDiagInfo
            {
                ActiveReplaysCount = m_replaysByClientId.Count,
                ClientsJoinedToRoomsCount = m_roomsByClientId.Count,
                CreatedRoomsCount = m_roomsById.Count,
                ClinetsWithPlayersCount = m_players.Count,
                LoggedInPlayersCount = m_playerToClientId.Count,
                LoggedInBotsCount = m_bots.Count,
                RunningMatchesCount = m_stats.MatchesCount,
            };
        }
    }
}

