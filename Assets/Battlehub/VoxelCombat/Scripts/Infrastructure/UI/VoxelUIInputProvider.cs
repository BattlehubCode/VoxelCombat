using UnityEngine;

using Battlehub.UIControls;

namespace Battlehub.VoxelCombat
{
    public class VoxelUIInputProvider : InputProvider
    {
        [SerializeField]
        private int m_localPlayerIndex;

        private IGameView m_gameView;
        private IPlayerCameraController m_cameraController;
        private IVoxelInputManager m_input;
        private bool m_isKeyboardAndMouse;

        public int LocalPlayerIndex
        {
            get { return m_localPlayerIndex; }
            set
            {
                m_localPlayerIndex = value;
                if(m_input == null)
                {
                    m_input = Dependencies.InputManager;
                }
                m_isKeyboardAndMouse = m_input.IsKeyboardAndMouse(m_localPlayerIndex);
            }
        }

        public override float HorizontalAxis
        {
            get
            {
                float hor = m_input.GetAxisRaw(InputAction.MoveSide, m_localPlayerIndex, false, false);
                return hor;
            }
        }

        public override float VerticalAxis
        {
            get { return m_input.GetAxisRaw(InputAction.MoveForward, m_localPlayerIndex, false, false); }
        }

        public override bool IsHorizontalButtonDown
        {
            get { return m_input.GetButtonDown(InputAction.MoveSide, m_localPlayerIndex, false, false); }
        }

        public override bool IsVerticalButtonDown
        {
            get { return m_input.GetButtonDown(InputAction.MoveForward, m_localPlayerIndex, false, false); }
        }

        public override float HorizontalAxis2
        {
            get
            {
                if(m_localPlayerIndex < 0)
                {
                    return Mathf.Clamp01(
                        m_input.GetAxisRaw(InputAction.MoveSide, m_localPlayerIndex, false, false) +
                        m_input.GetAxisRaw(InputAction.CursorX, m_localPlayerIndex, false, false));
                }

                if(m_isKeyboardAndMouse)
                {
                    return m_input.GetAxisRaw(InputAction.MoveSide, m_localPlayerIndex, false, false);
                }

                return m_input.GetAxisRaw(InputAction.CursorX, m_localPlayerIndex, false, false);
            }
        }

        public override bool IsHorizontal2ButtonDown
        {
            get
            {
                if(m_localPlayerIndex < 0)
                {
                    return m_input.GetButtonDown(InputAction.MoveSide, m_localPlayerIndex, false, false) ||
                        m_input.GetButtonDown(InputAction.CursorX, m_localPlayerIndex, false, false);
                }
                if (m_isKeyboardAndMouse)
                {
                    return m_input.GetButtonDown(InputAction.MoveSide, m_localPlayerIndex, false, false);
                }
                return m_input.GetButtonDown(InputAction.CursorX, m_localPlayerIndex, false, false);
            }
        }

        public override bool IsFunctionalButtonDown
        {
            get { return m_input.GetButtonDown(InputAction.LB, m_localPlayerIndex, false, false); }
        }

        public override bool IsFunctionalButtonPressed
        {
            get { return m_input.GetButton(InputAction.LB, m_localPlayerIndex, false, false); }
        }

        public override bool IsFunctional2ButtonDown
        {
            get { return m_input.GetButtonDown(InputAction.RB, m_localPlayerIndex, false, false); }
        }

        public override bool IsFunctional2ButtonPressed
        {
            get { return m_input.GetButton(InputAction.RB, m_localPlayerIndex, false, false); }
        }

        public override bool IsSubmitButtonDown
        {
            get
            {
                if (m_localPlayerIndex < 0)
                {
                    return m_input.GetButtonDown(InputAction.Submit, m_localPlayerIndex, false, false) ||
                        m_input.GetButtonDown(InputAction.A, m_localPlayerIndex, false, false);
                }
                if (m_isKeyboardAndMouse)
                {
                    return m_input.GetButtonDown(InputAction.Submit, m_localPlayerIndex, false, false);
                }
                return m_input.GetButtonDown(InputAction.A, m_localPlayerIndex, false, false);
            }
        }

        public override bool IsSubmitButtonUp
        {
            get
            {
                if(m_localPlayerIndex < 0)
                {
                    return m_input.GetButtonUp(InputAction.Submit, m_localPlayerIndex, false, false) ||
                        m_input.GetButtonUp(InputAction.A, m_localPlayerIndex, false, false);
                }
                if (m_isKeyboardAndMouse)
                {
                    return m_input.GetButtonUp(InputAction.Submit, m_localPlayerIndex, false, false);
                }
                return m_input.GetButtonUp(InputAction.A, m_localPlayerIndex, false, false);
            }
        }

        public override bool IsCancelButtonDown
        {
            get
            {
                if(m_localPlayerIndex < 0)
                {
                    return m_input.GetButtonDown(InputAction.Cancel, m_localPlayerIndex, false, false) ||
                         m_input.GetButtonDown(InputAction.B, m_localPlayerIndex, false, false);
                }
                if (m_isKeyboardAndMouse)
                {
                    return m_input.GetButtonDown(InputAction.Cancel, m_localPlayerIndex, false, false);
                }
                return m_input.GetButtonDown(InputAction.B, m_localPlayerIndex, false, false);
            }
        }

