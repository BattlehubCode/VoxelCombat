using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.UIControls
{
    public class VirtualToggleKey : Button
    {
        [SerializeField]
        private GameObject[] m_group1;

        [SerializeField]
        private GameObject[] m_group2;

        [SerializeField]
        private string m_group1Text;

        [SerializeField]
        private string m_group2Text;

        private Text m_text;

        private bool m_value;

        protected override void Awake()
        {
            base.Awake();

            m_text = GetComponentInChildren<Text>();

            ActivateDeactivateGroups();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            m_value = !m_value;
            ActivateDeactivateGroups();

        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);

            m_value = !m_value;
            ActivateDeactivateGroups();
        }

        private void ActivateDeactivateGroups()
        {
            if(m_group1 == null)
            {
                return;
            }

            if(m_value)
            {
                m_text.text = m_group2Text;
            }
            else
            {
                m_text.text = m_group1Text;
            }

            for (int i = 0; i < m_group1.Length; ++i)
            {
                m_group1[i].SetActive(!m_value);
            }

            for (int i = 0; i < m_group2.Length; ++i)
            {
                m_group2[i].SetActive(m_value);
            }
        }
    }

}
