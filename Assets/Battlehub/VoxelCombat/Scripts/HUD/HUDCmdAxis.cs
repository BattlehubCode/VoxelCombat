using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class HUDCmdAxis : Selectable
    {
        [SerializeField]
        private Image m_left;

        [SerializeField]
        private Image m_right;

        [SerializeField]
        private Image m_mid;

        [SerializeField]
        private Image m_top;

        [SerializeField]
        private Image m_bot;

        private int m_side;

        public int Side
        {
            get { return m_side; }
            set
            {
                m_side = value;
                UpdateVisualState();
            }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);

            m_side = 0;
            UpdateVisualState();
        }

       

        private void UpdateVisualState()
        {
            m_left.gameObject.SetActive(m_side == 1);
            m_right.gameObject.SetActive(m_side == 2);
            m_top.gameObject.SetActive(m_side == 3);
            m_bot.gameObject.SetActive(m_side == 4);
            m_mid.gameObject.SetActive(m_side == 0);
        }
    }
}
