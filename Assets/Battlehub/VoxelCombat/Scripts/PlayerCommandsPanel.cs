using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public delegate void PlayerCommandsPanelEventHandler();


    public class PlayerCommandsPanel : MonoBehaviour
    {
        public event PlayerCommandsPanelEventHandler Move;
        public event PlayerCommandsPanelEventHandler Attack;
        public event PlayerCommandsPanelEventHandler Auto;
        public event PlayerCommandsPanelEventHandler Wall;
        public event PlayerCommandsPanelEventHandler Bomb;
        public event PlayerCommandsPanelEventHandler Spawner;
        public event PlayerCommandsPanelEventHandler Split;
        public event PlayerCommandsPanelEventHandler Split4;
        public event PlayerCommandsPanelEventHandler Grow;
        public event PlayerCommandsPanelEventHandler Diminish;

        [SerializeField]
        private Button m_moveBtn;

        [SerializeField]
        private Button m_attackBtn;

        [SerializeField]
        private Button m_autoBtn;

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

        private IVoxelInputManager m_inputManager;
        private IVoxelGame m_gameState;
        private IUnitSelection m_selection;

        [SerializeField]
        private Button m_activeButton;

        public int LocalPlayerIndex
        {
            get;
            set;
        }

        private void Awake()
        {
            m_gameState = Dependencies.GameState;
            m_inputManager = Dependencies.InputManager;
            m_selection = Dependencies.UnitSelection;

            m_attackBtn.onClick.AddListener(OnAttack);
            m_moveBtn.onClick.AddListener(OnMove);
            m_autoBtn.onClick.AddListener(OnAuto);
            m_bombBtn.onClick.AddListener(OnBomb);
            m_wallBtn.onClick.AddListener(OnWall);
            m_spawnButton.onClick.AddListener(OnSpawn);
            m_splitButton.onClick.AddListener(OnSplit);
            m_split4Button.onClick.AddListener(OnSplit4);
            m_growButton.onClick.AddListener(OnGrow);
            m_diminishButton.onClick.AddListener(OnDiminish);

            UpdateState();
            m_selection.SelectionChanged += OnSelectionChanged;
        }

        private void OnDestroy()
        {
            if(m_attackBtn != null)
            {
                m_attackBtn.onClick.RemoveListener(OnAttack);
            }

            if(m_moveBtn != null)
            {
                m_moveBtn.onClick.RemoveListener(OnMove);
            }
            
            if(m_autoBtn != null)
            {
                m_autoBtn.onClick.RemoveListener(OnAuto);
            }
            
            if (m_bombBtn != null)
            {
                m_bombBtn.onClick.RemoveListener(OnBomb);
            }
            
            if(m_wallBtn != null)
            {
                m_wallBtn.onClick.RemoveListener(OnWall);
            }
            
            if(m_spawnButton != null)
            {
                m_spawnButton.onClick.RemoveListener(OnSpawn);
            }
            
            if(m_splitButton != null)
            {
                m_splitButton.onClick.RemoveListener(OnSplit);
            }
            
            if(m_split4Button != null)
            {
                m_split4Button.onClick.RemoveListener(OnSplit4);
            }
            
            if(m_growButton != null)
            {
                m_growButton.onClick.RemoveListener(OnGrow);
            }
            
            if(m_diminishButton != null)
            {
                m_diminishButton.onClick.RemoveListener(OnDiminish);
            }

            if(m_selection != null)
            {
                m_selection.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnSelectionChanged(int selectorIndex, int unitOwnerIndex, long[] selected, long[] unselected)
        {
            if (m_gameState.LocalToPlayerIndex(LocalPlayerIndex) != selectorIndex)
            {
                return;
            }

            UpdateState();
        }

        private void UpdateState()
        {
            int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);
            long[] selection = m_selection.GetSelection(playerIndex, playerIndex);

            m_attackBtn.gameObject.SetActive(selection.Length > 0);
            m_moveBtn.gameObject.SetActive(selection.Length > 0);
            m_autoBtn.gameObject.SetActive(selection.Length > 0);
            m_bombBtn.gameObject.SetActive(false);
            m_wallBtn.gameObject.SetActive(false);
            m_spawnButton.gameObject.SetActive(false);
            m_growButton.gameObject.SetActive(false);
            m_diminishButton.gameObject.SetActive(false);
            m_splitButton.gameObject.SetActive(false);
            m_split4Button.gameObject.SetActive(false);

            for (int i = 0; i < selection.Length; ++i)
            {
                IVoxelDataController dc = m_gameState.GetVoxelDataController(playerIndex, selection[i]);
                if (dc != null)
                {
                    if (dc.CanConvert((int)KnownVoxelTypes.Bomb))
                    {
                        m_bombBtn.gameObject.SetActive(true);
                    }

                    if (dc.CanConvert((int)KnownVoxelTypes.Ground))
                    {
                        m_wallBtn.gameObject.SetActive(true);
                    }

                    if (dc.CanConvert((int)KnownVoxelTypes.Spawner))
                    {
                        m_spawnButton.gameObject.SetActive(true);
                    }

                    if (dc.CanGrow())
                    {
                        m_growButton.gameObject.SetActive(true);
                    }

                    if (dc.CanDiminish())
                    {
                        m_diminishButton.gameObject.SetActive(true);
                    }

                    if (dc.CanSplit())
                    {
                        m_splitButton.gameObject.SetActive(true);
                    }

                    if (dc.CanSplit4())
                    {
                        m_split4Button.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void OnAttack()
        {
            ActivateButton(m_attackBtn);
            if(Attack != null)
            {
                Attack();
            }
        }
        private void OnMove()
        {
            ActivateButton(m_moveBtn);
            if(Move != null)
            {
                Move();
            }
        }

        private void OnAuto()
        {
            ActivateButton(m_autoBtn);
            if (Auto != null)
            {
                Auto();
            }
        }

        private void OnWall()
        {
            ActivateButton(m_wallBtn);
            if (Wall != null)
            {
                Wall();
            }
        }

        private void OnBomb()
        {
            ActivateButton(m_bombBtn);
            if (Bomb != null)
            {
                Bomb();
            }
        }

        private void OnSpawn()
        {
            ActivateButton(m_spawnButton);
            if (Spawner != null)
            {
                Spawner();
            }

        }

        private void OnSplit()
        {
            ActivateButton(m_splitButton);
            if (Split != null)
            {
                Split();
            }
        }

        private void OnSplit4()
        {
            ActivateButton(m_split4Button);
            if (Split4 != null)
            {
                Split4();
            }
        }

        private void OnGrow()
        {
            ActivateButton(m_growButton);
            if (Grow != null)
            {
                Grow();
            }
        }

        private void OnDiminish()
        {
            ActivateButton(m_diminishButton);
            if (Diminish != null)
            {
                Diminish();
            }
        }

        private void ActivateButton(Button button)
        {
            if (m_activeButton != null)
            {
                Transform activeOutline = m_activeButton.transform.Find("ActiveOutline");
                activeOutline.gameObject.SetActive(false);
            }
            m_activeButton = button;
            if(m_activeButton != null)
            {
                Transform activeOutline = m_activeButton.transform.Find("ActiveOutline");
                activeOutline.gameObject.SetActive(true);
            }
        }
    }
}
