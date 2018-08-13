using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface IGameView
    {
        event EventHandler Initialized;

        bool IsInitialized
        {
            get;
        }

        bool IsOn
        {
            get;
            set;
        }

        void Initialize(int viewportsCount, bool isOn);

        IGameViewport GetViewport(int index);

        IPlayerUnitController GetUnitController(int index);

        IPlayerSelectionController GetSelectionController(int index);

        IBoxSelector GetBoxSelector(int index);

        IPlayerCameraController GetCameraController(int index);

        IVirtualMouse GetVirtualMouse(int index);

        ITargetSelectionController GetTargetSelectionController(int index);
    }

    public class GameView : MonoBehaviour, IGameView
    {
        public event EventHandler Initialized;


        [SerializeField]
        private GameViewport m_gameViewportPrefab;

        [SerializeField]
        private Transform[] m_viewportPlaceholders;

        private GameViewport[] m_gameViewports;

        [SerializeField]
        private GameObject m_secondRow;

        [SerializeField]
        private GameObject m_secondCol;

        [SerializeField]
        private int m_viewportCount = 1;

        private bool m_isInitialized = false;

        //This camera will be destroyed when game will be started
        [SerializeField]
        private Camera m_initializationCamera;

        [SerializeField]
        private GameObject m_menuOverlay;
        
        [SerializeField]
        private bool m_isOn = false;
        public bool IsOn
        {
            get { return m_isOn; }
            set
            {
                if(!m_isInitialized && value)
                {
                    throw new InvalidOperationException("Call Initialize method first");
                }

                if (m_initializationCamera != null && value)
                {
                    Destroy(m_initializationCamera.gameObject);
                }

                if (m_isOn != value)
                {
                    m_isOn = value;
                    UpdateGameViewMode();
                }

                if (m_menuOverlay != null)
                {
                    m_menuOverlay.SetActive(m_isOn);
                }
            }
        }

        public bool IsInitialized
        {
            get { return m_isInitialized; }
        }

        public void Initialize(int viewportsCount, bool isOn)
        {
            if(m_initializationCamera && isOn)
            {
                Destroy(m_initializationCamera.gameObject);
            }

            if (m_menuOverlay != null)
            {
                m_menuOverlay.SetActive(isOn);
            }

            m_isInitialized = true;
            m_viewportCount = viewportsCount;
            m_isOn = isOn;

            InitViewports();
            UpdateGameViewMode();

            if(Initialized != null)
            {
                Initialized(this, EventArgs.Empty);
            }
        }


        private void UpdateGameViewMode()
        {
            UpdateCursorMode();

            if (m_gameViewports != null)
            {
                for (int i = 0; i < m_gameViewports.Length; ++i)
                {
                    GameViewport gameViewport = m_gameViewports[i];
                    gameViewport.Camera.gameObject.SetActive(m_isOn);

                    PlayerCameraController cameraController = gameViewport.GetComponent<PlayerCameraController>();
                    if (cameraController != null)
                    {
                        cameraController.gameObject.SetActive(m_isOn);
                    }
                }
            }
        }

        private void UpdateCursorMode()
        {
            CursorHelper.visible = !m_isOn;
            CursorHelper.lockState = m_isOn ? CursorLockMode.Locked : CursorLockMode.None;
        }


        public IGameViewport GetViewport(int index)
        {
            return m_viewportPlaceholders[index].GetComponentInChildren<GameViewport>(true);
        }

        private void Awake()
        {
            if(m_isOn)
            {
                Initialize(m_viewportCount, m_isOn);
            }
            else
            {
                if (m_menuOverlay != null)
                {
                    m_menuOverlay.SetActive(false);
                }
            }
        }
#if UNITY_EDITOR
        private void Update()
        {
            if (Dependencies.InputManager.GetButtonDown(InputAction.ToggleCursor, -1, true, false))
            {
                CursorHelper.visible = !CursorHelper.visible;
                if(CursorHelper.visible)
                {
                    CursorHelper.lockState = CursorLockMode.None;
                }
                else
                {
                    CursorHelper.lockState = CursorLockMode.Locked;
                } 
            }

            if (!CursorHelper.visible)
            {
                if(CursorHelper.lockState != CursorLockMode.Locked)
                {
                    CursorHelper.lockState = CursorLockMode.Locked;
                }
            }
        }
#endif


        private void InitViewports()
        {
            if (m_gameViewports != null)
            {
                for (int i = 0; i < m_gameViewports.Length; ++i)
                {
                    Destroy(m_gameViewports[i].gameObject);
                }
            }

            if (m_viewportPlaceholders.Length < m_viewportCount)
            {
                Debug.LogError("m_viewports.Length < m_playerCount");
            }
            else
            {
                m_gameViewports = new GameViewport[m_viewportCount];

                m_secondRow.SetActive(m_viewportCount > 2);
                m_secondCol.SetActive(m_viewportCount > 1);

                IEventSystemManager eventSystemMan = Dependencies.EventSystemManager;

                for (int i = 0; i < m_viewportCount; ++i)
                {
                    GameViewport gameViewport = Instantiate(m_gameViewportPrefab, m_viewportPlaceholders[i]);
                    gameViewport.Camera.gameObject.SetActive(m_isOn);
                    gameViewport.name = "Viewport" + i;
                    gameViewport.LocalPlayerIndex = i;
                    m_gameViewports[i] = gameViewport;

                    eventSystemMan.Apply(gameViewport.gameObject, i);

                    PlayerMenu playerMenu = gameViewport.GetComponent<PlayerMenu>();
                    if (playerMenu != null)
                    {
                        playerMenu.LocalPlayerIndex = i;
                    }
                }

                VoxelConsole console = GetComponentInParent<VoxelConsole>();
                console.Initialize();

                for (int i = 0; i < m_viewportCount; ++i)
                {
                    PlayerConsoleCommandHandler cmdHandler = m_viewportPlaceholders[i].GetComponentInChildren<PlayerConsoleCommandHandler>();
                    if (cmdHandler != null)
                    {
                        cmdHandler.LocalPlayerIndex = i;
                        cmdHandler.Initialize();
                    }
                }
            }
        }

        public IPlayerUnitController GetUnitController(int index)
        {
            if (index < 0 || m_gameViewports.Length <= index)
            {
                return null;
            }
            return m_gameViewports[index].GetComponent<PlayerUnitController>();
        }

        public IPlayerSelectionController GetSelectionController(int index)
        {
            if (index < 0 || m_gameViewports.Length <= index)
            {
                return null;
            }
            return m_gameViewports[index].GetComponent<PlayerSelectionController>();
        }

        public IBoxSelector GetBoxSelector(int index)
        {
            if (index < 0 || m_gameViewports.Length <= index)
            {
                return null;
            }
            return m_gameViewports[index].GetComponent<BoxSelection>();
        }

        public IPlayerCameraController GetCameraController(int index)
        {
            if(index < 0 || m_gameViewports.Length <= index)
            {
                return null;
            }

            return m_gameViewports[index].GetComponent<PlayerCameraController>();
        }

        public IVirtualMouse GetVirtualMouse(int index)
        {
            if (index < 0 || m_gameViewports.Length <= index)
            {
                return null;
            }

            return m_gameViewports[index].GetComponent<PlayerCameraController>();
        }

        public ITargetSelectionController GetTargetSelectionController(int index)
        {
            if (index < 0 || m_gameViewports.Length <= index)
            {
                return null;
            }
            return m_gameViewports[index].GetComponent<TargetSelectionController>();
        }
    }
}
