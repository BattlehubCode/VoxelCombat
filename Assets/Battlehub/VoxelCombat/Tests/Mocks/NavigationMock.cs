using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat.Tests
{
    public class NavigationMock : MonoBehaviour, INavigation
    {
        private Dictionary<string, object> m_args = new Dictionary<string, object>();
        public Dictionary<string, object> Args
        {
            get { return m_args; }
        }

        public bool CanGoBack
        {
            get;
            set;
        }

        public string Current
        {
            get;
            set;
        }

        public GameObject CurrentMenu
        {
            get;
            set;
        }

      
        public string PrevSceneName
        {
            get;
            set;
        }

        public void ClearHistory()
        {
        }

        public void GoBack()
        {
        }

        public void Navigate(string target)
        {
        }

        public void Navigate(string scene, string target, Dictionary<string, object> args)
        {
        }
    }
}
