using UnityEngine;
using InControl;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using System;
using Battlehub.UIControls;

namespace Battlehub.VoxelCombat
{
    //[DefaultExecutionOrder(-3000)]
    public class InControlAdapter : MonoBehaviour, IVoxelInputManager
    {
        private const string DevicesPersistentKey = "InControlAdapter.Devices";

        private GameObject[] m_selectedGameObject;
        private bool[] m_isInputFieldSelected;
        private bool[] m_isPointerOverGameObject;

        private GameObject m_commonSelectedGameObject;
        private bool m_commonPointerOverGameObject;

        private List<InputDevice> m_devices;
        private List<InputDevice> m_lastDisabledDevices;
        private IGlobalState m_gState;
        private IProgressIndicator m_progress;
        private IEventSystemManager m_eventSystemManager;
      
        private static readonly Dictionary<InputAction, InputControlType> m_mapping =
            new Dictionary<InputAction, InputControlType>
            {
                { InputAction.LMB, InputControlType.Button3 },
                { InputAction.RMB, InputControlType.Button4 },
                { InputAction.MMB, InputControlType.Button5 },

                { InputAction.LB, InputControlType.LeftBumper },
                { InputAction.RB, InputControlType.RightBumper },
                { InputAction.LT, InputControlType.LeftTrigger },
                { InputAction.RT, InputControlType.RightTrigger },
                { InputAction.LeftStickButton, InputControlType.LeftStickButton },
                { InputAction.RightStickButton, InputControlType.RightStickButton },
                { InputAction.DPadLeft, InputControlType.DPadLeft },
                { InputAction.DPadRight, InputControlType.DPadRight },
                { InputAction.DPadUp, InputControlType.DPadUp },
                { InputAction.DPadDown, InputControlType.DPadDown },

                { InputAction.A, InputControlType.Action1 },
                { InputAction.B, InputControlType.Action2 },
                { InputAction.X, InputControlType.Action3 },
                { InputAction.Y, InputControlType.Action4 },
                { InputAction.Start, InputControlType.Start },
                { InputAction.Back, InputControlType.Back },
                { InputAction.MoveForward, InputControlType.LeftStickY },
                { InputAction.MoveSide, InputControlType.LeftStickX },
                { InputAction.CursorY, InputControlType.RightStickY },
                { InputAction.CursorX, InputControlType.RightStickX },
                { InputAction.MouseY, InputControlType.RightStickY },
                { InputAction.MouseX, InputControlType.RightStickX },

                { InputAction.ToggleConsole, InputControlType.Button1 },
                { InputAction.ToggleCursor, InputControlType.Button2 },
                { InputAction.EditorCreate, InputControlType.Button3 },
                { InputAction.EditorDestroy, InputControlType.Button4 },
                { InputAction.EditorPan, InputControlType.Button5 },
                { InputAction.EditorRotate, InputControlType.Button6 },
                { InputAction.Cancel, InputControlType.Button18 },
                { InputAction.Submit, InputControlType.Button19 },
                { InputAction.Zoom, InputControlType.ScrollWheel },
           
            };

        public event InputEventHandler<int> DeviceEnabled;
        public event InputEventHandler<int> DeviceDisabled;
        public event InputEventHandler<object> ActiveDeviceChanged;

