using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public delegate void PlayerCommandsPanelEventHandler();


    public class PlayerCommandsPanel : MonoBehaviour
    {
        public event PlayerCommandsPanelEventHandler Wall;
        public event PlayerCommandsPanelEventHandler Bomb;
        public event PlayerCommandsPanelEventHandler Spawner;
        public event PlayerCommandsPanelEventHandler Split;
        public event PlayerCommandsPanelEventHandler Split4;
        public event PlayerCommandsPanelEventHandler Grow;
        public event PlayerCommandsPanelEventHandler Diminish;

        public event PlayerCommandsPanelEventHandler Closed;

        [SerializeField]
        private Button m_btnClose;

        [SerializeField]
        private Button m_wallBtn;

        [SerializeField]
        private Button m_bombBtn;

        [SerializeField]
        private Button m_spawnButton;

        [SerializeField]
        private Button m_splitButton;

        [SerializeField]
        private Button m_split4Button;

        [SerializeField]
        private Button m_growButton;

        [SerializeField]
        private Button m_diminishButton;

        [SerializeField]
        private Button[] m_order;

        private int m_selectedButton;

        private IVoxelInputManager m_inputManager;
        private IVoxelGame m_gameState;

        public int LocalPlayerIndex
        {
            get;
            set;
        }


        public bool m_isOpenedLatch;
        public bool IsOpen
        {
            get { return gameObject.activeSelf; }
        }

        public void SetIsOpen(bool value, 
            bool canCreateWall = false,
            bool canCreateBomb = false,
            bool canCreateSpawner = false,
            bool canSplit = false,
            bool canSplit4 = false,
            bool canGrow = false,
            bool canDiminish = false)
        {
            gameObject.SetActive(value);
            m_gameState.IsContextActionInProgress(LocalPlayerIndex, value);

            for (int i = 0; i < m_order.Length; ++i)
            {
                m_order[i].OnDeselect(null);
            }

            m_bombBtn.interactable = canCreateBomb;
            m_wallBtn.interactable = canCreateWall;
            m_spawnButton.interactable = canCreateSpawner;
            m_splitButton.interactable = canSplit;
            m_split4Button.interactable = canSplit4;
            m_growButton.interactable = canGrow;
            m_diminishButton.interactable = canDiminish;
      
            if (value)
            {
                m_selectedButton = -1;
                SelectNext();
            }
            else
            {
                if (Closed != null)
                {
                    Closed();
                }
            }

            m_isOpenedLatch = false;
        }

        private void Awake()
        {
            if(m_order.Length == 0)
            {
                m_order = new[]
                {
                    m_btnClose,
                    m_wallBtn,
                    m_bombBtn,
                    m_spawnButton,
                    m_splitButton,
                    m_split4Button,
                    m_growButton,
                    m_diminishButton,
                };
            }

            for(int i = 0; i < m_order.Length; ++i)
            {
                m_order[m_selectedButton].OnDeselect(null);
            }

            m_gameState = Dependencies.GameState;
            m_inputManager = Dependencies.InputManager;
        }

        private void OnDestroy()
        {
     
        }


        private float m_prevAxis;
        private void LateUpdate()
        {
            if(m_gameState.IsMenuOpened(LocalPlayerIndex))
            {
                return;
            }

            if(m_isOpenedLatch == false)
            {
                m_isOpenedLatch = IsOpen;
                return;
            }

            float axis = m_inputManager.GetAxisRaw(InputAction.MoveSide, LocalPlayerIndex);
            if(axis == 0)
            {
                axis = m_inputManager.GetAxisRaw(InputAction.CursorX, LocalPlayerIndex);
            }

            if(axis > 0)
            {
                axis = Mathf.CeilToInt(axis);
            }

            if(axis < 0)
            {
                axis = Mathf.FloorToInt(axis);
            }

            if (m_prevAxis != axis) 
            {
                if(axis > 0)
                {
                    SelectNext();
                }
                else if(axis < 0)
                {
                    SelectPrev();
                }

                m_prevAxis = axis;
            }

            if(m_inputManager.GetButtonDown(InputAction.A, LocalPlayerIndex))
            {
                switch(m_selectedButton)
                {
                    case 0:
                        SetIsOpen(false);
                        break;
                    case 1:
                        OnWall();
                        break;
                    case 2:
                        OnBomb();
                        break;
                    case 3:
                        OnSpawn();
                        break;
                    case 4:
                        OnSplit();
                        break;
                    case 5:
                        OnSplit4();
                        break;
                    case 6:
                        OnGrow();
                        break;
                    case 7:
                        OnDiminish();
                        break;
                }

                Debug.Log(LocalPlayerIndex);
                SetIsOpen(false);
            }

            if(m_inputManager.GetButtonDown(InputAction.B, LocalPlayerIndex))
            {
                SetIsOpen(false);
            }
        }

        private void SelectNext()
        {
            if(m_order.Length > 0)
            {
                if(m_selectedButton >= 0)
                {
                    m_order[m_selectedButton].OnDeselect(null);
                }

                do
                {
                    m_selectedButton++;
                    m_selectedButton %= m_order.Length;
                }
                while (!m_order[m_selectedButton].interactable);

                m_order[m_selectedButton].OnSelect(null);
            }
        }

        private void SelectPrev()
        {
            if (m_order.Length > 0)
            {
                if (m_selectedButton >= 0)
                {
                    m_order[m_selectedButton].OnDeselect(null);
                }

                do
                {
                    m_selectedButton--;
                    if (m_selectedButton < 0)
                    {
                        m_selectedButton = m_order.Length - 1;
                    }
                }
                while (!m_order[m_selectedButton].interactable);

                m_order[m_selectedButton].OnSelect(null);
            }
        }


        private void OnWall()
        {
            if(Wall != null)
            {
                Wall();
            }
        }

        private void OnBomb()
        {
            if(Bomb != null)
            {
                Bomb();
            }
        }

        private void OnSpawn()
        {
            if(Spawner != null)
            {
                Spawner();
            }

        }

        private void OnSplit()
        {
            if(Split != null)
            {
                Split();
            }
        }

        private void OnSplit4()
        {
            if(Split4 != null)
            {
                Split4();
            }
        }

        private void OnGrow()
        {
            if(Grow != null)
            {
                Grow();
            }
        }

        private void OnDiminish()
        {
            if(Diminish != null)
            {
                Diminish();
            }
        }
    }

}
