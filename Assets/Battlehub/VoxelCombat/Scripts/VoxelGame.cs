using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Battlehub.VoxelCombat
{
    public delegate void VoxelGameStateChangedHandler();

    public delegate void VoxelGameStateChangedHandler<T>(T arg);

    public interface IVoxelGame : IMatchView
    {
        event VoxelGameStateChangedHandler Started;
        event VoxelGameStateChangedHandler Completed;
        event VoxelGameStateChangedHandler<int> PlayerDefeated;
        event VoxelGameStateChangedHandler IsPausedChanged;

        event VoxelGameStateChangedHandler<int> ContextAction;
        bool IsContextActionInProgress(int localPlayerIndex);
        void IsContextActionInProgress(int localPlayerIndex, bool value);

        event VoxelGameStateChangedHandler<int> Menu;
        bool IsMenuOpened(int localPlayerIndex);
        void IsMenuOpened(int localPlayerIndex, bool value);

        string MapName
        {
            get;
        }

        int BotsCount
        {
            get;
        }

        int MaxPlayersCount
        {
            get;
        }


        int LocalPlayersCount
        {
            get;
        }


        bool IsStarted
        {
            get;
        }

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

        SerializedTask[] GetTaskTemplates(int playerIndex);

        SerializedTaskTemplate[] GetTaskTemplateData(int playerIndex);
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
        public event VoxelGameStateChangedHandler Started;
        public event VoxelGameStateChangedHandler<int> PlayerDefeated;
        public event VoxelGameStateChangedHandler Completed;
        public event VoxelGameStateChangedHandler<int> ContextAction;
        public event VoxelGameStateChangedHandler<int> Menu;
            
        private bool m_isStarted;
        public bool IsStarted
        {
            get { return m_isStarted; }
            private set { m_isStarted = value; }
        }

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
            if(ContextAction != null)
            {
                ContextAction(index);
            }
        }

        private bool[] m_isMenuOpened;

        public bool IsMenuOpened(int index)
        {
            return m_isMenuOpened[index];
        }
        public void IsMenuOpened(int index, bool value)
        {
            m_isMenuOpened[index] = value;
            if(Menu != null)
            {
                Menu(index);
            }
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


        public string MapName
        {
            get { return m_room != null ? m_room.MapInfo.Name : null; }
        }

        public int BotsCount
        {
            get { return m_players.Count(p => p.IsBot); }
        }

        public MapRoot Map
        {
            get { return m_voxelMap.Map; }
        }

        private IMatchEngineCli m_engine;
        private IGameServer m_gameServer;
        private IGameServer m_remoteGameServer;
        private IGlobalSettings m_gSettings;
        private IProgressIndicator m_progress;
        private INotification m_notification;
        private IVoxelMap m_voxelMap;
        private IGameView m_gameView;
        private IConsole m_console;
        private IVoxelMinimapRenderer m_minimap;

        private Guid[] m_localPlayers;
        private Player[] m_players;
        private PlayerStats[] m_playerStats;
        private Dictionary<int, VoxelAbilities>[] m_voxelAbilities;
        private IMatchPlayerControllerCli[] m_playerControllers;
        private SerializedTask[][] m_taskTemplates;
        private SerializedTaskTemplate[][] m_TaskTemplateData;
        private Room m_room;
        
        private void Awake()
        {
            m_progress = Dependencies.Progress;
            m_notification = Dependencies.Notification;
            m_voxelMap = Dependencies.Map;
            m_gameView = Dependencies.GameView;
         
            m_remoteGameServer = Dependencies.RemoteGameServer;
            m_gSettings = Dependencies.Settings;
            m_console = Dependencies.Console;
            m_minimap = Dependencies.Minimap;

            m_console.Command += OnConsoleCommand;

            m_engine = Dependencies.MatchEngine;
            m_engine.ReadyToStart += OnEngineReadyToStart;
            m_engine.Started += OnEngineStarted;
            m_engine.Error += OnEngineError;
            m_engine.Stopped += OnEngineStopped;
            m_engine.Ping += OnEnginePing;
            m_engine.Paused += OnEnginePaused;
            m_engine.ExecuteCommands += OnEngineCommands;

            INavigation navigation = Dependencies.Navigation;
            if (string.IsNullOrEmpty(navigation.PrevSceneName) || 
                navigation.PrevSceneName == SceneManager.GetActiveScene().name)
            {
                if(m_remoteGameServer != null)
                {
                    if(m_remoteGameServer.IsConnectionStateChanging)
                    {
                        m_remoteGameServer.ConnectionStateChanged += OnConnectionStateChanged;
                    }
                    else
                    {
                        OnConnectionStateChanged(new Error(), new ValueChangedArgs<bool>(false, m_remoteGameServer.IsConnected));
                    }    
                }
            }
            else
            {
                m_gameServer = Dependencies.GameServer;
            }

            m_progress.IsVisible = true;
        }
  
        private void Start()
        {
            INavigation navigation = Dependencies.Navigation;
            if (string.IsNullOrEmpty(navigation.PrevSceneName) ||
                navigation.PrevSceneName == SceneManager.GetActiveScene().name)
            {
                if (m_remoteGameServer == null)
                {
                    OnConnectionStateChanged(new Error(), new ValueChangedArgs<bool>(false, false));
                }
            }
            else
            {
                Dependencies.MatchServer.Activate();
            }
        }

        private void OnDestroy()
        {
            if (m_remoteGameServer != null)
            {
                m_remoteGameServer.ConnectionStateChanged -= OnConnectionStateChanged;
            }

            if (m_console != null)
            {
                m_console.Command -= OnConsoleCommand;
            }

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

        private void OnConnectionStateChanged(Error error, ValueChangedArgs<bool> payload)
        {
            m_gameServer = Dependencies.GameServer;

            TestGameInitArgs gameInitArgs = Dependencies.State.GetValue<TestGameInitArgs>("Battlehub.VoxelGame.TestGameInitArgs");
            if(gameInitArgs == null)
            {
                gameInitArgs = new TestGameInitArgs();
            }
            Dependencies.State.SetValue("Battlehub.VoxelGame.TestGameInitArgs", null);

            bool isConnected = false;
            if(m_remoteGameServer != null)
            {
                m_remoteGameServer.ConnectionStateChanged -= OnConnectionStateChanged;
                isConnected = m_remoteGameServer.IsConnected;
            }

            TestGameInit.Init(gameInitArgs.MapName, gameInitArgs.PlayersCount, gameInitArgs.BotsCount, isConnected, () => { }, initError => m_notification.ShowError(initError));
        }

        private void OnConsoleCommand(IConsole console, string cmd, params string[] args)
        {
            if(cmd == "launch")
            {
                TestGameInitArgs gameInitArgs = new TestGameInitArgs();
                if(args.Length > 2)
                {
                    gameInitArgs.MapName = args[2];
                }
                else
                {
                    console.Echo("launch <playersCount> <botsCount> <mapname>");
                    return;
                }

                if(args.Length > 0)
                {
                    int.TryParse(args[0], out gameInitArgs.PlayersCount);
                    gameInitArgs.PlayersCount = Mathf.Clamp(gameInitArgs.PlayersCount, 0, GameConstants.MaxLocalPlayers);
                }
                else
                {
                    console.Echo("launch <playersCount> <botsCount> <mapname>");
                    return;
                }

                if(args.Length > 1)
                {
                    int.TryParse(args[1], out gameInitArgs.BotsCount);
                    gameInitArgs.BotsCount = Mathf.Clamp(gameInitArgs.BotsCount, 0, GameConstants.MaxPlayers - gameInitArgs.BotsCount);
                }
                else
                {
                    console.Echo("launch <playersCount> <botsCount> <mapname>");
                    return;
                }

                if(Dependencies.Navigation != null)
                {
                    
                }
                DontDestroyOnLoadManager.DestroyAll();
                Dependencies.State.SetValue("Battlehub.VoxelGame.TestGameInitArgs", gameInitArgs);
                Dependencies.Navigation.Navigate("Game");
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

        private void OnEngineStarted(Error error, Player[] players, Guid[] localPlayers, VoxelAbilitiesArray[] voxelAbilities, SerializedTaskArray[] taskTemplates, SerializedTaskTemplatesArray[] taskTemplatesInfo, Room room)
        {
            m_room = room;

            IsReplay = room.Mode == GameMode.Replay;

            m_progress.IsVisible = false;

            if (m_engine.HasError(error))
            {
                m_notification.ShowError(error);
                return;
            }

            m_voxelMap.Map.SetPlayerCount(players.Length);
            m_voxelMap.IsOn = true;
            m_players = players;

            m_voxelAbilities = voxelAbilities.Select(va => va.Abilities.ToDictionary(a => a.Type)).ToArray();
            m_taskTemplates = taskTemplates.Select(t => t.Tasks).ToArray();
            m_TaskTemplateData = taskTemplatesInfo.Select(t => t.Templates).ToArray();
            if (IsReplay)
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


            IsStarted = true;
            if (Started != null)
            {
                Started();
            }

            INavigation nav = Dependencies.Navigation;
            if(nav != null && nav.Args != null && nav.Args.ContainsKey("mapeditor"))
            {
                m_console.Write("mapeditor");
                object args = nav.Args["mapeditor"];
                if(args != null)
                {
                    m_console.Write(args.ToString());
                }
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

        private void OnEngineCommands(Error error, long clientTick, CommandsBundle commandBundle)
        {
            if(m_engine.HasError(error))
            {
                if(error.Code != StatusCode.Outdated)
                {
                    m_notification.ShowError(error);
                    return;
                }
            }

            long serverTick = commandBundle.Tick;
            CommandsArray[] playersCommands = commandBundle.Commands;
            //List<TaskStateInfo> taskStateInfo = commandBundle.TasksStateInfo;
            bool isGameCompleted = commandBundle.IsGameCompleted;

            m_minimap.BeginUpdate();

            List<IMatchPlayerControllerCli> defeatedPlayers = null;
            for(int p = 0; p < playersCommands.Length; ++p)
            {
                CommandsArray commands = playersCommands[p];
                if(commands.Commands == null)
                {
                    continue;
                }

                if (error.Code == StatusCode.Outdated)
                {
                    Debug.LogWarning("Executing outdated command a little bit faster " + serverTick);
                }

                IMatchPlayerControllerCli playerController = m_playerControllers[p];
                long lagTicks = clientTick - serverTick;
                Debug.Assert(lagTicks >= 0);

                playerController.Execute(commands.Commands, clientTick, lagTicks);

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
                        PlayerDefeated(defeatedPlayer.Index);
                    }
                }
            }

            if (!m_gSettings.DisableFogOfWar)
            {
                m_minimap.EndUpdate();
            }
            

            IsCompleted = isGameCompleted;
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
            return playerController.Units.OfType<IMatchUnitAssetView>().Select(uav => uav.Id);
        }

        public IEnumerable<long> GetAssets(int playerIndex)
        {
            IMatchPlayerControllerCli playerController = m_playerControllers[playerIndex];
            return playerController.Assets.OfType<IMatchUnitAssetView>().Select(uav => uav.Id);
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

            long[] units = m_playerControllers[index].Units.OfType<IMatchUnitAssetView>().Select(uav => uav.Id).ToArray();

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

        public IMatchPlayerView GetPlayerView(int index)
        {
            return m_playerControllers[index];
        }

        public IMatchPlayerView GetPlayerView(Guid guid)
        {
            return m_playerControllers[GetPlayerIndex(guid)];
        }

        public bool IsSuitableCmdFor(Guid playerId, long unitIndex, int cmdCode)
        {
            throw new NotImplementedException();
        }

        public SerializedTask[] GetTaskTemplates(int playerIndex)
        {
            return m_taskTemplates[playerIndex];
        }

        public SerializedTaskTemplate[] GetTaskTemplateData(int playerIndex)
        {
            return m_TaskTemplateData[playerIndex];
        }

        public void Submit(int playerId, Cmd cmd)
        {
            m_engine.Submit(playerId, cmd);
        }
    }

}
