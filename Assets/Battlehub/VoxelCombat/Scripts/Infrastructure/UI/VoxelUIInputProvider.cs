using UnityEngine;

using Battlehub.UIControls;

namespace Battlehub.VoxelCombat
{
    public class VoxelUIInputProvider : InputProvider
    {
        [SerializeField]
        private int m_localPlayerIndex;

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
                float hor = m_input.GetAxisRaw(InputAction.MoveSide, m_localPlayerIndex, false);
                return hor;
            }
        }

        public override float VerticalAxis
        {
            get { return m_input.GetAxisRaw(InputAction.MoveForward, m_localPlayerIndex, false); }
        }

        public override bool IsHorizontalButtonDown
        {
            get { return m_input.GetButtonDown(InputAction.MoveSide, m_localPlayerIndex, false); }
        }

        public override bool IsVerticalButtonDown
        {
            get { return m_input.GetButtonDown(InputAction.MoveForward, m_localPlayerIndex, false); }
        }

        public override float HorizontalAxis2
        {
            get
            {
                if(m_localPlayerIndex < 0)
                {
                    return Mathf.Clamp01(
                        m_input.GetAxisRaw(InputAction.MoveSide, m_localPlayerIndex, false) +
                        m_input.GetAxisRaw(InputAction.CursorX, m_localPlayerIndex, false));
                }

                if(m_isKeyboardAndMouse)
                {
                    return m_input.GetAxisRaw(InputAction.MoveSide, m_localPlayerIndex, false);
                }

                return m_input.GetAxisRaw(InputAction.CursorX, m_localPlayerIndex, false);
            }
        }

        public override bool IsHorizontal2ButtonDown
        {
            get
            {
                if(m_localPlayerIndex < 0)
                {
                    return m_input.GetButtonDown(InputAction.MoveSide, m_localPlayerIndex, false) ||
                        m_input.GetButtonDown(InputAction.CursorX, m_localPlayerIndex, false);
                }
                if (m_isKeyboardAndMouse)
                {
                    return m_input.GetButtonDown(InputAction.MoveSide, m_localPlayerIndex, false);
                }
                return m_input.GetButtonDown(InputAction.CursorX, m_localPlayerIndex, false);
            }
        }

        public override bool IsFunctionalButtonPressed
        {
            get { return m_input.GetButton(InputAction.LB, m_localPlayerIndex, false); }
        }

        public override bool IsFunctional2ButtonPressed
        {
            get { return m_input.GetButton(InputAction.RB, m_localPlayerIndex, false); }
        }

        public override bool IsSubmitButtonDown
        {
            get
            {
                if (m_localPlayerIndex < 0)
                {
                    return m_input.GetButtonDown(InputAction.Submit, m_localPlayerIndex, false) ||
                        m_input.GetButtonDown(InputAction.A, m_localPlayerIndex, false);
                }
                if (m_isKeyboardAndMouse)
                {
                    return m_input.GetButtonDown(InputAction.Submit, m_localPlayerIndex, false);
                }
                return m_input.GetButtonDown(InputAction.A, m_localPlayerIndex, false);
            }
        }

        public override bool IsSubmitButtonUp
        {
            get
            {
                if(m_localPlayerIndex < 0)
                {
                    return m_input.GetButtonUp(InputAction.Submit, m_localPlayerIndex, false) ||
                        m_input.GetButtonUp(InputAction.A, m_localPlayerIndex, false);
                }
                if (m_isKeyboardAndMouse)
                {
                    return m_input.GetButtonUp(InputAction.Submit, m_localPlayerIndex, false);
                }
                return m_input.GetButtonUp(InputAction.A, m_localPlayerIndex, false);
            }
        }

        public override bool IsCancelButtonDown
        {
            get
            {
                if(m_localPlayerIndex < 0)
                {
                    return m_input.GetButtonDown(InputAction.Cancel, m_localPlayerIndex, false) ||
                         m_input.GetButtonDown(InputAction.B, m_localPlayerIndex, false);
                }
                if (m_isKeyboardAndMouse)
                {
                    return m_input.GetButtonDown(InputAction.Cancel, m_localPlayerIndex, false);
                }
                return m_input.GetButtonDown(InputAction.B, m_localPlayerIndex, false);
            }
        }

        public override bool IsAnyKeyDown
        {
            get { return m_input.IsAnyButtonDown(m_localPlayerIndex, false); }
        }

        public override Vector3 MousePosition
        {
            get
            {
                if (m_isKeyboardAndMouse || m_localPlayerIndex < 0)
                {
                    return m_input.MousePosition;
                }
                return new Vector3(-1, -1, -1);
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
            return false;
        }

        public override bool IsMousePresent
        {
            get
            {
                if(m_input.IsSuspended(m_localPlayerIndex))
                {
                    return false;
                }

                if(m_isKeyboardAndMouse || m_localPlayerIndex < 0)
                {
                    return Input.mousePresent;
                }
                return false;
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

        private void Awake()
        {
            m_input = Dependencies.InputManager;
        }

        private void Start()
        {
            m_isKeyboardAndMouse = m_input.IsKeyboardAndMouse(m_localPlayerIndex);
        }
    }
}

