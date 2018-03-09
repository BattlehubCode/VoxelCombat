using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public delegate void VoxelGameStateChangedHandler();

    public delegate void VoxelGameStateChangedHandler<T>(T arg);

    public interface IVoxelGame
    {
        event VoxelGameStateChangedHandler Completed;
        event VoxelGameStateChangedHandler<int> PlayerDefeated;
        event VoxelGameStateChangedHandler IsPausedChanged;

        int MaxPlayersCount
        {
            get;
        }

        int PlayersCount
        {
            get;
        }

        int LocalPlayersCount
        {
            get;
        }

        bool IsContextActionInProgress(int localPlayerIndex);
        void IsContextActionInProgress(int localPlayerIndex, bool value);

        bool IsMenuOpened(int localPlayerIndex);
        void IsMenuOpened(int localPlayerIndex, bool value);

        bool IsPaused
        {
            get;
            set;
        }

        bool IsCompleted
        {
            get;
        }

        bool IsPauseStateChanging
        {
            get;
        }

        bool IsReplay
        {
            get;
        }

        Guid GetLocalPlayerId(int index);

        int GetPlayerIndex(Guid id);

        Player GetPlayer(int index);

        PlayerStats GetStats(int index);

        int LocalToPlayerIndex(int index);

        int PlayerToLocalIndex(int index);

        bool IsLocalPlayer(int index);

        IVoxelDataController GetVoxelDataController(int playerIndex, long unitIndex);

        MatchAssetCli GetAsset(int playerIndex, long unitIndex);

        long GetAssetIndex(VoxelData data);

        IEnumerable<long> GetUnits(int playerIndex);

        IEnumerable<long> GetAssets(int playerIndex);
        
        Dictionary<int, VoxelAbilities> GetAbilities(int playerIndex);

        void SaveReplay(string name);
    }

    public class PlayerStats
    {
        public bool IsInRoom
        {
            get;
            set;
        }

        public int ControllableUnitsCount
        {
            get;
            set;
        }

        public PlayerStats(bool isInRoom, int unitsCount)
        {
            IsInRoom = isInRoom;
            ControllableUnitsCount = unitsCount;
        }
    }

    public class VoxelGame : MonoBehaviour, IVoxelGame
    {
        public event VoxelGameStateChangedHandler<int> PlayerDefeated;
        public event VoxelGameStateChangedHandler Completed;
        private bool m_isCompleted;

        public bool IsCompleted
        {
            get { return m_isCompleted; }
            private set
            {
                if(value && !m_isCompleted)
                {
                    m_isCompleted = true;
                    if(Completed != null)
                    {
                        Completed();
                        m_gameServer.SavePlayersStats( error =>
                        {
                        });
                    }
                }
            }
        }

        private const int m_maxPlayers = GameConstants.MaxPlayersIncludingNeutral; //zero is neutral
        public int MaxPlayersCount
        {
            get { return m_maxPlayers; }
        }

        public int PlayersCount
        {
            get { return m_players.Length; }
        }
        public int LocalPlayersCount
        {
            get { return m_localPlayers.Length; }
        }


        private bool[] m_isContextActionInProgress;
        public bool IsContextActionInProgress(int index)
        {
            return m_isContextActionInProgress[index];
        }

        public void IsContextActionInProgress(int index, bool value)
        {
            m_isContextActionInProgress[index] = value;
        }

        private bool[] m_isMenuOpened;

        public bool IsMenuOpened(int index)
        {
            return m_isMenuOpened[index];
        }
        public void IsMenuOpened(int index, bool value)
        {
            m_isMenuOpened[index] = value;
        }

        public event VoxelGameStateChangedHandler IsPausedChanged;
        private bool m_isPaused;
        public bool IsPaused
        {
            get { return m_isPaused; }
            set
            {
                if(m_isPaused != value)
                {
                    m_isPaused = value;

                    m_progress.IsVisible = true;

                    IsPauseStateChanging = true;

                    m_engine.Pause(value, error =>
                    {
                        IsPauseStateChanging = false;

                        m_progress.IsVisible = false;
                        if (m_engine.HasError(error))
                        {
                            m_notification.ShowError(error);
                            return;
                        }
                        
                        if (IsPausedChanged != null)
                        {
                            IsPausedChanged();
                        }
                    });
                }
            }
        }

        public bool IsPauseStateChanging
        {
            get;
            private set;
        }

        public bool IsReplay
        {
            get;
            private set;
        }

        private IMatchEngineCli m_engine;
        private IGameServer m_gameServer;
        private IGlobalSettings m_gSettings;
        private IProgressIndicator m_progress;
        private INotification m_notification;
        private IVoxelMap m_voxelMap;
        private IGameView m_gameView;

        private Guid[] m_localPlayers;
        private Player[] m_players;
        private PlayerStats[] m_playerStats;
        private Dictionary<int, VoxelAbilities>[] m_voxelAbilities;
        private IMatchPlayerControllerCli[] m_playerControllers;
        
        private void Awake()
        {
            m_progress = Dependencies.Progress;
            m_notification = Dependencies.Notification;
            m_voxelMap = Dependencies.Map;
            m_gameView = Dependencies.GameView;
            m_gameServer = Dependencies.GameServer;
            m_gSettings = Dependencies.Settings;

            m_engine = Dependencies.MatchEngine;
            m_engine.ReadyToStart += OnEngineReadyToStart;
            m_engine.Started += OnEngineStarted;
            m_engine.Error += OnEngineError;
            m_engine.Stopped += OnEngineStopped;
            m_engine.Ping += OnEnginePing;
            m_engine.Paused += OnEnginePaused;

            m_engine.ExecuteCommands += OnEngineCommands;

            INavigation navigation = Dependencies.Navigation;
            if (string.IsNullOrEmpty(navigation.PrevSceneName))
            {
                Dependencies.RemoteGameServer.ConnectionStateChanged += OnConnectionStateChanged;
            }
        }

        private void OnConnectionStateChanged(Error error, ValueChangedArgs<bool> payload)
        {
            Dependencies.RemoteGameServer.ConnectionStateChanged -= OnConnectionStateChanged;
            TestGameInit.Init(null, 2, 0, Dependencies.RemoteGameServer.IsConnected, () => { }, initError => m_notification.ShowError(initError));
        }

        private void Start()
        {
            INavigation navigation = Dependencies.Navigation;
            if (!string.IsNullOrEmpty(navigation.PrevSceneName))
            {
                Dependencies.MatchServer.Activate();
            }
        }

        private void OnDestroy()
        {
            if(m_engine != null)
            {
                m_engine.ReadyToStart -= OnEngineReadyToStart;
                m_engine.Started -= OnEngineStarted;
                m_engine.Error -= OnEngineError;
                m_engine.Stopped -= OnEngineStopped;
                m_engine.Ping -= OnEnginePing;
                m_engine.Paused -= OnEnginePaused;
                m_engine.ExecuteCommands -= OnEngineCommands;
            }
        }

        private void OnEngineReadyToStart(Error error)
        {
            if(m_engine.HasError(error))
            {
                m_notification.ShowError(error);
            }
            else
            {
                DownloadMapFromServer();
            }
        }

        private void DownloadMapFromServer()
        {
            m_progress.IsVisible = true;

            m_engine.DownloadMapData((error, mapData) =>
            {
                m_progress.IsVisible = false;
                if(m_engine.HasError(error))
                {
                    m_notification.ShowError(error);
                    return;
                }

                m_progress.IsVisible = true;
                m_voxelMap.IsOn = false;
                m_voxelMap.Load(mapData.Bytes, () =>
                {
                    ReadyToPlay(() =>
                    {
                        
                    });
                });
            });
        }

        private void ReadyToPlay(Action callback)
        {
            m_engine.ReadyToPlay(error =>
            {
                if (m_engine.HasError(error))
                {
                    m_notification.ShowError(error);
                }

                callback();
            });
        }

        private void OnEngineStarted(Error error, Player[] players, Guid[] localPlayers, VoxelAbilitiesArray[] voxelAbilities, Room room)
        {
            IsReplay = room.Mode == GameMode.Replay;

            m_progress.IsVisible = false;

            if (m_engine.HasError(error))
            {
                m_notification.ShowError(error);
                return;
            }

            m_voxelMap.Map.DestroyExtraPlayers(players.Length);
            m_voxelMap.IsOn = true;

       
            m_players = players;

            m_voxelAbilities = voxelAbilities.Select(va => va.Abilities.ToDictionary(a => a.Type)).ToArray();            

            if(IsReplay)
            {
                m_localPlayers = new[] { Guid.Empty };
                m_isContextActionInProgress = new bool[m_localPlayers.Length];
                m_isMenuOpened = new bool[m_localPlayers.Length];

                m_gameView.Initialize(1, true);
            }
            else
            {
                m_localPlayers = localPlayers;
                m_isContextActionInProgress = new bool[m_localPlayers.Length];
                m_isMenuOpened = new bool[m_localPlayers.Length];

                m_gameView.Initialize(m_localPlayers.Length, true);
            }

            
            m_playerControllers = new IMatchPlayerControllerCli[m_players.Length];
            for (int i = 0; i < m_playerControllers.Length; ++i)
            {
                m_playerControllers[i] = MatchFactoryCli.CreatePlayerController(transform, i, m_voxelAbilities);
            }

            for (int i = 0; i < m_playerControllers.Length; ++i)
            {
                m_playerControllers[i].ConnectWith(m_playerControllers);
            }

            m_playerStats = new PlayerStats[m_players.Length];
            for (int i = 0; i < m_playerStats.Length; ++i)
            {
                m_playerStats[i] = new PlayerStats(true, m_playerControllers[i].UnitsCount);
            }
        }

        private void OnEngineError(Error error)
        {
            m_notification.ShowError(error);
        }

        private void OnEngineStopped(Error error)
        {
            m_progress.IsVisible = false;

            if (m_playerControllers != null)
            {
                for (int i = 0; i < m_playerControllers.Length; ++i)
                {
                    IMatchPlayerControllerCli playerController = m_playerControllers[i];
                    if(playerController != null)
                    {
                        MatchFactoryCli.DestroyPlayerController(playerController);
                    }
                }
                m_playerControllers = null;
            }

            if (m_engine.HasError(error))
            {
                ShowErrorAndTryToLeaveRoomAndNavigateToMenu(error.ToString());
            }
            else
            {
                ShowErrorAndTryToLeaveRoomAndNavigateToMenu();
            }
        }

        private void ShowErrorAndTryToLeaveRoomAndNavigateToMenu(string errorMessage = "")
        {
            m_notification.ShowErrorWithAction("Disconnected from game server " + errorMessage, () =>
            {
                if(m_gameServer.IsConnected)
                {
                    m_progress.IsVisible = true;
                    m_gameServer.LeaveRoom(m_gSettings.ClientId, error =>
                    {
                        m_progress.IsVisible = false;
                        if(m_gameServer.HasError(error))
                        {
                            Debug.LogError("Unable to leave room " + error.ToString());
                        }
                        Dependencies.Navigation.Navigate("Menu", "LoginMenu4Players", null);
                    });
                }
                else
                {
                    Dependencies.Navigation.Navigate("Menu", "LoginMenu4Players", null);
                }
            });
        }

        private void OnEnginePing(Error error, RTTInfo payload)
        {
            if (m_engine.HasError(error))
            {
                m_notification.ShowError(error);
                return;
            }

           // Debug.Log("Engine Ping RTT " + payload.RTT + " RTT MAX " + payload.RTTMax);
        }

        private void OnEnginePaused(Error error, bool isPaused)
        {
            IsPaused = isPaused;
        }

        private void OnEngineCommands(Error error, long tick, CommandsBundle commandsBundle)
        {
            if(m_engine.HasError(error))
            {
                if(error.Code != StatusCode.HighPing)
                {
                    m_notification.ShowError(error);
                    return;
                }
            }

            List<IMatchPlayerControllerCli> defeatedPlayers = null;
            CommandsArray[] playersCommands = commandsBundle.Commands;
            for(int p = 0; p < playersCommands.Length; ++p)
            {
                CommandsArray commands = playersCommands[p];
                if(commands.Commands == null)
                {
                    continue;
                }

                if (error.Code == StatusCode.HighPing)
                {
                    Debug.LogWarning("Executing cmd with high ping " + commandsBundle.Tick);
                }

                IMatchPlayerControllerCli playerController = m_playerControllers[p];
                long lagTicks = tick - commandsBundle.Tick;
                Debug.Assert(lagTicks >= 0);

                playerController.Execute(commands.Commands, tick, lagTicks);

                PlayerStats stats = m_playerStats[p];
                if (stats.IsInRoom && !playerController.IsInRoom)
                {
                    stats.IsInRoom = false;
                    //Raise player deactivated event;
                    Debug.Log("Player " + m_players[p].Name + " has left the game");

                    if(defeatedPlayers == null)
                    {
                        defeatedPlayers = new List<IMatchPlayerControllerCli>();
                    }
                    defeatedPlayers.Add(playerController);
                }
                else if(playerController.ControllableUnitsCount == 0)
                {
                    if (defeatedPlayers == null)
                    {
                        defeatedPlayers = new List<IMatchPlayerControllerCli>();
                    }
                    defeatedPlayers.Add(playerController);
                }

                stats.ControllableUnitsCount = playerController.ControllableUnitsCount;
            }

            if (defeatedPlayers != null)
            {
                for (int i = 0; i < defeatedPlayers.Count; ++i)
                {
                    IMatchPlayerControllerCli defeatedPlayer = defeatedPlayers[i];
//#warning Temporary disabled due to strange bugs
                    defeatedPlayer.DestroyAllUnitsAndAssets();

                    if(PlayerDefeated != null)
                    {
                        PlayerDefeated(defeatedPlayer.PlayerIndex);
                    }
                }
            }

            IsCompleted = commandsBundle.IsGameCompleted;
        }

        public IMatchPlayerControllerCli GetMatchPlayerController(int playerIndex)
        {
            return m_playerControllers[playerIndex];
        }

        public IVoxelDataController GetVoxelDataController(int playerIndex, long unitIndex)
        {
            if(playerIndex < 0 || playerIndex >= m_playerControllers.Length)
            {
                throw new ArgumentOutOfRangeException("PlayerIndex is out of range. PlayerIndex = " + playerIndex);
            }

            IMatchPlayerControllerCli playerController = m_playerControllers[playerIndex];
            return playerController.GetVoxelDataController(unitIndex);
        }

        public MatchAssetCli GetAsset(int playerIndex, long unitIndex)
        {
            IMatchPlayerControllerCli playerController = m_playerControllers[playerIndex];
            return playerController.GetAsset(unitIndex);
        }

        public long GetAssetIndex(VoxelData data)
        {
            IMatchPlayerControllerCli playerController = m_playerControllers[data.Owner];
            return playerController.GetAssetIndex(data);
        }

        public IEnumerable<long> GetUnits(int playerIndex)
        {
            IMatchPlayerControllerCli playerController = m_playerControllers[playerIndex];
            return playerController.Units;
        }

        public IEnumerable<long> GetAssets(int playerIndex)
        {
            IMatchPlayerControllerCli playerController = m_playerControllers[playerIndex];
            return playerController.Assets;
        }


        public Guid GetLocalPlayerId(int index)
        {
            return m_localPlayers[index];
        }

        public int GetPlayerIndex(Guid id)
        {
            Player player = m_players.Where(p => p.Id == id).FirstOrDefault();
            if(player == null)
            {
                return -1;
            }

            return Array.IndexOf(m_players, player);
        }

        public bool IsLocalPlayer(int index)
        {
            Player player = m_players[index];
            return m_gameServer.IsLocal(m_gSettings.ClientId, player.Id);
        }

        public Player GetPlayer(int index)
        {
            return m_players[index];
        }

        public PlayerStats GetStats(int index)
        {
            PlayerStats stats = m_playerStats[index];

            long[] units = m_playerControllers[index].Units.ToArray();

            int count = 0;

            for(int i = 0; i < units.Length; ++i)
            {
                IVoxelDataController dc = GetVoxelDataController(index, units[i]);
                if(dc != null && VoxelData.IsControllableUnit(dc.ControlledData.Type) && dc.IsAlive)
                {
                    count++;
                }
            }

            stats.ControllableUnitsCount = count;

            return stats;
        }

        public int LocalToPlayerIndex(int index)
        {
            if(IsReplay)
            {
                return index + 1;
            }

            Guid playerId = GetLocalPlayerId(index);
            int playerIndex = GetPlayerIndex(playerId);

            return playerIndex;
        }

        public int PlayerToLocalIndex(int index)
        {
            if(IsReplay && index <= 4)
            {
                return index - 1;
            }

            Player player = m_players[index];
            
            return Array.IndexOf(m_localPlayers, player.Id);
        }


        public Dictionary<int, VoxelAbilities> GetAbilities(int playerIndex)
        {
            return m_voxelAbilities[playerIndex];
        }

        public void SaveReplay(string name)
        {
            m_progress.IsVisible = false;
            m_gameServer.SaveReplay(m_gSettings.ClientId, name, error =>
            {
                m_progress.IsVisible = false;
                if (m_gameServer.HasError(error))
                {
                    m_notification.ShowError(error);
                    return;
                }
            });
        }
    }

}
