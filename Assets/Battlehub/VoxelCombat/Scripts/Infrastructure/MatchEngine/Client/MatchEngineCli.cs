using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public delegate void MatchEngineCliEvent(Error error);
    public delegate void MatchEngineCliEvent<T>(Error error, T payload);
    public delegate void MatchEngineCliEvent<T, V>(Error error, T payload, V extra1);
    public delegate void MatchEngineCliEvent<T, V, W>(Error error, T payload, V extra1, W extra2);
    public delegate void MatchEngineCliEvent<T, V, W, Y>(Error error, T payload, V extra1, W extra2, Y extra3);
    public delegate void MatchEngineCliEvent<T, V, W, Y, Z>(Error error, T payload, V extra1, W extra2, Y extra3, Z extra4);
    public delegate void MatchEngineCliEvent<S, T, V, W, Y, Z>(Error error, S payload, T extra1, V extra2, W extra3, Y extra4, Z extra5);


    public interface IMatchEngineCli
    {
        event MatchEngineCliEvent ReadyToStart;
        /// <summary>
        /// All Players, Local Players, Abilities, Room
        /// </summary>
        event MatchEngineCliEvent<Player[], Guid[], VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], Room> Started;

        event MatchEngineCliEvent<RTTInfo> Ping;
        /// <summary>
        /// Possible Error -> StatusCode.HighPing, should be handled without animation
        /// </summary>
        event MatchEngineCliEvent<long, CommandsBundle> ExecuteCommands;
        event MatchEngineCliEvent Error;
        event MatchEngineCliEvent Stopped;
        event MatchEngineCliEvent<bool> Paused;

        ITaskEngine GetClientTaskEngine(int playerIndex);
        ITaskRunner GetTaskRunner(int playerIndex);
        IPathFinder GetPathFinder(int playerIndex);

        bool HasError(Error error);

        void DownloadMapData(MatchEngineCliEvent<MapData> callback);
        void ReadyToPlay(MatchEngineCliEvent callback);
        void Pause(bool pause, MatchEngineCliEvent callback);
        void Submit(int playerIndex, Cmd command);
    }

    public class MatchEngineCli : MonoBehaviour, IMatchEngineCli
    {
        private RTTInfo m_rttInfo = new RTTInfo { RTT = 0, RTTMax = 0 };
        public event MatchEngineCliEvent ReadyToStart;
        public event MatchEngineCliEvent<Player[], Guid[], VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], Room> Started;
        public event MatchEngineCliEvent<RTTInfo> Ping;

        public event MatchEngineCliEvent<long, CommandsBundle> ExecuteCommands;
        public event MatchEngineCliEvent Error;
        public event MatchEngineCliEvent Stopped;
        public event MatchEngineCliEvent<bool> Paused;

        private IMatchServer m_matchServer;
        private IGlobalSettings m_gSettings;
        private IVoxelMap m_map;
        private IVoxelGame m_game;

        private float m_pauseTime;
        private float m_nextTickTime;
        private CommandsQueue m_cmdQueue;
        private bool m_isInitialized;

        private IPathFinder[] m_pathFinders;
        private ITaskRunner[] m_taskRunners;
        private ITaskEngine[] m_taskEngines;
        private IBotController[] m_bots; //Client side controlled bots

        public IPathFinder GetPathFinder(int playerIndex)
        {
            return m_pathFinders[playerIndex];
        }

        public ITaskRunner GetTaskRunner(int playerIndex)
        {
            return m_taskRunners[playerIndex];
        }

        public ITaskEngine GetClientTaskEngine(int playerIndex)
        {
            return m_taskEngines[playerIndex];
        }

        private void Awake()
        {
            enabled = false;

            if (Dependencies.RemoteGameServer != null && Dependencies.RemoteGameServer.IsConnectionStateChanging)
            {
                Dependencies.RemoteGameServer.ConnectionStateChanged += OnRemoteGameServerConnectionStateChanged;
            }
            else
            {
                Init();
            }
        }

        private void OnRemoteGameServerConnectionStateChanged(Error error, ValueChangedArgs<bool> payload)
        {
            Dependencies.RemoteGameServer.ConnectionStateChanged -= OnRemoteGameServerConnectionStateChanged;
            Init();
        }

        private void Init()
        {
            m_matchServer = Dependencies.MatchServer;
            m_gSettings = Dependencies.Settings;
            m_map = Dependencies.Map;
            m_game = Dependencies.GameState;

            m_matchServer.Tick += OnTick;
            m_matchServer.ReadyToPlayAll += OnReadyToPlayAll;
            m_matchServer.Ping += OnPing;
            m_matchServer.Paused += OnPaused;
            m_matchServer.ConnectionStateChanged += OnConnectionStateChanged;
            if(!m_matchServer.IsConnectionStateChanging)
            {
                if(m_matchServer.IsConnected)
                {
                    OnConnectionStateChanged(new Error(), new ValueChangedArgs<bool>(false, true));
                }
            }

            enabled = false;
        }

        private void OnDestroy()
        {
            if(Dependencies.RemoteGameServer != null)
            {
                Dependencies.RemoteGameServer.ConnectionStateChanged -= OnRemoteGameServerConnectionStateChanged;
            }
            
            if (m_matchServer != null)
            {
                m_matchServer.Tick -= OnTick;
                m_matchServer.ReadyToPlayAll -= OnReadyToPlayAll;
                m_matchServer.Ping -= OnPing;
                m_matchServer.Paused -= OnPaused;
                m_matchServer.ConnectionStateChanged -= OnConnectionStateChanged;
            }

            if(m_taskEngines != null)
            {
                for (int i = 0; i < m_taskEngines.Length; ++i)
                {
                    ITaskEngine taskEngine = m_taskEngines[i];
                    if (taskEngine != null)
                    {
                        taskEngine.ClientRequest -= ProcessClientRequest;
                        MatchFactoryCli.DestroyTaskEngine(taskEngine);
                    }
                }
            }

            if(m_taskRunners != null)
            {
                for (int i = 0; i < m_taskRunners.Length; ++i)
                {
                    ITaskRunner taskRunner = m_taskRunners[i];
                    if (taskRunner != null)
                    {
                        MatchFactoryCli.DestroyTaskRunner(taskRunner);
                    }
                }
            }

            if(m_pathFinders != null)
            {
                for (int i = 0; i < m_pathFinders.Length; ++i)
                {
                    IPathFinder pathFinder = m_pathFinders[i];
                    if (pathFinder != null)
                    {
                        MatchFactoryCli.DestroyPathFinder(pathFinder);
                    }
                }
            }

            if(m_bots != null)
            {
                for(int i = 0; i < m_bots.Length; ++i)
                {
                    IBotController botController = m_bots[i];
                    if(botController != null)
                    {
                        MatchFactoryCli.DestroyBotController(botController);
                    }
                }
            }
        }

        private void Update()
        {
            for (int i = 0; i < m_taskRunners.Length; ++i)
            {
                ITaskRunner taskRunner = m_taskRunners[i];
                if (taskRunner != null)
                {
                    taskRunner.Update();
                }
            }

            for (int i = 0; i < m_pathFinders.Length; ++i)
            {
                IPathFinder pathFinder = m_pathFinders[i];
                if (pathFinder != null)
                {
                    pathFinder.Update();
                }
            }

            if(m_bots != null)
            {
                for (int i = 0; i < m_bots.Length; ++i)
                {
                    IBotController bot = m_bots[i];
                    if (bot != null)
                    {
                        bot.Update(Time.realtimeSinceStartup);
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            OnFixedUpdate();
        }


        private void OnFixedUpdate()
        {
            if (Time.realtimeSinceStartup < m_nextTickTime)
            {
                return;
            }

            long tick;
            CommandsBundle commands = m_cmdQueue.Tick(out tick);
            if (commands != null)
            {
                Error error = new Error(StatusCode.OK);
                if (commands.Tick < tick)
                {
                    error.Code = StatusCode.Outdated;
                }
                else
                {
                    m_nextTickTime += GameConstants.MatchEngineTick;
                    OnTick();
                }

                if (ExecuteCommands != null)
                {
                    ExecuteCommands(error, tick, commands);
                }

                if (commands.TasksStateInfo != null && commands.TasksStateInfo.Count > 0)
                {
                    HandleTaskStateChanged(commands.TasksStateInfo);
                }

                if (commands.ClientRequests != null && commands.ClientRequests.Count > 0)
                {
                    ProcessRequest(commands.ClientRequests);
                }
            }
            else
            {
                m_nextTickTime += GameConstants.MatchEngineTick;
                OnTick();
            }
        }

        private void OnTick()
        {
            for (int i = 0; i < m_taskRunners.Length; ++i)
            {
                ITaskRunner taskRunner = m_taskRunners[i];
                if (taskRunner != null)
                {
                    taskRunner.Tick();
                }
            }

            for (int i = 0; i < m_pathFinders.Length; ++i)
            {
                IPathFinder pathFinder = m_pathFinders[i];
                if (pathFinder != null)
                {
                    pathFinder.Tick();
                }
            }

            for (int i = 0; i < m_taskEngines.Length; ++i)
            {
                ITaskEngine taskEngine = m_taskEngines[i];
                if (taskEngine != null)
                {
                    taskEngine.Tick();
                }
            }
        }

        private void HandleTaskStateChanged(List<TaskStateInfo> taskStateInfo)
        {
            for(int i = 0; i < taskStateInfo.Count; ++i)
            {
                TaskStateInfo tsi = taskStateInfo[i];
                ITaskEngine taskEngine = m_taskEngines[tsi.PlayerId];
                if (taskEngine != null)
                {
                    taskEngine.SetTaskState(tsi.TaskId, tsi.State, tsi.StatusCode);
                }
            }
        }

        private void ProcessRequest(List<ClientRequest> clientRequests)
        {
            for(int i = 0; i < clientRequests.Count; ++i)
            {
                ClientRequest request = clientRequests[i];
                ProcessRequest(request, SubmitResponse);
            }
        }

        private void ProcessClientRequest(ClientRequest request)
        {
            ProcessRequest(request, processedRequest =>
            {
                m_taskEngines[processedRequest.PlayerIndex].SubmitResponse(processedRequest);
            });
        }

        private void ProcessRequest(ClientRequest request, Action<ClientRequest> callback)
        {
            Cmd cmd = request.Cmd;

            if (cmd != null)
            {
                if(cmd.Code == CmdCode.GrantBotCtrl)
                {
                    m_bots = new IBotController[m_game.PlayersCount];
                    for (int i = 0; i < m_bots.Length; ++i)
                    {
                        Player player = m_game.GetPlayer(i);
                        if (player.IsActiveBot)
                        {
                            IBotController bot = MatchFactoryCli.CreateBotController(player, m_taskEngines[i]);
                            bot.Init();
                            m_bots[i] = bot;
                        }
                    }
                    callback(request);
                }
                else if(cmd.Code == CmdCode.DenyBotCtrl)
                {
                    m_bots = new IBotController[m_game.PlayersCount];
                    for (int i = 0; i < m_bots.Length; ++i)
                    {
                        IBotController bot = m_bots[i];
                        if(bot != null)
                        {
                            bot.Reset();
                        }
                    }
                    m_bots = null;
                    callback(request);
                }
                else
                {
                    IVoxelDataController dc = m_game.GetVoxelDataController(request.PlayerIndex, cmd.UnitIndex);
                    if (cmd.Code != CmdCode.Move || dc == null)
                    {
                        if (dc == null)
                        {
                            request.Cmd.ErrorCode = CmdResultCode.Fail_NoUnit;
                        }
                        else
                        {
                            request.Cmd.ErrorCode = CmdResultCode.Fail_NotSupported;
                        }

                        SubmitResponse(request);
                    }
                    else
                    {
                        CoordinateCmd coordinateCmd = (CoordinateCmd)cmd;
                        Debug.Assert(coordinateCmd.Coordinates.Length > 1);
                        IPathFinder pathFinder = m_pathFinders[request.PlayerIndex];
#warning PathFinder should igore dataController.ControlledVoxelData
                        pathFinder.Find(cmd.UnitIndex, -1, dc.Clone(), coordinateCmd.Coordinates, (unitIndex, path) =>
                        {
                            coordinateCmd.Coordinates = path;
                            request.Cmd = coordinateCmd;
                            callback(request);
                        }, null);
                    }
                }
              
            }
        }

        private void SubmitResponse(ClientRequest request)
        {
            m_matchServer.SubmitResponse(m_gSettings.ClientId, request, (error, response) =>
            {
                if (m_matchServer.HasError(error))
                {
                    if (Error != null)
                    {
                        Error(error);
                    }
                }
            });
        }

        public bool HasError(Error error)
        {
            return m_matchServer.HasError(error);
        }

        public void DownloadMapData(MatchEngineCliEvent<MapData> callback)
        {
            m_matchServer.DownloadMapData(m_gSettings.ClientId, (error, data) => callback(error, data));
        }

        public void ReadyToPlay(MatchEngineCliEvent callback)
        {
            m_matchServer.ReadyToPlay(m_gSettings.ClientId, error => callback(error));
        }

        public void Pause(bool pause, MatchEngineCliEvent callback)
        {
            if(!m_isInitialized)
            {
                throw new InvalidOperationException("Wait for ReadyToPlayAll event");
            }

            enabled = !pause;
            if(enabled)
            {
                m_nextTickTime += (Time.realtimeSinceStartup - m_pauseTime);
            }
            else
            {
                m_pauseTime = Time.realtimeSinceStartup;
            }

            m_matchServer.Pause(m_gSettings.ClientId, pause, error => callback(error));
        }

        public void Submit(int playerIndex, Cmd command)
        {
            if(command.Code == CmdCode.Composite)
            {
                CompositeCmd composite = (CompositeCmd)command;
                for(int i = 0; i < composite.Commands.Length; ++i)
                {
                    long unitId = composite.Commands[i].UnitIndex;
                    m_pathFinders[playerIndex].Terminate(unitId);
                }
            }
            else
            {
               // if(command.Code != )
                m_pathFinders[playerIndex].Terminate(command.UnitIndex);
            }
            
            m_matchServer.Submit(m_gSettings.ClientId, playerIndex, command, (error, returnedCommand) =>
            {
                if (m_matchServer.HasError(error))
                {
                    Error(error);
                    m_matchServer.Disconnect();
                    return;
                }
            });
        }

        private void OnTick(Error error, CommandsBundle payload)
        {
            if(m_matchServer.HasError(error))
            {
                Error(error);
                m_matchServer.Disconnect();
                return;
            }
            m_cmdQueue.Enqueue(payload);
        }

        private void OnReadyToPlayAll(Error error, Player[] players, Guid[] localPlayers, VoxelAbilitiesArray[] abilities, SerializedTaskArray[] taskTemplates, SerializedTaskTemplatesArray[] TaskTemplateData, Room room)
        { 
            if (m_matchServer.HasError(error))
            {
                Error(error);
                m_matchServer.Disconnect();
                return;
            }
      
            //if(m_rttInfo.RTT < GameConstants.MatchEngineTick)
            //{
            //    //In case of low rtt we offset client timer by one tick to the past
            //    m_prevTickTime += GameConstants.MatchEngineTick;
            //}


            long maxPing = GameConstants.PingTimeout;
            if(m_rttInfo.RTTMax > maxPing)
            {
                Debug.LogWarningFormat("Ping is too high {0} ms", m_rttInfo.RTTMax * GameConstants.MatchEngineTick);
                //set max ping to RTT_MAX?
            }
            m_nextTickTime = Time.realtimeSinceStartup + Math.Max(0, (maxPing - m_rttInfo.RTTMax) / 4) * GameConstants.MatchEngineTick;

            m_cmdQueue = new CommandsQueue(maxPing);
            m_isInitialized = true;

            m_taskEngines = new ITaskEngine[players.Length];
            m_pathFinders = new IPathFinder[players.Length];
            m_taskRunners = new ITaskRunner[players.Length];

            for (int i = 0; i < players.Length; ++i)
            {
                Player player = players[i];
              
                if(player.IsBot || localPlayers.Contains(players[i].Id))
                {
                    ITaskRunner taskRunner = MatchFactoryCli.CreateTaskRunner();
                    m_taskRunners[i] = taskRunner;
                    IPathFinder pathFinder = MatchFactoryCli.CreatePathFinder(m_map.Map);
                    m_pathFinders[i] = pathFinder;

                    ITaskEngine taskEngine = MatchFactoryCli.CreateTaskEngine(m_game, taskRunner, pathFinder);
                    taskEngine.ClientRequest += ProcessClientRequest;
                    m_taskEngines[i] = taskEngine;
                }
            }

            if (Started != null)
            {
                Started(new Error(StatusCode.OK), players, localPlayers, abilities, taskTemplates, TaskTemplateData, room);
            }

            enabled = true; //update method will be called
        }

        private void OnPing(Error error, RTTInfo payload)
        {
            m_rttInfo = payload;

            if (m_matchServer.HasError(error))
            {
                Error(error);
                m_matchServer.Disconnect();
                return;
            }

            if(Ping != null)
            {
                Ping(new Error(StatusCode.OK), payload);
            }

            m_matchServer.Pong(m_gSettings.ClientId, pongError =>
            {
                if(m_matchServer.HasError(pongError))
                {
                    Stopped(pongError);
                }
            });
        }

        private void OnPaused(Error error, bool payload)
        {
            if (Paused != null)
            {
                Paused(error, payload);
            }
        }

        private void OnConnectionStateChanged(Error error, ValueChangedArgs<bool> args)
        {
            if (args.NewValue)
            {
                if(HasError(error))
                {
                    Error(error);
                    return;
                }

                if (m_isInitialized)
                {
                    throw new NotSupportedException("RECONNECT IS NOT SUPPORTED");
                   // Debug.LogWarning("RECONNECT IS NOT TESTED");
                   // enabled = true;
                   // m_startTime = Time.realtimeSinceStartup;
                }
                else
                {
                    if(ReadyToStart != null)
                    {
                        ReadyToStart(error);
                    }
                }
            }
            else
            {
                enabled = false;

                if(Stopped != null)
                {
                    Stopped(error);
                }
            }
        }

    }
}
