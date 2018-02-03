using Battlehub.UIControls;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class ReplaysMenu : BaseMenuBehaviour
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
 
        private IGlobalSettings m_gSettings;
        private IProgressIndicator m_progress;
        private INavigation m_navigation;

        private ReplayInfo[] m_replays;
        private Room m_room;

        protected override void Awake()
        {
            base.Awake();

            m_navigation = Dependencies.Navigation;

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

        protected override void OnEnable()
        {
            base.OnEnable();
            m_root.SetActive(true);
            m_goBackButton.interactable = m_navigation.CanGoBack;

            m_replaysListBox.Items = null;
            m_progress.IsVisible = true;
            GameServer.GetReplays(m_gSettings.ClientId, (error, replays) =>
            {
                m_progress.IsVisible = false;
                if (GameServer.HasError(error))
                {
                    OutputError(error);
                    return;
                }

                m_replays = replays.OrderByDescending(r => r.DateTime).ToArray();
                DataBindReplays();

                IndependentSelectable.Select(m_replaysListBox.gameObject);
                m_replaysListBox.IsFocused = true;
            });
        }

        private void DataBindReplays()
        {
            m_replaysListBox.Items = m_replays;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_root != null)
            {
                m_root.SetActive(false);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
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

        private void OnCreateButtonClick()
        {
            m_progress.IsVisible = true;
            DetroyRoomIfCreated(() =>
            {
                ReplayInfo replayInfo = (ReplayInfo)m_replaysListBox.SelectedItem;

                GameServer.CreateRoom(m_gSettings.ClientId, replayInfo.MapId, GameMode.Replay, (error, room) =>
                {
                    if (GameServer.HasError(error))
                    {
                        m_progress.IsVisible = false;
                        OutputError(error);
                        return;
                    }

                    m_room = room;

                    BotType[] botTypes = new BotType[replayInfo.PlayerNames.Length];
                    for (int i = 0; i < botTypes.Length; ++i)
                    {
                        botTypes[i] = BotType.Replay;
                    }

                    GameServer.CreateBots(m_gSettings.ClientId, replayInfo.PlayerNames, botTypes, (error2, guids2, room2) =>
                    {
                        if (GameServer.HasError(error2))
                        {
                            m_progress.IsVisible = false;
                            OutputError(error2);
                            return;
                        }

                        m_room = room2;
                        Launch();
                    });
                });
            },
            error =>
            {
                m_progress.IsVisible = false;
                OutputError(error);
            });

        }

        private void Launch()
        {
            ReplayInfo replayInfo = (ReplayInfo)m_replaysListBox.SelectedItem;
            GameServer.SetReplay(m_gSettings.ClientId, replayInfo.Id, error4 =>
            {
                m_progress.IsVisible = false;
                if (GameServer.HasError(error4))
                {
                    OutputError(error4);
                    return;
                }

                GameServer.Launch(m_gSettings.ClientId, (error, serverUrl) =>
                {
                    if (GameServer.HasError(error))
                    {
                        m_progress.IsVisible = false;
                        OutputError(error);
                        return;
                    }

                    m_gSettings.MatchServerUrl = serverUrl;

                    m_navigation.ClearHistory();
                    m_navigation.Navigate("Game");
                });
            });
        }

        private void OnGoBack()
        {
            DetroyRoomIfCreated(() => m_navigation.GoBack(), error => OutputError(error, () => m_navigation.GoBack()));
        }

        private void OutputError(Error error, Action action = null)
        {
            m_errorNotification.ShowErrorWithAction(error, action);
        }

        private void DetroyRoomIfCreated(Action done, Action<Error> onError)
        {
            GameServer.GetRoom(m_gSettings.ClientId, (error, room) =>
            {
                if(GameServer.HasError(error) && error.Code != StatusCode.NotFound)
                {
                    onError(error);
                    return;
                }

                m_room = room;

                if (m_room != null)
                {
                    GameServer.DestroyRoom(m_gSettings.ClientId, m_room.Id, (error2, guid) =>
                    {
                        if (GameServer.HasError(error2))
                        {
                            onError(error2);
                            return;
                        }

                        done();
                    });
                }
                else
                {
                    done();
                }
            });     
        }
    }
}
