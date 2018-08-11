using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Battlehub.VoxelCombat
{
    //This is for testing purposes only and does not needed on client -> should be moved to server
    public class PingTimer
    {
        private class PingInfo
        {
            public float m_pingTime;
            public float[] m_intervals;
            public int m_index;
            public bool m_isInitialized;
        }

        private float m_time;

        private Dictionary<Guid, PingInfo> m_pingInfo = new Dictionary<Guid, PingInfo>();

        private bool m_initialized;

        public PingTimer(Guid[] clientIds, int intervalsCount)
        {
            for (int i = 0; i < clientIds.Length; ++i)
            {
                m_pingInfo.Add(clientIds[i],
                    new PingInfo
                    {
                        m_intervals = new float[intervalsCount]
                    });
            }
        }

        public void Update(float time)
        {
            m_time = time;
        }

        public void OnClientDisconnected(Guid clientId, Action initializedCallback)
        {
            m_pingInfo.Remove(clientId);
            if (m_pingInfo.Values.All(pi => pi.m_isInitialized))
            {
                if(!m_initialized)
                {
                    m_initialized = true;
                    initializedCallback();
                }
                
            }
        }

        public void PingAll()
        {
            foreach(PingInfo pingInfo in m_pingInfo.Values)
            {
                pingInfo.m_pingTime = m_time;
            }
        }

        public void Ping(Guid clientId)
        {
            if (m_pingInfo.ContainsKey(clientId))
            {
                m_pingInfo[clientId].m_pingTime =  m_time;
            }
        }

        public RTTInfo Pong(Guid clientId, Action initializedCallback)
        {
            PingInfo pingInfo =  m_pingInfo[clientId];

            float interval = m_time - pingInfo.m_pingTime;

            pingInfo.m_intervals[pingInfo.m_index] = interval;
            pingInfo.m_index++;
            pingInfo.m_index %= pingInfo.m_intervals.Length;
            if (pingInfo.m_index == 0)
            {
                if (!pingInfo.m_isInitialized && m_pingInfo.Values.Where(pi => pi != pingInfo).All(pi => pi.m_isInitialized))
                {
                    if (!m_initialized)
                    {
                        m_initialized = true;
                        initializedCallback();
                    }
                }
                pingInfo.m_isInitialized = true;
            }

            RTTInfo rtt = new RTTInfo();

            rtt.RTT = pingInfo.m_intervals.Average();
            rtt.RTTMax = m_pingInfo.Values.Select(pi => pi.m_intervals.Average()).Max();

            return rtt;
        }
    }

    public class BotControlManager
    {
        private IMatchEngine m_engine;
        private ITimeService m_time;
        private Player[] m_players;
        private Guid[] m_clientIds;
        private int m_botCtrlIndex = -1;
        private Guid m_clientId;
        private float m_pingTime;
        private const float m_timeout = 5;

        //public bool UseServer
        //{
        //    get { return false; }
        //}

        public BotControlManager(ITimeService time, IMatchEngine engine, Player[] players, Guid[] clientIds)
        {
            m_engine = engine;
            m_time = time;
            m_players = players;
            m_clientIds = clientIds;
        }

        public bool IsActiveBot(int playerIndex)
        {
            if (m_players[playerIndex] == null)
            {
                return false;
            }
            return m_players[playerIndex].IsActiveBot;
        }

        public bool HasControl(Guid clientId)
        {
            return m_clientId == clientId;
        }

        public void ActivateNext()
        {
            if(m_botCtrlIndex >= 0)
            {
                Player player = m_players[m_botCtrlIndex];
                if(player != null)
                {
                    m_engine.DenyBotCtrl(m_botCtrlIndex);
                }
            }

            bool isActivated = false;
            for(int i = 0; i < m_players.Length; ++i)
            {
                m_botCtrlIndex++;
                m_botCtrlIndex %= m_players.Length;
                Player player = m_players[m_botCtrlIndex];
                if(m_clientId != m_clientIds[m_botCtrlIndex] && player != null && !player.IsBot)
                {
                    m_clientId = m_clientIds[m_botCtrlIndex];
                    m_engine.GrantBotCtrl(m_botCtrlIndex);
                    m_pingTime = m_time.Time;
                    isActivated = true;
                }
            }

            if(!isActivated)
            {
                m_clientId = Guid.Empty;
                m_botCtrlIndex = -1;
            }
        }

        public void Ping(Guid clientId)
        {
            if(m_botCtrlIndex < 0)
            {
                return;
            }
            
            if(m_clientId == clientId && m_players[m_botCtrlIndex] != null)
            {
                m_pingTime = m_time.Time;
            }
        }

        public void Update()
        {
            if (m_botCtrlIndex < 0)
            {
                return;
            }

            if ((m_time.Time - m_pingTime) > m_timeout)
            {
                ActivateNext();
            }
        }

        public void Disconnect(int playerIndex)
        {
            m_players[playerIndex] = null;
            if(m_clientIds[playerIndex] == m_clientId)
            {
                m_clientId = Guid.Empty;
            }
            m_clientIds[playerIndex] = Guid.Empty;
            
        }
    }
   
    public class MatchServerImpl : IMatchServer, ILoop, IMatchServerDiagnostics
    {
        private void GetRidOfWarnings()
        {
            ConnectionStateChanging(new Error());
            ConnectionStateChanged(new Error(), new ValueChangedArgs<bool>(false, false));
        }

        private readonly ServerEventArgs<Player[], Dictionary<Guid, Dictionary<Guid, Player>>, VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], Room> m_readyToPlayAllArgs = new ServerEventArgs<Player[], Dictionary<Guid, Dictionary<Guid, Player>>, VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], Room>();
        private readonly ServerEventArgs<CommandsBundle> m_tickArgs = new ServerEventArgs<CommandsBundle>();
        private readonly ServerEventArgs<RTTInfo> m_pingArgs = new ServerEventArgs<RTTInfo>();
        private readonly ServerEventArgs<bool> m_pausedArgs = new ServerEventArgs<bool>();

        public event ServerEventHandler<ServerEventArgs<Player[], Dictionary<Guid, Dictionary<Guid, Player>>, VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], Room>> ReadyToPlayAll;
        public event ServerEventHandler<ServerEventArgs<CommandsBundle>> Tick;
        public event ServerEventHandler<ServerEventArgs<RTTInfo>> Ping;
        public event ServerEventHandler<ServerEventArgs<bool>> Paused;
        public event ServerEventHandler<ValueChangedArgs<bool>> ConnectionStateChanged;
        public event ServerEventHandler ConnectionStateChanging;
        public event ServerEventHandler<ServerEventArgs<ChatMessage>> ChatMessage;
        private IMatchEngine m_engine;
        private IReplaySystem m_replay;
        private ProtobufSerializer m_serializer;

        private float m_pauseTime;
        private float m_prevTickTime;
        private long m_tick;
        private PingTimer m_pingTimer;
        private bool m_initialized;
        private bool m_initializationStarted;

        private class PlayerCmd
        {
            public int PlayerIndex;
            public Cmd Cmd;

            public PlayerCmd(int playerIndex, Cmd cmd)
            {
                PlayerIndex = playerIndex;
                Cmd = cmd;
            }
        }

        private readonly Queue<PlayerCmd> m_preInitCommands = new Queue<PlayerCmd>();

        private Player m_neutralPlayer;
        private Guid m_serverIdentity = new Guid(ConfigurationManager.AppSettings["ServerIdentity"]);

        //private readonly List<Player> m_players;
        private readonly List<Guid> m_clientIds;
        private readonly HashSet<Guid> m_registeredClients;
        private readonly HashSet<Guid> m_readyToPlayClients;
        private readonly Dictionary<Guid, Dictionary<Guid, Player>> m_clientIdToPlayers;
        private readonly Dictionary<Guid, Player> m_players;
        private readonly Dictionary<Guid, Guid> m_playerToClientId;
        private Dictionary<Guid, VoxelAbilities[]> m_abilities;
        private Dictionary<Guid, SerializedTask[]> m_taskTemplates;
        private Dictionary<Guid, SerializedTaskTemplate[]> m_taskTemplatesInfo;

        private IBotController[] m_bots = new IBotController[0];
        private Room m_room;
    
        private string m_persistentDataPath;

        private bool enabled;
        private ITimeService m_time;
        private BotControlManager m_botControlManager;

        public bool IsConnectionStateChanging
        {
            get { throw new NotSupportedException(); }
        }

        public bool IsConnected
        {
            get { throw new NotSupportedException(); }
        }

        public void Activate()
        {
            throw new NotSupportedException();
        }

        public void Deactivate()
        {
            throw new NotSupportedException();
        }

        public MatchServerImpl(ITimeService timeService, string persistentDataPath, Guid creatorClientId, Room room, Guid[] clientIds, Player[] players, ReplayData replay)
        {
            m_serializer = new ProtobufSerializer();

            m_time = timeService;
            m_persistentDataPath = persistentDataPath;
            m_room = room;
            m_room.CreatorClientId = creatorClientId;

            m_registeredClients = new HashSet<Guid>();
            m_readyToPlayClients = new HashSet<Guid>();
            m_clientIdToPlayers = new Dictionary<Guid, Dictionary<Guid, Player>>();
            m_playerToClientId = new Dictionary<Guid, Guid>();
            m_clientIds = clientIds.ToList();
            for (int i = 0; i < m_clientIds.Count; ++i)
            {
                Guid clientId = m_clientIds[i];
                if (clientId != Guid.Empty)
                {
                    Dictionary<Guid, Player> idToPlayer;
                    if (!m_clientIdToPlayers.TryGetValue(clientId, out idToPlayer))
                    {
                        idToPlayer = new Dictionary<Guid, Player>();
                        m_clientIdToPlayers.Add(clientId, idToPlayer);
                    }
                    Player player = players[i];
                    idToPlayer.Add(player.Id, player);
                    m_playerToClientId.Add(player.Id, clientId);
                }
            }

            m_players = players.ToDictionary(p => p.Id);

            //Adding neutral player to room
            m_neutralPlayer = new Player();
            m_neutralPlayer.BotType = BotType.Neutral;
            m_neutralPlayer.Name = "Neutral";
            m_neutralPlayer.Id = Guid.NewGuid();

            //Dictionary<Guid, Player> idToPlayer = new Dictionary<Guid, Player>();
            //idToPlayer.Add(m_neutralPlayer.Id, m_neutralPlayer);
            m_players.Add(m_neutralPlayer.Id, m_neutralPlayer);

            if (!m_room.Players.Contains(m_neutralPlayer.Id))
            {
                m_room.Players.Insert(0, m_neutralPlayer.Id);
                m_clientIds.Insert(0, Guid.Empty);
            }

            m_abilities = new Dictionary<Guid, VoxelAbilities[]>();
            m_taskTemplates = new Dictionary<Guid, SerializedTask[]>();
            m_taskTemplatesInfo = new Dictionary<Guid, SerializedTaskTemplate[]>();
            for (int i = 0; i < m_room.Players.Count; ++i)
            {
                m_abilities.Add(m_room.Players[i], CreateDefaultAbilities());
                m_taskTemplates.Add(m_room.Players[i], CreateDefaultTaskTemplates());
                m_taskTemplatesInfo.Add(m_room.Players[i], CreateDefaultTaskTemplateInfo());

            }

            m_pingTimer = new PingTimer(m_clientIdToPlayers.Keys.ToArray(), 3);

            if (replay != null)
            {
                m_replay = MatchFactory.CreateReplayPlayer();
                m_replay.Load(replay);
            }
        }

        public void Destroy()
        {
            enabled = false;

            int index = m_room.Players.IndexOf(m_neutralPlayer.Id);
            if(index >= 0)
            {
                m_room.Players.RemoveAt(index);
                m_clientIds.RemoveAt(index);
            }

            if (m_engine != null)
            {
                m_engine.OnSubmitted -= OnEngineCommandSubmitted;
                MatchFactory.DestroyMatchEngine(m_engine);
                m_engine = null;
            }
        }

        private VoxelAbilities[] CreateDefaultAbilities()
        {
            List<VoxelAbilities> abilities = new List<VoxelAbilities>();
            Array voxelTypes = Enum.GetValues(typeof(KnownVoxelTypes));
            for (int typeIndex = 0; typeIndex < voxelTypes.Length; ++typeIndex)
            {
                VoxelAbilities ability = new VoxelAbilities((int)voxelTypes.GetValue(typeIndex));
                abilities.Add(ability);
            }
            return abilities.ToArray();
        }


        private SerializedTask[] CreateDefaultTaskTemplates()
        {
            List<SerializedTask> taskTemplates = new List<SerializedTask>();
            taskTemplates.Add(SerializedTask.FromTaskInfo(TaskInfo.EatGrowSplit4()));
            return taskTemplates.ToArray();
        }

        private SerializedTaskTemplate[] CreateDefaultTaskTemplateInfo()
        {
            return new[] { new SerializedTaskTemplate { Name = "Eat Grow Split4", Col = 2, Row = 2 } };
        }

        public bool IsLocal(Guid clientId, Guid playerId)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Check whether status has error code
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool HasError(Error error)
        {
            return error.Code != StatusCode.OK;
        }

        public void RegisterClient(Guid clientId, ServerEventHandler callback)
        {
            if(!m_registeredClients.Contains(clientId))
            {
                m_registeredClients.Add(clientId);
            }
            callback(new Error(StatusCode.OK));
        }

        public void UnregisterClient(Guid clientId, ServerEventHandler callback)
        {
            m_registeredClients.Remove(clientId);

            Dictionary<Guid, Player> disconnectedPlayers;
            if (m_clientIdToPlayers.TryGetValue(clientId, out disconnectedPlayers))
            {
                foreach (Guid playerId in disconnectedPlayers.Keys)
                {
                    int playerIndex = m_room.Players.IndexOf(playerId);

                    //m_players.Remove(playerId);

                    m_botControlManager.Disconnect(playerIndex);
                    
                    //m_room.Players.RemoveAt(playerIndex);

                    Cmd cmd = new Cmd(CmdCode.LeaveRoom, -1);

                    //#warning Fix Engine to handle LeaveRoom command without removing PlayerControllers. Just change colors or destroy units
                    if (m_initialized)
                    {
                        m_botControlManager.ActivateNext();

                        m_engine.Submit(playerIndex, cmd);
                    }
                    else
                    {
                        m_preInitCommands.Enqueue(new PlayerCmd(playerIndex, cmd));
                    }

                    m_playerToClientId.Remove(playerId);
                }

                m_readyToPlayClients.Remove(clientId);
                m_clientIdToPlayers.Remove(clientId);

                if(!m_initialized)
                {
                    Error error = new Error(StatusCode.OK);
                    m_pingTimer.OnClientDisconnected(clientId, () => OnPingPongCompleted(error, clientId));

                    if (!HasError(error))
                    {
                        TryToInitEngine(callback);
                    }
                    else
                    {
                        callback(error);
                    }
                }
            }
        }

        public void DownloadMapData(Guid clientId, ServerEventHandler<MapData> callback)
        {
            if (!m_clientIdToPlayers.ContainsKey(clientId))
            {
                if (m_room != null)
                {
                    if (m_room.CreatorClientId != clientId && m_room.Mode != GameMode.Replay)
                    {
                        Error error = new Error(StatusCode.NotRegistered);
                        callback(error, null);
                        return;
                    }
                }
                else
                {
                    Error error = new Error(StatusCode.NotRegistered);
                    callback(error, null);
                    return;
                }
            }

            DownloadMapData(callback);
        }

        private void DownloadMapData(ServerEventHandler<MapData> callback)
        {
            DownloadMapDataById(m_room.MapInfo.Id, (error, mapDataBytes) =>
            {
                MapData mapData = null;
                try
                {
                    if (!HasError(error))
                    {
                        mapData = m_serializer.Deserialize<MapData>(mapDataBytes);
                    }
                }
                catch (Exception e)
                {
                    error = new Error(StatusCode.UnhandledException) { Message = e.ToString() };
                }

                callback(error, mapData);
            });
        }

        public void DownloadMapData(Guid clientId, ServerEventHandler<byte[]> callback)
        {
            if (!m_clientIdToPlayers.ContainsKey(clientId))
            {
                if (m_room != null)
                {
                    if (m_room.CreatorClientId != clientId && m_room.Mode != GameMode.Replay)
                    {
                        Error error = new Error(StatusCode.NotRegistered);
                        callback(error, null);
                        return;
                    }
                }
                else
                {
                    Error error = new Error(StatusCode.NotRegistered);
                    callback(error, null);
                    return;
                }
            }

            DownloadMapDataById(m_room.MapInfo.Id, callback);
        }

        private void DownloadMapDataById(Guid mapId, ServerEventHandler<byte[]> callback)
        {
            byte[] mapData = new byte[0];
            Error error = new Error();

            string dataPath = m_persistentDataPath + "/Maps/";
            string filePath = dataPath + mapId + ".data";
            if (!File.Exists(filePath))
            {
                error.Code = StatusCode.NotFound;

                callback(error, mapData);
            }
            else
            {
                error.Code = StatusCode.OK;
                try
                {
                    mapData = File.ReadAllBytes(filePath);
                }
                catch (Exception e)
                {
                    error.Code = StatusCode.UnhandledException;
                    error.Message = e.Message;
                }

                callback(error, mapData);
            }
        }

        public void ReadyToPlay(Guid clientId, ServerEventHandler callback)
        {
            if (!m_clientIdToPlayers.ContainsKey(clientId))
            {
                if (m_room != null)
                {
                    if (m_room.CreatorClientId != clientId && m_room.Mode != GameMode.Replay)
                    {
                        Error error = new Error(StatusCode.NotRegistered);
                        callback(error);
                        return;
                    }
                }
                else
                {
                    Error error = new Error(StatusCode.NotRegistered);
                    callback(error);
                    return;
                }
            }

            if(m_room == null || m_room.Mode != GameMode.Replay)
            {
                if (!m_readyToPlayClients.Contains(clientId))
                {
                    m_readyToPlayClients.Add(clientId);
                }
            }

            TryToInitEngine(callback);
        }

        public void Submit(Guid clientId, int playerIndex, Cmd cmd, ServerEventHandler<Cmd> callback)
        {
            Error error = new Error(StatusCode.OK);
            if (!m_clientIdToPlayers.ContainsKey(clientId))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, cmd);
                return;
            }

            if(cmd.Code == CmdCode.LeaveRoom)
            {
                error.Code = StatusCode.NotAllowed;
            }
            else
            {
                if (!m_initialized)
                {
                    error.Code = StatusCode.NotAllowed;
                    error.Message = "Match is not initialized";
                }
                else if(!enabled)
                {
                    error.Code = StatusCode.Paused;
                    error.Message = "Match is paused"; 
                }
                else if (playerIndex < 0 || playerIndex >= m_room.Players.Count)
                {
                    error.Code = StatusCode.NotAllowed;
                    error.Message = "Wrong player index";
                }
                else
                {
                    if(m_botControlManager.IsActiveBot(playerIndex))
                    {
                        if (m_botControlManager.HasControl(clientId))
                        {
                            m_engine.Submit(playerIndex, cmd); // if I will use RTT Ticks then it will be possible to reverse order of commands sent by client (which is BAD!)
                        }
                        else
                        {
                            error.Code = StatusCode.NotAuthorized;
                            error.Message = "Not authorized";
                        }
                    }
                    else
                    {
                        Guid playerClientId;
                        if (m_playerToClientId.TryGetValue(m_room.Players[playerIndex], out playerClientId))
                        {
                            if(clientId == playerClientId)
                            {
                                m_engine.Submit(playerIndex, cmd);
                            }
                            else
                            {
                                error.Code = StatusCode.NotAuthorized;
                                error.Message = "Not authorized";
                            }
                        }
                        else
                        {
                            error.Code = StatusCode.NotAuthorized;
                            error.Message = "Not authorized";
                        }
                    }       
                }
            }
            callback(error, cmd);
        }

        public void SubmitResponse(Guid clientId, ClientRequest response, ServerEventHandler<ClientRequest> callback)
        {
            Error error = new Error(StatusCode.OK);
            if (!m_clientIdToPlayers.ContainsKey(clientId))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, response);
                return;
            }

            if (!m_initialized)
            {
                error.Code = StatusCode.NotAllowed;
                error.Message = "Match is not initialized";
            }
            else if (!enabled)
            {
                error.Code = StatusCode.Paused;
                error.Message = "Match is paused";
            }
            else
            {
                m_engine.SubmitResponse(response); 
            }

            callback(error, response);
        }


        private void OnEngineCommandSubmitted(int playerIndex, Cmd cmd)
        {
            m_replay.Record(playerIndex, cmd, m_tick);
        }

        public void Pong(Guid clientId, ServerEventHandler callback)
        {
            Error error = new Error(StatusCode.OK);
            if (!m_clientIdToPlayers.ContainsKey(clientId))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error);
                return;
            }

            callback(error);

            if(m_botControlManager != null)
            {
                m_botControlManager.Ping(clientId);
            }
            
            RTTInfo rttInfo = m_pingTimer.Pong (clientId, () => OnPingPongCompleted(error, clientId));
            m_pingTimer.Ping(clientId);
            if (Ping != null)
            {
                m_pingArgs.Arg = rttInfo;
                m_pingArgs.Targets = new[] { clientId };
                Ping(error, m_pingArgs);
            }


        }

        private void OnPingPongCompleted(Error error, Guid clientId)
        {
            //Currently MatchEngine will be launched immediately and it does not care about different RTT for diffierent clients.
            //Some clients will look -50 ms to the past and some clients will look -500 ms or more to the past.
            //Is this a big deal? Don't know... Further investigation and playtest needed

            if(m_engine == null)
            {
                throw new InvalidOperationException("m_engine == null");
            }

            Player[] players;
            VoxelAbilitiesArray[] abilities;
            SerializedTaskArray[] taskInfo;
            SerializedTaskTemplatesArray[] taskTemplateInfo;
            if (m_room != null)
            {
                enabled = true;
                m_prevTickTime = m_time.Time;
                m_initialized = true;

                error.Code = StatusCode.OK;
                players = new Player[m_players.Count];

                List<IBotController> bots = new List<IBotController>();

                //Will override or
                abilities = new VoxelAbilitiesArray[m_players.Count];
                taskInfo = new SerializedTaskArray[m_players.Count];
                taskTemplateInfo = new SerializedTaskTemplatesArray[m_players.Count];
                for (int i = 0; i < m_players.Count; ++i)
                {
                    Player player = m_players[m_room.Players[i]];

                    players[i] = player;
                    abilities[i] = m_abilities[m_room.Players[i]];
                    taskInfo[i] = m_taskTemplates[m_room.Players[i]];
                    taskTemplateInfo[i] = m_taskTemplatesInfo[m_room.Players[i]];

                    if (player.IsBot && player.BotType != BotType.Replay)
                    {
                        bots.Add(MatchFactory.CreateBotController(player, m_engine.GetTaskEngine(i)));
                    }
                }

                m_bots = bots.ToArray();
            }
            else
            {
                error.Code = StatusCode.NotFound;
                error.Message = "Room not found";
                players = new Player[0];
                abilities = new VoxelAbilitiesArray[0];
                taskInfo = new SerializedTaskArray[0];
                taskTemplateInfo = new SerializedTaskTemplatesArray[0];
            }

            m_readyToPlayAllArgs.Arg = players;
            m_readyToPlayAllArgs.Arg2 = m_clientIdToPlayers;
            m_readyToPlayAllArgs.Arg3 = abilities;
            m_readyToPlayAllArgs.Arg4 = taskInfo;
            m_readyToPlayAllArgs.Arg5 = taskTemplateInfo;
            m_readyToPlayAllArgs.Arg6 = m_room;
            m_readyToPlayAllArgs.Except = Guid.Empty;

            if (HasError(error))
            {
                ReadyToPlayAll(error, m_readyToPlayAllArgs);
            }
                        
            m_botControlManager = new BotControlManager(
                m_time, 
                m_engine, 
                m_room.Players.Select(p => m_players.ContainsKey(p) ? m_players[p] : null).ToArray(),
                m_clientIds.ToArray());
            m_botControlManager.ActivateNext();
        }

        public void Pause(Guid clientId, bool pause, ServerEventHandler callback)
        {
            Error error = new Error(StatusCode.OK);
            if (!m_clientIdToPlayers.ContainsKey(clientId))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error);
                return;
            }

            if (!m_initialized)
            {
                error.Code = StatusCode.NotAllowed;
                error.Message = "Match is not initialized";
            }
            else
            {
                enabled = !pause;
                if(enabled)
                {
                    m_prevTickTime += (m_time.Time - m_pauseTime);
                }
                else
                {
                    m_pauseTime = m_time.Time;
                }
            }

            if (Paused != null)
            {
                m_pausedArgs.Arg = pause;
                m_pausedArgs.Except = clientId;
                Paused(error, m_pausedArgs);
            }
            callback(error);
        }

        public void GetReplay(Guid clientId, ServerEventHandler<ReplayData, Room> callback)
        {
            Error error = new Error(StatusCode.OK);
            if (!m_clientIdToPlayers.ContainsKey(clientId) && clientId != m_serverIdentity)
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, null, null);
                return;
            }

            ReplayData replay = m_replay.Save();
            if (!m_initialized)
            {
                error.Code = StatusCode.NotAllowed;
                error.Message = "Match was not initialized";
            }
            callback(error, replay, m_room);
        }

        public void SendMessage(Guid clientId, ChatMessage message, ServerEventHandler<Guid> callback)
        {
            Error error = new Error(StatusCode.OK);
            if (!m_clientIdToPlayers.ContainsKey(clientId))
            {
                error.Code = StatusCode.NotRegistered;
                callback(error, message.MessageId);
                return;
            }

            if (message.ReceiverIds == null || message.ReceiverIds.Length == 0)
            {
                if (ChatMessage != null)
                {
                    ChatMessage(error, new ServerEventArgs<ChatMessage>(message));
                }
                callback(error, message.MessageId);
            }
            else
            {
                List<Guid> receivers = new List<Guid>();
                for (int i = 0; i < message.ReceiverIds.Length; ++i)
                {
                    Guid receiver = message.ReceiverIds[i];
                    Guid receiverClientId;
                    if (m_playerToClientId.TryGetValue(receiver, out receiverClientId))
                    {
                        if (!receivers.Contains(receiverClientId))
                        {
                            receivers.Add(receiverClientId);
                        }
                    }
                }

                if (receivers.Count > 0)
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

        private void TryToInitEngine(ServerEventHandler callback)
        {
            if (!m_initializationStarted && m_engine == null && (m_room != null && m_room.Mode == GameMode.Replay ||  m_readyToPlayClients.Count > 0 && m_readyToPlayClients.Count == m_clientIdToPlayers.Count))
            {
                InitEngine(callback);
            }
            else
            {
                callback(new Error(StatusCode.OK));
            }
        }

        private void InitEngine(ServerEventHandler callback)
        {
            m_initializationStarted = true;
            DownloadMapData((Error error, MapData mapData) =>
            {
                m_initializationStarted = false;
                if (HasError(error))
                {
                    if (callback != null)
                    {
                        callback(error);
                    }
                }
                else
                {
                    MapRoot mapRoot = m_serializer.Deserialize<MapRoot>(mapData.Bytes);
                    IMatchEngine engine = MatchFactory.CreateMatchEngine(mapRoot, m_room.Players.Count);

                    Dictionary<int, VoxelAbilities>[] allAbilities = new Dictionary<int, VoxelAbilities>[m_room.Players.Count];
                    for (int i = 0; i < m_room.Players.Count; ++i)
                    {
                        allAbilities[i] = m_abilities[m_room.Players[i]].ToDictionary(a => a.Type);
                    }

                    if (m_replay == null)
                    {
                        m_replay = MatchFactory.CreateReplayRecorder();
                    }

                    //Zero is neutral
                    for (int i = 0; i < m_room.Players.Count; ++i)
                    {
                        Guid playerGuid = m_room.Players[i];
                        engine.RegisterPlayer(m_room.Players[i], i, allAbilities);
                    }
                    engine.CompletePlayerRegistration();

                    if(m_engine != null)
                    {
                        m_engine.OnSubmitted -= OnEngineCommandSubmitted;
                    }
                
                    m_engine = engine;
                    m_engine.OnSubmitted += OnEngineCommandSubmitted;

                    if (callback != null)
                    {
                        callback(error);
                    }

                    if(m_room.Mode != GameMode.Replay)
                    {
                        m_pingTimer.PingAll();

                        if (Ping != null)
                        {
                            m_pingArgs.Arg = new RTTInfo();
                            m_pingArgs.Except = Guid.Empty;
                            Ping(new Error(StatusCode.OK), m_pingArgs);
                        }
                    }
                    else
                    {
                        OnPingPongCompleted(new Error(StatusCode.OK), m_room.CreatorClientId);
                    }
                   
                }
            });
        }


        public bool Start(ITimeService time)
        {
            m_time = time;

            if (m_pingTimer != null)
            {
                m_pingTimer.Update(m_time.Time);
            }

            if(!enabled)
            {
                return false;
            }

            List<Guid> notRegisteredClients = new List<Guid>();
            foreach(KeyValuePair<Guid, Dictionary<Guid, Player>> kvp in m_clientIdToPlayers)
            {
                if(!m_registeredClients.Contains(kvp.Key))
                {
                    notRegisteredClients.Add(kvp.Key);
                }               
            }

            for(int i = 0; i < notRegisteredClients.Count; ++i)
            {
                m_clientIdToPlayers.Remove(notRegisteredClients[i]);
            }

            m_prevTickTime = m_time.Time;
            if (ReadyToPlayAll != null)
            {
                ReadyToPlayAll(new Error(StatusCode.OK), m_readyToPlayAllArgs);
            }

            while(m_preInitCommands.Count > 0)
            {
                PlayerCmd playerCmd = m_preInitCommands.Dequeue();
                m_engine.Submit(playerCmd.PlayerIndex, playerCmd.Cmd);
            }

            return true;
        }

        public void Update()
        {
            if(m_pingTimer != null)
            {
                m_pingTimer.Update(m_time.Time);
            }

            if(!enabled)
            {
                return;
            }

            m_engine.Update();

            //if(m_botControlManager.UseServer)
            //{
                //for (int i = 0; i < m_bots.Length; ++i)
                //{
                //    m_bots[i].Update(m_time.Time);
                //}
            //}
            //else
            {
                m_botControlManager.Update();
            }
            
            FixedUpdate();
        }

        private void FixedUpdate()
        {
            if(m_engine == null)
            {
                return;
            }

            while ((m_time.Time - m_prevTickTime) >= GameConstants.MatchEngineTick)
            {
                m_replay.Tick(m_engine, m_tick);
                
                CommandsBundle commands;
                if (m_engine.Tick(out commands))
                {
                    commands.Tick = m_tick;
                    if (Tick != null)
                    {
                        Error error = new Error(StatusCode.OK);
                        m_tickArgs.Except = Guid.Empty;
                        m_tickArgs.Arg = commands;
                        Tick(error, m_tickArgs);
                    }
                }

                m_tick++;
                m_prevTickTime += GameConstants.MatchEngineTick;
            }
        }

        public void CancelRequests()
        {
            throw new NotSupportedException();
        }

        public MatchServerDiagInfo GetDiagInfo()
        {
            return new MatchServerDiagInfo
            {
                IsInitializationStarted = m_initializationStarted,
                IsInitialized = m_initialized,
                IsEnabled = enabled,
                IsMatchEngineCreated = m_engine != null,
                IsReplay = m_room != null && m_room.Mode == GameMode.Replay,
                ServerRegisteredClientsCount = m_registeredClients.Count,
                ReadyToPlayClientsCount = m_readyToPlayClients.Count,
                ClientsWithPlayersCount = m_clientIdToPlayers.Count,
                PlayersCount = m_players.Count,
                BotsCount = m_bots.Length,
            };
        }

        public void GetTaskTemplates(Guid clientId, Guid playerId, ServerEventHandler<SerializedTask[], SerializedTaskTemplate[]> callback)
        {
            throw new NotImplementedException();
        }

        public void SaveTaskTemplate(Guid clientId, Guid playerId, SerializedTask taskTemplate, SerializedTaskTemplate templateInfo, ServerEventHandler callback)
        {
            throw new NotImplementedException();
        }
    }
}
