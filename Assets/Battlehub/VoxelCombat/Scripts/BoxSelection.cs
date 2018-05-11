using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public enum BoxSelectionMethod
    {
        LooseFitting,
        BoundsCenter,
        TansformCenter
    }

    public class FilteringArgs : EventArgs
    {
        private bool m_cancel;

        public bool Cancel
        {
            get { return m_cancel; }
            set
            {
                if (value) //can't reset cancel flag
                {
                    m_cancel = true;
                }
            }
        }

        public GameObject Object
        {
            get;
            set;
        }

        public void Reset()
        {
            m_cancel = false;
        }
    }

    public class BoxSelectEventArgs : EventArgs
    {
        public GameObject[] Result
        {
            get;
            private set;
        }

        public BoxSelectEventArgs(GameObject[] result)
        {
            Result = result;
        }
    }

    public interface IBoxSelector
    {
        void Activate();

        bool IsActive
        {
            get;
        }

        event EventHandler<FilteringArgs> Filtering;
        event EventHandler<BoxSelectEventArgs> Selected;
    }


    /// <summary>
    /// Box Selection
    /// </summary>
    public class BoxSelection : MonoBehaviour, IBoxSelector
    {
        [SerializeField]
        private GameViewport m_viewport;

        private IVoxelInputManager m_inputManager;
        private IVirtualMouse m_mouse;
        
        [SerializeField]
        private Image m_image;
        [SerializeField]
        private RectTransform m_rectTransform;
        [SerializeField]
        private BoxSelectionMethod m_method;

        private bool m_isDragging;
        private Vector2 m_startMousePosition;
        private Vector2 m_startPt;
        private Vector2 m_endPt;

        public event EventHandler<FilteringArgs> Filtering;
        public event EventHandler<BoxSelectEventArgs> Selected;

        public bool IsActive
        {
            get { return enabled; }
        }

        public void Activate()
        {
            enabled = true;
        }

        private void Awake()
        {      
            m_image.type = Image.Type.Sliced;
            m_image.raycastTarget = false;

            m_rectTransform.sizeDelta = new Vector2(0, 0);
            m_rectTransform.pivot = new Vector2(0, 0);
            m_rectTransform.anchoredPosition = new Vector3(0, 0);

            m_inputManager = Dependencies.InputManager;
          
            enabled = false;
        }

        private void Start()
        {
            m_mouse = Dependencies.GameView.GetVirtualMouse(m_viewport.LocalPlayerIndex);
        }

        private void OnEnable()
        {
        }

        private void LateUpdate()
        {
            if (m_inputManager.GetButtonDown(InputAction.LMB, m_viewport.LocalPlayerIndex))
            {
                m_startMousePosition = m_mouse.VirtualMousePosition;
              
                m_isDragging = GetPoint(out m_startPt);
                if (m_isDragging)
                {
                    m_startPt.x = Mathf.Round(m_startPt.x);
                    m_startPt.y = Mathf.Round(m_startPt.y);

                    m_rectTransform.anchoredPosition = m_startPt;
                    m_rectTransform.sizeDelta = new Vector2(0, 0);
                    CursorHelper.SetCursor(this, null, Vector3.zero, CursorMode.Auto);
                }
            }
            else if (m_inputManager.GetButtonUp(InputAction.LMB, m_viewport.LocalPlayerIndex, false, false))
            {
                if (m_isDragging)
                {
                    m_isDragging = false;

                    HitTest();
                    m_rectTransform.sizeDelta = new Vector2(0, 0);
                    CursorHelper.ResetCursor(this);
                    enabled = false;
                }
            }
            else if (m_isDragging)
            {
                GetPoint(out m_endPt);
                m_endPt.x = Mathf.Round(m_endPt.x);
                m_endPt.y = Mathf.Round(m_endPt.y);

                Vector2 size = (m_endPt - m_startPt);
          

                m_rectTransform.sizeDelta = new Vector2(Mathf.Abs(size.x) + 1, Mathf.Abs(size.y) + 1);
                m_rectTransform.localScale = new Vector3(Mathf.Sign(size.x), Mathf.Sign(size.y), 1);
            }
        }

        private void HitTest()
        {
            if (m_rectTransform.sizeDelta.magnitude < 5f)
            {
                return;
            }

            Vector3 center = (m_startMousePosition + m_mouse.VirtualMousePosition) / 2;
            center.z = 0.0f;
            Bounds selectionBounds = new Bounds(center, m_rectTransform.sizeDelta);

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(m_viewport.Camera);

            HashSet<GameObject> selection = new HashSet<GameObject>();
            Collider[] colliders = FindObjectsOfType<Collider>();
            FilteringArgs args = new FilteringArgs();
            for (int i = 0; i < colliders.Length; ++i)
            {
                Collider c = colliders[i];
                Bounds bounds = c.bounds;
                GameObject go = c.gameObject;
                TrySelect(ref selectionBounds, selection, args, ref bounds, go , frustumPlanes);
            }

            if(Selected != null)
            {
                Selected(this, new BoxSelectEventArgs(selection.ToArray()));
            }
        }

        private void TrySelect(ref Bounds selectionBounds, HashSet<GameObject> selection, FilteringArgs args, ref Bounds bounds, GameObject go, Plane[] frustumPlanes)
        {
            bool select;
            if (m_method == BoxSelectionMethod.LooseFitting)
            {
                select = LooseFitting(ref selectionBounds, ref bounds);
            }
            else if (m_method == BoxSelectionMethod.BoundsCenter)
            {
                select = BoundsCenter(ref selectionBounds, ref bounds);
            }
            else
            {
                select = TransformCenter(ref selectionBounds, go.transform);
            }

            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
            {
                select = false;
            }

            if (select)
            {
                if (!selection.Contains(go))
                {
                    if (Filtering != null)
                    {
                        args.Object = go;
                        Filtering(this, args);
                        if (!args.Cancel)
                        {

                            selection.Add(go);
                        }
                        args.Reset();
                    }
                    else
                    {
                        selection.Add(go);
                    }
                }
            }


        }

        private bool TransformCenter(ref Bounds selectionBounds, Transform tr)
        {
            Vector3 screenPoint = m_viewport.Camera.WorldToScreenPoint(tr.position);
            screenPoint.z = 0;
            return selectionBounds.Contains(screenPoint);
        }

        private bool BoundsCenter(ref Bounds selectionBounds, ref Bounds bounds)
        {
            Vector3 screenPoint = m_viewport.Camera.WorldToScreenPoint(bounds.center);
            screenPoint.z = 0;
            return selectionBounds.Contains(screenPoint);
        }

        private bool LooseFitting(ref Bounds selectionBounds, ref Bounds bounds)
        {
            Vector3 p0 = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
            Vector3 p1 = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z);
            Vector3 p2 = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z);
            Vector3 p3 = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z);
            Vector3 p4 = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
            Vector3 p5 = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z);
            Vector3 p6 = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z);
            Vector3 p7 = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z);

            p0 = m_viewport.Camera.WorldToScreenPoint(p0);
            p1 = m_viewport.Camera.WorldToScreenPoint(p1);
            p2 = m_viewport.Camera.WorldToScreenPoint(p2);
            p3 = m_viewport.Camera.WorldToScreenPoint(p3);
            p4 = m_viewport.Camera.WorldToScreenPoint(p4);
            p5 = m_viewport.Camera.WorldToScreenPoint(p5);
            p6 = m_viewport.Camera.WorldToScreenPoint(p6);
            p7 = m_viewport.Camera.WorldToScreenPoint(p7);

            float minX = Mathf.Min(p0.x, p1.x, p2.x, p3.x, p4.x, p5.x, p6.x, p7.x);
            float maxX = Mathf.Max(p0.x, p1.x, p2.x, p3.x, p4.x, p5.x, p6.x, p7.x);
            float minY = Mathf.Min(p0.y, p1.y, p2.y, p3.y, p4.y, p5.y, p6.y, p7.y);
            float maxY = Mathf.Max(p0.y, p1.y, p2.y, p3.y, p4.y, p5.y, p6.y, p7.y);
            Vector3 min = new Vector2(minX, minY);
            Vector3 max = new Vector2(maxX, maxY);

            Bounds b = new Bounds((min + max) / 2, (max - min));
            return selectionBounds.Intersects(b);
        }

        private bool GetPoint(out Vector2 localPoint)
        {
            bool result = m_viewport.ScreenPointToViewport(m_mouse.VirtualMousePosition, out localPoint);
            return result;
        }
    }

}
