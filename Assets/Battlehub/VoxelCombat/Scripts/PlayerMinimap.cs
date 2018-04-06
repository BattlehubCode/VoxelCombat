using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Battlehub.VoxelCombat
{
    public class PlayerMinimap : UIBehaviour
    {
        private IVoxelMinimapRenderer m_minimap;
        private IPlayerCameraController m_cameraController;
        private IVoxelInputManager m_input;
        
        [SerializeField]
        private GameViewport m_viewport;
        [SerializeField]
        private RawImage m_background;
        [SerializeField]
        private RawImage m_foreground;
        [SerializeField]
        private UILineRenderer m_frustumProjection;
        [SerializeField]
        private RectTransform m_rtMapBounds;
        private float m_rootRadius;
        private Vector2 m_prevCursor;
        private bool m_manipulating;
        private Vector3 m_prevCamPos;
        private Quaternion m_prevCamRot;


        protected override void Awake()
        {
            base.Awake();

            m_minimap = Dependencies.Minimap;
            m_input = Dependencies.InputManager;

            m_minimap.Loaded += OnLoaded;
            m_background.texture = m_minimap.Background;
            m_foreground.texture = m_minimap.Foreground;

            m_frustumProjection.Points = new Vector2[5];
        }
        protected override void Start()
        {
            m_cameraController = Dependencies.GameView.GetCameraController(m_viewport.LocalPlayerIndex);
    
            base.Start();
            StartCoroutine(Fit());
        }

        private void Update()
        {
            Transform camTransform = m_viewport.Camera.transform;

            if (camTransform.position != m_prevCamPos || camTransform.rotation != m_prevCamRot)
            {
                m_prevCamPos = camTransform.position;
                m_prevCamRot = camTransform.rotation;

                float angle = camTransform.eulerAngles.y;
                m_rtMapBounds.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

                ProjectCamera(m_rtMapBounds.rotation);
            }

            if (m_input.GetButtonDown(InputAction.LMB, m_viewport.LocalPlayerIndex))
            {
                Vector2 pt;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)m_rtMapBounds.parent, m_cameraController.VirtualMousePosition, null, out pt))
                {
                    float normalizedDistance = pt.magnitude / m_rootRadius;
                    if (normalizedDistance < 1)
                    {
                        m_manipulating = true;
                        m_cameraController.VirtualMouseSensitivityScale = 0.2f;
                    }
                }
            }
            if (m_input.GetButtonUp(InputAction.LMB, m_viewport.LocalPlayerIndex))
            {
                m_manipulating = false;
                m_cameraController.VirtualMouseSensitivityScale = 1.0f;
            }

            if (m_manipulating)
            {
                if (m_input.GetButton(InputAction.LMB, m_viewport.LocalPlayerIndex))
                {
                    if (m_cameraController.VirtualMousePosition != m_prevCursor)
                    {
                        Vector2 pt;
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)m_rtMapBounds.parent, m_cameraController.VirtualMousePosition, null, out pt))
                        {
                            float normalizedDistance = pt.magnitude / m_rootRadius;
                            if (normalizedDistance < 1)
                            {
                                float angle = camTransform.eulerAngles.y;
                                Vector3 dir = Quaternion.Euler(new Vector3(0, angle, 0)) * new Vector3(pt.x, 0, pt.y).normalized;
                                m_cameraController.SetMapPivot(dir, normalizedDistance);
                            }
                        }
                        m_prevCursor = m_cameraController.VirtualMousePosition;
                    }
                }
            }
        }

        private void ProjectCamera(Quaternion rotation)
        {
            Camera camera = m_viewport.Camera;

            Plane p = new Plane(Vector3.up, Vector3.zero);

            Ray r0 = camera.ViewportPointToRay(new Vector3(0, 0, 0));
            Ray r1 = camera.ViewportPointToRay(new Vector3(0, 1, 0));
            Ray r2 = camera.ViewportPointToRay(new Vector3(1, 1, 0));
            Ray r3 = camera.ViewportPointToRay(new Vector3(1, 0, 0));

            float distance;
            Debug.Assert(p.Raycast(r0, out distance));
            Vector3 p0 = r0.GetPoint(distance) - m_cameraController.BoundsCenter;
            Debug.Assert(p.Raycast(r1, out distance));
            Vector3 p1 = r1.GetPoint(distance) - m_cameraController.BoundsCenter;
            Debug.Assert(p.Raycast(r2, out distance));
            Vector3 p2 = r2.GetPoint(distance) - m_cameraController.BoundsCenter;
            Debug.Assert(p.Raycast(r3, out distance));
            Vector3 p3 = r3.GetPoint(distance) - m_cameraController.BoundsCenter;

            float scale = m_rootRadius / m_cameraController.BoundsRadius;
            p0 *= scale;
            p1 *= scale;
            p2 *= scale;
            p3 *= scale;
     
            p0.y = p0.z;
            p1.y = p1.z;
            p2.y = p2.z;
            p3.y = p3.z;
            p0.z = 0;
            p1.z = 0;
            p2.z = 0;
            p3.z = 0;

            p0 = rotation * p0;
            p1 = rotation * p1;
            p2 = rotation * p2;
            p3 = rotation * p3;

            //Maybe replace with line renderer?

            m_frustumProjection.Points[0] = p0;
            m_frustumProjection.Points[1] = p1;
            m_frustumProjection.Points[2] = p2;
            m_frustumProjection.Points[3] = p3;
            m_frustumProjection.Points[4] = p0;
            m_frustumProjection.SetAllDirty();
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
            yield return new WaitForEndOfFrame();

            RectTransform parentRT = (RectTransform)m_rtMapBounds.parent;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parentRT);
            m_rootRadius = bounds.extents.x;
            
            float offset = m_rootRadius - m_rootRadius * Mathf.Sqrt(2.0f) / 2.0f;
            m_rtMapBounds.offsetMin = new Vector2(offset, offset);
            m_rtMapBounds.offsetMax = new Vector2(-offset, -offset);

            ProjectCamera(m_rtMapBounds.rotation);
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
            m_foreground.texture = m_minimap.Foreground;
        }
    }
}
