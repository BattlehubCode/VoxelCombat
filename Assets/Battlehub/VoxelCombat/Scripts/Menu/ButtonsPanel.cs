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

        public int LocalPlayerIndex
        {
            get;
            set;
        }

        private EventSystem m_eventSystem;

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
                    SelectDefault();

                    if (IsOpenedChanged != null)
                    {
                        IsOpenedChanged(this);
                    }
                }
            }
        }

        protected virtual void SelectDefault()
        {
            IndependentSelectable.Select(m_sequence[0]);
            m_sequence[0].OnSelect(null);
        }

        public void SetText(int action, string text)
        {
            m_sequence[action].GetComponentInChildren<Text>().text = text;
        }
 
        protected virtual void Start()
        {
            m_eventSystem = IndependentSelectable.GetEventSystem(m_sequence[0]);
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