        private bool m_isInInitializationState;
        public bool IsInInitializationState
        {
            get { return m_isInInitializationState; }
            set
            {
                if(m_isInInitializationState != value)
                {                   
                    m_isInInitializationState = value;
                    if (m_isInInitializationState)
                    {
                        
                        if(m_devices != null)
                        {
                            for (int i = m_devices.Count - 1; i >= 0; i--)
                            {
                                if (m_devices[i] == null)
                                {
                                    m_devices.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
            }
        }

        public Vector3 MousePosition
        {
            get { return Input.mousePosition; }
        }

        public int DeviceCount
        {
            get
            {
                if(m_devices == null)
                {
                    m_gState = Dependencies.State;
                    m_devices = m_gState.GetValue<List<InputDevice>>(DevicesPersistentKey);
                }
                
                return m_devices != null ? m_devices.Count : 0;
            }
        }

        

        private void Start()
        {
            m_gState = Dependencies.State;
            m_progress = Dependencies.Progress;


            m_eventSystemManager = Dependencies.EventSystemManager;
            for (int i = 0; i < m_eventSystemManager.EventSystemCount; ++i)
            {
                IndependentEventSystem eventSystem = m_eventSystemManager.GetEventSystem(i);
                eventSystem.EventSystemUpdate += OnEventSystemUpdate;
            }
            m_eventSystemManager.CommonEventSystem.EventSystemUpdate += OnCommonEventSystemUpdate;

            m_isPointerOverGameObject = new bool[m_eventSystemManager.EventSystemCount];
            m_isInputFieldSelected = new bool[m_eventSystemManager.EventSystemCount];
            m_selectedGameObject = new GameObject[m_eventSystemManager.EventSystemCount];

            InputManager.OnDeviceAttached += OnDeviceAttached;
            InputManager.OnDeviceDetached += OnDeviceDetached;
            InputManager.OnActiveDeviceChanged += OnActiveDeviceChanged;

            if (!m_gState.HasKey(DevicesPersistentKey))
            {
                m_devices = new List<InputDevice>(InputManager.Devices.Count);
                m_gState.SetValue(DevicesPersistentKey, m_devices);
            }
            else
            {
                m_devices = m_gState.GetValue<List<InputDevice>>(DevicesPersistentKey);
                for (int i = 0; i < m_devices.Count; ++i)
                {
                    UnityInputDevice device = m_devices[i] as UnityInputDevice;
                    if(device == null)
                    {
                        if (m_devices[i] != null)
                        {
                            m_devices[i].IsSuspended = true;
                        }
                        m_devices[i] = null;
                    }
                    else
                    {
                        InputDevice replacementDevice = InputManager.Devices.OfType<UnityInputDevice>().Where(dev => dev.JoystickId == device.JoystickId).FirstOrDefault();
                        if(replacementDevice != null)
                        {
                            replacementDevice.IsSuspended = false;
                            m_devices[i] = replacementDevice;
                        }
                        else
                        {
                            if (m_devices[i] != null)
                            {
                                m_devices[i].IsSuspended = true;
                            }
                            m_devices[i] = null;
                        }
                    }
                }
            }

            LogDevices();
        }


        private void OnDestroy()
        {
            if(m_eventSystemManager != null)
            {
                for (int i = 0; i < m_eventSystemManager.EventSystemCount; ++i)
                {
                    IndependentEventSystem eventSystem = m_eventSystemManager.GetEventSystem(i);
                    eventSystem.EventSystemUpdate -= OnEventSystemUpdate;
                }

                m_eventSystemManager.CommonEventSystem.EventSystemUpdate -= OnCommonEventSystemUpdate;
            }
       
            InputManager.OnDeviceAttached -= OnDeviceAttached;
            InputManager.OnDeviceDetached -= OnDeviceDetached;
            InputManager.OnActiveDeviceChanged -= OnActiveDeviceChanged;
        }

        private void OnDeviceDetached(InputDevice detachedDevice)
        {
            detachedDevice.IsSuspended = true;

            if (IsInInitializationState)
            {
                int index = m_devices.IndexOf(detachedDevice);

                if(index >= 0)
                {
                    if (DeviceDisabled != null)
                    {
                        DeviceDisabled(index);
                    }
                    
                    m_devices.Remove(detachedDevice);
                }
            }
            else
            {
                for (int i = 0; i < m_devices.Count; ++i)
                {
                    if (m_devices[i] == detachedDevice)
                    {
                        if (DeviceDisabled != null)
                        {
                            DeviceDisabled(i);
                        }
                        m_devices[i] = null;
                        break;
                    }
                }
            }
          
            LogDevices();
        }

        private void OnDeviceAttached(InputDevice attachedDevice)
        {
            if(!IsInInitializationState)
            {
                for (int i = 0; i < m_devices.Count; ++i)
                {
                    if (m_devices[i] == null)
                    {
                        m_devices[i] = attachedDevice;
                        attachedDevice.IsSuspended = false;
                        if (DeviceEnabled != null)
                        {
                            DeviceEnabled(i);
                        }
                        break;
                    }
                }
            }
          
            LogDevices();
        }

        private void OnActiveDeviceChanged(InputDevice obj)
        {
            if(ActiveDeviceChanged != null)
            {
                ActiveDeviceChanged(obj);
            }
        }

        public int GetDeviceIndex(object device)
        {
            return m_devices.IndexOf(device as InputDevice);
        }

        public bool IsKeyboardAndMouse(int index)
        {
            if(m_devices == null || index >= m_devices.Count || index < 0)
            {
                return false;
            }

            return m_devices[index].Name == InControlKeyboardProfile.ProfileName;
        }

        public bool IsSuspended(int index)
        {
            if (m_devices == null || m_devices.Count == 0)
            {
                return true;
            }

            if(index < 0)
            {
                bool allSuspended = true;
                for(int i = 0; i < m_devices.Count; ++i)
                {
                    InputDevice device = m_devices[i];
                    if(device != null && !device.IsSuspended)
                    {
                        allSuspended = false;
                        break;
                    }
                }
                return allSuspended;
            }

            if(index >= m_devices.Count)
            {
                return true;
            }

            return m_devices[index].IsSuspended;
        }

        public void Resume(int index)
        {
            if (m_devices == null)
            {
                return;
            }
            InputDevice device = m_devices[index];
            if(device != null)
            {
                device.IsSuspended = false;
            }
        }

        public void Suspend(int index)
        {
            if(m_devices == null)
            {
                return;
            }
            InputDevice device = m_devices[index];
            if (device != null)
            {
                device.IsSuspended = true;
            }
        }

        public void SuspendAll()
        {
            if(m_devices != null)
            {
                for (int i = 0; i < m_devices.Count; ++i)
                {
                    Suspend(i);
                }
            } 
        }

        public void ResumeAll()
        {
            if (m_devices != null)
            {
                for (int i = 0; i < m_devices.Count; ++i)
                {
                    Resume(i);
                }
            }
        }

        public void ActivateAll()
        {
            if(m_devices == null)
            {
                m_devices = new List<InputDevice>();
                m_gState.SetValue(DevicesPersistentKey, m_devices);
            }

            m_devices.Clear();

            for(int i = 0; i < InputManager.Devices.Count; ++i)
            {
                InputDevice device = InputManager.Devices[i];
                device.IsSuspended = false;
                m_devices.Add(device);
                if (DeviceEnabled != null)
                {
                    DeviceEnabled(m_devices.Count - 1);
                }
            }
        }

        public void DeactivateDevice(int index)
        {
            if(!IsInInitializationState)
            {
                throw new System.InvalidOperationException("Is not in initialization state");
            }

            InputDevice device = m_devices[index];
            device.IsSuspended = true;

            if(m_lastDisabledDevices == null)
            {
                m_lastDisabledDevices = new List<InputDevice>();
                m_lastDisabledDevices.Add(device);
            }

            m_devices.RemoveAt(index);

            if (DeviceDisabled != null)
            {
                DeviceDisabled(index);
            }
        }

        private bool IsMasked(int player, bool isMaskedBySelectedUI, bool isMaskedByPointerOverUI)
        {
            if (isMaskedBySelectedUI)
            {
                if (player > -1)
                {
                    if (m_selectedGameObject[player] != null)
                    {
                        return true;
                    }
                }
                else
                {
                    if (m_commonSelectedGameObject != null)
                    {
                        return true;
                    }
                }
            }

            if (isMaskedByPointerOverUI)
            {
                if (player > -1)
                {
                    if (m_isPointerOverGameObject[player])
                    {
                        return true;
                    }
                }
                else
                {
                    if (m_commonPointerOverGameObject)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsAnyButtonDown(int player, bool isMaskedBySelectedUI, bool isMaskedByPointerOverUI)
        {
            if(IsMasked(player, isMaskedBySelectedUI, isMaskedByPointerOverUI))
            {
                return false;
            }

            if (IsSuspended(player))
            {
                return false;
            }

            //if(IsKeyboardAndMouse(player))
            //{
            //    return Input.anyKeyDown;
            //}

            InputDevice inputDevice;
            if (player >= 0)
            {
                inputDevice = (m_devices.Count > player) ? m_devices[player] : null;
            }
            else
            {
                inputDevice = InputManager.ActiveDevice;
            }

            if (inputDevice != null && !inputDevice.IsSuspended)
            {
                bool value = inputDevice.AnyButton.WasPressed || inputDevice.MenuWasPressed;
              
                return value;
            }
            return false;
        }

        public float GetAxisRaw(InputAction action, int player, bool isMaskedBySelectedUI, bool isMaskedByPointerOverUI)
        {
            if (IsMasked(player, isMaskedBySelectedUI, isMaskedByPointerOverUI))
            {
                return 0.0f;
            }
            InputDevice inputDevice;
            if (player >= 0)
            {
                inputDevice = (m_devices.Count > player) ? m_devices[player] : null;
            }
            else
            {
                inputDevice = InputManager.ActiveDevice;
            }

            if (inputDevice != null && !inputDevice.IsSuspended)
            {
                if (action == InputAction.MoveForward)
                {
                    float value = inputDevice.GetControl(m_mapping[action]).Value;
                    if (value == 0)
                    {
                        return inputDevice.DPadY;
                    }
                    return value;
                    
                }
                else if (action == InputAction.MoveSide)
                {
                    float value = inputDevice.GetControl(m_mapping[action]).Value;
                    if(value == 0)
                    {
                        return inputDevice.DPadX;
                    }
                    return value;
                    
                }
                else
                {
                    return inputDevice.GetControl(m_mapping[action]).Value;
                }

            }
            return 0;
        }



        public bool GetButton(InputAction action, int player, bool isMaskedBySelectedUI, bool isMaskedByPointerOverUI)
        {
            if (IsMasked(player, isMaskedBySelectedUI, isMaskedByPointerOverUI))
            {
                return false;
            }
            InputDevice inputDevice;
            if (player >= 0)
            {
                inputDevice = (m_devices.Count > player) ? m_devices[player] : null;
            }
            else
            {
                inputDevice = InputManager.ActiveDevice;
            }

            if (inputDevice != null && !inputDevice.IsSuspended)
            {
                bool value = inputDevice.GetControl(m_mapping[action]).IsPressed;
                if (action == InputAction.MoveForward)
                {
                    if (!value)
                    {
                        return inputDevice.DPad.Up.IsPressed || inputDevice.DPad.Down.IsPressed;
                    }
                }
                else if (action == InputAction.MoveSide)
                {
                    if (!value)
                    {
                        return inputDevice.DPad.Left.IsPressed || inputDevice.DPad.Right.IsPressed;
                    }
                }
                return value;
            }
            return false;
        }

        public bool GetButtonDown(InputAction action, int player, bool isMaskedBySelectedUI, bool isMaskedByPointerOverUI)
        {
            if (IsMasked(player, isMaskedBySelectedUI, isMaskedByPointerOverUI))
            {
                return false;
            }

            InputDevice inputDevice;
            if(player >= 0)
            {
                inputDevice = (m_devices.Count > player) ? m_devices[player] : null;
            }
            else
            {
                inputDevice = InputManager.ActiveDevice;
            }

            if (inputDevice != null && !inputDevice.IsSuspended)
            {
                bool value = inputDevice.GetControl(m_mapping[action]).WasPressed;
                if (action == InputAction.MoveForward)
                {
                    if (!value)
                    {
                        return inputDevice.DPad.Up.WasPressed || inputDevice.DPad.Down.WasPressed;
                    }
                }
                else if (action == InputAction.MoveSide)
                {
                    if (!value)
                    {
                        return inputDevice.DPad.Left.WasPressed || inputDevice.DPad.Right.WasPressed;
                    }
                }
                return value;
            }
            return false;
        }

        public bool GetButtonUp(InputAction action, int player, bool isMaskedBySelectedUI, bool isMaskedByPointerOverUI)
        {
            if (IsMasked(player, isMaskedBySelectedUI, isMaskedByPointerOverUI))
            {
                return false;
            }

            InputDevice inputDevice;
            if (player >= 0)
            {
                inputDevice = (m_devices.Count > player) ? m_devices[player] : null;
            }
            else
            {
                inputDevice = InputManager.ActiveDevice;
            }
            if (inputDevice != null && !inputDevice.IsSuspended)
            {
                bool value = inputDevice.GetControl(m_mapping[action]).WasReleased;
                if (action == InputAction.MoveForward)
                {
                    if (!value)
                    {
                        return inputDevice.DPad.Up.WasReleased || inputDevice.DPad.Down.WasReleased;
                    }
                }
                else if (action == InputAction.MoveSide)
                {
                    if (!value)
                    {
                        return inputDevice.DPad.Left.WasReleased || inputDevice.DPad.Right.WasReleased;
                    }
                }
                return value;
            }
            return false;
        }

        private void OnEventSystemUpdate()
        {
            IndependentEventSystem eventSystem = EventSystem.current as IndependentEventSystem;
            if(eventSystem == null)
            {
                return;
            }

            m_isPointerOverGameObject[eventSystem.Index] = eventSystem.IsPointerOverGameObject();

            GameObject selectedGameObject = m_selectedGameObject[eventSystem.Index];
            if (eventSystem.currentSelectedGameObject != selectedGameObject)
            {
                GameObject newGameObject = eventSystem.currentSelectedGameObject;
                m_selectedGameObject[eventSystem.Index] = eventSystem.currentSelectedGameObject;
                m_isInputFieldSelected[eventSystem.Index] = eventSystem.currentSelectedGameObject != null && eventSystem.currentSelectedGameObject.GetComponent<InputField>() != null;
            }
        }


        private void OnCommonEventSystemUpdate()
        {
            IndependentEventSystem eventSystem = EventSystem.current as IndependentEventSystem;
            if (eventSystem == null)
            {
                return;
            }

            m_commonPointerOverGameObject = eventSystem.IsPointerOverGameObject();

            GameObject selectedGameObject = m_commonSelectedGameObject;
            if (eventSystem.currentSelectedGameObject != selectedGameObject)
            {
                GameObject newGameObject = eventSystem.currentSelectedGameObject;
                m_commonSelectedGameObject = eventSystem.currentSelectedGameObject;
            }
        }

        private void Update()
        {
            if(IsInInitializationState)
            {
                for(int i = 0; i < InputManager.Devices.Count; ++i)
                {
                    InputDevice device = InputManager.Devices[i];
                    if(device.AnyButton.WasPressed)
                    {
                        if(!m_devices.Contains(device))
                        {
                            if(m_lastDisabledDevices == null || !m_lastDisabledDevices.Contains(device))
                            {
                                if(!m_progress.IsVisible)
                                {
                                    device.IsSuspended = false;
                                }
                               
                                m_devices.Add(device);
                                if (DeviceEnabled != null)
                                {
                                    DeviceEnabled(m_devices.Count - 1);
                                }
                            }
                            
                        }
                    }
                }
            }

            m_lastDisabledDevices = null;
        }

        private void LogDevices()
        {
            Debug.LogFormat("Attached Devices");
            for (int i = 0; i < InputManager.Devices.Count; ++i)
            {
                InputDevice device = InputManager.Devices[i];
                Debug.LogFormat("{0}. {1}", i, device.Name);
            }

            Debug.LogFormat("Enabled Devices");
            for (int i = 0; i < m_devices.Count; ++i)
            {
                InputDevice device = m_devices[i];
                if(device != null)
                {
                    Debug.LogFormat("{0}. {1}", i, device.Name);
                }
            }
        }

    }
}

