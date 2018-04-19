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

        [SerializeField]
        private int m_index = -1;
        public int Index
        {
            get { return m_index; }
        }

        //[SerializeField]
        private bool m_isRoot = false;

        private GameObject m_selectGO;
        private bool m_selectOnLateUpdate;

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
                base.SetSelectedGameObject(firstSelectedGameObject);
                ExecuteEvents.Execute(firstSelectedGameObject, null, ExecuteEvents.selectHandler);

                m_wasEnabledInCurrentFrame = false;
            }

            if(m_selectOnLateUpdate)
            {
                SetSelectedGameObject(m_selectGO);
                m_selectGO = null;
                m_selectOnLateUpdate = false;
            }

            if (EventSystemLateUpdate != null)
            {
                EventSystemLateUpdate();
            }
            current = m_root;
        }

        public void SetSelectedGameObjectOnLateUpdate(GameObject selectGO)
        {
            m_selectGO = selectGO;            
            m_selectOnLateUpdate = true;
        }


        public override string ToString()
        {
            return name;
        }
    }

}
