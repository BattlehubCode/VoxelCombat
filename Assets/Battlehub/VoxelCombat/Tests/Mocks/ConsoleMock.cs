using UnityEngine;

namespace Battlehub.VoxelCombat.Tests
{
    public class ConsoleMock : MonoBehaviour, IConsole
    {
        public int LocalPlayerIndex
        {
            get;
            set;
        }

        public event ConsoleCommandHandler Command;
        public void RaiseCommand(string cmd, params string[] args)
        {
            if(Command != null)
            {
                Command(this, cmd, args);
            }
        }

        public void Echo(string message)
        {
        }

        public IConsole GetChild(int index)
        {
            return null;
        }

        public void Write(string message)
        {
        }   
    }
}