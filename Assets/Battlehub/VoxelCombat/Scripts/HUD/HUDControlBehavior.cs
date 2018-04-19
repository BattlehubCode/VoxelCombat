using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{

    public delegate void HUDControlBehaviorEventHandler();

    public class HUDControlBehavior : EventTrigger
    {
        public event HUDControlBehaviorEventHandler Selected;
        public event HUDControlBehaviorEventHandler Deselected;

        private Selectable m_selectable;

        private bool m_isDisabled;
        private Color m_normalColorBackup;
        private Color m_highlightColorBackup;
        private Color m_highlightColorBackup2;
        public bool IsDisabled
        {
            get { return m_isDisabled; }
            set
            {
                if(m_isDisabled == value)
                {
                    return;
                }
                m_isDisabled = value;
                var colors = m_selectable.colors;

                if(m_isDisabled)
                {
                    m_highlightColorBackup = m_highlightColor;
                    m_highlightColor = m_disabledColor;

                    m_highlightColorBackup2 = colors.highlightedColor;
                    m_normalColorBackup = colors.normalColor;
                    colors.normalColor = m_disabledColor;
                    colors.highlightedColor = m_disabledColor;
                    m_selectable.colors = colors;

                    if(m_isSelected)
                    {
                        for (int i = 0; i < m_graphicsEx.Length; ++i)
                        {
                            m_graphicsEx[i].color = m_highlightColor;
                        }
                    }
                }
                else
                {
                    m_highlightColor = m_highlightColorBackup;
                    colors.highlightedColor = m_highlightColorBackup2;
                    colors.normalColor = m_normalColorBackup;
                    m_selectable.colors = colors;

                    if(m_isSelected)
                    {
                        for (int i = 0; i < m_graphicsEx.Length; ++i)
                        {
                            m_graphicsEx[i].color = m_highlightColor;
                        }
                    }
                }
            }
        }

        [SerializeField]
        private Color m_disabledColor;

        [SerializeField]
        private Color m_highlightColor;

        [SerializeField]
        private Color m_normalColor;

        [SerializeField]
        private Graphic[] m_graphicsEx;

        [SerializeField]
        private bool m_snapCursor = true;

        private IGameViewport m_viewport;
        private IGameView m_gameView;
        private IVirtualMouse m_virtualMouse;
        private bool m_isSelected;
        private RectTransform m_rt;
        private Vector3[] m_coners = new Vector3[4];
        private bool m_isKeyboardAndMouse;

        private void Awake()
        {
            m_rt = GetComponent<RectTransform>();
            m_gameView = Dependencies.GameView;
            m_selectable = GetComponent<Selectable>();
            
            m_viewport = GetComponentInParent<GameViewport>();
            m_virtualMouse = m_gameView.GetVirtualMouse(m_viewport.LocalPlayerIndex);

            m_isKeyboardAndMouse = Dependencies.InputManager.IsKeyboardAndMouse(m_viewport.LocalPlayerIndex);
        }

        public override void OnSelect(BaseEventData eventData)
        {            
            if(m_selectable.navigation.mode != UnityEngine.UI.Navigation.Mode.None)
            {
                for (int i = 0; i < m_graphicsEx.Length; ++i)
                {
                    m_graphicsEx[i].color = m_highlightColor;
                }

                m_isSelected = true;

                if (!m_isKeyboardAndMouse && m_snapCursor)
                {
                    SnapCursor();
                }

            }
            base.OnSelect(eventData);

            if(Selected != null)
            {
                Selected();
            }
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            m_isSelected = false;

            for (int i = 0; i < m_graphicsEx.Length; ++i)
            {
                m_graphicsEx[i].color = m_normalColor;
            }

            base.OnDeselect(eventData);

            if (Deselected != null)
            {
                Deselected();
            }
        }

        private void OnEnable()
        {
            for (int i = 0; i < m_graphicsEx.Length; ++i)
            {
                if(m_isSelected)
                {
                    m_graphicsEx[i].color = m_highlightColor;
                }
                else
                {
                    m_graphicsEx[i].color = m_normalColor;
                }
            }
        }
       
        private void OnDisable()
        {
            m_isSelected = false;

            for (int i = 0; i < m_graphicsEx.Length; ++i)
            {
                m_graphicsEx[i].color = m_normalColor;
            }

            if (Deselected != null)
            {
                Deselected();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if(m_isSelected)
            {
                if (!m_isKeyboardAndMouse && m_snapCursor)
                {
                    SnapCursor();
                }
            }
        }

        private void SnapCursor()
        {
            m_rt.GetWorldCorners(m_coners);
            Vector3 mid = m_coners[0] + (m_coners[2] - m_coners[0]) / 2;
            m_virtualMouse.VirtualMousePosition = RectTransformUtility.WorldToScreenPoint(null, mid);
        }
    }
}
