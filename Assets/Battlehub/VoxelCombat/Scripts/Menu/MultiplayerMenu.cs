using Battlehub.UIControls;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class MultiplayerMenu : MonoBehaviour
    {
        [SerializeField]
        private Button m_joinButton;

        [SerializeField]
        private Button m_createButton;

        [SerializeField]
        private Button m_goBackButton;

        [SerializeField]
        private GameObject m_root;

        [SerializeField]
        private InputProvider m_inputProvider;

        private INavigation m_navigation;

        private void Awake()
        {
            m_navigation = Dependencies.Navigation;

            m_joinButton.onClick.AddListener(OnJoinClick);
            m_createButton.onClick.AddListener(OnCreateClick);
            m_goBackButton.onClick.AddListener(OnGoBack);
        }

        private void OnDestroy()
        {
            if (m_joinButton != null)
            {
                m_joinButton.onClick.RemoveListener(OnJoinClick);
            }

            if (m_createButton != null)
            {
                m_createButton.onClick.RemoveListener(OnCreateClick);
            }

            if (m_goBackButton != null)
            {
                m_goBackButton.onClick.RemoveListener(OnGoBack);
            }
        }

        private void OnEnable()
        {
            m_root.SetActive(true);
            IndependentSelectable.Select(m_joinButton.gameObject);
        }

        private void OnDisable()
        {
            if (m_root != null)
            {
                m_root.SetActive(false);
            }
        }

        private void Update()
        {
            if (m_inputProvider.IsCancelButtonDown)
            {
                IndependentSelectable.Select(m_goBackButton.gameObject);
                m_goBackButton.onClick.Invoke();
            }
            else if (m_inputProvider.IsAnyKeyDown && !Input.GetMouseButtonDown(0))
            {
                EventSystem eventSystem = IndependentSelectable.GetEventSystem(m_joinButton);
                if (eventSystem.currentSelectedGameObject == null)
                {
                    IndependentSelectable.Select(m_joinButton.gameObject, 1);
                }
            }
        }

        private void OnJoinClick()
        {
            m_navigation.Navigate("JoinMenu");
        }

        private void OnCreateClick()
        {
            m_navigation.Navigate("CreateRoomMenu");
        }

        private void OnGoBack()
        {
            m_navigation.GoBack();
        }
    }
}


