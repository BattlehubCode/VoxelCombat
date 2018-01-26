using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.UIControls
{
    public delegate void VirtualKeyEventHandler(VirtualKey sender);

    public class VirtualKey : Button, IEndSubmitHandler
    {
        public event VirtualKeyEventHandler KeyDown;
        public event VirtualKeyEventHandler KeyUp;

        [SerializeField]
        private KeyCode m_keyCode;
        public KeyCode KeyCode
        {
            get { return m_keyCode; }
        }

        [SerializeField]
        private bool m_isFunctional;
        public bool IsFunctional
        {
            get { return m_isFunctional; }
        }

        [SerializeField]
        private Text m_text;

        public string Char
        {
            get { return m_text.text; }
            set { m_text.text = value; }
        }

        protected override void Awake()
        {
            base.Awake();
            m_text = GetComponentInChildren<Text>();

            if(gameObject.GetComponent<IndependentSelectable>() == null)
            {
                gameObject.AddComponent<IndependentSelectable>();
            }
        }

        public new bool IsPressed
        {
            get { return IsPressed(); }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            if (KeyDown != null)
            {
                KeyDown(this);
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            if (KeyUp != null)
            {
                KeyUp(this);
            }
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(SelectionState.Pressed, false);

            if (KeyDown != null)
            {
                KeyDown(this);
            }
        }

        public void OnEndSubmit(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(currentSelectionState, false);

            if (KeyUp != null)
            {
                KeyUp(this);
            }
        }
    }

}
