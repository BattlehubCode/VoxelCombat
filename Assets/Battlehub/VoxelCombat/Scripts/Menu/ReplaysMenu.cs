using Battlehub.UIControls;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class ReplaysMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_root;

        [SerializeField]
        private Button m_goBackButton;

        [SerializeField]
        private VirtualizingListBox m_replaysListBox;
        [SerializeField]
        private Button m_createButton;

        [SerializeField]
        private Notification m_errorNotification;

        [SerializeField]
        private InputProvider m_inputProvider;

        private IGameServer m_gameServer;
        private IGlobalSettings m_gSettings;
        private IProgressIndicator m_progress;
        private INavigation m_navigation;

        private ReplayInfo[] m_replays;


        private void Awake()
        {
            m_navigation = Dependencies.Navigation;
            m_gameServer = Dependencies.GameServer;
            m_gSettings = Dependencies.Settings;
            m_progress = Dependencies.Progress;

            m_createButton.interactable = m_replaysListBox.SelectedItem != null;
            m_createButton.onClick.AddListener(OnCreateButtonClick);

            m_goBackButton.interactable = m_navigation.CanGoBack;
            m_goBackButton.onClick.AddListener(OnGoBack);

            m_replaysListBox.CanReorder = false;
            m_replaysListBox.CanDrag = false;
            m_replaysListBox.CanEdit = false;
            m_replaysListBox.CanRemove = false;
            m_replaysListBox.ItemDataBinding += OnItemDataBinding;
            m_replaysListBox.SelectionChanged += OnSelectionChanged;
            m_replaysListBox.Submit += OnReplaysListBoxSubmit;
            m_replaysListBox.Cancel += OnReplaysListBoxCancel;
        }

        private void OnEnable()
        {
            m_root.SetActive(true);
            m_goBackButton.interactable = m_navigation.CanGoBack;

            m_replaysListBox.Items = null;
            m_progress.IsVisible = true;
            m_gameServer.GetReplays(m_gSettings.ClientId, (error, replays) =>
            {
                m_progress.IsVisible = false;
                if (m_gameServer.HasError(error))
                {
                    OutputError(error);
                    return;
                }

                m_replays = replays.OrderByDescending(r => r.DateTime).ToArray();
                DataBindMaps();

                IndependentSelectable.Select(m_replaysListBox.gameObject);
                m_replaysListBox.IsFocused = true;
            });
        }

        private void DataBindMaps()
        {
            m_replaysListBox.Items = m_replays;
        }

        private void OnDisable()
        {
            if (m_root != null)
            {
                m_root.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (m_root != null)
            {
                m_root.SetActive(false);
            }

            if (m_goBackButton != null)
            {
                m_goBackButton.onClick.RemoveListener(OnGoBack);
            }

            if (m_replaysListBox != null)
            {
                m_replaysListBox.SelectionChanged -= OnSelectionChanged;
                m_replaysListBox.ItemDataBinding -= OnItemDataBinding;
                m_replaysListBox.Submit -= OnReplaysListBoxSubmit;
                m_replaysListBox.Cancel -= OnReplaysListBoxCancel;
            }

            if (m_createButton != null)
            {
                m_createButton.onClick.RemoveListener(OnCreateButtonClick);
            }
        }


        private void Update()
        {
            if (!m_progress.IsVisible)
            {
                if (m_inputProvider.IsAnyKeyDown && !Input.GetMouseButtonDown(0))
                {
                    EventSystem eventSystem = IndependentSelectable.GetEventSystem(m_replaysListBox);
                    if (eventSystem.currentSelectedGameObject == null)
                    {
                        IndependentSelectable.Select(m_replaysListBox.gameObject);
                        m_replaysListBox.IsFocused = true;
                    }
                }
            }
        }

        private void OnReplaysListBoxCancel(object sender, System.EventArgs e)
        {
            IndependentSelectable.Select(m_goBackButton.gameObject);
            m_goBackButton.onClick.Invoke();
        }

        private void OnReplaysListBoxSubmit(object sender, System.EventArgs e)
        {
            IndependentSelectable.Select(m_createButton.gameObject);
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs e)
        {
            m_createButton.interactable = m_replaysListBox.SelectedItem != null;
        }

        private void OnItemDataBinding(object sender, ItemDataBindingArgs e)
        {
            ReplayInfo replayInfo = (ReplayInfo)e.Item;

            Text text = e.ItemPresenter.GetComponent<Text>();            
            text.text = string.Format("{0} [{1}]", replayInfo.Name, new System.DateTime(replayInfo.DateTime).ToLocalTime().ToString());
        }

        private void OnMapModeChanged(int index)
        {
            DataBindMaps();
        }

        private void OnCreateButtonClick()
        {
            m_progress.IsVisible = true;
            ReplayInfo replayInfo = (ReplayInfo)m_replaysListBox.SelectedItem;

            m_gameServer.CreateRoom(m_gSettings.ClientId, replayInfo.MapId, GameMode.Replay, (error, room) =>
            {
                if (m_gameServer.HasError(error))
                {
                    m_progress.IsVisible = false;
                    OutputError(error);
                    return;
                }

                BotType[] botTypes = new BotType[replayInfo.PlayerNames.Length];
                for(int i = 0; i < botTypes.Length; ++i)
                {
                    botTypes[i] = BotType.Replay;
                }

                m_gameServer.CreateBots(m_gSettings.ClientId, replayInfo.PlayerNames, botTypes, (error2, guids2, room2) =>
                {
                    m_progress.IsVisible = false;
                    if (m_gameServer.HasError(error2))
                    {
                        OutputError(error2);
                        return;
                    }

                    m_gameServer.Launch(m_gSettings.ClientId, (error3, serverUrl) =>
                    {
                        if (m_gameServer.HasError(error3))
                        {
                            OutputError(error3);
                            return;
                        }

                        m_gameServer.SetReplay(m_gSettings.ClientId, replayInfo.Id, error4 =>
                        {
                            if (m_gameServer.HasError(error4))
                            {
                                OutputError(error4);
                                return;
                            }

                            m_navigation.ClearHistory();
                            m_navigation.Navigate("Game");
                        });
                    });
                });
            });
        }

        private void OnGoBack()
        {
            Debug.Assert(m_navigation.CanGoBack);
            m_navigation.GoBack();
        }

        private void OutputError(Error error)
        {
            Debug.LogWarning(StatusCode.ToString(error.Code) + " " + error.Message);
            m_errorNotification.Show(StatusCode.ToString(error.Code) + " " + error.Message);
        }
    }
}
