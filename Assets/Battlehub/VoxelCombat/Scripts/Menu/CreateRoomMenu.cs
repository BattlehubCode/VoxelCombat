using Battlehub.UIControls;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class CreateRoomMenu : BaseMenuBehaviour
    {
        [SerializeField]
        private GameObject m_root;

        [SerializeField]
        private Button m_goBackButton;

        [SerializeField]
        private Dropdown m_modeDropDown;

        [SerializeField]
        private VirtualizingListBox m_mapsListBox;
        [SerializeField]
        private Button m_createButton;

        [SerializeField]
        private Notification m_errorNotification;

        [SerializeField]
        private InputProvider m_inputProvider;

        private IGlobalSettings m_gSettings;
        private IProgressIndicator m_progress;
        private INavigation m_navigation;


        private MapInfo[] m_maps;

        private GameMode SelectedGameMode
        {
            get { return (GameMode)(1 << m_modeDropDown.value); }
        }
        protected override void Awake()
        {
            base.Awake();
            m_navigation = Dependencies.Navigation;
            m_gSettings = Dependencies.Settings;
            m_progress = Dependencies.Progress;

       
            m_modeDropDown.onValueChanged.AddListener(OnMapModeChanged);

            m_createButton.interactable = m_mapsListBox.SelectedItem != null;
            m_createButton.onClick.AddListener(OnCreateButtonClick);

            m_goBackButton.interactable = m_navigation.CanGoBack;
            m_goBackButton.onClick.AddListener(OnGoBack);

            m_mapsListBox.CanReorder = false;
            m_mapsListBox.CanDrag = false;
            m_mapsListBox.CanEdit = false;
            m_mapsListBox.CanRemove = false;
            m_mapsListBox.ItemDataBinding += OnItemDataBinding;
            m_mapsListBox.SelectionChanged += OnSelectionChanged;
            m_mapsListBox.Submit += OnMapsListBoxSubmit;
            m_mapsListBox.Cancel += OnMapsListBoxCancel;
            
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_root.SetActive(true);
            m_goBackButton.interactable = m_navigation.CanGoBack;

            m_mapsListBox.Items = null;
            m_progress.IsVisible = true;
            GameServer.GetPlayers(m_gSettings.ClientId, (error, players) =>
            {
                m_progress.IsVisible = false;
                if (GameServer.HasError(error))
                {
                    OutputError(error);
                    return;
                }

                m_progress.IsVisible = true;
                GameServer.GetMaps(m_gSettings.ClientId, (error2, mapsInfo) =>
                {

                    m_progress.IsVisible = false;
                    if (GameServer.HasError(error2))
                    {
                        OutputError(error2);
                        return;
                    }

                    m_maps = mapsInfo.Where(m => m.MaxPlayers >= players.Length).ToArray();
                    DataBindMaps();

                    IndependentSelectable.Select(m_mapsListBox.gameObject);
                    m_mapsListBox.IsFocused = true;
                });
            });
            
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
            if(m_root != null)
            {
                m_root.SetActive(false);
            }

            if(m_goBackButton != null)
            {
                m_goBackButton.onClick.RemoveListener(OnGoBack);
            }

            if(m_modeDropDown != null)
            {
                m_modeDropDown.onValueChanged.RemoveListener(OnMapModeChanged);
            }
            
            if (m_mapsListBox != null)
            {
                m_mapsListBox.SelectionChanged -= OnSelectionChanged;
                m_mapsListBox.ItemDataBinding -= OnItemDataBinding;
                m_mapsListBox.Submit -= OnMapsListBoxSubmit;
                m_mapsListBox.Cancel -= OnMapsListBoxCancel;
            }

            if(m_createButton != null)
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
                    EventSystem eventSystem = IndependentSelectable.GetEventSystem(m_mapsListBox);
                    if (eventSystem.currentSelectedGameObject == null)
                    {
                        IndependentSelectable.Select(m_mapsListBox.gameObject);
                        m_mapsListBox.IsFocused = true;
                    }
                }
            }
        }

        private void DataBindMaps()
        {
            m_mapsListBox.Items = m_maps.Where(m => (m.SupportedModes & SelectedGameMode) != 0).ToArray();
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs e)
        {
            m_createButton.interactable = m_mapsListBox.SelectedItem != null;
        }

        private void OnItemDataBinding(object sender, ItemDataBindingArgs e)
        {
            MapInfo mapInfo = (MapInfo)e.Item;

            Text text = e.ItemPresenter.GetComponent<Text>();
            text.text = string.Format("{0}, [Max Players: {1}]",  mapInfo.Name, mapInfo.MaxPlayers);
        }


        private void OnMapsListBoxCancel(object sender, System.EventArgs e)
        {
            IndependentSelectable.Select(m_goBackButton.gameObject);
            m_goBackButton.onClick.Invoke();
        }

        private void OnMapsListBoxSubmit(object sender, System.EventArgs e)
        {
            IndependentSelectable.Select(m_createButton.gameObject);
        }

        private void OnMapModeChanged(int index)
        {
            DataBindMaps();
        }

        private void OnCreateButtonClick()
        {
            m_progress.IsVisible = true;
            MapInfo mapInfo = (MapInfo)m_mapsListBox.SelectedItem;
            GameServer.CreateRoom(m_gSettings.ClientId, mapInfo.Id, SelectedGameMode, (error, room) =>
            {
                m_progress.IsVisible = false;
                if(GameServer.HasError(error))
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
