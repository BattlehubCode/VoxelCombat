using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public interface IProgressIndicator
    {
        bool IsVisible
        {
            get;
            set;
        }

        void SetText(string text);
        
        IProgressIndicator GetChild(int index);
    }

    public class ProgressIndicator : MonoBehaviour, IProgressIndicator
    {
        private static ProgressIndicator m_root;

        private readonly List<ProgressIndicator> m_children = new List<ProgressIndicator>();

        [SerializeField]
        private Text m_text;

        [SerializeField]
        private GameObject m_target;

        private int m_counter;

        public bool IsVisible
        {
            get { return m_target.activeSelf; }
            set
            {
                if(m_counter == 0 && !value)
                {
                    return;
                }

                if(m_counter == 1 && !value)
                {
                    if(m_index == -1)
                    {
                        m_inputManager.ResumeAll();
                    }
                    else
                    {
                        m_inputManager.Resume(m_index);
                    }
                    
                    m_target.SetActive(value);
                    m_counter--;
                }
                else if(m_counter == 0 && value)
                {
                    if(m_index == -1)
                    {
                        m_inputManager.SuspendAll();
                    }
                    else
                    {
                        m_inputManager.Suspend(m_index);
                    }

                    m_target.SetActive(value);
                    m_counter++;
                }
                else
                {
                    if(value)
                    {
                        m_counter++;
                    }
                    else
                    {
                        m_counter--;
                    }
                }
            }
        }

        private int m_index = -1;

        private IVoxelInputManager m_inputManager;

        public void SetText(string text)
        {
            m_text.text = text;
        }

        private void Awake()
        {
            m_inputManager = Dependencies.InputManager;
            m_target.SetActive(false);
            m_counter = 0;
        }

        private void Start()
        {    
            if(m_root == null)
            {
                m_root = this;
            }
        }

        private void OnEnable()
        {
            if(m_root != null && m_root != this)
            {
                m_root.m_children.Add(this);
                for (int i = 0; i < m_root.m_children.Count; ++i)
                {
                    m_root.m_children[i].m_index = i;
                }

            }
        }

        private void OnDisable()
        {
            if(m_root != null && m_root != this)
            {
                m_root.m_children.Remove(this);
                for (int i = 0; i < m_root.m_children.Count; ++i)
                {
                    m_root.m_children[i].m_index = i;
                }
            }
        }

        private void OnDestroy()
        {
            if(m_root == this)
            {
                m_root = null;
            }
        }

        public IProgressIndicator GetChild(int index)
        {
            for(int i = 0; i < m_children.Count; ++i)
            {
                ProgressIndicator progress = m_children[i];
                if(progress != null)
                {
                    PlayerUIZone zone = progress.GetComponentInParent<PlayerUIZone>();
                    if(zone.LocalPlayerIndex == index)
                    {
                        return progress;
                    }
                }
            }

            return null;
        }
    }
}

