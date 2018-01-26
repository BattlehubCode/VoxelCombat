using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class TestMenu : MonoBehaviour
    {
        [SerializeField]
        private Button m_p2cButton;
        [SerializeField]
        private Button m_p2pButton;
        [SerializeField]
        private Button m_2p2cButton;
        [SerializeField]
        private Button m_quit;

        [SerializeField]
        private int m_selectedIndex;

        private Button[] m_buttons;

        private IVoxelInputManager m_inputManager;
        private INavigation m_navigation;
        private IGameServer m_server;
        private IGlobalSettings m_settings;
        private IProgressIndicator m_progress;
        private IGlobalState m_gState;

        
        
        private void Start()
        {
            //Debug.Log(Application.persistentDataPath);

            m_inputManager = Dependencies.InputManager;
            m_navigation = Dependencies.Navigation;
            m_server = Dependencies.GameServer;
            m_settings = Dependencies.Settings;
            m_progress = Dependencies.Progress;
            m_gState = Dependencies.State;


            HashSet<Guid> m_loggedInPlayers = m_gState.GetValue<HashSet<Guid>>("LocalGameServer.m_loggedInPlayers");
            if(m_loggedInPlayers != null && m_loggedInPlayers.Count > 0)
            {
                m_progress.IsVisible = true;
                foreach (Guid logginInPlayer in m_loggedInPlayers.ToArray())
                {
                    m_server.Logoff(m_settings.ClientId, logginInPlayer, (e, p) => 
                    {
                        m_progress.IsVisible = false;
                    });
                }
            }
            

            m_buttons = new [] { m_p2cButton, m_p2pButton, m_2p2cButton, m_quit };
            ChangeSelection(-1, m_selectedIndex);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            
        }

        private float m_delay = 0.0f;
        private float m_sign;

        private void Update()
        {
            if(m_progress.IsVisible)
            {
                return;
            }

            float value = m_inputManager.GetAxisRaw(InputAction.MoveForward, 0) + m_inputManager.GetAxisRaw(InputAction.CursorY, 0);
            if (value == 0)
            {
                value = m_inputManager.GetAxisRaw(InputAction.MoveForward, 1) + m_inputManager.GetAxisRaw(InputAction.CursorY, 1);
            }
            if (m_sign != Mathf.Sign(value))
            {
                m_sign = Mathf.Sign(value);
                m_delay = 0;
            }

            if(value != 0)
            {
                m_delay -= Time.deltaTime;

                if (m_delay <= 0)
                {
                    int prevIndex = m_selectedIndex;
                    if (m_sign < 0)
                    {
                        m_selectedIndex++;
                        if (m_selectedIndex >= m_buttons.Length)
                        {
                            m_selectedIndex = 0;
                        }
                    }
                    else if (m_sign > 0)
                    {
                        m_selectedIndex--;
                        if (m_selectedIndex < 0)
                        {
                            m_selectedIndex = m_buttons.Length - 1;
                        }
                    }

                    ChangeSelection(prevIndex, m_selectedIndex);
                    m_delay = 0.2f;
                }
            }


            if(m_inputManager.GetButtonDown(InputAction.A, 0) || m_inputManager.GetButtonDown(InputAction.Action9, 0) ||
                m_inputManager.GetButtonDown(InputAction.A, 1) || m_inputManager.GetButtonDown(InputAction.Action9, 1))
            {
                if (m_selectedIndex == 0)
                {
                    TestGameInit.Init("game4", 1, 1, () =>
                    {
                        m_navigation.Navigate("Game");
                    },
                    OutputError());
                }
                else if (m_selectedIndex == 1)
                {
                    TestGameInit.Init("game4", 2, 0, () =>
                    {
                        m_navigation.Navigate("Game");
                    },
                    OutputError());
                }
                else if (m_selectedIndex == 2)
                {
                    TestGameInit.Init("game4", 2, 2, () =>
                    {
                        m_navigation.Navigate("Game");
                    },
                    OutputError());
                }
                else if (m_selectedIndex == 3)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif

                }
            }
          
        }

        private static System.Action<Error> OutputError()
        {
            return error => Debug.Log("Error : " + error.Code + " " + error.Message);
        }

        private void ChangeSelection(int prevIndex, int index)
        {
            if(prevIndex >= 0 && prevIndex < m_buttons.Length)
            {
                m_buttons[prevIndex].OnDeselect(null);
            }

            m_buttons[index].OnSelect(null);
        }
    }
}

