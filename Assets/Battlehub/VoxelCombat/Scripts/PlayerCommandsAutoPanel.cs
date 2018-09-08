using Battlehub.UIControls;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class PlayerCommandsAutoPanel : MonoBehaviour
    {
        public event PlayerCommandsPanelEventHandler<int> Action;

        [SerializeField]
        private GameViewport m_viewport;

        [SerializeField]
        private GameObject m_root;

        [SerializeField]
        private Button[] m_buttons;

        [SerializeField]
        private GameObject m_tooltipRoot;

        [SerializeField]
        private Text m_tooltip;

        private string[] m_names;

        private IVoxelInputManager m_inputManager;
        private IVoxelGame m_gameState;
        private IUnitSelection m_selection;
        private IEventSystemManager m_eventSystemMananger;
        private IndependentEventSystem m_eventSystem;
        private GameObject m_lastSelected;
        
        private int LocalPlayerIndex
        {
            get { return m_viewport.LocalPlayerIndex; }
        }

        public bool IsOpen
        {
            get { return m_root.activeSelf; }
            set
            {
                m_root.SetActive(value);

                m_selection.SelectionChanged -= OnSelectionChanged;

                if (IsOpen)
                {
                    m_selection.SelectionChanged += OnSelectionChanged;
                    UpdateState();
                    if (m_lastSelected == null || m_lastSelected.GetComponent<HUDControlBehavior>().IsDisabled)
                    {
                        m_eventSystem.SetSelectedGameObjectOnLateUpdate(m_buttons[m_buttons.Length / 2].gameObject);
                    }
                    else
                    {
                        m_eventSystem.SetSelectedGameObjectOnLateUpdate(m_lastSelected);
                    }

                    m_names = new string[m_buttons.Length];
                    for(int i = 0; i < m_names.Length; ++i)
                    {
                        m_names[i] = "Locked";
                    }

                    int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);
                    SerializedNamedTaskLaunchInfo[] templateInfo = m_gameState.GetTaskTemplateData(playerIndex);
                    for(int i = 0; i < templateInfo.Length; ++i)
                    {
                        int index = templateInfo[i].Index;

                        if(index == i)
                        {
                            m_names[i] = templateInfo[i].Name;
                        }   
                    }
                }
            }
        }

        private void Awake()
        {
            m_gameState = Dependencies.GameState;
            m_inputManager = Dependencies.InputManager;
            m_selection = Dependencies.UnitSelection;
            m_eventSystemMananger = Dependencies.EventSystemManager;

            for(int i = 0; i < m_buttons.Length; ++i)
            {
                int index = i;
                m_buttons[i].onClick.AddListener(() => { OnAction(index); });
            }
        }

        private void Start()
        {
            m_eventSystem = m_eventSystemMananger.GetEventSystem(LocalPlayerIndex);

            for (int i = 0; i < m_buttons.Length; ++i)
            {
                AddListeners(m_buttons[i]);
            }
        }
        private void OnDestroy()
        {
            for (int i = 0; i < m_buttons.Length; ++i)
            {
                RemoveListeners(m_buttons[i]);
                m_buttons[i].onClick.RemoveListener(() => OnAction(i));
            }

            if (m_selection != null)
            {
                m_selection.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnAction(int index)
        {
            if(m_buttons[index].GetComponent<HUDControlBehavior>().IsDisabled)
            {
                return;
            }

            if(Action != null)
            {
                Action(index);
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

            if (selection.Length == 0)
            {
                IsOpen = false;
                return;
            }

            for(int i = 0; i < m_buttons.Length; ++i)
            {
                m_buttons[i].gameObject.SetActive(selection.Length > 0);

                if(i != 12)
                {
                    HUDControlBehavior hcb = m_buttons[i].GetComponent<HUDControlBehavior>();
                    hcb.IsDisabled = true;
                }
            }

            if (m_eventSystem.currentSelectedGameObject != null && m_eventSystem.currentSelectedGameObject.GetComponent<HUDControlBehavior>().IsDisabled)
            {
                m_eventSystem.SetSelectedGameObjectOnLateUpdate(m_buttons[0].gameObject);
            }
        }

        private void AddListeners(Selectable selectable)
        {
            AddListener(selectable, EventTriggerType.Select, OnSelect);
            AddListener(selectable, EventTriggerType.Deselect, OnDeselect);
            if (m_inputManager.IsKeyboardAndMouse(LocalPlayerIndex))
            {
                AddListener(selectable, EventTriggerType.PointerEnter, OnPointerEnter);
                AddListener(selectable, EventTriggerType.PointerExit, OnPointerExit);
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
            if(selectable == m_buttons[12])
            {
                return "Eat Split Grow";
            }

            return "Locked";
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