        public override bool IsAnyKeyDown
        {
            get { return m_input.IsAnyButtonDown(m_localPlayerIndex, false, false); }
        }

        public override Vector3 MousePosition
        {
            get
            {
                if (m_localPlayerIndex < 0)
                {
                    return m_input.MousePosition;
                }

                if (m_gameView != null && m_cameraController != null)
                {
                    return m_cameraController.VirtualMousePosition;
                }

                return m_input.MousePosition;

                //if (m_isKeyboardAndMouse)
                //{
                //    if(m_gameView != null && m_cameraController != null)
                //    {
                //        return m_cameraController.VirtualMousePosition;
                //    }

                //    return m_input.MousePosition;
                //}

                //if(m_localPlayerIndex < 0)
                //{
                //    return m_input.MousePosition;
                //}
                //return new Vector3(-1, -1, -1);
            }
        }

        public override bool IsMouseButtonDown(int button)
        {
            if(m_input.IsSuspended(m_localPlayerIndex))
            {
                return false;
            }

            if(m_isKeyboardAndMouse || m_localPlayerIndex < 0)
            {
                return Input.GetMouseButtonDown(button); 
            }
            return m_input.GetButtonDown(InputAction.LB, m_localPlayerIndex, false, false); 
        }

        public override bool IsMousePresent
        {
            get
            {
                if(m_input.IsSuspended(m_localPlayerIndex))
                {
                    return false;
                }

                /*
                if(m_isKeyboardAndMouse || m_localPlayerIndex < 0)
                {
                    return Input.mousePresent;
                }
                return false;
                */
                return true;
            }
        }

        public override bool IsKeyboardPresent
        {
            get { return m_isKeyboardAndMouse; }
        }


        public override int TouchCount
        {
            get { return 0; }
        }

        public override Touch GetTouch(int i)
        {
            return default(Touch);
        }

        public override bool IsTouchSupported
        {
            get { return false; }
        }

        public override float GetAxisRaw(string axisName)
        {
            throw new System.NotImplementedException("GetAxisRaw " + axisName);
        }

        public override bool GetMouseButton(int button)
        {
            if(button == 0)
            {
                if(m_isKeyboardAndMouse)
                {
                    return m_input.GetButton(InputAction.LMB, LocalPlayerIndex, false, false);
                }
                return m_input.GetButton(InputAction.LB, LocalPlayerIndex, false, false);
            }
            return false;
        }

        public override bool GetMouseButtonUp(int button)
        {
            if (button == 0)
            {
                if(m_isKeyboardAndMouse)
                {
                    return m_input.GetButtonUp(InputAction.LMB, LocalPlayerIndex, false, false);
                }
                return m_input.GetButtonUp(InputAction.LB, LocalPlayerIndex, false, false);
            }
            return false;
        }

        public override bool GetMouseButtonDown(int button)
        {
            if (button == 0)
            {
                if(m_isKeyboardAndMouse)
                {
                    return m_input.GetButtonDown(InputAction.LMB, LocalPlayerIndex, false, false);
                }
                return  m_input.GetButtonDown(InputAction.LB, LocalPlayerIndex, false, false);
            }
            return false;
        }

        public override bool GetButtonDown(string buttonName)
        {
            throw new System.NotImplementedException("GetButtonDown " + buttonName);
        }

        private void Awake()
        {
            m_gameView = Dependencies.GameView;
            m_input = Dependencies.InputManager;
            m_input.DeviceEnabled += OnDeviceEnabled;
            m_input.DeviceDisabled += OnDeviceDisabled;
        }

        private void Start()
        {
            m_isKeyboardAndMouse = m_input.IsKeyboardAndMouse(m_localPlayerIndex);
            if(m_gameView != null)
            {
                if (m_gameView.IsInitialized)
                {
                    m_cameraController = m_gameView.GetCameraController(m_localPlayerIndex);
                }
                else
                {
                    m_gameView.Initialized += OnGameViewInitialized;
                }
            }  
        }

        private void OnGameViewInitialized(object sender, System.EventArgs e)
        {
            m_gameView.Initialized -= OnGameViewInitialized;
            m_cameraController = m_gameView.GetCameraController(m_localPlayerIndex);
        }

        private void OnDestroy()
        {
            if (m_gameView != null)
            {
                m_gameView.Initialized -= OnGameViewInitialized;
            }

            if (m_input != null)
            {
                m_input.DeviceEnabled -= OnDeviceEnabled;
                m_input.DeviceDisabled -= OnDeviceDisabled;
            }   
        }

        private void OnDeviceDisabled(int arg)
        {
            m_isKeyboardAndMouse = m_input.IsKeyboardAndMouse(m_localPlayerIndex);
        }

        private void OnDeviceEnabled(int arg)
        {
            m_isKeyboardAndMouse = m_input.IsKeyboardAndMouse(m_localPlayerIndex);
        }
    }
}

