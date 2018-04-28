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

        bool HasError(Error error);

        void  DownloadMapData(MatchEngineCliEvent<MapData> callback);
        
        void ReadyToPlay(MatchEngineCliEvent callback);


        void Pause(bool pause, MatchEngineCliEvent callback);

        void Submit(Guid playerId, Cmd command);
    }



    public class MatchEngineCli : MonoBehaviour, IMatchEngineCli
    {
        private RTTInfo m_rttInfo = new RTTInfo { RTT = 0, RTTMax = 0 };
        public event MatchEngineCliEvent ReadyToStart;
        public event MatchEngineCliEvent<Player[], Guid[], VoxelAbilitiesArray[], Room> Started;
        public event MatchEngineCliEvent<RTTInfo> Ping;

        /// <summary>
        /// Possible Error -> StatusCode.HighPing, should be handled without animation
        /// </summary>
        public event MatchEngineCliEvent<long, CommandsBundle> ExecuteCommands;
        public event MatchEngineCliEvent Error;
        public event MatchEngineCliEvent Stopped;
        public event MatchEngineCliEvent<bool> Paused;

        private IMatchServer m_matchServer;
        private IGlobalSettings m_gSettings;

        private float m_pauseTime;
        private float m_prevTickTime;
        private long m_tick;
        private readonly Queue<CommandsBundle> m_commands = new Queue<CommandsBundle>();
        private bool m_isInitialized;

        private void Awake()
        {
            enabled = false;

            if (Dependencies.RemoteGameServer.IsConnectionStateChanging)
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

            m_matchServer.Tick += OnTick;
            m_matchServer.ReadyToPlayAll += OnReadyToPlayAll;
            m_matchServer.Ping += OnPing;
            m_matchServer.Paused += OnPaused;
            m_matchServer.ConnectionStateChanged += OnConnectionStateChanged;

            enabled = false;
        }

        private void OnDestroy()
        {
            Dependencies.RemoteGameServer.ConnectionStateChanged -= OnRemoteGameServerConnectionStateChanged;

            if (m_matchServer != null)
            {
                
                m_matchServer.Tick -= OnTick;
                m_matchServer.ReadyToPlayAll -= OnReadyToPlayAll;
                m_matchServer.Ping -= OnPing;
                m_matchServer.Paused -= OnPaused;
                m_matchServer.ConnectionStateChanged -= OnConnectionStateChanged;
            }
        }

        private void FixedUpdate()
        {
            while ((Time.realtimeSinceStartup - m_prevTickTime) >= GameConstants.MatchEngineTick)
            {
                //Exec commands from current tick
                if(m_commands.Count != 0)
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

                            if(m_commands.Count > 0)
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
                        }
                    }
                }
                m_tick++;
                m_prevTickTime += GameConstants.MatchEngineTick;
            }
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

        public void Submit(Guid playerId, Cmd command)
        {
            m_matchServer.Submit(m_gSettings.ClientId, playerId, command, error =>
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

            enabled = true; //update method will be called
            m_prevTickTime = Time.realtimeSinceStartup;
            if(m_rttInfo.RTT < GameConstants.MatchEngineTick)
            {
                //In case of low rtt we offset client timer by one tick to the past
                m_prevTickTime += GameConstants.MatchEngineTick;
            }
            
            m_isInitialized = true;

            if (Started != null)
            {
                Started(new Error(StatusCode.OK), players, localPlayers, payload, room);
            }
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
                    ReadyToStart(error);
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
