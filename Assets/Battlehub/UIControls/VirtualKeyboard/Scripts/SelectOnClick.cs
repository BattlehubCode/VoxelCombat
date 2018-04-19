using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.UIControls
{
    public class SelectOnClick : MonoBehaviour
    {
        [SerializeField]
        private IndependentEventSystem m_eventSystem;

        [SerializeField]
        private Button m_button;

        [SerializeField]
        private Selectable m_selectable;

        private void Start()
        {
            if(m_button == null)
            {
                m_button = GetComponent<Button>();
            }

            if(m_eventSystem == null)
            {
                m_eventSystem = GetComponentInParent<IndependentEventSystem>();
            }

            m_button.onClick.AddListener(OnButtonClick);
        }

        private void OnDestroy()
        {
            if(m_button != null)
            {
                m_button.onClick.RemoveListener(OnButtonClick);
            }
        }

        private void OnButtonClick()
        {
            m_eventSystem.SetSelectedGameObjectOnLateUpdate(m_selectable.gameObject);
            //m_selectable.Select();
            //m_selectable.OnSelect(null);
        }
    }

}
