using UnityEngine;
using UnityEngine.UI;
using Battlehub.UIControls;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Linq;

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
        private GameObject m_commandsRoot;

        [SerializeField]
        private HUDCmdAxis m_axis;

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


        [SerializeField]
        private Transform m_defLeft;
        [SerializeField]
        private Transform[] m_leftCol;

        [SerializeField]
        private Transform m_defRight;
        [SerializeField]
        private Transform[] m_rightCol;

        [SerializeField]
        private Transform m_defTop;
        [SerializeField]
        private Transform[] m_topRow;

        [SerializeField]
        private Transform m_defBottom;
        [SerializeField]
        private Transform[] m_bottomRow;

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
                    m_minimap.gameObject.SetActive(!m_isActive);
                    m_commandsRoot.gameObject.SetActive(m_isActive);

                    //m_eventSystem.EventSystemUpdate -= OnEventSystemUpdate;
                    m_selection.SelectionChanged -= OnSelectionChanged;
                    
                    if (m_isActive)
                    {
                        //m_eventSystem.EventSystemUpdate += OnEventSystemUpdate;
                        m_selection.SelectionChanged += OnSelectionChanged;
                    }
                    UpdateState();
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
            m_eventSystem.EventSystemUpdate += OnEventSystemUpdate;
        }

        private void OnDestroy()
        {
            if (m_eventSystem != null)
            {
                m_eventSystem.EventSystemUpdate -= OnEventSystemUpdate;
            }

            if (m_cancelBtn != null)
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
            if (!IsActive)
            {
                bool dPadLeft = m_inputManager.GetButtonDown(InputAction.DPadLeft, LocalPlayerIndex, false);
                bool dPadRight = m_inputManager.GetButtonDown(InputAction.DPadRight, LocalPlayerIndex, false);
                bool dPadUp = m_inputManager.GetButtonDown(InputAction.DPadUp, LocalPlayerIndex, false);
                bool dPadDown = m_inputManager.GetButtonDown(InputAction.DPadDown, LocalPlayerIndex, false);

                if (dPadLeft)
                {
                    
                    IsActive = true;
                    m_eventSystem.SetSelectedGameObject(m_defLeft.gameObject);
                }
                else if (dPadRight)
                {
                    
                    IsActive = true;
                    m_eventSystem.SetSelectedGameObject(m_defRight.gameObject);
                }
                else if (dPadUp)
                {
                    
                    IsActive = true;
                    m_eventSystem.SetSelectedGameObject(m_defTop.gameObject);
                }
                else if (dPadDown)
                {
                    
                    IsActive = true;
                    m_eventSystem.SetSelectedGameObject(m_defBottom.gameObject);
                }
            }
            // else if (m_inputManager.GetButton(InputAction.))
            else if (m_inputManager.GetButtonDown(InputAction.B, LocalPlayerIndex, false))
            {
                m_eventSystem.SetSelectedGameObject(null);
                IsActive = false;
            }
            else if(m_inputManager.GetButtonDown(InputAction.Start, LocalPlayerIndex, false))
            {
                m_eventSystem.SetSelectedGameObject(m_minimap.gameObject);
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
                    bool? canBomb = dc.CanConvert((int)KnownVoxelTypes.Bomb);
                    if (canBomb.HasValue)
                    {
                        m_bombBtn.gameObject.SetActive(true);
                        //m_bombBtn.interactable = canBomb.Value;
                    }

                    bool? canGround = dc.CanConvert((int)KnownVoxelTypes.Ground);
                    if (canGround.HasValue)
                    {
                        m_wallBtn.gameObject.SetActive(true);
                        //m_wallBtn.interactable = canGround.Value;
                    }

                    bool? canSpawner = dc.CanConvert((int)KnownVoxelTypes.Spawner);
                    if (canSpawner.HasValue)
                    {
                        m_spawnButton.gameObject.SetActive(true);
                        //m_spawnButton.interactable = canSpawner.Value;
                    }

                    bool? canGrow = dc.CanGrow();
                    if (canGrow.HasValue)
                    {
                        m_growButton.gameObject.SetActive(true);
                        //m_growButton.interactable = canGrow.Value;
                    }

                    bool? canDiminish = dc.CanDiminish();
                    if (canDiminish.HasValue)
                    {
                        m_diminishButton.gameObject.SetActive(true);
                        //m_diminishButton.interactable = canDiminish.Value;
                    }

                    bool? canSplit = dc.CanSplit();
                    if (canSplit.HasValue)
                    {
                        m_splitButton.gameObject.SetActive(true);
                        //m_splitButton.interactable = canSplit.Value;
                    }

                    bool? canSplit4 = dc.CanSplit4();
                    if (canSplit4.HasValue)
                    {
                        m_split4Button.gameObject.SetActive(true);
                        //m_split4Button.interactable = canSplit4.Value;
                    }
                }
            }

            SetUpNavigation();
        }

        private void SetUpNavigation()
        {
            Selectable[] leftSelectables = m_leftCol.Where(t => t.childCount > 0).Select(t => t.GetChild(0).GetComponent<Selectable>()).Where(s => s.interactable).ToArray();
            Selectable[] rightSelectables = m_rightCol.Where(t => t.childCount > 0).Select(t => t.GetChild(0).GetComponent<Selectable>()).Where(s => s.interactable).ToArray();
            Selectable[] topSelectables = m_topRow.Where(t => t.childCount > 0).Select(t => t.GetChild(0).GetComponent<Selectable>()).Where(s => s.interactable).ToArray();
            Selectable[] botSelectables = m_bottomRow.Where(t => t.childCount > 0).Select(t => t.GetChild(0).GetComponent<Selectable>()).Where(s => s.interactable).ToArray();

            VerticalNav(leftSelectables, false);
            VerticalNav(rightSelectables, true);
            HorizontalNav(topSelectables, true);
            HorizontalNav(botSelectables, false);

            UnityEngine.UI.Navigation nav = m_axis.navigation;
            nav.mode = UnityEngine.UI.Navigation.Mode.Explicit;

            Selectable defaultLeft = m_defLeft.GetComponentInChildren<Selectable>();
            nav.selectOnLeft = defaultLeft.interactable ? defaultLeft : leftSelectables.FirstOrDefault();

            Selectable defaultRight = m_defRight.GetComponentInChildren<Selectable>();
            nav.selectOnRight = defaultRight.interactable ? defaultRight : rightSelectables.FirstOrDefault();

            Selectable defaultTop = m_defTop.GetComponentInChildren<Selectable>();
            nav.selectOnUp = defaultTop.interactable ? defaultTop : topSelectables.FirstOrDefault();

            Selectable defaultBot = m_defBottom.GetComponentInChildren<Selectable>();
            nav.selectOnDown = defaultBot.interactable ? defaultBot : botSelectables.FirstOrDefault();

            m_axis.navigation = nav;
        }

        private void HorizontalNav(Selectable[] selectables, bool top)
        {
            for (int i = 0; i < selectables.Length; ++i)
            {
                UnityEngine.UI.Navigation nav = selectables[i].navigation;
                nav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                
                nav.selectOnRight = i + 1 < selectables.Length ? selectables[i + 1] : null;
                nav.selectOnLeft = i - 1 >= 0 ? selectables[i - 1] : null;

                if(top)
                {
                    nav.selectOnDown = m_axis;
                }
                else
                {
                    nav.selectOnUp = m_axis;
                }

                selectables[i].navigation = nav;
            }
        }

        private void VerticalNav(Selectable[] selectables, bool right)
        {
            for (int i = 1; i < selectables.Length - 1; ++i)
            {
                UnityEngine.UI.Navigation nav = selectables[i].navigation;
                nav.mode = UnityEngine.UI.Navigation.Mode.Explicit;

                nav.selectOnDown = i + 1 < selectables.Length ? selectables[i + 1] : null;
                nav.selectOnUp = i - 1 >= 0 ? selectables[i - 1] : null;

                if(right)
                {
                    nav.selectOnLeft = m_axis;
                }
                else
                {
                    nav.selectOnRight = m_axis;
                }

                selectables[i].navigation = nav;
            }
        }

        private void OnCancel()
        {
            if(Cancel != null)
            {
                Cancel();
            }
        }

        private void OnAttack()
        {
            if(Attack != null)
            {
                Attack();
            }
        }
        private void OnMove()
        {
            if(Move != null)
            {
                Move();
            }
        }

        private void OnAuto()
        {
            if (Auto != null)
            {
                Auto();
            }
        }

        private void OnWall()
        {
            if (Wall != null)
            {
                Wall();
            }
        }

        private void OnBomb()
        {
            if (Bomb != null)
            {
                Bomb();
            }
        }

        private void OnSpawn()
        {
            if (Spawner != null)
            {
                Spawner();
            }

        }

        private void OnSplit()
        {
            if (Split != null)
            {
                Split();
            }
        }

        private void OnSplit4()
        {
            if (Split4 != null)
            {
                Split4();
            }
        }

        private void OnGrow()
        {
            if (Grow != null)
            {
                Grow();
            }
        }

        private void OnDiminish()
        {
            if (Diminish != null)
            {
                Diminish();
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
            if (selectable == m_attackBtn)
            {
                return "Attack";
            }
            else if (selectable == m_autoBtn)
            {
                return "Automatic Action";
            }
            else if (selectable == m_bombBtn)
            {
                return "Convert to Bomb";
            }
            else if (selectable == m_spawnButton)
            {
                return "Convert to Farm";
            }
            else if (selectable == m_wallBtn)
            {
                return "Convert to Wall";
            }
            else if (selectable == m_growButton)
            {
                return "Grow";
            }
            else if (selectable == m_diminishButton)
            {
                return "Compress";
            }
            else if (selectable == m_splitButton)
            {
                return "Subdivide";
            }
            else if (selectable == m_split4Button)
            {
                return "Split";
            }
            else if (selectable == m_cancelBtn)
            {
                return "Stop";
            }
            else if (selectable == m_moveBtn)
            {
                return "Move";
            }

            return null;
        }

        public void UpdateAxis(Selectable selectable)
        {
            Selectable[] leftSelectables = m_leftCol.Where(t => t.childCount > 0).Select(t => t.GetChild(0).GetComponent<Selectable>()).ToArray();
            if(leftSelectables.Contains(selectable))
            {
                m_axis.Side = 1;
                return;
            }
            Selectable[] rightSelectables = m_rightCol.Where(t => t.childCount > 0).Select(t => t.GetChild(0).GetComponent<Selectable>()).ToArray();
            if(rightSelectables.Contains(selectable))
            {
                m_axis.Side = 2;
                return;
            }
            Selectable[] topSelectables = m_topRow.Where(t => t.childCount > 0).Select(t => t.GetChild(0).GetComponent<Selectable>()).ToArray();
            if(topSelectables.Contains(selectable))
            {
                m_axis.Side = 3;
                return;
            }
            Selectable[] botSelectables = m_bottomRow.Where(t => t.childCount > 0).Select(t => t.GetChild(0).GetComponent<Selectable>()).ToArray();
            if(botSelectables.Contains(selectable))
            {
                m_axis.Side = 4;
                return;
            }
        }

        public void OnSelect(Selectable selectable, BaseEventData data)
        {
            string txt = SelectableToText(selectable);
            m_tooltipRoot.SetActive(!string.IsNullOrEmpty(txt));
            m_tooltip.text = txt;
            UpdateAxis(selectable);
        }

        public void OnDeselect(Selectable selectable, BaseEventData data)
        {
            m_tooltipRoot.SetActive(false);
            m_tooltip.text = null;
        }


    }
}
