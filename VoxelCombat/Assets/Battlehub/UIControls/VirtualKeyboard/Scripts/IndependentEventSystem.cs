using UnityEngine;
using UnityEngine.EventSystems;

namespace Battlehub.UIControls
{
    public delegate void IndependentEventHandler();

    [DefaultExecutionOrder(-1999)]
    public class IndependentEventSystem : EventSystem
    {
        public event IndependentEventHandler EventSystemUpdate;
        public event IndependentEventHandler EventSystemLateUpdate;

        private static EventSystem m_root;


        //[SerializeField]
        private bool m_isRoot = false;

        protected override void Awake()
        {
            base.Awake();
            if(m_isRoot)
            {
                if (m_root == null)
                {
                    m_root = this;
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            if(m_root == null)
            {
                m_root = this;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if(m_root == this)
            {
                m_root = null;
            }
        }

        private bool m_wasEnabledInCurrentFrame;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_wasEnabledInCurrentFrame = true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void Update()
        {
            if (m_root == null)
            {
                m_root = this;
            }


            current = this;
            base.Update();
            if(EventSystemUpdate != null)
            {
                EventSystemUpdate();
            }
            current = m_root;
        }

        private void LateUpdate()
        {
            current = this;        
            if (m_wasEnabledInCurrentFrame && firstSelectedGameObject)
            {
                SetSelectedGameObject(firstSelectedGameObject);
                ExecuteEvents.Execute(firstSelectedGameObject, null, ExecuteEvents.selectHandler);

                m_wasEnabledInCurrentFrame = false;
            }
            if (EventSystemLateUpdate != null)
            {
                EventSystemLateUpdate();
            }
            current = m_root;
        }

        public override string ToString()
        {
            return name;
        }
    }

}
