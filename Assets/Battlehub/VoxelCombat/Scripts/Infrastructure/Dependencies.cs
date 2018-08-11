using Battlehub.VoxelCombat.Tests;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class Dependencies : MonoBehaviour
    {
        [SerializeField]
        private bool InstantiateRemoteServer = true;
        [SerializeField]
        private bool InstantiateLocalServer = true;
        [SerializeField]
        private bool TestMode = false;

        private void Awake()
        {
            m_logger = new Logger();
            m_gState = new GlobalState();

            if(TestMode)
            {
                m_console = FindObjectOfType<ConsoleMock>();
                m_game = FindObjectOfType<VoxelGame>();
                m_map = FindObjectOfType<VoxelMapMock>();
                m_minimap = FindObjectOfType<MinmapRendererMock>();
                m_progress = FindObjectOfType<ProgressMock>();
                m_notification = FindObjectOfType<NotificationMock>();
                m_navigation = FindObjectOfType<NavigationMock>();
                m_gameView = FindObjectOfType<GameViewMock>();

                UnitSelectionMock[] selection = FindObjectsOfType<UnitSelectionMock>();
                if (selection.Length > 0)
                {
                    m_unitSelection = selection[0];
                }

                if (selection.Length > 1)
                {
                    m_targetSelection = selection[1];
                }
            }
            else
            {
                m_inputManager = FindObjectOfType<InControlAdapter>();
                m_console = FindObjectOfType<VoxelConsole>();
                m_game = FindObjectOfType<VoxelGame>();
                m_voxelFactory = FindObjectOfType<VoxelFactory>();
                m_effectFactory = FindObjectOfType<ParticleEffectFactory>();
                m_map = FindObjectOfType<VoxelMap>();
                m_minimap = FindObjectOfType<VoxelMinimapRenderer>();
                m_progress = FindObjectOfType<ProgressIndicator>();
                m_materialsCache = FindObjectOfType<MaterialsCache>();
                m_gameView = FindObjectOfType<GameView>();
                m_navigation = FindObjectOfType<Navigation>();
                m_eventSystemManager = FindObjectOfType<EventSystemManager>();
                m_notification = FindObjectOfType<Notification>();

                UnitSelection[] selection = FindObjectsOfType<UnitSelection>();
                if (selection.Length > 0)
                {
                    m_unitSelection = selection[0];
                }

                if (selection.Length > 1)
                {
                    m_targetSelection = selection[1];
                }
            }

            m_job = FindObjectOfType<Job>();
            m_settings = FindObjectOfType<GlobalSettings>();
            m_matchEngine = FindObjectOfType<MatchEngineCli>();
            m_remoteMatchServer = FindObjectOfType<RemoteMatchServer>();
            m_localMatchServer = FindObjectOfType<LocalMatchServer>();

            GameObject serverGO = null;
            if (!m_remoteGameServer)
            {
                m_remoteGameServer = FindObjectOfType<RemoteGameServer>();
                if(!m_remoteGameServer && InstantiateRemoteServer)
                {
                    if(serverGO == null)
                    {
                        serverGO = new GameObject();
                        serverGO.name = "Server";
                        serverGO.AddComponent<Dispatcher.Dispatcher>();
                    }
                    m_remoteGameServer = serverGO.AddComponent<RemoteGameServer>();
                }
            }

            if(!m_localGameServer )
            {
                m_localGameServer = FindObjectOfType<LocalGameServer>();
                if (!m_localGameServer && InstantiateLocalServer)
                {
                    if (serverGO == null)
                    {
                        serverGO = new GameObject();
                        serverGO.name = "Server";
                        serverGO.AddComponent<Dispatcher.Dispatcher>();
                    }
                    m_localGameServer = serverGO.AddComponent<LocalGameServer>();
                }
            }

            if(m_serializersPool == null)
            {
                m_serializersPool = new SerializersPool(10);
            }
        }

        private void OnDestroy()
        {
            m_logger = null;
            m_inputManager = null;
            m_console = null;
            m_game = null;
            m_voxelFactory = null;
            m_effectFactory = null;
            m_map = null;
            m_minimap = null;
            m_progress = null;
            m_job = null;
            m_materialsCache = null;
            m_gameView = null;
            m_settings = null;
            m_matchEngine = null;
            //m_remoteGameServer = null;
            //m_localGameServer = null;
            m_remoteMatchServer = null;
            m_localMatchServer = null;
            m_navigation = null;
            m_notification = null;
            m_unitSelection = null;
            m_targetSelection = null;
            m_eventSystemManager = null;

            m_gState = null;

            //Don't do it serializers are always required. no need to cleanup
            //m_serializersPool = null;
        }

        private static Pool<ProtobufSerializer> m_serializersPool;
        public static Pool<ProtobufSerializer> Serializer
        {
            get { return m_serializersPool; }
        }

        private static ILogger m_logger;
        public static ILogger Logger
        {
            get { return m_logger; }
        }

        private static IEventSystemManager m_eventSystemManager;
        public static IEventSystemManager EventSystemManager
        {
            get { return m_eventSystemManager; }
        }

        private static IUnitSelection m_targetSelection;
        public static IUnitSelection TargetSelection
        {
            get { return m_targetSelection; }
        }

        private static IUnitSelection m_unitSelection;
        public static IUnitSelection UnitSelection
        {
            get { return m_unitSelection; }
        }

        private static INotification m_notification;
        public static INotification Notification
        {
            get { return m_notification; }
        }

        private static INavigation m_navigation;
        public static INavigation Navigation
        {
            get { return m_navigation; }
        }

        private static IGlobalState m_gState;
        public static IGlobalState State
        {
            get { return m_gState; }
        }

        private static IMatchEngineCli m_matchEngine;
        public static IMatchEngineCli MatchEngine
        {
            get { return m_matchEngine; }
        }

        private static RemoteGameServer m_remoteGameServer;
        public static IGameServer RemoteGameServer
        {
            get { return m_remoteGameServer; }
        }

        private static LocalGameServer m_localGameServer;
        public static IGameServer LocalGameServer
        {
            get { return m_localGameServer; }
        }

        public static IGameServer GameServer
        {
            get
            {
                if(m_remoteGameServer != null && (m_remoteGameServer.IsConnected || m_remoteGameServer.IsConnectionStateChanging))
                {
                    return m_remoteGameServer;
                }
                else
                {
                    return m_localGameServer;
                }
            }
        }

        private static IMatchServer m_localMatchServer;
        public static IMatchServer LocalMatchServer
        {
            get { return m_localMatchServer; }
        }

        private static IMatchServer m_remoteMatchServer;
        public static IMatchServer RemoteMatchServer
        {
            get { return m_remoteMatchServer; }
        }

        public static IMatchServer MatchServer
        {
            get
            {
                if (m_remoteGameServer != null && (m_remoteGameServer.IsConnected || m_remoteGameServer.IsConnectionStateChanging)) //This is not mistake
                {
                    return m_remoteMatchServer;
                }
                else
                {
                    return m_localMatchServer;
                }
            }
        }

        private static IGlobalSettings m_settings;
        public static IGlobalSettings Settings
        {
            get { return m_settings; }
        }

        private static IGameView m_gameView;
        public static IGameView GameView
        {
            get { return m_gameView; }
        }

        private static IMaterialsCache m_materialsCache;
        public static IMaterialsCache MaterialsCache
        {
            get { return m_materialsCache; }
        }

        private static IVoxelInputManager m_inputManager;
        public static IVoxelInputManager InputManager
        {
            get { return m_inputManager; }
        }

        private static IConsole m_console;
        public static IConsole Console
        {
            get { return m_console; }
        }

        private static IVoxelGame m_game;
        public static IVoxelGame GameState
        {
            get { return m_game; }
        }

        private static IVoxelFactory m_voxelFactory;
        public static IVoxelFactory VoxelFactory
        {
            get { return m_voxelFactory; }
        }

        private static IParticleEffectFactory m_effectFactory;
        public static IParticleEffectFactory EffectFactory
        {
            get { return m_effectFactory; }
        }

        private static IVoxelMap m_map;
        public static IVoxelMap Map
        {
            get { return m_map; }
        }

        private static IVoxelMinimapRenderer m_minimap;
        public static IVoxelMinimapRenderer Minimap
        {
            get { return m_minimap; }
        }

        private static IProgressIndicator m_progress;
        public static IProgressIndicator Progress
        {
            get { return m_progress; }
        }

        private static IJob m_job;
        public static IJob Job
        {
            get { return m_job; }
        }

               
    }
}


