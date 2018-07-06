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
        private GameObject m_commandsRoot;

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

        private IVirtualMouse m_virtualMouse;
        private IVoxelInputManager m_inputManager;
        private IVoxelGame m_gameState;
        private IUnitSelection m_selection;
        private IEventSystemManager m_eventSystemMananger;
        private IndependentEventSystem m_eventSystem;
        private GameObject m_lastSelected;

        private bool m_isOpen;
        public bool IsOpen
        {
            get { return m_isOpen; }
            set
            {
                int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);
                long[] selection = m_selection.GetSelection(playerIndex, playerIndex);
                bool newValue = value && selection.Length > 0;
                if (m_isOpen != newValue)
                {
                    m_isOpen = newValue;
                    m_commandsRoot.gameObject.SetActive(m_isOpen);
                    m_selection.SelectionChanged -= OnSelectionChanged;

                    if (m_isOpen)
                    {
                        m_selection.SelectionChanged += OnSelectionChanged;
                    }
                    UpdateState();

                    if(m_virtualMouse == null)
                    {
                        m_virtualMouse = Dependencies.GameView.GetVirtualMouse(LocalPlayerIndex);
                    }

                    if (m_isOpen)
                    {
                        if(m_lastSelected == null || m_lastSelected.GetComponent<HUDControlBehavior>().IsDisabled)
                        {
                            m_eventSystem.SetSelectedGameObject(m_autoBtn.gameObject);
                        }
                        else
                        {
                            m_eventSystem.SetSelectedGameObject(m_lastSelected);
                        }
                        
                        m_virtualMouse.BackupVirtualMouse();
                        m_virtualMouse.IsVirtualMouseEnabled = m_inputManager.IsKeyboardAndMouse(LocalPlayerIndex);
                        m_virtualMouse.IsVirtualMouseCursorVisible = m_inputManager.IsKeyboardAndMouse(LocalPlayerIndex);
                    }
                    else
                    {
                        m_virtualMouse.RestoreVirtualMouse();
                        m_eventSystem.SetSelectedGameObject(null);
                    }

                    m_gameState.IsContextActionInProgress(LocalPlayerIndex, m_isOpen);
                }
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

            m_gameState.ContextAction += OnContextAction;
        
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

        }

        private void AddListeners(Selectable selectable)
        {
            AddListener(selectable, EventTriggerType.Select, OnSelect);
            AddListener(selectable, EventTriggerType.Deselect, OnDeselect);
            if(m_inputManager.IsKeyboardAndMouse(LocalPlayerIndex))
            {
                AddListener(selectable, EventTriggerType.PointerEnter, OnPointerEnter);
                AddListener(selectable, EventTriggerType.PointerExit, OnPointerExit);
            }
        }

        private void Start()
        {
            m_eventSystem = m_eventSystemMananger.GetEventSystem(LocalPlayerIndex);

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
        private void OnDestroy()
        {
            if(m_gameState != null)
            {
                m_gameState.ContextAction -= OnContextAction;
            }

            if (m_cancelBtn != null)
            {
                RemoveListeners(m_cancelBtn);
                m_cancelBtn.onClick.RemoveListener(OnCancel);
            }

            if (m_attackBtn != null)
            {
                RemoveListeners(m_attackBtn);
                m_attackBtn.onClick.RemoveListener(OnAttack);
            }

            if (m_moveBtn != null)
            {
                RemoveListeners(m_moveBtn);
                m_moveBtn.onClick.RemoveListener(OnMove);
            }

            if (m_autoBtn != null)
            {
                RemoveListeners(m_autoBtn);
                m_autoBtn.onClick.RemoveListener(OnAuto);
            }

            if (m_bombBtn != null)
            {
                RemoveListeners(m_bombBtn);
                m_bombBtn.onClick.RemoveListener(OnBomb);
            }

            if (m_wallBtn != null)
            {
                RemoveListeners(m_wallBtn);
                m_wallBtn.onClick.RemoveListener(OnWall);
            }

            if (m_spawnButton != null)
            {
                RemoveListeners(m_spawnButton);
                m_spawnButton.onClick.RemoveListener(OnSpawn);
            }

            if (m_splitButton != null)
            {
                RemoveListeners(m_splitButton);
                m_splitButton.onClick.RemoveListener(OnSplit);
            }

            if (m_split4Button != null)
            {
                RemoveListeners(m_split4Button);
                m_split4Button.onClick.RemoveListener(OnSplit4);
            }

            if (m_growButton != null)
            {
                RemoveListeners(m_growButton);
                m_growButton.onClick.RemoveListener(OnGrow);
            }

            if (m_diminishButton != null)
            {
                RemoveListeners(m_diminishButton);
                m_diminishButton.onClick.RemoveListener(OnDiminish);
            }

            if (m_selection != null)
            {
                m_selection.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void Update()
        {
            if (m_inputManager.GetButtonDown(InputAction.B, LocalPlayerIndex, false, false))
            {
                if (IsOpen)
                {
                    IsOpen = false;
                }
            }
        }

        private void OnContextAction(int playerIndex)
        {
            if(!m_gameState.IsContextActionInProgress(playerIndex))
            {
                IsOpen = false;
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

            if(selection.Length == 0)
            {
                IsOpen = false;
                return;
            }

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

        //    HUDControlBehavior hcbCancel = m_cancelBtn.GetComponent<HUDControlBehavior>();
         //   HUDControlBehavior hcbAttack = m_attackBtn.GetComponent<HUDControlBehavior>();
         //   HUDControlBehavior hcbMove = m_moveBtn.GetComponent<HUDControlBehavior>();
           // HUDControlBehavior hcbAuto = m_autoBtn.GetComponent<HUDControlBehavior>();
            HUDControlBehavior hcbBomb = m_bombBtn.GetComponent<HUDControlBehavior>();
            HUDControlBehavior hcbWall = m_wallBtn.GetComponent<HUDControlBehavior>();
            HUDControlBehavior hcbSpawn = m_spawnButton.GetComponent<HUDControlBehavior>();
            HUDControlBehavior hcbGrow = m_growButton.GetComponent<HUDControlBehavior>();
            HUDControlBehavior hcbDiminish = m_diminishButton.GetComponent<HUDControlBehavior>();
            HUDControlBehavior hcbSplit = m_splitButton.GetComponent<HUDControlBehavior>();
            HUDControlBehavior hcbSplit4 = m_split4Button.GetComponent<HUDControlBehavior>();

            hcbBomb.IsDisabled = true;
            hcbWall.IsDisabled = true;
            hcbSpawn.IsDisabled = true;
            hcbGrow.IsDisabled = true;
            hcbDiminish.IsDisabled = true;
            hcbSplit.IsDisabled = true;
            hcbSplit4.IsDisabled = true;

            for (int i = 0; i < selection.Length; ++i)
            {
                IVoxelDataController dc = m_gameState.GetVoxelDataController(playerIndex, selection[i]);
                if (dc != null)
                {
                    CmdResultCode canBomb = dc.CanConvertImmediate((int)KnownVoxelTypes.Bomb);
                    if (canBomb != CmdResultCode.Fail_NotSupported)
                    {
                        m_bombBtn.gameObject.SetActive(true);
                        if(canBomb == CmdResultCode.Success)
                        {
                            hcbBomb.IsDisabled = false;
                        }   
                    }

                    CmdResultCode canGround = dc.CanConvertImmediate((int)KnownVoxelTypes.Ground);
                    if (canGround != CmdResultCode.Fail_NotSupported)
                    {
                        m_wallBtn.gameObject.SetActive(true);
                        if (canGround == CmdResultCode.Success)
                        {
                            hcbWall.IsDisabled = false;
                        }
                    }

                    CmdResultCode canSpawner = dc.CanConvertImmediate((int)KnownVoxelTypes.Spawner);
                    if (canSpawner != CmdResultCode.Fail_NotSupported)
                    {
                        m_spawnButton.gameObject.SetActive(true);
                        if(canSpawner == CmdResultCode.Success)
                        {
                            hcbSpawn.IsDisabled = false;
                        }    
                    }

                    CmdResultCode canGrow = dc.CanGrowImmediate();
                    if (canGrow != CmdResultCode.Fail_NotSupported)
                    {
                        m_growButton.gameObject.SetActive(true);
                        if(canGrow == CmdResultCode.Success)
                        {
                            hcbGrow.IsDisabled = false;
                        }
                    }

                    CmdResultCode canDiminish = dc.CanDiminishImmediate();
                    if (canDiminish != CmdResultCode.Fail_NotSupported)
                    {
                        m_diminishButton.gameObject.SetActive(true);
                        if(canDiminish == CmdResultCode.Success)
                        {
                            hcbDiminish.IsDisabled = false;
                        }
                    }

                    CmdResultCode canSplit = dc.CanSplitImmediate();
                    if (canSplit != CmdResultCode.Fail_NotSupported)
                    {
                        m_splitButton.gameObject.SetActive(true);
                        if(canSplit == CmdResultCode.Success)
                        {
                            hcbSplit.IsDisabled = false;
                        }
                    }

                    CmdResultCode canSplit4 = dc.CanSplit4Immediate();
                    if (canSplit4 != CmdResultCode.Fail_NotSupported)
                    {
                        m_split4Button.gameObject.SetActive(true);
                        if(canSplit4 == CmdResultCode.Success)
                        {
                            hcbSplit4.IsDisabled = false;
                        }
                    }
                }
            }


            if(m_eventSystem.currentSelectedGameObject != null && m_eventSystem.currentSelectedGameObject.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                m_eventSystem.SetSelectedGameObjectOnLateUpdate(m_autoBtn.gameObject);
            }
        }

        private void OnCancel()
        {
            if(m_cancelBtn.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            IsOpen = false;
            if (Cancel != null)
            {
                Cancel();
            }
        }

        private void OnAttack()
        {
            if (m_attackBtn.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            IsOpen = false;
            if (Attack != null)
            {
                Attack();
            }
        }
        private void OnMove()
        {
            if (m_moveBtn.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            IsOpen = false;
            if (Move != null)
            {
                Move();
            }
        }

        private void OnAuto()
        {
            if (m_autoBtn.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            IsOpen = false;
            if (Auto != null)
            {
                Auto();
            }
        }

        private void OnWall()
        {
            if (m_wallBtn.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            IsOpen = false;
            if (Wall != null)
            {
                Wall();
            }
        }

        private void OnBomb()
        {
            if (m_bombBtn.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            IsOpen = false;
            if (Bomb != null)
            {
                Bomb();
            }
        }

        private void OnSpawn()
        {
            if (m_spawnButton.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            IsOpen = false;
            if (Spawner != null)
            {
                Spawner();
            }

        }

        private void OnSplit()
        {
            if (m_splitButton.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            IsOpen = false;
            if (Split != null)
            {
                Split();
            }
        }

        private void OnSplit4()
        {
            if (m_split4Button.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            IsOpen = false;
            if (Split4 != null)
            {
                Split4();
            }
        }

        private void OnGrow()
        {
            if (m_growButton.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            IsOpen = false;
            if (Grow != null)
            {
                Grow();
            }
        }

        private void OnDiminish()
        {
            if (m_diminishButton.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            IsOpen = false;
            if (Diminish != null)
            {
                Diminish();
            }
        }

        private void AddListener(Selectable selectable, EventTriggerType eventID, UnityAction<Selectable, BaseEventData> listener)
        {
            EventTrigger trigger = selectable.GetComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = eventID;
            entry.callback.AddListener(data => listener(selectable, data));
            trigger.triggers.Add(entry);
        }

        private void RemoveListeners(Selectable selectable)
        {
            EventTrigger trigger = selectable.GetComponent<EventTrigger>();
            if (trigger != null)
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
                if (selectable.GetComponent<HUDControlBehavior>().IsDisabled)
                {
                    return "Can't grow. Must collect 64 cubes.";
                }

                return "Grow";
            }
            else if (selectable == m_diminishButton)
            {
                if (selectable.GetComponent<HUDControlBehavior>().IsDisabled)
                {
                    return "Cube of minimal size could not be compressed.";
                }

                return "Compress";
            }
            else if (selectable == m_splitButton)
            {
                if (selectable.GetComponent<HUDControlBehavior>().IsDisabled)
                {
                    return "Can't subdivide. Must collect 64 cubes.";
                }
                return "Subdivide";
            }
            else if (selectable == m_split4Button)
            {
                if (selectable.GetComponent<HUDControlBehavior>().IsDisabled)
                {
                    return "Can't split. Must collect 64 cubes.";
                }
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

        public void OnSelect(Selectable selectable, BaseEventData data)
        {
            string txt = SelectableToText(selectable);
            m_tooltipRoot.SetActive(!string.IsNullOrEmpty(txt));
            m_tooltip.text = txt;
            m_lastSelected = selectable.gameObject;
        }

        public void OnDeselect(Selectable selectable, BaseEventData data)
        {
            m_tooltipRoot.SetActive(false);
            m_tooltip.text = null;
        }

        public void OnPointerEnter(Selectable selectable, BaseEventData data)
        {
            IndependentSelectable.Select(selectable);
        }

        public void OnPointerExit(Selectable selectable, BaseEventData data)
        {
            IndependentSelectable.Unselect(selectable);
        }

    }
}
