using Battlehub.UIControls;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public delegate void LoginMenuEventHandler(LoginMenu sender);
    public delegate void LoginMenuEventHandler<T>(LoginMenu sender, T arg);

    public class LoginMenu : MonoBehaviour
    {
        public event LoginMenuEventHandler Go;
        public event LoginMenuEventHandler CancelGo;
        public event LoginMenuEventHandler<Player> LoggedIn;
        public event LoginMenuEventHandler<Player> LoggedOff;
        public event LoginMenuEventHandler Disabled;

        [SerializeField]
        private GameObject m_root;

        public Transform Root
        {
            get { return m_root.transform; }
        }

        [SerializeField]
        private Button m_disableButton;

        [SerializeField]
        private Button m_loginButton;

        [SerializeField]
        private Button m_signUpButton;

        [SerializeField]
        private Button m_logoffButton;

        [SerializeField]
        private Button m_goButton;

        [SerializeField]
        private Button m_readyButton;

        [SerializeField]
        private GameObject m_loggedInPanel;

        [SerializeField]
        private Text m_playerNameTxt;

        [SerializeField]
        private Text m_playerVictoriesTxt;

        [SerializeField]
        private Text m_playerDefeatsTxt;

        [SerializeField]
        private Text m_playerRankTxt;

        [SerializeField]
        private GameObject m_loggedOffPanel;

        [SerializeField]
        private LoginPanel m_loginPanel;

        [SerializeField]
        private SignupPanel m_signUpPanel;

        [SerializeField]
        private PlayerUIZone m_playerUIZone;

        [SerializeField]
        private int m_localPlayerIndex;
        public int LocalPlayerIndex
        {
            get { return m_localPlayerIndex; }
            set
            {
                m_localPlayerIndex = value;
                m_playerUIZone.LocalPlayerIndex = value;
            }
        }

        public Player Player
        {
            get { return m_player; }
            set { SetPlayer(value); }
        }

        public bool IsInProgress
        {
            get
            {
                if(m_progress == null)
                {
                    return false;
                }
                IProgressIndicator progress = m_progress.GetChild(LocalPlayerIndex);
                if(progress != null)
                {
                    return progress.IsVisible;
                }

                return false;
            }
        }

        private bool m_isVirtualKeyboardEnabled;
        public bool IsVirtualKeyboardEnabled
        {
            get { return m_isVirtualKeyboardEnabled; }
            set
            {
                m_isVirtualKeyboardEnabled = value;

                InputFieldWithVirtualKeyboard[] inputFields = m_root.GetComponentsInChildren<InputFieldWithVirtualKeyboard>(true);
                for (int i = 0; i < inputFields.Length; ++i)
                {
                    InputFieldWithVirtualKeyboard inputField = inputFields[i];
                    inputField.VirtualKeyboardEnabled = m_isVirtualKeyboardEnabled;
                }
            }
        }

        private IGameServer GameServer
        {
            get { return Dependencies.GameServer; }
        }

        private INotification m_notification;
        private IProgressIndicator m_progress;
        private IGlobalSettings m_gSettings;
        private IVoxelInputManager m_inputManager;

        private Player m_player;

        private IEnumerator m_coSubscribe;

        private string RememberedLogin
        {
            get { return PlayerPrefs.GetString("bf942302-0053-4500-9ca7-0d7aa9a7daa3" + LocalPlayerIndex); }
            set { PlayerPrefs.SetString("bf942302-0053-4500-9ca7-0d7aa9a7daa3" + LocalPlayerIndex, value); }
        }

        private byte[] RememberedPasswordHash
        {
            get
            {
                string str = PlayerPrefs.GetString("c0ebfce5-7da3-486f-870f-949e80e9db5b" + LocalPlayerIndex);
                if(string.IsNullOrEmpty(str)) { return new byte[0]; }
                return Convert.FromBase64String(str);
            }
            set { PlayerPrefs.SetString("c0ebfce5-7da3-486f-870f-949e80e9db5b" + LocalPlayerIndex, Convert.ToBase64String(value)); }
        }


        private void Awake()
        {
            m_notification = Dependencies.Notification;
            m_progress = Dependencies.Progress;
            m_gSettings = Dependencies.Settings;
            m_inputManager = Dependencies.InputManager;

            m_disableButton.gameObject.SetActive(m_inputManager.DeviceCount > 1);
            m_inputManager.DeviceEnabled += OnDeviceEnabled;
            m_inputManager.DeviceDisabled += OnDeviceDisabled;

            m_loginPanel.Login += OnLogin;
            m_loginPanel.LoginCancel += OnLoginCancel;
            m_signUpPanel.Login += OnSignup;
            m_signUpPanel.LoginCancel += OnLoginCancel;
        }

        private void OnDeviceEnabled(int arg)
        {
            m_disableButton.gameObject.SetActive(m_inputManager.DeviceCount > 1);
        }

        private void OnDeviceDisabled(int arg)
        {
            m_disableButton.gameObject.SetActive(m_inputManager.DeviceCount > 1);
        }

        private void OnEnable()
        {
            SelectFirstButton();

            m_coSubscribe = CoSubscribe();

            StartCoroutine(m_coSubscribe);
        }

        private IEnumerator CoSubscribe()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            m_disableButton.onClick.AddListener(OnDisableClick);
            m_loginButton.onClick.AddListener(OnLoginClick);
            m_signUpButton.onClick.AddListener(OnSignupClick);
            m_logoffButton.onClick.AddListener(OnLogoffClick);
            m_goButton.onClick.AddListener(OnGoClick);
            m_readyButton.onClick.AddListener(OnReadyClick);

            m_coSubscribe = null;

        }

        private void SelectFirstButton()
        {
            if(m_loginPanel.isActiveAndEnabled || m_signUpPanel.isActiveAndEnabled)
            {
                return;
            }

            if (m_player != null)
            {
                m_loggedInPanel.SetActive(true);
                IndependentSelectable.Select(m_goButton.gameObject);
                m_goButton.OnSelect(null);
            }
            else
            {
                m_loggedOffPanel.SetActive(true);
                IndependentSelectable.Select(m_loginButton.gameObject);
                m_loginButton.OnSelect(null);
            }
        }

        private void OnDisable()
        {
            if(m_coSubscribe != null)
            {
                StopCoroutine(m_coSubscribe);
                m_coSubscribe = null;
            }

            if (m_progress != null)
            {
                IProgressIndicator progress = m_progress.GetChild(LocalPlayerIndex);
                if(progress != null)
                {
                    progress.IsVisible = false;
                }
            }

            if(m_loggedOffPanel != null)
            {
                m_loggedOffPanel.SetActive(false);
            }

            if(m_loggedInPanel != null)
            {
                m_loggedInPanel.SetActive(false);
            }

            if(m_signUpPanel != null)
            {
                m_signUpPanel.gameObject.SetActive(false);
            }

            if(m_loginPanel != null)
            {
                m_loginPanel.gameObject.SetActive(false);
            }

            if(m_goButton != null)
            {
                m_goButton.gameObject.SetActive(true);
            }

            if(m_readyButton != null)
            {
                m_readyButton.gameObject.SetActive(false);
            }

            if (m_disableButton != null)
            {
                m_disableButton.onClick.RemoveListener(OnDisableClick);
            }
            if (m_loginButton != null)
            {
                m_loginButton.onClick.RemoveListener(OnLoginClick);
            }
            if (m_signUpButton != null)
            {
                m_signUpButton.onClick.RemoveListener(OnSignupClick);
            }
            if (m_logoffButton != null)
            {
                m_logoffButton.onClick.RemoveListener(OnLogoffClick);
            }
            if (m_goButton != null)
            {
                m_goButton.onClick.RemoveListener(OnGoClick);
            }

            if (m_readyButton != null)
            {
                m_readyButton.onClick.RemoveListener(OnReadyClick);
            }
        }

        private void OnDestroy()
        {   
            if(m_inputManager != null)
            {
                m_inputManager.DeviceEnabled -= OnDeviceEnabled;
                m_inputManager.DeviceDisabled -= OnDeviceDisabled;
            }

            if (m_loginPanel != null)
            {
                m_loginPanel.Login -= OnLogin;
                m_loginPanel.LoginCancel -= OnLoginCancel;
            }

            if (m_signUpPanel != null)
            {
                m_signUpPanel.Login -= OnSignup;
                m_signUpPanel.LoginCancel -= OnLoginCancel;
            }
        }

        private void OnDisableClick()
        {
            m_notification.GetChild(LocalPlayerIndex).Close();

            if (Disabled != null)
            {
                Disabled(this);
            }
        }

        private void OnLoginClick()
        {
            if(!string.IsNullOrEmpty(RememberedLogin))
            {
                m_loginPanel.Set(RememberedLogin);
            }

            m_loggedOffPanel.SetActive(false);
            m_loginPanel.gameObject.SetActive(true);
            m_notification.GetChild(LocalPlayerIndex).Close();
        }

        private void OnSignupClick()
        {
            m_loggedOffPanel.SetActive(false);
            m_signUpPanel.gameObject.SetActive(true);
            m_notification.GetChild(LocalPlayerIndex).Close();
        }

        private void OnLogoffClick()
        {
            RememberedLogin = null;
            RememberedPasswordHash = new byte[0];
          
            m_notification.GetChild(LocalPlayerIndex).Close();

            IProgressIndicator progress = m_progress.GetChild(LocalPlayerIndex);
            progress.IsVisible = true;

            m_loggedInPanel.SetActive(false);

            GameServer.Logoff(m_gSettings.ClientId, m_player.Id, (error, payerId) =>
            {    
                if(!isActiveAndEnabled)
                {
                    return;
                }

                progress.IsVisible = false;
                if (GameServer.HasError(error))
                {
                    m_loggedInPanel.SetActive(true);
                    m_notification.GetChild(LocalPlayerIndex).ShowError(error, m_logoffButton.gameObject);
                    return;
                }

                if(LoggedOff != null)
                {
                    LoggedOff(this, m_player);
                }

                SetPlayer(null);
            });
        }

        private void OnGoClick()
        {
            m_notification.GetChild(LocalPlayerIndex).Close();

            m_goButton.gameObject.SetActive(false);
            m_readyButton.gameObject.SetActive(true);

            IndependentSelectable.Select(m_readyButton.gameObject);

            if (Go != null)
            {
                Go(this);
            }

        }

        private void OnReadyClick()
        {
            m_notification.GetChild(LocalPlayerIndex).Close();

            if (CancelGo != null)
            {
                CancelGo(this);
            }

            m_goButton.gameObject.SetActive(true);
            m_readyButton.gameObject.SetActive(false);

            IndependentSelectable.Select(m_goButton.gameObject);
        }

        private void OnLoginCancel()
        {
            m_loggedOffPanel.SetActive(true);
            m_signUpPanel.gameObject.SetActive(false);
            m_loginPanel.gameObject.SetActive(false);

            SelectFirstButton();
        }

        private void OnLogin(string name, string password, bool hasChanged, bool rememberMe)
        {
            m_loginPanel.gameObject.SetActive(false);

            IProgressIndicator progress = m_progress.GetChild(LocalPlayerIndex);
            progress.IsVisible = true;
            
            if(!hasChanged && RememberedLogin != null)
            {
                GameServer.Login(name, RememberedPasswordHash, m_gSettings.ClientId, (error, playerId) =>
                {
                    if (!isActiveAndEnabled)
                    {
                        return;
                    }

                    if (GameServer.HasError(error))
                    {
                        progress.IsVisible = false;
                        m_notification.GetChild(LocalPlayerIndex).ShowError(error, m_loginButton.gameObject);
                        m_loggedOffPanel.SetActive(true);
                        SelectFirstButton();
                        return;
                    }

                    if (!rememberMe)
                    {
                        RememberedLogin = null;
                        RememberedPasswordHash = new byte[0];
                    }
              
                    GameServer.GetPlayer(m_gSettings.ClientId, playerId, OnGetPlayerCompleted);
                });
            }
            else
            {
                GameServer.Login(name, password, m_gSettings.ClientId, (error, playerId, pwdHash) =>
                {
                    if (!isActiveAndEnabled)
                    {
                        return;
                    }

                    if (GameServer.HasError(error))
                    {
                        progress.IsVisible = false;
                        m_notification.GetChild(LocalPlayerIndex).ShowError(error, m_loginButton.gameObject);
                        m_loggedOffPanel.SetActive(true);
                        SelectFirstButton();
                        return;
                    }

                    if (rememberMe)
                    {
                        RememberedLogin = name;
                        RememberedPasswordHash = pwdHash;
                    }
                    else
                    {
                        RememberedLogin = null;
                        RememberedPasswordHash = new byte[0];
                    }

                    GameServer.GetPlayer(m_gSettings.ClientId, playerId, OnGetPlayerCompleted);
                });
            }
        }

        private void OnSignup(string name, string password, bool hasChanged, bool rememberMe)
        {
            m_signUpPanel.gameObject.SetActive(false);

            IProgressIndicator progress = m_progress.GetChild(LocalPlayerIndex);
            progress.IsVisible = true;

            GameServer.SignUp(name, password, m_gSettings.ClientId, (error, playerId, pwdHash) =>
            {
                if (!isActiveAndEnabled)
                {
                    return;
                }

                if (GameServer.HasError(error))
                {
                    if (error.Code == StatusCode.AlreadyExists)
                    {
                        progress.IsVisible = false;
                        m_notification.GetChild(LocalPlayerIndex).ShowError("User with this name already exists", m_signUpButton.gameObject);
                        m_loggedOffPanel.SetActive(true);
                        SelectFirstButton();
                    }
                    else
                    {
                        progress.IsVisible = false;
                        m_notification.GetChild(LocalPlayerIndex).ShowError(error, m_signUpButton.gameObject);
                        m_loggedOffPanel.SetActive(true);
                        SelectFirstButton();
                    }

                    return;
                }

                if (rememberMe)
                {
                    RememberedLogin = name;
                    RememberedPasswordHash = pwdHash;
                }
                else
                {
                    RememberedLogin = null;
                    RememberedPasswordHash = new byte[0];
                }

                GameServer.GetPlayer(m_gSettings.ClientId, playerId, OnGetPlayerCompleted);
            });
        }


        private void OnGetPlayerCompleted(Error error, Player player)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            m_progress.GetChild(LocalPlayerIndex).IsVisible = false;
            if (GameServer.HasError(error))
            {
                //#warning Show Error with try again button. This button will repeate last operation.
                m_notification.GetChild(LocalPlayerIndex).ShowError(error, m_loginButton.gameObject); 
                return;
            }

            SetPlayer(player);
            if (LoggedIn != null)
            {
                LoggedIn(this, m_player);
            }
        }

        private void SetPlayer(Player player)
        {
            if(m_player != player)
            {
                m_player = player;
                if (m_player != null)
                {
                    m_loggedInPanel.SetActive(true);
                    m_loggedOffPanel.SetActive(false);

                    m_playerNameTxt.text = m_player.Name;
                    m_playerVictoriesTxt.text = "Victories: " + m_player.Victories;
                    m_playerDefeatsTxt.text = "Defeats: " + "Unknown";
                    m_playerRankTxt.text = "Rank: " + "Unknown";
                }
                else
                {
                    m_loggedInPanel.SetActive(false);
                    m_loggedOffPanel.SetActive(true);
                }

                m_loginPanel.gameObject.SetActive(false);
                m_signUpPanel.gameObject.SetActive(false);
                SelectFirstButton();
            }
        }

        /*
        public void ExportPlayers()
        {
            string path = Application.streamingAssetsPath + "/playerStats.txt";

            Dictionary<Guid, Player> players = Dependencies.State.GetValue<Dictionary<Guid, Player>>("LocalGameServer.m_players");
            
            File.Delete(Application.streamingAssetsPath + "/playerStats.txt");

            foreach(Player player in players.Values.OrderByDescending(p => p.Victories))
            {
                File.AppendAllText(path, player.Name + " " + player.Victories + Environment.NewLine);
            }

            Debug.Log("Players Exported " + path);
        }
         */

    }

}
