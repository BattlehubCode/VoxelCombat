using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public enum InputAction
    {
        //Infrastructure
        ToggleConsole = 0,
        ToggleMenu = 1,
        Quit = 2,
        SaveReplay = 3,

        //Game
        MoveForward = 10,
        MoveSide = 15,

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
        Action9 = 111,
        Action0 = 112,
        Cancel = 113,
        Submit = 114,

        //MapEditor
        EditorCreate = 500,
        EditorDestroy = 510,
        EditorPan = 520,
        EditorRotate = 530,

        //Debug
        ToggleCursor = 1000,

        

    }
    public class CommandRouter : MonoBehaviour
    {
        private IConsole m_console;
        private IGameView m_gameView;
        private IVoxelGame m_game;

        private void Awake()
        {
            m_console = Dependencies.Console;
            m_gameView = Dependencies.GameView;
            m_game = Dependencies.GameState;
            
            
            m_console.Command += OnCommand;
        }

        private void OnDestroy()
        {
            m_console.Command -= OnCommand;
        }

        private void OnCommand(string cmd, params string[] args)
        {
            InputAction commonCmd;
            PlayerUnitConsoleCmd playerUnitCmd;
            if (cmd.TryParse(true, out playerUnitCmd))
            {
                int playerIndex = -1;
                if(args.Length > 0 && args[0].StartsWith("p"))
                {
                    if(args[0] == "p0")
                    {
                        playerIndex = 0;
                    }
                    else if(args[0] == "p1")
                    {
                        playerIndex = 1;
                    }
                    else if(args[0] == "p2")
                    {
                        playerIndex = 2;
                    }
                    else if(args[0] == "p3")
                    {
                        playerIndex = 3;
                    }
                }

                if(playerIndex >= 0)
                {
                    for (int i = 0; i < args.Length - 1; ++i)
                    {
                        args[i] = args[i + 1];
                    }

                    Array.Resize(ref args, args.Length - 1);
                    IPlayerUnitController playerUnitController = m_gameView.GetUnitController(playerIndex);
                    if (playerUnitController != null)
                    {
                        playerUnitController.SubmitConsoleCommand(playerUnitCmd, args, m_console);
                    }
                    else
                    {
                        m_console.Echo("PlayerController for player " + playerIndex + " does not exist");
                    }
                }
                else
                {
                    int playersCount = m_game.LocalPlayersCount;
                    for (int i = 0; i < playersCount; ++i)
                    {
                        IPlayerUnitController playerUnitController = m_gameView.GetUnitController(i);
                        if (playerUnitController != null)
                        {
                            playerUnitController.SubmitConsoleCommand(playerUnitCmd, args, m_console);
                        }
                    }
                }
            }
            else if (cmd.TryParse(true, out commonCmd))
            {
                if (commonCmd == InputAction.Quit)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    Application.Quit();
                }
                else if(commonCmd == InputAction.SaveReplay)
                {
                    string replayName = "Replay";
                    if (args.Length > 0) 
                    {
                        replayName = args[0];
                    }

                    m_game.SaveReplay(replayName);
                }
            }
        }     
    }

    public static class EnumExt
    {
        public static bool TryParse<TEnum>(this string value, bool ignoreCase, out TEnum result)
            where TEnum : struct, IConvertible
        {
            var retValue = value == null ?
                false :
                Enum.IsDefined(typeof(TEnum), value);
            result = retValue ? (TEnum)Enum.Parse(typeof(TEnum), value) : default(TEnum);

            if (!retValue && ignoreCase)
            {
                string[] names = Enum.GetNames(typeof(TEnum));
                for (int i = 0; i < names.Length; ++i)
                {
                    if (string.Compare(names[i], value, true) == 0)
                    {
                        result = (TEnum)Enum.Parse(typeof(TEnum), names[i]);
                        retValue = true;
                        break;
                    }
                }
            }

            return retValue;
        }
    }

}
