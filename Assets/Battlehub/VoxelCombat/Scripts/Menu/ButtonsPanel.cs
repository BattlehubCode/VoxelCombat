using Battlehub.UIControls;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using System;

namespace Battlehub.VoxelCombat
{
    public delegate void ButtonsPanelEventHandler(ButtonsPanel sender);
    public delegate void ButtonsPanelEventHandler<T>(ButtonsPanel sender, T code);

    public class ButtonsPanel : MonoBehaviour
    {
        public event ButtonsPanelEventHandler IsOpenedChanged;
        public event ButtonsPanelEventHandler<int> Action;

        [SerializeField]
        private int m_localPlayerIndex = -1;
        public int LocalPlayerIndex
        {
            get { return m_localPlayerIndex; }
            set { m_localPlayerIndex = value; }
        }

        private IndependentEventSystem m_eventSystem;
        private IVoxelInputManager m_inputManager;

        [SerializeField]
        protected Button[] m_sequence;

        public Button[] Sequence
        {
            get { return m_sequence; }
        }


        public virtual bool IsOpened
        {
            get { return gameObject.activeSelf; }
            set
            {
                if(IsOpened != value)
                {
                    gameObject.SetActive(value);

                    if (IsOpenedChanged != null)
                    {
                        IsOpenedChanged(this);
                    }

                    if (IsOpened)
                    {
                        SelectDefault();
                    }
                }
            }
        }


        protected virtual void SelectDefault()
        {
            if(m_sequence.Length > 0)
            {
                m_eventSystem.SetSelectedGameObjectOnLateUpdate(m_sequence[0].gameObject);
                m_sequence[0].OnSelect(null);
            }
          
         
        }

        public void SetText(int action, string text)
        {
            if(m_sequence.Length > 0)
            {
                m_sequence[action].GetComponentInChildren<Text>().text = text;
            }
        }

        protected virtual void Awake()
        {
            if(m_sequence.Length > 0)
            {
                m_eventSystem = IndependentSelectable.GetEventSystem(m_sequence[0]);
            }
            
        }
 
        protected virtual void Start()
        {
            SelectDefault();

            for (int i = 0; i < m_sequence.Length; ++i)
            {
                m_sequence[i].onClick.AddListener(OnButtonClick);
            }
        }

        protected virtual void OnDestroy()
        {
            for (int i = 0; i < m_sequence.Length; ++i)
            {
                m_sequence[i].onClick.RemoveListener(OnButtonClick);
            }
        }

        protected virtual void OnEnable()
        {
            SelectDefault();
        }

        protected virtual void OnDisable()
        {

        }

        //protected virtual void Update()
        //{
            
        //}

        protected virtual void OnAction(int index)
        {
            RaiseAction(index);
        }

        protected void RaiseAction(int index)
        {
            if (Action != null)
            {
                Action(this, index);
            }
        }

        private void OnButtonClick()
        {
            int index = Array.IndexOf(m_sequence.Select(s => s.gameObject).ToArray(), m_eventSystem.currentSelectedGameObject);
            if(index >= 0)
            {
                OnAction(index);
            }
        }
    }

}
