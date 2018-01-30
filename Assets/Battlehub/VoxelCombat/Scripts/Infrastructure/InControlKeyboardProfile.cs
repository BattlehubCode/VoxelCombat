using InControl;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class InControlKeyboardProfile : UnityInputDeviceProfile
    {
        public const string ProfileName = "Keyboard/Mouse";

        [SerializeField]
        private bool m_isMenu;

        public InControlKeyboardProfile()
        {
            Name = ProfileName;
            Meta = "A keyboard and mouse combination for menu.";

            // This profile only works on desktops.
            SupportedPlatforms = new[]
            {
                "Windows",
                "Mac",
                "Linux"
            };

            Sensitivity = 1.0f;
            LowerDeadZone = 0.0f;
            UpperDeadZone = 1.0f;

            ButtonMappings = new[]
            {
                new InputControlMapping
                {
                    Handle = "Select",
                    Target = InputControlType.LeftBumper,
					Source = KeyCodeButton( KeyCode.Space, KeyCode.LeftShift )
                },
                new InputControlMapping
                {
                    Handle = "Move",
                    Target = InputControlType.Action1,
					Source = KeyCodeButton( KeyCode.Return, KeyCode.W )
                },
                new InputControlMapping
                {
                    Handle = "Build",
                    Target = InputControlType.Action2,
					Source = KeyCodeButton( KeyCode.Q, KeyCode.B )
                },
                new InputControlMapping
                {
                    Handle = "Select Target",
                    Target = InputControlType.Action3,					
					Source = KeyCodeButton( KeyCode.A )
                },
                new InputControlMapping
                {
                    Handle = "Patrol",
                    Target = InputControlType.Action4,
					Source = KeyCodeButton( KeyCode.E )
                },

                new InputControlMapping
                {
                    Handle = "ToggleConsole",
                    Target = InputControlType.Button1,
					Source = KeyCodeButton( KeyCode.BackQuote )
                },

                new InputControlMapping
                {
                    Handle = "ToggleCursor",
                    Target = InputControlType.Button2,
					Source = KeyCodeButton( KeyCode.Keypad5 )
                },

                new InputControlMapping
                {
                    Handle = "EditorCreate",
                    Target = InputControlType.Button3,
					Source = MouseButton0
                },
                new InputControlMapping
                {
                    Handle = "EditorDestroy",
                    Target = InputControlType.Button4,
					Source = MouseButton1
                },
                new InputControlMapping
                {
                    Handle = "EditorPan",
                    Target = InputControlType.Button5,
					Source = MouseButton2
                },
                new InputControlMapping
                {
                    Handle = "EditorRotate",
                    Target = InputControlType.Button6,
					Source = KeyCodeButton( KeyCode.LeftAlt )
                },
                new InputControlMapping
                {
                    Handle = "Cancel",
                    Target = InputControlType.Button18,
                    Source = KeyCodeButton( KeyCode.Escape )
                },
                new InputControlMapping
                {
                    Handle = "Submit",
                    Target = InputControlType.Button19,
                    Source = KeyCodeButton(KeyCode.Return)
                },
                new InputControlMapping
                {
                    Handle = "Back",
                    Target = InputControlType.Back,
                    Source = KeyCodeButton( KeyCode.Escape )
                },
            };

            AnalogMappings = new[]
            {
                new InputControlMapping {
                    Handle = "Move X",
                    Target = InputControlType.LeftStickX,
                    Source = KeyCodeAxis( KeyCode.LeftArrow, KeyCode.RightArrow )
                },
                new InputControlMapping {
                    Handle = "Move Y",
                    Target = InputControlType.LeftStickY,
                    Source = KeyCodeAxis( KeyCode.DownArrow, KeyCode.UpArrow )
                },
                new InputControlMapping
                {
                    Handle = "Look Z",
                    Target = InputControlType.ScrollWheel,
                    Source = MouseScrollWheel,
                    Raw    = true,
                    Scale  = 0.1f
                }
            };
        }
    }
}

