using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

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

        private Dictionary<Guid, PingInfo> m_pingInfo = new Dictionary<Guid, PingInfo>();

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

        public void Ping(Guid clientId)
        {
            if(m_pingInfo.ContainsKey(clientId))
            {
                m_pingInfo[clientId].m_pingTime = Time.realtimeSinceStartup;
            }
            else
            {
                //For debugging purposes. Should be removed later
                m_pingInfo[Guid.Empty].m_pingTime = Time.realtimeSinceStartup;
            }    
        }

        public RTTInfo Pong(Guid clientId, Action initializedCallback)
        {
            PingInfo pingInfo = m_pingInfo.ContainsKey(clientId) ?
                m_pingInfo[clientId] :
                m_pingInfo[Guid.Empty]; //For debugging purposes. Should be removed later

            float interval = Time.realtimeSinceStartup - pingInfo.m_pingTime;

            pingInfo.m_intervals[pingInfo.m_index] = interval;
            pingInfo.m_index++;
            pingInfo.m_index %= pingInfo.m_intervals.Length;
            if(pingInfo.m_index == 0)
            {
                if(!pingInfo.m_isInitialized && m_pingInfo.Values.Where(pi => pi != pingInfo).All(pi => pi.m_isInitialized))
                {
                    initializedCallback();
                    pingInfo.m_isInitialized = true;
                }
            }


            RTTInfo rtt = new RTTInfo();

            rtt.RTT = pingInfo.m_intervals.Average();
            rtt.RTTMax = m_pingInfo.Values.Select(pi => pi.m_intervals.Average()).Max();

            return rtt;
        }
    }

    public class LocalMatchServer : MonoBehaviour, IMatchServer
    {
        public event ServerEventHandler<Player[], Guid[], VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], Room> ReadyToPlayAll;
        
        public event ServerEventHandler<CommandsBundle> Tick;
        public event ServerEventHandler<RTTInfo> Ping;
        public event ServerEventHandler<bool> Paused;
        public event ServerEventHandler<ValueChangedArgs<bool>> ConnectionStateChanged;
        public event ServerEventHandler ConnectionStateChanging;
        public event ServerEventHandler<ChatMessage> ChatMessage;

        public bool IsConnectionStateChanging
        {
            get;
            private set;
        }

        public bool IsConnected
        {
            get;
            private set;
        }

        private IGlobalState m_gState;
        private IJob m_job;
        private IMatchEngine m_engine;
        private IReplaySystem m_replay;

        private float m_pauseTime;
        private float m_prevTickTime;
        private long m_tick;
        private PingTimer m_pingTimer;
        private bool m_initialized;

        private Player m_neutralPlayer;
        private HashSet<Guid> m_loggedInPlayers;
        private Dictionary<Guid, Player> m_players;
        private Dictionary<Guid, VoxelAbilities[]> m_abilities;
        private Dictionary<Guid, SerializedTask[]> m_tasks;
        private Dictionary<Guid, SerializedTaskTemplate[]> m_serializedTaskTemplates;

        private Guid m_botAuthority;
        private IBotController[] m_bots;
        private Room m_room;

        [SerializeField]
        private int m_lag = 0;
        public int Lag
        {
            get { return m_lag; }
            set { m_lag = value; }
        }

        private string m_persistentDataPath;

        private void Awake()
        {
            if (Dependencies.RemoteGameServer != null && Dependencies.RemoteGameServer.IsConnected)
            {
                gameObject.SetActive(false);
                return;
            }
            m_gState = Dependencies.State;
            m_job = Dependencies.Job;

            //Adding neutral player to room
            m_neutralPlayer = new Player();
            m_neutralPlayer.BotType = BotType.Neutral;
            m_neutralPlayer.Name = "Neutral";
            m_neutralPlayer.Id = Guid.NewGuid();

            enabled = false; //Will be set to true when match engine will be ready
        }

        public void Connect()
        {
            if (ConnectionStateChanging != null)
            {
                ConnectionStateChanging(new Error(StatusCode.OK));
            }
            bool wasConnected = IsConnected;
            IsConnected = true;
            if (ConnectionStateChanged != null)
            {
                ConnectionStateChanged(new Error(StatusCode.OK), new ValueChangedArgs<bool>(wasConnected, IsConnected));
            }
        }

        public void Disconnect()
        {
            if (ConnectionStateChanging != null)
            {
                ConnectionStateChanging(new Error(StatusCode.OK));
            }
            bool wasConnected = IsConnected;
            IsConnected = false;
            if (ConnectionStateChanged != null)
            {
                ConnectionStateChanged(new Error(StatusCode.OK), new ValueChangedArgs<bool>(wasConnected, IsConnected));
            }
        }


        public void Activate()
        {
            if (Dependencies.RemoteGameServer != null && Dependencies.RemoteGameServer.IsConnected)
            {
                return;
            }

            if(m_gState == null)
            {
                m_gState = Dependencies.State;
            }
            if(m_job == null)
            {
                m_job = Dependencies.Job;
            }
            //m_persistentDataPath = Application.persistentDataPath;
            m_persistentDataPath = Application.streamingAssetsPath;

            m_loggedInPlayers = m_gState.GetValue<HashSet<Guid>>("LocalGameServer.m_loggedInPlayers");
            if (m_loggedInPlayers == null)
            {
                m_loggedInPlayers = new HashSet<Guid>();
            }

            m_room = m_gState.GetValue<Room>("LocalGameServer.m_room");
            if (m_room == null)
            {
                m_room = new Room();
                m_room.Players = new List<Guid>();
                m_room.MapInfo = new MapInfo
                {
                    Name = "Default",
                    MaxPlayers = GameConstants.MaxPlayers,
                    SupportedModes = GameMode.All
                };
            }

            m_players = m_gState.GetValue<Dictionary<Guid, Player>>("LocalGameServer.m_players");
            if (m_players == null)
            {
                m_players = new Dictionary<Guid, Player>();
            }

            if (!m_players.ContainsKey(m_neutralPlayer.Id))
            {
                m_players.Add(m_neutralPlayer.Id, m_neutralPlayer);
            }

            if(!m_room.Players.Contains(m_neutralPlayer.Id))
            {
                m_room.Players.Insert(0, m_neutralPlayer.Id);
            }

            m_abilities = new Dictionary<Guid, VoxelAbilities[]>();
            m_tasks = new Dictionary<Guid, SerializedTask[]>();
            m_serializedTaskTemplates = new Dictionary<Guid, SerializedTaskTemplate[]>();
            for (int i = 0; i < m_room.Players.Count; ++i)
            {
                m_abilities.Add(m_room.Players[i], CreateDefaultAbilities());
                m_tasks.Add(m_room.Players[i], CreateDefaultTaskTemplates());
                m_serializedTaskTemplates.Add(m_room.Players[i], CreateDefaultTaskTemplateData());

            }

            Guid[] clientIds = new[] { Guid.Empty };
            m_pingTimer = new PingTimer(clientIds, 3);

            ReplayData replay =  m_gState.GetValue<ReplayData>("LocalGameServer.m_replay");
            if(replay != null)
            {
                m_replay = MatchFactory.CreateReplayPlayer();
                m_replay.Load(replay);
            }
            Connect();
        }
     
        public void Deactivate()
        {
          
        }

        private void OnDestroy()
        {
            if(m_gState != null)
            {
                m_gState.SetValue("LocalGameServer.m_replay", null);
            }

            if(m_players != null)
            {
                m_players.Remove(m_neutralPlayer.Id);
            }

            if(m_room != null)
            {
                m_room.Players.Remove(m_neutralPlayer.Id);
            }

            if(m_bots != null)
            {
                for (int i = 0; i < m_bots.Length; ++i)
                {
                    IBotController bot = m_bots[i];
                    if (bot != null)
                    {
                        MatchFactory.DestroyBotController(bot);
                    }
                }
            }

            if (m_engine != null)
            {
                MatchFactory.DestroyMatchEngine(m_engine);
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
            taskTemplates.Add(SerializedTask.FromTaskInfo(TaskInfo.EatGrowSplit4(new TaskInputInfo(), new TaskInputInfo())));
            return taskTemplates.ToArray();
        }

        private SerializedTaskTemplate[] CreateDefaultTaskTemplateData()
        {
            return new[] { new SerializedTaskTemplate { Name = "Eat Grow Split4", Col = 2, Row = 2, Type = TaskTemplateType.EatGrowSplit4 } };
        }

        public bool IsLocal(Guid clientId, Guid playerId)
        {
            return m_loggedInPlayers.Contains(playerId); 
        }

        public void RegisterClient(Guid clientId, ServerEventHandler callback)
        {
            throw new NotSupportedException();
        }

        public void UnregisterClient(Guid clientId, ServerEventHandler callback)
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

        public void DownloadMapData(Guid clientId, ServerEventHandler<byte[]> callback)
        {
            throw new NotSupportedException();
        }

        public void DownloadMapData(Guid clientId, ServerEventHandler<MapData> callback)
        {
            DownloadMapDataById(m_room.MapInfo.Id, callback);
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

                if (m_lag == 0)
                {
                    callback(error, mapData);
                }
                else
                {
                    m_job.Submit(() => { Thread.Sleep(m_lag); return null; }, result => callback(error, mapData));
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


        public void ReadyToPlay(Guid clientId, ServerEventHandler callback)
        {
            Error error = new Error();
            error.Code = StatusCode.OK;

            if (m_lag == 0)
            {
                InitEngine(clientId, callback); 
            }
            else
            {
                m_job.Submit(() => 
                {
                    Thread.Sleep(m_lag); return null;
                }, 
                result =>
                {
                    InitEngine(clientId, callback); 
                });
            }
        }

        public void Submit(Guid clientId, int playerIndex, Cmd cmd, ServerEventHandler<Cmd> callback)
        {
            cmd = ProtobufSerializer.DeepClone(cmd);

            Error error = new Error();
            error.Code = StatusCode.OK;
           
            if(cmd.Code == CmdCode.LeaveRoom)
            {
                error.Code = StatusCode.NotAllowed;
                error.Message = "Use LeaveRoom method instead";
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
                else
                {
                    m_replay.Record(playerIndex, cmd, m_tick);
                    m_engine.Submit(playerIndex, cmd); // if I will use RTT Ticks then it will be possible to reverse order of commands sent by client (which is BAD!)
                }
            }
            
            if (m_lag == 0)
            {
                callback(error, cmd);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(m_lag); return null; }, result => callback(error, cmd));
            }
        }

        public void SubmitResponse(Guid clientId, ClientRequest response, ServerEventHandler<ClientRequest> callback)
        {
            Error error = new Error();
            error.Code = StatusCode.OK;

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
                Cmd cmd = response.Cmd;
                if(cmd != null && cmd.Code != CmdCode.DenyBotCtrl && cmd.Code != CmdCode.GrantBotCtrl)
                {
                    m_engine.SubmitResponse(response);
                }
            }

            if (m_lag == 0)
            {
                callback(error, response);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(m_lag); return null; }, result => callback(error, response));
            }
        }

        public void Pong(Guid clientId, ServerEventHandler callback)
        {
            Error error = new Error();
            error.Code = StatusCode.OK;

            m_job.Submit(() =>
            {
                Thread.Sleep(400);
                return null;
            },
            result =>
            {
                callback(error);

                RTTInfo rttInfo = m_pingTimer.Pong(clientId, () =>
                {
                    //Currently MatchEngine will be launched immediately and it does not care about different RTT for diffierent clients.
                    //Some clients will look -50 ms to the past and some clients will look -500 ms or more to the past.
                    //Is this a big deal? Don't know... Further investigation and playtest needed

                    enabled = true;
                    m_prevTickTime = Time.realtimeSinceStartup;
                    m_initialized = true;

                    m_room = m_gState.GetValue<Room>("LocalGameServer.m_room");

                    Player[] players;
                    VoxelAbilitiesArray[] abilities;
                    SerializedTaskArray[] serializedTasks;
                    SerializedTaskTemplatesArray[] serializedTaskTemplates;

                    if (m_room != null)
                    {
                        error.Code = StatusCode.OK;
                        players = new Player[m_room.Players.Count];

                        List<IBotController> bots = new List<IBotController>();

                        //Will override or
                        abilities = new VoxelAbilitiesArray[m_room.Players.Count];
                        serializedTasks = new SerializedTaskArray[m_room.Players.Count];
                        serializedTaskTemplates = new SerializedTaskTemplatesArray[m_room.Players.Count];
                        for (int i = 0; i < m_room.Players.Count; ++i)
                        {
                            Player player = m_players[m_room.Players[i]];

                            players[i] = player;
                            abilities[i] = m_abilities[m_room.Players[i]];
                            serializedTasks[i] = m_tasks[m_room.Players[i]];
                            serializedTaskTemplates[i] = m_serializedTaskTemplates[m_room.Players[i]];
                            if (player.IsBot && player.BotType != BotType.Replay && player.BotType != BotType.Neutral)
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
                        serializedTasks = new SerializedTaskArray[0];
                        serializedTaskTemplates = new SerializedTaskTemplatesArray[0];
                    }

                    RaiseReadyToPlayAll(error, players, abilities, serializedTasks, serializedTaskTemplates);
                    m_engine.GrantBotCtrl(0);
                });

                m_pingTimer.Ping(clientId);
                if(Ping != null)
                {
                    Ping(error, rttInfo);
                }
            });
        }

        private void RaiseReadyToPlayAll(Error error, Player[] players, VoxelAbilitiesArray[] abilities, SerializedTaskArray[] serializedTasks, SerializedTaskTemplatesArray[] serializedTaskTemplates)
        {
            m_job.Submit(() =>
            {
                Thread.Sleep(100);
                return null;
            },
            result =>
            {
                if (ReadyToPlayAll != null)
                {
                    ReadyToPlayAll(error, players, m_loggedInPlayers.ToArray(), abilities, serializedTasks, serializedTaskTemplates, m_room);
                }
            });
        }

        public void Pause(Guid clientId, bool pause, ServerEventHandler callback)
        {
            Error error = new Error(StatusCode.OK);
            if(!m_initialized)
            {
                error.Code = StatusCode.NotAllowed;
                error.Message = "Match is not initialized";
            }
            else
            {
                enabled = !pause;
                if(enabled)
                {
                    m_prevTickTime += (Time.realtimeSinceStartup - m_pauseTime);
                }
                else
                {
                    m_pauseTime = Time.realtimeSinceStartup;
                }
            }

            if (m_lag == 0)
            {
                callback(error);
                if(Paused != null)
                {
                    Paused(error, pause);
                }
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(m_lag); return null; }, result =>
                {
                    callback(error);
                    if(Paused != null)
                    {
                        Paused(error, pause);
                    }
                });
            }
        }

        public void GetReplay(Guid clientId, ServerEventHandler<ReplayData, Room> callback)
        {
            ReplayData replay = m_replay.Save();
            Error error = new Error(StatusCode.OK);
            if (!m_initialized)
            {
                error.Code = StatusCode.NotAllowed;
                error.Message = "Match was not initialized";
            }
            if (m_lag == 0)
            {
                callback(error, replay, m_room);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(m_lag); return null; }, result =>
                {
                    callback(error, replay, m_room);
                });
            }
        }


        public void GetTaskTemplates(Guid clientId, Guid playerId, ServerEventHandler<SerializedTask[], SerializedTaskTemplate[]> callback)
        {
            Error error = new Error(StatusCode.OK);
            if (!m_initialized)
            {
                error.Code = StatusCode.NotAllowed;
                error.Message = "Match was not initialized";
            }
            if (m_lag == 0)
            {

                callback(error, m_tasks[playerId], m_serializedTaskTemplates[playerId]);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(m_lag); return null; }, result =>
                {
                    callback(error, m_tasks[playerId], m_serializedTaskTemplates[playerId]);
                });
            }
        }

        public void SaveTaskTemplate(Guid clientId, Guid playerId, SerializedTask taskTemplate, SerializedTaskTemplate TaskTemplateData, ServerEventHandler callback)
        {
            Error error = new Error(StatusCode.OK);
            if (!m_initialized)
            {
                error.Code = StatusCode.NotAllowed;
                error.Message = "Match was not initialized";
            }
            else
            {
                SerializedTask[] templates;
                if (!m_tasks.TryGetValue(playerId, out templates))
                {
                    templates = new SerializedTask[1];
                }
                else
                {
                    Array.Resize(ref templates, templates.Length + 1);
                }

                SerializedTaskTemplate[] templateInfos;
                if (!m_serializedTaskTemplates.TryGetValue(playerId, out templateInfos))
                {
                    templateInfos = new SerializedTaskTemplate[1];
                }
                else
                {
                    Array.Resize(ref templateInfos, templateInfos.Length + 1);
                }

                templates[templates.Length - 1] = taskTemplate;
                templateInfos[templateInfos.Length - 1] = TaskTemplateData;
            }

            if (m_lag == 0)
            {
                callback(error);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(m_lag); return null; }, result =>
                {
                    callback(error);
                });
            }
        }

        private void InitEngine(Guid clientId, ServerEventHandler callback)
        {
            if(m_engine != null)
            {
                MatchFactory.DestroyMatchEngine(m_engine);
            }

            m_engine = null;

            DownloadMapData(m_room.MapInfo.Id, (error, mapData) =>
            {
                if (HasError(error))
                {
                    if (callback != null)
                    {
                        callback(error);
                    }
                }
                else
                {
                    m_job.Submit(() =>
                    {
                        MapRoot mapRoot = ProtobufSerializer.Deserialize<MapRoot>(mapData.Bytes);
                        return mapRoot;
                    },
                    result =>
                    {
                        MapRoot mapRoot = (MapRoot)result;

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
                 
                        m_prevTickTime = Time.realtimeSinceStartup;

                        m_engine = engine;

                     
                        if(callback != null )
                        {
                            callback(error);
                        }

                        m_pingTimer.Ping(clientId);

                        if(Ping != null)
                        {
                            Ping(new Error(StatusCode.OK), new RTTInfo());
                        }
                    });
                }
            });
        }

        private void Update()
        {
            m_engine.Update();

            //for (int i = 0; i < m_bots.Length; ++i)
            //{
            //    m_bots[i].Update(Time.realtimeSinceStartup);
            //}
        }

        private void FixedUpdate()
        {
            if(m_engine == null)
            {
                Debug.LogError("Enabled before Engine initialization");
                return;
            }

            while ((Time.realtimeSinceStartup - m_prevTickTime) >= GameConstants.MatchEngineTick)
            {
                m_replay.Tick(m_engine, m_tick);

                CommandsBundle commands;
                if(m_engine.Tick(out commands))
                {
                    commands.Tick = m_tick;
                    if (Tick != null)
                    {
                        Error error = new Error(StatusCode.OK);
                        Tick(error, commands);
                    }
                }

                m_tick++;
                m_prevTickTime += GameConstants.MatchEngineTick;
            }
        }

        public void CancelRequests()
        {
            m_job.CancelAll();
        }

        public void SendMessage(Guid clientId, ChatMessage message, ServerEventHandler<Guid> callback)
        {
            Error error = new Error();
            if (m_lag == 0)
            {
                if (ChatMessage != null)
                {
                    ChatMessage(error, message);
                }

                callback(error, message.MessageId);
            }
            else
            {
                m_job.Submit(() => { Thread.Sleep(m_lag); return null; },
                    result =>
                    {
                        if (ChatMessage != null)
                        {
                            ChatMessage(error, message);
                        }

                        callback(error, message.MessageId);
                    });
            }
        }

    }

}
