using Battlehub.UIControls;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class RoomMenu : MonoBehaviour
    {
        private static readonly string[] m_playerNames =
        {
            "Red",
            "Blue",
            "Green",
            "Yellow",
            "Orange",
            "Purple",
            "Teal",
            "Black"
        };

        [SerializeField]
        private GameObject m_root;

        [SerializeField]
        private Button m_backButton;

        [SerializeField]
        private Button m_goButton;

        [SerializeField]
        private Button m_addBotButton;

        [SerializeField]
        private Button m_removeBotButton;

        [SerializeField]
        private Transform m_playersPanel;

        [SerializeField]
        private GameObject m_playerPresenterPrefab;

        [SerializeField]
        private Notification m_errorNotification;

        [SerializeField]
        private InputProvider m_inputProvider;

        private IProgressIndicator m_progress;
        private IGameServer m_gameServer;
        private IGlobalSettings m_gSettings;
        private INavigation m_navigation;
        
        private Room m_room;
        private MapInfo m_mapInfo;
        private Player[] m_players;

        private bool m_isReady;

        private bool IsRoomCreator
        {
            get
            {
                if(m_room == null)
                {
                    return false;
                }

                return m_gameServer.IsLocal(m_gSettings.ClientId, m_room.CreatorPlayerId);
            }
        }

        private void Awake()
        {
            m_progress = Dependencies.Progress;
            m_gSettings = Dependencies.Settings;
            m_navigation = Dependencies.Navigation;

            m_gameServer = Dependencies.GameServer;
            m_gameServer.JoinedRoom += OnJoinedRoom;
            m_gameServer.ReadyToLaunch += OnReadyToLaunch;

            m_backButton.onClick.AddListener(OnBackClick);
            m_goButton.onClick.AddListener(OnGoClick);
            m_addBotButton.onClick.AddListener(OnAddBotClick);
            m_removeBotButton.onClick.AddListener(OnRemoveBotClick);

            UpdateButtonsState();
        }

        private void OnDestroy()
        {
            if(m_backButton != null)
            {
                m_backButton.onClick.RemoveListener(OnBackClick);
            }

            if (m_goButton != null)
            {
                m_goButton.onClick.RemoveListener(OnGoClick);
            }

            if (m_addBotButton != null)
            {
                m_addBotButton.onClick.RemoveListener(OnAddBotClick);
            }

            if (m_removeBotButton != null)
            {
                m_removeBotButton.onClick.RemoveListener(OnRemoveBotClick);
            }

            if(m_gameServer != null)
            {
                m_gameServer.JoinedRoom -= OnJoinedRoom;
                m_gameServer.ReadyToLaunch -= OnReadyToLaunch;
                
            }
        }

        private void OnEnable()
        {
            m_isReady = false;
            m_root.SetActive(true);
            GetAndDataBind();
        }

        private void OnDisable()
        {
            if(m_root != null)
            {
                m_root.SetActive(false);
            }

            if(m_playersPanel != null)
            {
                foreach (Transform child in m_playersPanel)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void Update()
        {
            if(!m_progress.IsVisible)
            {
                if(m_inputProvider.IsCancelButtonDown)
                {
                    IndependentSelectable.Select(m_backButton.gameObject);
                    m_backButton.onClick.Invoke();
                }
                else if (m_inputProvider.IsAnyKeyDown && !Input.GetMouseButtonDown(0))
                {
                    EventSystem eventSystem = IndependentSelectable.GetEventSystem(m_addBotButton);
                    if (eventSystem.currentSelectedGameObject == null)
                    {
                        if (m_addBotButton.interactable)
                        {
                            IndependentSelectable.Select(m_addBotButton.gameObject, 1);
                        }
                        else
                        {
                            IndependentSelectable.Select(m_goButton.gameObject, 1);
                        }
                    }
                }
            }
        }


        private void OnReadyToLaunch(Error error, Room room)
        {
            m_room = room;
            GetPlayersAndDataBind();
        }

        private void Launch()
        {
            m_progress.IsVisible = true;
            m_gameServer.Launch(m_gSettings.ClientId, (error, serverUrl) =>
            {
                m_progress.IsVisible = false;
                if (m_gameServer.HasError(error))
                {
                    if(error.Code == StatusCode.NotReady)
                    {
                        GetAndDataBind();
                    }
                    else
                    {
                        OutputError(error);
                    }
                    return;
                }

                m_gSettings.MatchServerUrl = serverUrl;

                m_navigation.ClearHistory();
                m_navigation.Navigate("Game");
            });
        }

        private void OnJoinedRoom(Error error, Guid[] sender, Room room)
        {
            m_room = room;
            GetPlayersAndDataBind();
        }

        private void GetAndDataBind()
        {
            GetRoom(() =>
            {
                GetPlayersAndDataBind();
            });
        }

        private void GetPlayersAndDataBind()
        {
            GetPlayers(() =>
            {
                DataBind();
                UpdateButtonsState();
                if (m_goButton.interactable)
                {
                    IndependentSelectable.Select(m_goButton.gameObject);
                }
                else if (m_addBotButton.interactable)
                {
                    IndependentSelectable.Select(m_addBotButton.gameObject);
                }
                else
                {
                    IndependentSelectable.Select(m_goButton.gameObject);
                }

                if(m_room.IsReadyToLauch)
                {
                    Launch();
                }
            });
        }

        private void GetRoom(Action done)
        {
            m_progress.IsVisible = true;
            m_gameServer.GetRoom(m_gSettings.ClientId, (error, room) =>
            {
                m_progress.IsVisible = false;
                if(m_gameServer.HasError(error))
                {
                    OutputError(error);
                    return;
                }

                m_room = room;
                done();
            });
        }

        private void GetPlayers(Action done)
        {
            m_progress.IsVisible = true;
            m_gameServer.GetPlayers(m_gSettings.ClientId, m_room.Id, (error, players) =>
            {
                m_progress.IsVisible = false;
                if (m_gameServer.HasError(error))
                {
                    OutputError(error);
                    return;
                }

                m_players = players;
                done();
            });
        }

        private void DataBind()
        {
            foreach(Transform child in m_playersPanel)
            {
                Destroy(child.gameObject);
            }

            for(int i = 0; i < m_players.Length; ++i)
            {
                GameObject textGo = Instantiate(m_playerPresenterPrefab, m_playersPanel);

                Text text = textGo.GetComponent<Text>();

                Player player = m_players[i];

                bool isReady = false;
                if(m_room.ReadyToLaunchPlayers != null)
                {
                    isReady = m_room.ReadyToLaunchPlayers.Contains(player.Id);
                }

                text.text = player.Name + " " + ((isReady) ? "Ready" : "Not Ready");
            }
        }

        private void UpdateButtonsState()
        {
            m_backButton.interactable = m_navigation.CanGoBack;

            Text goButtonText = m_goButton.GetComponentInChildren<Text>();
            goButtonText.text = m_isReady ? "Ready" : "GO";
            
            if(m_players == null || m_room == null)
            {
                m_goButton.interactable = false;
                m_addBotButton.interactable = false;
                m_removeBotButton.interactable = false;
            }
            else
            {
                m_goButton.interactable = m_players.Length > 1;
                m_addBotButton.interactable = IsRoomCreator && m_players.Length < m_room.MapInfo.MaxPlayers;
                m_removeBotButton.interactable = IsRoomCreator && m_players.Any(p => p.BotType != BotType.None);
            }

            if(!m_addBotButton.interactable)
            {
                IndependentSelectable.Select(m_goButton.gameObject);
            }
            
            if(!m_removeBotButton.interactable)
            {
                IndependentSelectable.Select(m_addBotButton.gameObject);
            }
        }

        private void OnAddBotClick()
        {
            if(m_progress.IsVisible)
            {
                return;
            }

            Debug.Assert(m_players.Length < m_room.MapInfo.MaxPlayers);

            m_progress.IsVisible = true;
            m_gameServer.CreateBot(m_gSettings.ClientId, m_playerNames[m_players.Length], BotType.Hard, (error, botId, room) =>
            {
                m_progress.IsVisible = false;
                if(m_gameServer.HasError(error))
                {
                    OutputError(error);
                    return;
                }
                m_room = room;
                GetPlayers(() =>
                {
                    DataBind();
                    UpdateButtonsState();
                });
            });
        }

        private void OnRemoveBotClick()
        {
            if (m_progress.IsVisible)
            {
                return;
            }
            Debug.Assert(m_players.Any(p => p.BotType != BotType.None));
            m_progress.IsVisible = true;
            m_gameServer.DestroyBot(m_gSettings.ClientId, m_players.Last(p => p.BotType != BotType.None).Id, (error, botId, room) =>
            {
                m_progress.IsVisible = false;
                if (m_gameServer.HasError(error))
                {
                    OutputError(error);
                    return;
                }
                m_room = room;
                m_players = m_players.Where(p => p.Id != botId).ToArray();
                DataBind();
                UpdateButtonsState();
            });
        }

        private void OnBackClick()
        {
            m_gameServer.CancelRequests();
            m_progress.IsVisible = true;
            m_gameServer.DestroyRoom(m_gSettings.ClientId, m_room.Id, (error, roomId) =>
            {
                m_progress.IsVisible = false;
                if (m_gameServer.HasError(error))
                {
                    OutputError(error, () => m_navigation.GoBack());
                    return;
                }
                m_navigation.GoBack();
            });
        }

        private void OnGoClick()
        {
            if (m_progress.IsVisible)
            {
                return;
            }

            m_isReady = !m_isReady;
            UpdateButtonsState();
            m_progress.IsVisible = true;
            m_gameServer.SetReadyToLaunch(m_gSettings.ClientId, m_isReady, (error, room) =>
            {
                m_progress.IsVisible = false;
                if(!m_gameServer.HasError(error) || error.Code == StatusCode.AlreadyLaunched)
                {
                    m_room = room;
                    GetPlayersAndDataBind();
                }
                else
                {
                    OutputError(error);
                }
            });
        }

        private void OutputError(Error error, Action action = null)
        {
            m_errorNotification.ShowErrorWithAction(error, action);
        }

    }

}
