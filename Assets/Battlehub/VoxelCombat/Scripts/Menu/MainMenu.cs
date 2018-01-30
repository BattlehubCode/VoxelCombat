using Battlehub.UIControls;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField]
        private Button m_compaignButton;

        [SerializeField]
        private Button m_multiplayerButton;

        [SerializeField]
        private Button m_replaysButton;

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
        }

        private void Start()
        {
            m_compaignButton.onClick.AddListener(OnCompaignClick);
            m_multiplayerButton.onClick.AddListener(OnMultiplayerClick);
            m_replaysButton.onClick.AddListener(OnReplaysClick);
            m_goBackButton.onClick.AddListener(OnGoBack);
        }

        private void OnDestroy()
        {
            if(m_compaignButton != null)
            {
                m_compaignButton.onClick.RemoveListener(OnCompaignClick);
            }
            
            if(m_multiplayerButton != null)
            {
                m_multiplayerButton.onClick.RemoveListener(OnMultiplayerClick);
            }
            
            if(m_replaysButton != null)
            {
                m_replaysButton.onClick.RemoveListener(OnReplaysClick);
            }

            if(m_goBackButton != null)
            {
                m_goBackButton.onClick.RemoveListener(OnGoBack);
            }
        }

        private void OnEnable()
        {
            m_root.SetActive(true);
            IndependentSelectable.Select(m_multiplayerButton.gameObject);
        }

        private void OnDisable()
        {
            EventSystem eventSystem = IndependentSelectable.GetEventSystem(m_multiplayerButton.gameObject);
            if(eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
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
                EventSystem eventSystem = IndependentSelectable.GetEventSystem(m_multiplayerButton);
                if (eventSystem.currentSelectedGameObject == null)
                {
                    IndependentSelectable.Select(m_multiplayerButton.gameObject, 1);
                }
            }
        }


        private void OnCompaignClick()
        {
            
        }

        private void OnMultiplayerClick()
        {
            m_navigation.Navigate("MultiplayerMenu");
        }

        private void OnReplaysClick()
        {
            m_navigation.Navigate("ReplaysMenu");
        }

        private void OnGoBack()
        {
            m_navigation.GoBack();
        }
    }
}
