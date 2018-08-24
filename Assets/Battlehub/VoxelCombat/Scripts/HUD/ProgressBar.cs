using UnityEngine;

namespace Battlehub.VoxelCombat
{
    [ExecuteInEditMode]
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer m_ui;

        private float m_fillProgress = 0;

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
                    m_fillProgress = 0;
                    if (Animate)
                    {
                        enabled = true;
                    }
                    else
                    {
                        m_ui.transform.localScale = new Vector3(m_progress, 1, 1);
                    }                    
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

        private void Update()
        {
            if(Mathf.Approximately(m_ui.transform.localScale.x, m_progress))
            {
                enabled = false;
            }

            m_fillProgress += Time.deltaTime;
            m_ui.transform.localScale = new Vector3(Mathf.Lerp(m_ui.transform.localScale.x, m_progress, m_fillProgress), 1, 1);
        }
    }
}

