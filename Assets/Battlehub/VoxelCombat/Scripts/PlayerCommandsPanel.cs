using UnityEngine;
using UnityEngine.UI;
using Battlehub.UIControls;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Battlehub.VoxelCombat
{
    public delegate void PlayerCommandsPanelEventHandler();


    public class PlayerCommandsPanel : MonoBehaviour
    {
        public event PlayerCommandsPanelEventHandler Cancel;
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
        private Selectable m_minimap;

        [SerializeField]
        private Button m_cancelBtn;

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

        [SerializeField]
        private GameObject m_tooltipRoot;

        [SerializeField]
        private Text m_tooltip;

        private IVoxelInputManager m_inputManager;
        private IVoxelGame m_gameState;
        private IUnitSelection m_selection;
        private IEventSystemManager m_eventSystemMananger;
        private IndependentEventSystem m_eventSystem;

        private int m_bNavIndex = 0;
        private UIBehaviour[] m_bNavSequence;

        private Button m_activeButton;
        private bool m_isActive;
        
        public bool IsActive
        {
            get { return m_isActive; }
            set
            {
                int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);
                long[] selection = m_selection.GetSelection(playerIndex, playerIndex);
                bool newValue = value && selection.Length > 0;
                if (m_isActive != newValue)
                {
                    m_isActive =  newValue;

                    m_eventSystem.EventSystemUpdate -= OnEventSystemUpdate;

                    if (m_isActive)
                    {
                        m_eventSystem.EventSystemUpdate += OnEventSystemUpdate;
                    }
                }

                m_gameState.IsContextActionInProgress(LocalPlayerIndex, m_isActive);
            }
        }

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
            m_eventSystemMananger = Dependencies.EventSystemManager;


            m_bNavSequence = new[]
            {
                m_autoBtn,
                m_minimap,
                null
            };


            m_cancelBtn.onClick.AddListener(OnCancel);
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

            AddListeners(m_cancelBtn);
            AddListeners(m_attackBtn);
            AddListeners(m_moveBtn);
            AddListeners(m_autoBtn);
            AddListeners(m_bombBtn);
            AddListeners(m_wallBtn);
            AddListeners(m_spawnButton);
            AddListeners(m_splitButton);
            AddListeners(m_split4Button);
            AddListeners(m_growButton);
            AddListeners(m_diminishButton);
        }
        private void AddListeners(Selectable selectable)
        {
            AddListener(selectable, EventTriggerType.Select, OnSelect);
            AddListener(selectable, EventTriggerType.Deselect, OnDeselect);
        }

        private void Start()
        {
            bool isKeyboardAndMouse = m_inputManager.IsKeyboardAndMouse(LocalPlayerIndex);
            var nav = m_minimap.navigation;
            nav.mode = isKeyboardAndMouse ?
                UnityEngine.UI.Navigation.Mode.None :
                UnityEngine.UI.Navigation.Mode.Explicit;

            m_eventSystem = m_eventSystemMananger.GetEventSystem(LocalPlayerIndex);
            UpdateState();
            m_selection.SelectionChanged += OnSelectionChanged;
        }

        private void OnDestroy()
        {
            if(m_eventSystem != null)
            {
                m_eventSystem.EventSystemUpdate -= OnEventSystemUpdate;
            }

            if(m_cancelBtn != null)
            {
                RemoveListeners(m_cancelBtn);
                m_cancelBtn.onClick.RemoveListener(OnCancel);
            }

            if(m_attackBtn != null)
            {
                RemoveListeners(m_attackBtn);
                m_attackBtn.onClick.RemoveListener(OnAttack);
            }

            if(m_moveBtn != null)
            {
                RemoveListeners(m_moveBtn);
                m_moveBtn.onClick.RemoveListener(OnMove);
            }
            
            if(m_autoBtn != null)
            {
                RemoveListeners(m_autoBtn);
                m_autoBtn.onClick.RemoveListener(OnAuto);
            }
            
            if (m_bombBtn != null)
            {
                RemoveListeners(m_bombBtn);
                m_bombBtn.onClick.RemoveListener(OnBomb);
            }
            
            if(m_wallBtn != null)
            {
                RemoveListeners(m_wallBtn);
                m_wallBtn.onClick.RemoveListener(OnWall);
            }
            
            if(m_spawnButton != null)
            {
                RemoveListeners(m_spawnButton);
                m_spawnButton.onClick.RemoveListener(OnSpawn);
            }
            
            if(m_splitButton != null)
            {
                RemoveListeners(m_splitButton);
                m_splitButton.onClick.RemoveListener(OnSplit);
            }
            
            if(m_split4Button != null)
            {
                RemoveListeners(m_split4Button);
                m_split4Button.onClick.RemoveListener(OnSplit4);
            }
            
            if(m_growButton != null)
            {
                RemoveListeners(m_growButton);
                m_growButton.onClick.RemoveListener(OnGrow);
            }
            
            if(m_diminishButton != null)
            {
                RemoveListeners(m_diminishButton);
                m_diminishButton.onClick.RemoveListener(OnDiminish);
            }

            if(m_selection != null)
            {
                m_selection.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnEventSystemUpdate()
        {
            if(m_inputManager.GetButtonDown(InputAction.B, LocalPlayerIndex, false))
            {
                UIBehaviour ui = m_bNavSequence[m_bNavIndex];
                if(ui != null)
                {
                    m_eventSystem.SetSelectedGameObject(ui.gameObject);
                }
                else
                {
                    m_eventSystem.SetSelectedGameObject(null);
                    IsActive = false;
                }
                m_bNavIndex++;
                m_bNavIndex %= m_bNavSequence.Length;
            }
            else if(m_inputManager.GetButtonDown(InputAction.X, LocalPlayerIndex, false))
            {
                m_bNavIndex = 0;
                m_eventSystem.SetSelectedGameObject(null);
                IsActive = false;
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

            m_cancelBtn.gameObject.SetActive(selection.Length > 0);
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

        private void OnCancel()
        {
            ActivateButton(m_cancelBtn);
            if(Cancel != null)
            {
                Cancel();
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

        private void AddListener(Selectable selectable, EventTriggerType eventID, UnityAction<Selectable, BaseEventData> listener)
        {
            EventTrigger trigger = selectable.GetComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID =  eventID;
            entry.callback.AddListener(data => listener(selectable, data));
            trigger.triggers.Add(entry);
        }

        private void RemoveListeners(Selectable selectable)
        {
            EventTrigger trigger = selectable.GetComponent<EventTrigger>();
            if(trigger != null)
            {
                trigger.triggers.Clear();
            }
        }


        private string SelectableToText(Selectable selectable)
        {
            if(selectable == m_attackBtn)
            {
                return "Attack";
            }
            else if(selectable == m_autoBtn)
            {
                return "Automatic Action";
            }
            else if(selectable == m_bombBtn)
            {
                return "Convert to Bomb";
            }
            else if(selectable == m_spawnButton)
            {
                return "Convert to Farm";
            }
            else if(selectable == m_wallBtn)
            {
                return "Convert to Wall";
            }
            else if(selectable == m_growButton)
            {
                return "Grow";
            }
            else if(selectable == m_diminishButton)
            {
                return "Compress";
            }
            else if(selectable == m_splitButton)
            {
                return "Subdivide";
            }
            else if(selectable == m_split4Button)
            {
                return "Split";
            }

            return null;
        }
 
        public void OnPointerEnter(Selectable selectable, BaseEventData data)
        {
            //IndependentSelectable.Select(selectable);
           
        }

        public void OnPointerExit(Selectable selectable, BaseEventData data)
        {
           
        }

        public void OnSelect(Selectable selectable, BaseEventData data)
        {
            string txt = SelectableToText(selectable);
            m_tooltipRoot.SetActive(!string.IsNullOrEmpty(txt));
            m_tooltip.text = txt;
        }

        public void OnDeselect(Selectable selectable, BaseEventData data)
        {
            m_tooltipRoot.SetActive(false);
            m_tooltip.text = null;
        }
    }
}
