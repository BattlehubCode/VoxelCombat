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
        [SerializeField]
        private RawImage m_foreground;
        [SerializeField]
        private RectTransform m_rtMapBounds;

        protected override void Awake()
        {
            base.Awake();
            m_minimap = Dependencies.Minimap;
            m_minimap.Loaded += OnLoaded;
            //m_minimap.ForegroundChanged += OnForegroundChanged;
            m_background.texture = m_minimap.Background;
            m_foreground.texture = m_minimap.Foreground;
        }
        protected override void Start()
        {
            base.Start();
            StartCoroutine(Fit());
        }

        private void Update()
        {
            m_rtMapBounds.rotation = Quaternion.Euler(new Vector3(0, 0, m_viewport.Camera.transform.eulerAngles.y));
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if(isActiveAndEnabled)
            {
                StartCoroutine(Fit());
            }
        }

        private IEnumerator Fit()
        {
            yield return new WaitForEndOfFrame();

            RectTransform parentRT = (RectTransform)m_rtMapBounds.parent;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parentRT);
            float radius = bounds.extents.x;
            float offset = radius - radius * Mathf.Sqrt(2.0f) / 2.0f;

            m_rtMapBounds.offsetMin = new Vector2(offset, offset);
            m_rtMapBounds.offsetMax = new Vector2(-offset, -offset);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_minimap != null)
            {
                m_minimap.Loaded -= OnLoaded;
                //m_minimap.ForegroundChanged -= OnForegroundChanged;
            }
        }

        private void OnLoaded(object sender, System.EventArgs e)
        {
            m_background.texture = m_minimap.Background;
            m_foreground.texture = m_minimap.Foreground;
        }

        //private void OnForegroundChanged(object sender, System.EventArgs e)
        //{
        //    m_foreground.texture = m_minimap.Foreground;
        //}
    }
}
