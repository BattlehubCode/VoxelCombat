using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public delegate void MatchEngineCliEvent(Error error);
    public delegate void MatchEngineCliEvent<T>(Error error, T payload);
    public delegate void MatchEngineCliEvent<T, V>(Error error, T payload, V extra1);
    public delegate void MatchEngineCliEvent<T, V, W>(Error error, T payload, V extra1, W extra2);
    public delegate void MatchEngineCliEvent<T, V, W, Y>(Error error, T payload, V extra1, W extra2, Y extra3);
    public delegate void MatchEngineCliEvent<T, V, W, Y, Z>(Error error, T payload, V extra1, W extra2, Y extra3, Z extra4);

    public interface IMatchEngineCli
    {
        event MatchEngineCliEvent ReadyToStart;
        /// <summary>
        /// All Players, Local Players, Abilities, Room
        /// </summary>
        event MatchEngineCliEvent<Player[], Guid[], VoxelAbilitiesArray[], Room> Started; 

        event MatchEngineCliEvent<RTTInfo> Ping;
        /// <summary>
        /// Possible Error -> StatusCode.HighPing, should be handled without animation
        /// </summary>
        event MatchEngineCliEvent<long, CommandsBundle> ExecuteCommands;
        event MatchEngineCliEvent Error;
        event MatchEngineCliEvent Stopped;
        event MatchEngineCliEvent<bool> Paused;

        IPathFinder PathFinder
        {
            get;
        }

        ITaskRunner TaskRunner
        {
            get;
        }

        IPathFinder BotPathFinder
        {
            get;
        }

        ITaskRunner BotTaskRunner
        {
            get;
        }

        bool HasError(Error error);

        void  DownloadMapData(MatchEngineCliEvent<MapData> callback);
        
        void ReadyToPlay(MatchEngineCliEvent callback);

        void Pause(bool pause, MatchEngineCliEvent callback);

        void Submit(int playerIndex, Cmd command);
    }

    public class MatchEngineCli : MonoBehaviour, IMatchEngineCli
    {
        private RTTInfo m_rttInfo = new RTTInfo { RTT = 0, RTTMax = 0 };
        public event MatchEngineCliEvent ReadyToStart;
        public event MatchEngineCliEvent<Player[], Guid[], VoxelAbilitiesArray[], Room> Started;
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
        private float m_prevTickTime;
        private long m_tick;
        private readonly Queue<CommandsBundle> m_commands = new Queue<CommandsBundle>();
        private bool m_isInitialized;

        private IPathFinder m_pathFinder;
        private ITaskRunner m_taskRunner;
        private IPathFinder m_botPathFinder;
        private ITaskRunner m_botTaskRunner;

        public IPathFinder PathFinder
        {
            get { return m_pathFinder; }
        }

        public ITaskRunner TaskRunner
        {
            get { return m_taskRunner; }
        }

        public IPathFinder BotPathFinder
        {
            get { return m_botPathFinder; }
        }

        public ITaskRunner BotTaskRunner
        {
            get { return m_botTaskRunner; }
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

            if(m_pathFinder != null)
            {
                MatchFactoryCli.DestroyPathFinder(m_pathFinder);
            }
            if(m_taskRunner != null)
            {
                MatchFactoryCli.DestroyTaskRunner(m_taskRunner);
            }
            if(m_botPathFinder != null)
            {
                MatchFactoryCli.DestroyPathFinder(m_botPathFinder);
            }
            if(m_botTaskRunner != null)
            {
                MatchFactoryCli.DestroyTaskRunner(m_botTaskRunner);
            }
        }

        private void Update()
        {
            m_pathFinder.Update();
            m_taskRunner.Update();
            m_botPathFinder.Update();
            m_botTaskRunner.Update();
        }


        private void FixedUpdate()
        {
            while ((Time.realtimeSinceStartup - m_prevTickTime) >= GameConstants.MatchEngineTick)
            {
                m_pathFinder.Tick();
                m_taskRunner.Tick();
                m_botPathFinder.Tick();
                m_botTaskRunner.Tick();

                //Exec commands from current tick
                if (m_commands.Count != 0)
                {
                    Error error = new Error(StatusCode.OK);
                    CommandsBundle commands = m_commands.Peek();
                 
                    if (commands.Tick == m_tick)
                    {
                        m_commands.Dequeue();

                        if (ExecuteCommands != null)
                        {
                            ExecuteCommands(error, m_tick, commands);
                        }

                        if (commands.ClientRequests != null && commands.ClientRequests.Count > 0)
                        {
                            HandleClientRequests(commands.ClientRequests);
                        }
                    }
                    else if(m_tick < (commands.Tick - 8)) //This means the diff between server time and client time > 400ms, so we try to make adjustment
                    {
                        #warning DON'T KNOW IF IT'S SAFE TO MAKE SUCH ADJUSTMENT
                        m_tick++;
                        Debug.LogWarning("Diff between server time and client time is too high. Probabliy ping is lower then measured initially");
                    }
                    else if (m_tick > commands.Tick)
                    {
                        error.Code = StatusCode.HighPing;
                        while(commands != null && m_tick > commands.Tick)
                        {
                            m_commands.Dequeue();

                            if (ExecuteCommands != null)
                            {
                                ExecuteCommands(error, m_tick, commands);
                            }

                            if (commands.ClientRequests != null && commands.ClientRequests.Count > 0)
                            {
                                HandleClientRequests(commands.ClientRequests);
                            }

                            if (m_commands.Count > 0)
                            {
                                commands = m_commands.Peek();
                            }
                            else
                            {
                                commands = null;
                            }
                        }


                        if(commands != null && commands.Tick == m_tick)
                        {
                            m_commands.Dequeue();

                            error.Code = StatusCode.OK;
                            if (ExecuteCommands != null)
                            {
                                ExecuteCommands(error, m_tick, commands);
                            }

                            if (commands.ClientRequests != null && commands.ClientRequests.Count > 0)
                            {
                                HandleClientRequests(commands.ClientRequests);
                            }
                        }
                    }
                }
                m_tick++;
                m_prevTickTime += GameConstants.MatchEngineTick;
            }
        }

        private void HandleClientRequests(List<ClientRequest> clientRequests)
        {
            for(int i = 0; i < clientRequests.Count; ++i)
            {
                ClientRequest request = clientRequests[i];
                Cmd cmd = request.Cmd;

                if(cmd != null)
                {
                    IVoxelDataController dc = m_game.GetVoxelDataController(request.PlayerIndex, cmd.UnitIndex);
                    if(cmd.Code != CmdCode.Move || dc == null)
                    {
                        request.Cmd.ErrorCode = CmdErrorCode.Failed;
                        SubmitResponse(request);
                    }
                    else
                    {
                        CoordinateCmd coordinateCmd = (CoordinateCmd)cmd;
                        Debug.Assert(coordinateCmd.Coordinates.Length > 1);
                        IPathFinder pathFinder = m_game.IsLocalPlayer(request.PlayerIndex) ? m_pathFinder : m_botPathFinder;
                        #warning PathFinder should igore dataController.ControlledVoxelData
                        pathFinder.Find(cmd.UnitIndex, -1, dc.Clone(), coordinateCmd.Coordinates, (unitIndex, path) =>
                        {
                            coordinateCmd.Coordinates = path;
                            request.Cmd = coordinateCmd;
                            SubmitResponse(request);
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
                m_prevTickTime += (Time.realtimeSinceStartup - m_pauseTime);
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
                    m_pathFinder.Terminate(unitId, playerIndex);
                }
            }
            else
            {
                m_pathFinder.Terminate(command.UnitIndex, playerIndex);
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
            m_commands.Enqueue(payload);
        }

        private void OnReadyToPlayAll(Error error, Player[] players, Guid[] localPlayers, VoxelAbilitiesArray[] payload, Room room)
        { 
            if (m_matchServer.HasError(error))
            {
                Error(error);
                m_matchServer.Disconnect();
                return;
            }
      
            m_prevTickTime = Time.realtimeSinceStartup;
            if(m_rttInfo.RTT < GameConstants.MatchEngineTick)
            {
                //In case of low rtt we offset client timer by one tick to the past
                m_prevTickTime += GameConstants.MatchEngineTick;
            }

            m_isInitialized = true;

            m_pathFinder = MatchFactoryCli.CreatePathFinder(m_map.Map, players.Length);
            m_taskRunner = MatchFactoryCli.CreateTaskRunner(players.Length);

            m_botPathFinder = MatchFactoryCli.CreatePathFinder(m_map.Map, players.Length);
            m_botTaskRunner = MatchFactoryCli.CreateTaskRunner(players.Length);

            if (Started != null)
            {
                Started(new Error(StatusCode.OK), players, localPlayers, payload, room);
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
                    Debug.LogWarning("RECONNECT IS NOT TESTED");
                    enabled = true;
                    m_prevTickTime = Time.realtimeSinceStartup;
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
