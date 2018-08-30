using UnityEngine;

namespace Battlehub.VoxelCombat
{
    [ExecuteInEditMode]
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer m_ui;

        //private float m_fillProgress = 0;

        [SerializeField]
        private float m_progress;
        public float Progress
        {
            get { return m_progress; }
            set
            {
                float newValue =  Mathf.Clamp(value, 0, 1);
                if(m_progress != newValue)
                {
                    m_progress = newValue;
                    //if(m_progress < m_fillProgress)
                    //{
                    //    m_fillProgress = m_progress;
                    //}
                    //if (Animate)
                    //{
                    //    enabled = true;
                    //}
                    //else
                    //{
                    //    m_fillProgress = m_progress;
                    //}
                    m_ui.transform.localScale = new Vector3(m_progress, 1, 1);
                }
            }
        }

        [SerializeField]
        private bool m_animate = true;
        public bool Animate
        {
            get { return m_animate; }
            set { m_animate = value; }
        }

        private void Awake()
        {
            m_ui.transform.localScale = new Vector3(m_progress, 1, 1);
            
        }

        //private void Update()
        //{
        //    m_fillProgress = Mathf.Lerp(m_fillProgress, m_progress, Time.deltaTime);
        //    m_ui.transform.localScale = new Vector3(Mathf.Lerp(m_ui.transform.localScale.x, 1, m_fillProgress), 1, 1);
        //}
    }
}

