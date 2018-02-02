using Battlehub.UIControls;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class JoinMenu : BaseMenuBehaviour
    {
        [SerializeField]
        private GameObject m_root;

        [SerializeField]
        private Button m_goBackButton;

        [SerializeField]
        private VirtualizingListBox m_roomsListBox;

        [SerializeField]
        private Button m_joinButton;

        [SerializeField]
        private Notification m_errorNotification;

        [SerializeField]
        private InputProvider m_inputProvider;

        private IGameServer m_gameServer;
        private IGlobalSettings m_gSettings;
        private IProgressIndicator m_progress;
        private INavigation m_navigation;
        private Room[] m_rooms;

        protected override void Awake()
        {
            base.Awake();

            m_navigation = Dependencies.Navigation;
            m_gameServer = Dependencies.GameServer;
            m_gSettings = Dependencies.Settings;
            m_progress = Dependencies.Progress;

            m_joinButton.interactable = m_roomsListBox.SelectedItem != null;
            m_joinButton.onClick.AddListener(OnJoinButtonClick);

            m_goBackButton.interactable = m_navigation.CanGoBack;
            m_goBackButton.onClick.AddListener(OnGoBack);

            m_roomsListBox.CanReorder = false;
            m_roomsListBox.CanDrag = false;
            m_roomsListBox.CanEdit = false;
            m_roomsListBox.CanRemove = false;
            m_roomsListBox.ItemDataBinding += OnItemDataBinding;
            m_roomsListBox.SelectionChanged += OnSelectionChanged;
            m_roomsListBox.Submit += OnMapsListBoxSubmit;
            m_roomsListBox.Cancel += OnMapsListBoxCancel;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
       
            m_root.SetActive(true);
            m_goBackButton.interactable = m_navigation.CanGoBack;

            m_roomsListBox.Items = null;
            m_progress.IsVisible = true;
            m_gameServer.GetPlayers(m_gSettings.ClientId, (error, players) =>
            {
                m_progress.IsVisible = false;
                if (m_gameServer.HasError(error))
                {
                    OutputError(error);
                    return;
                }

                m_progress.IsVisible = true;
                m_gameServer.GetRooms(m_gSettings.ClientId, 0, 100, (error2, rooms) =>
                {

                    m_progress.IsVisible = false;
                    if (m_gameServer.HasError(error2))
                    {
                        OutputError(error2);
                        return;
                    }

                    m_rooms = rooms.Where(r => r.MapInfo.MaxPlayers - r.Players.Count >= players.Length).ToArray();
                    DataBindMaps();

                    IndependentSelectable.Select(m_roomsListBox.gameObject);
                    m_roomsListBox.IsFocused = true;
                });
            });

        }

        private void DataBindMaps()
        {
            m_roomsListBox.Items = m_rooms;
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


            if (m_roomsListBox != null)
            {
                m_roomsListBox.SelectionChanged -= OnSelectionChanged;
                m_roomsListBox.ItemDataBinding -= OnItemDataBinding;
                m_roomsListBox.Submit -= OnMapsListBoxSubmit;
                m_roomsListBox.Cancel -= OnMapsListBoxCancel;
            }

            if (m_joinButton != null)
            {
                m_joinButton.onClick.RemoveListener(OnJoinButtonClick);
            }
        }

        private void Update()
        {
            if (!m_progress.IsVisible)
            {
                if (m_inputProvider.IsAnyKeyDown && !Input.GetMouseButtonDown(0))
                {
                    EventSystem eventSystem = IndependentSelectable.GetEventSystem(m_roomsListBox);
                    if (eventSystem.currentSelectedGameObject == null)
                    {
                        IndependentSelectable.Select(m_roomsListBox.gameObject);
                        m_roomsListBox.IsFocused = true;
                    }
                }
            }
        }


        private void OnSelectionChanged(object sender, SelectionChangedArgs e)
        {
            m_joinButton.interactable = m_roomsListBox.SelectedItem != null;
        }

        private void OnItemDataBinding(object sender, ItemDataBindingArgs e)
        {
            Room room = (Room)e.Item;

            Text text = e.ItemPresenter.GetComponent<Text>();
            text.text = string.Format("{0}, [Max Players: {1}/{2}]", room.MapInfo.Name, room.Players.Count, room.MapInfo.MaxPlayers);
        }


        private void OnMapsListBoxCancel(object sender, System.EventArgs e)
        {
            IndependentSelectable.Select(m_goBackButton.gameObject);
            m_goBackButton.onClick.Invoke();
        }

        private void OnMapsListBoxSubmit(object sender, System.EventArgs e)
        {
            IndependentSelectable.Select(m_joinButton.gameObject);
        }

        private void OnJoinButtonClick()
        {
            m_progress.IsVisible = true;
            Room room = (Room)m_roomsListBox.SelectedItem;
            m_gameServer.JoinRoom(m_gSettings.ClientId, room.Id, (error, result) =>
            {
                m_progress.IsVisible = false;
                if (m_gameServer.HasError(error))
                {
                    OutputError(error);
                    return;
                }

                LoadRoom();
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

        private void LoadRoom()
        {
            m_navigation.Navigate("RoomMenu");
        }
    }
}


