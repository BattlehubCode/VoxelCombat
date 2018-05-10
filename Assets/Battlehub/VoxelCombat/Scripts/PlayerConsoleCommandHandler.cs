using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class PlayerConsoleCommandHandler : MonoBehaviour
    {
        public int LocalPlayerIndex
        {
            get;
            set;
        }

        private IConsole m_console;
        private IUnitSelection m_unitSelection;
        private IVoxelGame m_gameState;

        private void Start()
        {
            m_gameState = Dependencies.GameState;
            m_unitSelection = Dependencies.UnitSelection;
        }

        public void Initialize()
        {
            if(m_console == null)
            {
                m_console = Dependencies.Console.GetChild(LocalPlayerIndex);
                m_console.Command += OnCommand;
            }
        }

        private void OnDestroy()
        {
            if(m_console != null)
            {
                m_console.Command -= OnCommand;
            }
        }

        private void OnCommand(IConsole console, string cmd, params string[] args)
        {
            if(cmd == "unitinfo")
            {
                int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);
                long[] selection = m_unitSelection.GetSelection(playerIndex, playerIndex);
               
                for (int i = 0; i < selection.Length; ++i)
                {
                    long unitIndex = selection[i];
                    IVoxelDataController dataController = m_gameState.GetVoxelDataController(playerIndex, unitIndex);

                    m_console.Echo(string.Format("unit = {0}, type = {1}",
                        unitIndex,
                        dataController.ControlledData.Type));

                    m_console.Echo(string.Format("coord = {0}", dataController.Coordinate));
                    m_console.Echo(string.Format("health = {0}", dataController.ControlledData.Health));
                    m_console.Echo("----------------------------------------------------------------------------");
                }
            }
            else if(cmd == "playerinfo")
            {
                int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);
                PlayerStats playerStats = m_gameState.GetStats(playerIndex);
                Player player = m_gameState.GetPlayer(playerIndex);
           
                m_console.Echo(string.Format("player index = {0} ", playerIndex));
                m_console.Echo(string.Format("player id = {0} ", player.Id));
                m_console.Echo(string.Format("player name = {0} ", player.Name));
                m_console.Echo(string.Format("bot type = {0} ", player.BotType));
                m_console.Echo(string.Format("ctrl units count = {0} ", playerStats.ControllableUnitsCount));
                m_console.Echo(string.Format("is in room = {0} ", playerStats.IsInRoom));

                var unitsByType = m_gameState.GetUnits(playerIndex).Select(unitIndex => m_gameState.GetVoxelDataController(playerIndex, unitIndex)).GroupBy(unit => unit.ControlledData.Type);
                foreach (var unitGroup in unitsByType)
                {
                    m_console.Echo(string.Format("units of type {0} = {1} ", unitGroup.Key, unitGroup.Count()));
                }

                m_console.Echo("----------------------------------------------------------------------------");

            }
            else if(cmd == "quit")
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif

                Application.Quit();
            }
            else
            {
                cmd = cmd.ToLower();
                cmd = char.ToUpper(cmd[0]) + cmd.Substring(1);
                if(Enum.GetNames(typeof(PlayerUnitConsoleCmd)).Contains(cmd))
                {
                    IPlayerUnitController unitController = Dependencies.GameView.GetUnitController(LocalPlayerIndex);
                    unitController.SubmitConsoleCommand((PlayerUnitConsoleCmd)Enum.Parse(typeof(PlayerUnitConsoleCmd), cmd), args, console);
                }
            }
        }
    }

}

