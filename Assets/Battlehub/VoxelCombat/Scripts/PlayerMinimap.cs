using Battlehub.UIControls;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class PlayerMinimap : UIBehaviour
    {
        private IVoxelMinimapRenderer m_minimap;
        private IPlayerCameraController m_cameraController;
        private IVoxelInputManager m_input;
        private IVoxelGame m_gameState;
        private IndependentEventSystem m_eventSystem;
        private IVoxelMap m_voxelMap;

        [SerializeField]
        private GameObject m_root;

        [SerializeField]
        private Selectable m_selectableMinimap;
        private HUDControlBehavior m_hudControlBehavior;
        private RectTransformChangeListener m_rtChangeListener;

        [SerializeField]
        private GameViewport m_viewport;
        [SerializeField]
        private RawImage m_background;
        [SerializeField]
        private RawImage m_foreground;
        [SerializeField]
        private RawImage m_fogOfWar;
        [SerializeField]
        private Material m_foregroundLayerMaterial;

        //[SerializeField]
        //private UILineRenderer m_frustumProjection;
        //[SerializeField]
        //private Material m_frustumMaterial;

        [SerializeField]
        private RectTransform m_frustumApproximation;

        [SerializeField]
        private RectTransform m_rtMapBounds;

        private float m_rootRadius;
        //private float m_scaledRootRadius;
        private Vector2 m_prevCursor;
       
        private Vector3 m_prevCamPos;
        private Quaternion m_prevCamRot;
        private CanvasScaler m_scaler;

        private bool m_gotFocus;
        private bool m_mouseManipulation;
        private Vector2 m_virtualMousePostion;
        private Vector3[] m_corners = new Vector3[4];
        private PlayerCamCtrlSettings m_camCtrlSettings;

        protected override void Awake()
        {
            base.Awake();
            
            m_gameState = Dependencies.GameState;
            m_minimap = Dependencies.Minimap;
            m_input = Dependencies.InputManager;
            m_voxelMap = Dependencies.Map;
            m_scaler = GetComponentInParent<CanvasScaler>();
       
            m_gameState.Menu += OnMenu;
            m_gameState.ContextAction += OnContextAction;
            
    
          
            m_rtChangeListener = m_selectableMinimap.GetComponent<RectTransformChangeListener>();
            m_rtChangeListener.RectTransformChanged += OnMinimapRectTransformChanged;

            m_hudControlBehavior = m_selectableMinimap.GetComponent<HUDControlBehavior>();
            m_hudControlBehavior.Selected += OnMinimapSelected;
            m_hudControlBehavior.Deselected += OnMinimapDeselected;
        }

        protected override void Start()
        {
            var nav = m_selectableMinimap.navigation;
            nav.mode = m_input.IsKeyboardAndMouse(m_viewport.LocalPlayerIndex) ? UnityEngine.UI.Navigation.Mode.None : UnityEngine.UI.Navigation.Mode.Explicit;
            m_selectableMinimap.navigation = nav;

            m_minimap.Loaded += OnLoaded;
            m_background.texture = m_minimap.Background;
            m_foreground.texture = m_minimap.Foreground;
            m_fogOfWar.texture = m_minimap.FogOfWar[m_gameState.LocalToPlayerIndex(m_viewport.LocalPlayerIndex)];

            Material foregroundMaterial = Instantiate(m_foregroundLayerMaterial);
            foregroundMaterial.SetTexture("_MaskTex", m_fogOfWar.texture);
            m_foreground.material = foregroundMaterial;

            m_eventSystem = Dependencies.EventSystemManager.GetEventSystem(m_viewport.LocalPlayerIndex);
            m_cameraController = Dependencies.GameView.GetCameraController(m_viewport.LocalPlayerIndex);
            m_camCtrlSettings = Dependencies.Settings.PlayerCamCtrl[m_viewport.LocalPlayerIndex];

            base.Start();
            StartCoroutine(Fit());
            UpdateVisibility();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (m_minimap != null)
            {
                m_minimap.Loaded -= OnLoaded;
            }

            if (m_gameState != null)
            {
                m_gameState.Menu -= OnMenu;
                m_gameState.ContextAction -= OnContextAction;
            }

            if(m_rtChangeListener != null)
            {
                m_rtChangeListener.RectTransformChanged -= OnMinimapRectTransformChanged;
            }

            if(m_hudControlBehavior != null)
            {
                m_hudControlBehavior.Selected -= OnMinimapSelected;
                m_hudControlBehavior.Deselected -= OnMinimapDeselected;
            }
        }

        private void Update()
        {
            if (m_gameState.IsContextActionInProgress(m_viewport.LocalPlayerIndex))
            {
                return;
            }

            if (m_gameState.IsMenuOpened(m_viewport.LocalPlayerIndex))
            {
                return;
            }

            if (m_gameState.IsPaused || m_gameState.IsPauseStateChanging)
            {
                return;
            }

            Transform camTransform = m_viewport.Camera.transform;
            if (camTransform.position != m_prevCamPos || camTransform.rotation != m_prevCamRot)
            {
                m_prevCamPos = camTransform.position;
                m_prevCamRot = camTransform.rotation;

                float angle = camTransform.eulerAngles.y;
                m_rtMapBounds.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

                ProjectCamera(m_rtMapBounds.rotation);
            }

            if (m_input.GetButtonDown(InputAction.Start, m_viewport.LocalPlayerIndex, false, false))
            {
                m_gotFocus = !m_gotFocus;
                if (m_gotFocus)
                {
                    m_eventSystem.SetSelectedGameObjectOnLateUpdate(m_selectableMinimap.gameObject);
                }
                else
                {
                    m_cameraController.CenterVirtualMouse();
                    m_eventSystem.SetSelectedGameObjectOnLateUpdate(null);
                }
            }

            if (m_input.GetButtonDown(InputAction.LMB, m_viewport.LocalPlayerIndex, false, false))
            {
                Vector2 pt;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)m_rtMapBounds.parent, m_cameraController.VirtualMousePosition, null, out pt))
                {
                    float normalizedDistance = pt.magnitude / m_rootRadius;
                    if (normalizedDistance <= 1)
                    {
                        m_mouseManipulation = true;
                        m_virtualMousePostion = m_cameraController.VirtualMousePosition;
                        Move(camTransform, true, 0, 0, 0, 0);
                    }
                }
            }
            else if (m_input.GetButtonUp(InputAction.LMB, m_viewport.LocalPlayerIndex, false, false))
            {
                m_mouseManipulation = false;
            }

            if (m_gotFocus || m_input.GetButton(InputAction.LB, m_viewport.LocalPlayerIndex, false, false) && IsCursorInMinimapBounds())
            {
                if (m_input.GetButtonDown(InputAction.B, m_viewport.LocalPlayerIndex, false, false))
                {
                    m_gotFocus = false;
                    m_cameraController.CenterVirtualMouse();
                    m_eventSystem.SetSelectedGameObjectOnLateUpdate(null);
                }
                else if(m_input.GetButtonDown(InputAction.LB, m_viewport.LocalPlayerIndex, false, false))
                {
                    m_virtualMousePostion = m_cameraController.VirtualMousePosition;
                    Move(camTransform, true, 0, 0, 0, 0);
                }
                else
                {
                    bool aPressed = m_input.GetButton(InputAction.A, m_viewport.LocalPlayerIndex, false, false);
                    bool pivotPreciseMode = aPressed | m_input.GetButton(InputAction.RightStickButton, m_viewport.LocalPlayerIndex, false, false);

                    float pivotMultiplier = 1;
                    if (!pivotPreciseMode)
                    {
                        pivotMultiplier = 5;
                    }

                    float deltaY = m_input.GetAxisRaw(InputAction.MoveForward, m_viewport.LocalPlayerIndex, false, false) * Time.deltaTime * m_camCtrlSettings.MoveSensitivity * pivotMultiplier;
                    float deltaX = m_input.GetAxisRaw(InputAction.MoveSide, m_viewport.LocalPlayerIndex, false, false) * Time.deltaTime * m_camCtrlSettings.MoveSensitivity * pivotMultiplier;

                    bool cursorPreciseMode = aPressed | m_input.GetButton(InputAction.LeftStickButton, m_viewport.LocalPlayerIndex, false, false);
                    float cursorMultiplier = 4;
                    if (!cursorPreciseMode)
                    {
                        cursorMultiplier = 12;
                    }
                    float cursorY = m_input.GetAxisRaw(InputAction.CursorY, m_viewport.LocalPlayerIndex, false, false) * Time.deltaTime * m_camCtrlSettings.CursorSensitivity * cursorMultiplier;
                    float cursorX = m_input.GetAxisRaw(InputAction.CursorX, m_viewport.LocalPlayerIndex, false, false) * Time.deltaTime * m_camCtrlSettings.CursorSensitivity * cursorMultiplier;

                    Move(camTransform, false, deltaY, deltaX, cursorY, cursorX);
                }
            }

            if (m_mouseManipulation)
            {
                if (m_input.GetButton(InputAction.LMB, m_viewport.LocalPlayerIndex, false, false))
                {
                    float cursorMultiplier = 12;
                    float cursorY = m_input.GetAxisRaw(InputAction.CursorY, m_viewport.LocalPlayerIndex, false, false) * Time.deltaTime * m_camCtrlSettings.CursorSensitivity * cursorMultiplier;
                    float cursorX = m_input.GetAxisRaw(InputAction.CursorX, m_viewport.LocalPlayerIndex, false, false) * Time.deltaTime * m_camCtrlSettings.CursorSensitivity * cursorMultiplier;

                    Move(camTransform, false, cursorY, cursorX, 0, 0);
                }
            }
        }


   
        private void Move(Transform camTransform, bool forceSetMapPivot, float deltaY, float deltaX, float cursorY, float cursorX)
        {
                    
            if(deltaX != 0 || deltaY != 0)
            {
                m_virtualMousePostion += new Vector2(deltaX, deltaY);

                cursorX = 0;
                cursorY = 0;
            }
            else
            {
                m_virtualMousePostion += new Vector2(cursorX, cursorY);
            }

            if (m_virtualMousePostion != m_prevCursor || forceSetMapPivot)
            {
                Vector2 pt;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)m_rtMapBounds.parent, m_virtualMousePostion, null, out pt))
                {
                    float normalizedDistance = pt.magnitude / m_rootRadius;
                    float angle = camTransform.eulerAngles.y;
                    Vector3 dir = Quaternion.Euler(new Vector3(0, angle, 0)) * new Vector3(pt.x, 0, pt.y).normalized;

                    if (normalizedDistance <= 1)
                    {
                        if(deltaX != 0 || deltaY != 0 || forceSetMapPivot)
                        {
                            m_cameraController.SetMapPivot(dir, normalizedDistance);
                        }   
                    }
                    else
                    {
                        if (deltaX != 0 || deltaY != 0 || forceSetMapPivot)
                        {
                            m_cameraController.SetMapPivot(dir, 1);
                            ProjectCursorToMinimap(m_viewport.Camera.transform);
                            m_virtualMousePostion = m_cameraController.VirtualMousePosition;
                          
                        }
                        else
                        {
                            ((RectTransform)m_rtMapBounds.parent).GetWorldCorners(m_corners);
                            Vector3 center = m_corners[1] + (m_corners[3] - m_corners[1]) / 2;
                            Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(null, center);
                            m_virtualMousePostion = screenCenter + pt.normalized * m_rootRadius;
                        }
                    }
                }
                else
                {
                    if (deltaX != 0 || deltaY != 0)
                    {
                        m_virtualMousePostion -= new Vector2(deltaX, deltaY);
                    }
                    else
                    {
                        m_virtualMousePostion -= new Vector2(cursorX, cursorY);
                    }
                }

                m_prevCursor = m_virtualMousePostion;
            }

            m_cameraController.VirtualMousePosition = m_virtualMousePostion;
        }

        private void OnMinimapSelected()
        {
            Transform camTransform = m_viewport.Camera.transform;
            if (!m_gotFocus)
            {
                // m_gotFocus = true;
                m_eventSystem.SetSelectedGameObjectOnLateUpdate(null);
                m_virtualMousePostion = m_cameraController.VirtualMousePosition;
                m_prevCursor = m_virtualMousePostion;
                Move(camTransform, true, 0, 0, 0, 0);
            }
            else
            {
                ProjectCursorToMinimap(camTransform);
                m_virtualMousePostion = m_cameraController.VirtualMousePosition;
                m_prevCursor = m_virtualMousePostion;
            }
        }

        private void OnMinimapDeselected()
        {
            m_gotFocus = false;
        }

        private void ProjectCursorToMinimap(Transform camTransform)
        {
            ((RectTransform)m_rtMapBounds.parent).GetWorldCorners(m_corners);
            Vector3 center = m_corners[1] + (m_corners[3] - m_corners[1]) / 2;
            Vector3 screenCenter = RectTransformUtility.WorldToScreenPoint(null, center);

            Vector3 toPivot = m_cameraController.TargetPivot - m_cameraController.BoundsCenter;
            toPivot.y = 0;

            float angle = camTransform.eulerAngles.y;
            Vector3 dir = Quaternion.Euler(new Vector3(0, -angle, 0)) * toPivot.normalized;
            dir.y = dir.z;
            dir.z = 0;


            float normalizedOffset = toPivot.magnitude / m_cameraController.BoundsRadius;
            m_cameraController.VirtualMousePosition = screenCenter + dir * normalizedOffset * m_rootRadius * m_scaler.scaleFactor;
        }

        private void OnMinimapRectTransformChanged()
        {
            StartCoroutine(Fit());
        }

        private void OnContextAction(int localPlayerIndex)
        {
            UpdateVisibility();
        }

        private void OnMenu(int localPlayerIndex)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (m_gameState.IsContextActionInProgress(m_viewport.LocalPlayerIndex) || m_gameState.IsMenuOpened(m_viewport.LocalPlayerIndex))
            {
                m_root.SetActive(false);
                m_gotFocus = false;
                m_mouseManipulation = false;
            }
            else
            {
                m_root.SetActive(true);
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

            m_frustumApproximation.offsetMin = new Vector2(p1.x, p3.y);
            m_frustumApproximation.offsetMax = new Vector2(p2.x, p1.y);

            //Maybe replace with line renderer?

            //m_frustumProjection.Points[0] = p0;
            //m_frustumProjection.Points[1] = p1;
            //m_frustumProjection.Points[2] = p2;
            //m_frustumProjection.Points[3] = p3;
            //m_frustumProjection.Points[4] = p0;
            //m_frustumProjection.SetAllDirty();
        }

        private IEnumerator Fit()
        {
            yield return new WaitForEndOfFrame();
            
            CalculateRootRadius();

            
            float offset = m_rootRadius - m_rootRadius * Mathf.Sqrt(2.0f) / 2.0f;
            //m_scaledRootRadius = m_rootRadius -  offset;

            //m_rtMapBounds.offsetMin = new Vector2(offset, offset);
            // m_rtMapBounds.offsetMax = new Vector2(-offset, -offset);


            Rect mapBoundsRect = m_rtMapBounds.rect;
            mapBoundsRect.width -= 2 * offset;
            mapBoundsRect.height -= 2 * offset;
            Vector3 mapCenter = new Vector3(mapBoundsRect.width, mapBoundsRect.height) * 0.5f;
            float mapSize = mapBoundsRect.width;

            int size = m_voxelMap.Map.GetMapSizeWith(GameConstants.MinVoxelActorWeight);
            float pixelsPerUnit = mapSize / size;

            MapRect mapBounds = m_voxelMap.MapBounds;
            float mapBoundsWidth = mapBounds.ColsCount * pixelsPerUnit;
            float mapBoundsHeight = mapBounds.RowsCount * pixelsPerUnit;
            Vector3 mapBoundsTopLeft = new Vector3(mapBounds.Col * pixelsPerUnit, mapBounds.Row * pixelsPerUnit, 0);
            Vector3 mapBoundsCenter = mapBoundsTopLeft + new Vector3(mapBoundsWidth, mapBoundsHeight) / 2;
            float mapBoundsSize = Mathf.Max(mapBoundsWidth, mapBoundsHeight);

            float ext = (mapSize - mapBoundsSize) / 2;
            Vector3 offsetMin = (mapCenter - mapBoundsCenter) - new Vector3(ext, ext, 0);
            Vector3 offsetMax = (mapCenter - mapBoundsCenter) + new Vector3(ext, ext, 0);

            RectTransform rtFogOfWar = m_fogOfWar.GetComponent<RectTransform>();
            rtFogOfWar.offsetMin = offsetMin;
            rtFogOfWar.offsetMax = offsetMax;

            RectTransform rtForeground = m_foreground.GetComponent<RectTransform>();
            rtForeground.offsetMin = offsetMin;
            rtForeground.offsetMax = offsetMax;

            RectTransform rtBackground = m_background.GetComponent<RectTransform>();
            rtBackground.offsetMin = offsetMin;
            rtBackground.offsetMax = offsetMax;

            ProjectCamera(m_rtMapBounds.rotation);
        }


        private bool IsCursorInMinimapBounds()
        {
            Vector2 pt;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)m_rtMapBounds.parent, m_cameraController.VirtualMousePosition, null, out pt))
            {
                float normalizedDistance = pt.magnitude / m_rootRadius;
                return normalizedDistance <= 1.1f;
            }
            return false;
        }


        private void CalculateRootRadius()
        {
            //RectTransform parentRT = (RectTransform)m_rtMapBounds.parent;
            //Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parentRT);
            // m_rootRadius = bounds.extents.x;

            RectTransform parentRT = (RectTransform)m_rtMapBounds.parent;
            m_rootRadius = parentRT.rect.width / 2;

            Debug.Log(m_rootRadius);
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            m_background.texture = m_minimap.Background;
            m_foreground.texture = m_minimap.Foreground;
            m_fogOfWar.texture = m_minimap.FogOfWar[m_gameState.LocalToPlayerIndex(m_viewport.LocalPlayerIndex)];
        }
    }
}
