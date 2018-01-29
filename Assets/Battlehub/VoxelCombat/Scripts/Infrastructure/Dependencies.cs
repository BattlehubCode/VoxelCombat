using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class Dependencies : MonoBehaviour
    {
        private void Awake()
        {
            //m_inputManager = FindObjectOfType<VoxelInputManager>();
            m_inputManager = FindObjectOfType<InControlAdapter>();
            m_console = FindObjectOfType<VoxelConsole>();
            m_game = FindObjectOfType<VoxelGame>();
            m_voxelFactory = FindObjectOfType<VoxelFactory>();
            m_effectFactory = FindObjectOfType<ParticleEffectFactory>();
            m_map = FindObjectOfType<VoxelMap>();
            m_progress = FindObjectOfType<ProgressIndicator>();
            m_job = FindObjectOfType<Job>();
            m_materialsCache = FindObjectOfType<MaterialsCache>();
            m_gameView = FindObjectOfType<GameView>();
            m_settings = FindObjectOfType<GlobalSettings>();
            m_matchEngine = FindObjectOfType<MatchEngineCli>();
            m_chatServer = FindObjectOfType<RemoteChatServer>();
            m_gameServer = FindObjectOfType<RemoteGameServer>();
            m_matchServer = FindObjectOfType<RemoteMatchServer>();
            m_navigation = FindObjectOfType<Navigation>();
            m_eventSystemManager = FindObjectOfType<EventSystemManager>();
            m_notification = FindObjectOfType<Notification>();
            
            UnitSelection[] selection = FindObjectsOfType<UnitSelection>();
            if(selection.Length > 0)
            {
                m_unitSelection = selection[0];
            }

            if(selection.Length > 1)
            {
                m_targetSelection = selection[1];
            }

            m_gState = new GlobalState();
        }

        private void OnDestroy()
        {
            m_inputManager = null;
            m_console = null;
            m_game = null;
            m_voxelFactory = null;
            m_effectFactory = null;
            m_map = null;
            m_progress = null;
            m_job = null;
            m_materialsCache = null;
            m_gameView = null;
            m_settings = null;
            m_matchEngine = null;
            m_chatServer = null;
            m_gameServer = null;
            m_matchServer = null;
            m_navigation = null;
            m_notification = null;
            m_unitSelection = null;
            m_targetSelection = null;
            m_eventSystemManager = null;

            m_gState = null;
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

        private static IChatServer m_chatServer;
        public static IChatServer ChatServer
        {
            get { return m_chatServer; }
        }

        private static IGameServer m_gameServer;
        public static IGameServer GameServer
        {
            get { return m_gameServer; }
        }

        private static IMatchServer m_matchServer;
        public static IMatchServer MatchServer
        {
            get { return m_matchServer; }
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


