using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class PlayerMinimap : UIBehaviour
    {
        private IVoxelMinimapRenderer m_minimap;

        [SerializeField]
        private GameViewport m_viewport;
        [SerializeField]
        private RawImage m_background;
        private RectTransform m_backgroundRT;

        protected override void Awake()
        {
            base.Awake();
            m_minimap = Dependencies.Minimap;
            m_minimap.Loaded += OnLoaded;
            m_background.texture = m_minimap.Background;

            m_backgroundRT = m_background.GetComponent<RectTransform>();
        }

        protected override void Start()
        {
            base.Start();
            StartCoroutine(Fit());
        }

        private void Update()
        {
            m_backgroundRT.rotation = Quaternion.Euler(new Vector3(0, 0, m_viewport.Camera.transform.eulerAngles.y));
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            StartCoroutine(Fit());
        }

        private IEnumerator Fit()
        {
            yield return new WaitForEndOfFrame();

            RectTransform parentRT = (RectTransform)m_backgroundRT.parent;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parentRT);
            float radius = bounds.extents.x;
            float offset = radius - radius * Mathf.Sqrt(2.0f) / 2.0f;

            m_backgroundRT.offsetMin = new Vector2(offset, offset);
            m_backgroundRT.offsetMax = new Vector2(-offset, -offset);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_minimap != null)
            {
                m_minimap.Loaded -= OnLoaded;
            }
        }

        private void OnLoaded(object sender, System.EventArgs e)
        {
            m_background.texture = m_minimap.Background;
        }
    }
}
