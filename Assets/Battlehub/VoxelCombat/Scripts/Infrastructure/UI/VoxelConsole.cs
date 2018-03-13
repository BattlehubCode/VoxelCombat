using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Battlehub.VoxelCombat
{
    public delegate void ConsoleCommandHandler(IConsole console, string cmd, params string[] args);

    public interface IConsole
    {
        event ConsoleCommandHandler Command;

        int LocalPlayerIndex
        {
            get;
        }

        IConsole GetChild(int index);
      
        void Echo(string message);
        void Write(string message);
    }

    public class VoxelConsole : MonoBehaviour, IConsole
    {
        public event ConsoleCommandHandler Command;

        private Dictionary<int, IConsole> m_children = new Dictionary<int, IConsole>();

        public int LocalPlayerIndex
        {
            get { return -1; }
        }

        public void Initialize()
        {
            m_children = GetComponentsInChildren<ConsolePanel>(true).ToDictionary(cp => cp.LocalPlayerIndex, cp => (IConsole)cp);
            foreach(IConsole console in m_children.Values)
            {
                console.Command += OnConsoleCommand;
            }
        }

        private void OnConsoleCommand(IConsole console, string cmd, params string[] args)
        {
            if(Command != null)
            {
                Command(console, cmd, args);
            }
        }

        public void Echo(string message)
        {
            foreach(IConsole console in m_children.Values)
            {
                console.Echo(message);
            }
        }

        public void Write(string message)
        {
            foreach (IConsole console in m_children.Values)
            {
                console.Write(message);
            }
        }
        public IConsole GetChild(int index)
        {
            IConsole result = null;
            if(m_children.TryGetValue(index, out result))
            {
                return result;
            }
            return null;
        }
    }
}

