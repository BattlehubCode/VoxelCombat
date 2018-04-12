using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    
    public class HUDControlBehavior : EventTrigger
    {
        private Selectable m_selectable;
        private Color m_highlightColor;

        [SerializeField]
        private Graphic[] m_graphicsEx;

        private void Awake()
        {
            m_selectable = GetComponent<Selectable>();
            ColorBlock colors = m_selectable.colors;

            m_highlightColor = colors.highlightedColor;
            colors.highlightedColor = colors.normalColor;
            m_selectable.colors = colors;
        }

        public override void OnSelect(BaseEventData eventData)
        {
            ColorBlock colors = m_selectable.colors;
            colors.highlightedColor = m_highlightColor;
            m_selectable.colors = colors;

            for(int i = 0; i < m_graphicsEx.Length; ++i)
            {
                m_graphicsEx[i].color = m_highlightColor;
            }

            base.OnSelect(eventData);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            ColorBlock colors = m_selectable.colors;
            colors.highlightedColor = colors.normalColor;
            m_selectable.colors = colors;

            for (int i = 0; i < m_graphicsEx.Length; ++i)
            {
                Color color = m_highlightColor;
                color.a = 0;
                m_graphicsEx[i].color = color;
            }

            base.OnDeselect(eventData);
        }
    }
}
