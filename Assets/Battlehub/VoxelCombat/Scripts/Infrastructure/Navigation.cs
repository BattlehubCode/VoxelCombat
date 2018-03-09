using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{

    public interface INavigation
    {
        Dictionary<string, object> Args
        {
            get;
        }

        void ClearHistory();

        string Current
        {
            get;
        }

        GameObject CurrentMenu
        {
            get;
        }

        string PrevSceneName
        {
            get;
        }
     
        void Navigate(string target);

        void Navigate(string scene, string target, Dictionary<string, object> args);

        bool CanGoBack
        {
            get;
        }
        void GoBack();

        
    }

    public class Navigation : MonoBehaviour, INavigation
    {
        [SerializeField]
        private GameObject[] m_menus;

        private IGlobalState GState
        {
            get { return Dependencies.State; }
        }
        
        private Stack<string> m_localNavigationStack = new Stack<string>();

        private string m_current;
        public string Current
        {
            get { return m_current; }
        }

        public GameObject CurrentMenu
        {
            get { return m_menus.Where(m => m.name == m_current).FirstOrDefault(); }
        }

        
        public string PrevSceneName
        {
            get { return GState.GetValue<string>("Battlehub.VoxelCombat.Navigation.prevScene"); }
            private set { GState.SetValue("Battlehub.VoxelCombat.Navigation.prevScene", value); }
        }

        public Dictionary<string, object> Args
        {
            get { return GState.GetValue<Dictionary<string, object>>("Battlehub.VoxelCombat.Navigation.args"); }
            private set { GState.SetValue("Battlehub.VoxelCombat.Navigation.args", value); }
        }

        private string Target
        {
            get { return GState.GetValue<string>("Battlehub.VoxelCombat.Navigation.target"); }
            set { GState.SetValue("Battlehub.VoxelCombat.Navigation.target", value); }
        }

        private void Awake()
        {
            if (m_menus == null || m_menus.Length == 0)
            {
                //Debug.LogError("Set menus array");
                return;
            }
           
            if(Target != null)
            {
                m_current = m_menus.First().name;
                m_localNavigationStack.Push(m_current);
                Load(Target);
                Target = null;
            }
            else
            {
                m_current = m_menus.First().name;
                Load(m_current);
            }
        }
        


        public void ClearHistory()
        {
            m_localNavigationStack.Clear();
        }

        public void Navigate(string scene, string target, Dictionary<string, object> args)
        {
            Args = args;
            Target = target;
            m_localNavigationStack.Clear();
            SceneManager.LoadScene(scene);
        }

        public void Navigate(string target)
        {
            Args = null;            

            m_localNavigationStack.Push(m_current);

            Load(target);

            m_current = target;
        }

        public bool CanGoBack
        {
            get { return m_localNavigationStack.Count > 0; }
        }

        public void GoBack()
        {
            if(CanGoBack)
            {
                m_current = m_localNavigationStack.Pop();
                Load(m_current);
            }
        }

        private void Load(string target)
        {
            GameObject menu = m_menus.Where(m => m.name == target).FirstOrDefault();
            if (menu != null)
            {
                for (int i = 0; i < m_menus.Length; ++i)
                {
                    m_menus[i].SetActive(false);
                }

                menu.SetActive(true);
            }
            else
            {
                m_localNavigationStack.Clear();
                PrevSceneName = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene(target);
            }
        }

    }
}

