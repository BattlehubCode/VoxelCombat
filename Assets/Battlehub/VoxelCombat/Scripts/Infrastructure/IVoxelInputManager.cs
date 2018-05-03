using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public enum InputAction
    {
        //Infrastructure
        ToggleConsole = 0,
        Quit = 2,
        SaveReplay = 3,

        //Game
        MoveForward = 10,
        MoveSide = 15,
        DPadLeft = 16,
        DPadRight = 17,
        DPadUp = 18,
        DPadDown = 19,

        MouseX = 90,
        MouseY = 91,

        CursorX = 100,
        CursorY = 101,

        Zoom = 102,
        LB = 103,
        RB = 104,
        A = 105,
        X = 106,
        B = 107,
        Action6 = 108,
        Action7 = 109,
        Y = 110,
        Start = 111,
        Back = 112,
        Cancel = 113,
        Submit = 114,
        LT = 115,
        RT = 116,
        RMB = 117,
        LMB = 118,
        MMB = 119,
        LeftStickButton = 120,
        RightStickButton = 121,

        //MapEditor
        EditorCreate = 500,
        EditorDestroy = 510,
        EditorPan = 520,
        EditorRotate = 530,

        //Debug
        ToggleCursor = 1000,
    }

    [Serializable]
    public class InputBinding
    {
        public string Name;
        public InputAction Action;
        public string AxisName;
        public string AltAxisName;
        public bool isMaskedByUI = true;
        public int Player;
    }

    public delegate void InputEventHandler<T>(T arg);

    public interface IVoxelInputManager
    {
        event InputEventHandler<int> DeviceEnabled;
        event InputEventHandler<int> DeviceDisabled;

        bool IsInInitializationState
        {
            get;
            set;
        }

        int DeviceCount
        {
            get;
        }

        Vector3 MousePosition
        {
            get;
        }

        int GetDeviceIndex(object device);

        bool IsSuspended(int index);

        void Resume(int index);

        void Suspend(int index);

        void ResumeAll();

        void SuspendAll();

        void ActivateAll();

        void DeactivateDevice(int index);

        bool IsKeyboardAndMouse(int index);

        bool IsAnyButtonDown(int player, bool isMaskedBySelectedUI = true, bool isMaskedByPointerOverUI = true);

        float GetAxisRaw(InputAction action, int player, bool isMaskedBySelectedUI = true, bool isMaskedByPointerOverUI = true);

        bool GetButtonDown(InputAction action, int player, bool isMaskedBySelectedUI = true, bool isMaskedByPointerOverUI = true);

        bool GetButton(InputAction action, int player, bool isMaskedBySelectedUI = true, bool isMaskedByPointerOverUI = true);

        bool GetButtonUp(InputAction action, int player, bool isMaskedBySelectedUI = true, bool isMaskedByPointerOverUI = true);
    }

}