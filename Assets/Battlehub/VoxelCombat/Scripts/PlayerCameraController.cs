using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public interface IVirtualMouse
    {
        Vector2 VirtualMousePosition
        {
            get;
            set;
        }

        float VirtualMouseSensitivityScale
        {
            get;
            set;
        }

        bool IsVirtualMouseCursorVisible
        {
            get;
            set;
        }

        bool IsVirtualMouseEnabled
        {
            get;
            set;
        }


        void CenterVirtualMouse();
        void BackupVirtualMouse();
        void RestoreVirtualMouse();
    }


    public interface IPlayerCameraController : IVirtualMouse
    {
        bool IsInputEnabled
        {
            get;
            set;

        }
        Vector3 Pivot
        {
            get;
        }

        Vector3 TargetPivot
        {
            get;
        }

        MapPos MapPivot
        {
            get;
            set;
        }
        
        int Weight
        {
            get;
        }

        Ray Ray
        {
            get;
        }

        Vector3 Cursor
        {
            get;
        }

        Vector3 BoundsCenter
        {
            get;
        }

        float BoundsRadius
        {
            get;
        }

        MapPos MapCursor
        {
            get;
        }


        bool InScreenBounds(Vector2 point);

        Vector2 WorldToScreenPoint(Vector3 worldPoint);

        void SetVirtualMousePosition(Coordinate coordinate, bool lockMouse, bool animate);

        void SetMapPivot(Vector3 offCenterDir, float normailzedDistance);

        bool IsVisible(MapPos pos, int weight);

        void MovePivot(Vector2 offset);
    }

    public class PlayerCameraController : MonoBehaviour, IPlayerCameraController
    {

        [SerializeField]
        private GameViewport m_viewport;

        [SerializeField]
        private RectTransform m_cursorIconTransform;

        [SerializeField]
        private Image m_cursorIcon;
        [SerializeField]
        private Image m_rotationIcon;

        [SerializeField]
        private Sprite m_spritePointer;
        [SerializeField]
        private Sprite m_spriteRotateCW;
        [SerializeField]
        private Sprite m_spriteRotateCCW;
        [SerializeField]
        private Sprite[] m_spriteArrows;

        private const int MoveViewportMargin = 13;
        private int m_clampCursorPadding = 0;
        private Vector3 m_prevMouseOffset;
        
        private Camera m_camera;
        private float m_camDistance;
        private Vector3 m_camToVector;
        private float m_camAngle;
        private Plane m_groundPlane = new Plane(Vector3.up, Vector3.zero);
        private Plane m_hitPlane;
     
        private Rect m_camPixelRect;
        private object m_voxelCameraRef;

        private IVoxelInputManager m_inputManager;
        private IGlobalSettings m_settings;
        private IVoxelMap m_voxelMap;
        private IVoxelGame m_gameState;
        private IBoxSelector m_boxSelector;

        private int m_localPlayerIndex;
        private int LocalPlayerIndex
        {
            get { return m_localPlayerIndex; }
            set
            {
                if (m_localPlayerIndex != value)
                {
                    m_localPlayerIndex = value;
                    if (m_viewport != null) //if start method was called
                    {
                        GetViewportAndCamera();
                        ReadPlayerCamSettings();
                        SetCameraPosition();
                    }
                }
            }
        }

        private MapCell m_targetPivotCell;
        private Vector3 m_targetPivot;
        public Vector3 TargetPivot
        {
            get { return m_targetPivot; }
            set { m_targetPivot = value; }
        }

        private Transform m_pivot;
        public Vector3 Pivot
        {
            get { return m_pivot.transform.position; }
            private set { m_pivot.transform.position = value; }
        }

        private bool m_isInputEnabled = true;
        public bool IsInputEnabled
        {
            get { return m_isInputEnabled; }
            set { m_isInputEnabled = value; }
        }

        private bool m_lockInputDuringPivotAnimation;
        private MapPos m_mapPivot;
        public MapPos MapPivot
        {
            get { return m_mapPivot; }
            set
            {
                m_lockInputDuringPivotAnimation = true;
                SetMapPivot(value);
            }
        }

        private void SetMapPivot(MapPos value)
        {
            m_mapPivot = value;

            m_targetPivotCell = m_voxelMap.GetCell(m_mapPivot, GameConstants.MinVoxelActorWeight, null);

            m_targetPivot = m_voxelMap.GetWorldPosition(m_mapPivot, GameConstants.MinVoxelActorWeight);

            if (m_targetPivotCell != null)
            {
                m_targetPivot.y = m_targetPivotCell.GetTotalHeight((int)KnownVoxelTypes.Ground) * GameConstants.UnitSize;
            }
            else
            {
                m_targetPivot.y = 0;
            }

            int pow = (int)Mathf.Pow(2, GameConstants.VoxelCameraWeight - GameConstants.MinVoxelActorWeight);

            MapPos camPos = m_voxelMap.GetCameraPosition(m_voxelCameraRef);
            int deltaRow = m_mapPivot.Row / pow - camPos.Row;
            int deltaCol = m_mapPivot.Col / pow - camPos.Col;

            if (deltaRow != 0 || deltaCol != 0)
            {
                m_voxelMap.MoveCamera(deltaRow, deltaCol, m_voxelCameraRef);
            }

            UpdateCursors();
        }

        public int Weight
        {
            get { return GameConstants.MinVoxelActorWeight; }
        }

        private bool m_animateMousePosition;
        private bool m_lockMouseToWorld;
        private Vector3 m_lockMousePosWorld;

        private Vector2 m_virtualMousePosition;
        public Vector2 VirtualMousePosition
        {
            get { return m_virtualMousePosition; }
            set
            {
                SetVirtualMousePosition(value);
            }
        }

        private void SetVirtualMousePosition(Vector2 value)
        {
            if (m_virtualMousePosition != value)
            {
                m_virtualMousePosition = value;
                ClampVirtualMousePosition();
                UpdateCursors();
            }

            if (m_cursorIconTransform != null)
            {
                m_cursorIconTransform.transform.position = m_virtualMousePosition;
            }
        }

        private float m_virtualMouseSensitivityScale = 1.0f;
        public float VirtualMouseSensitivityScale
        {
            get { return m_virtualMouseSensitivityScale; }
            set { m_virtualMouseSensitivityScale = value; }
        }

        private void UpdateCursors()
        {
            m_cursor = Hit(m_virtualMousePosition);
            m_mapCursor = m_voxelMap.GetMapPosition(m_cursor, Weight);
        }

        public bool IsVirtualMouseCursorVisible
        {
            get { return m_cursorIcon.gameObject.activeSelf; }
            set { m_cursorIcon.gameObject.SetActive(value); }
        }

        private bool m_isVirtualMouseEnabled = true;
        public bool IsVirtualMouseEnabled
        {
            get { return m_isVirtualMouseEnabled; }
            set { m_isVirtualMouseEnabled = value; }
        }


       
        private float m_virtualMouseSensitivityScaleBackup;
        private bool m_isVirtualMouseCursorVisibleBackup;
        private bool m_isVirtualMouseEnabledBackup;
        private bool m_isVirtualMouseBackup;
        public void BackupVirtualMouse()
        {
            if(!m_isVirtualMouseBackup)
            {
                m_virtualMouseSensitivityScaleBackup = VirtualMouseSensitivityScale;
                m_isVirtualMouseCursorVisibleBackup = IsVirtualMouseCursorVisible;
                m_isVirtualMouseEnabledBackup = IsVirtualMouseEnabled;
                m_isVirtualMouseBackup = true;
            }
        }

        public void RestoreVirtualMouse()
        {
            if(m_isVirtualMouseBackup)
            {
                VirtualMouseSensitivityScale = m_virtualMouseSensitivityScaleBackup;
                IsVirtualMouseCursorVisible = m_isVirtualMouseCursorVisibleBackup;
                IsVirtualMouseEnabled = m_isVirtualMouseEnabledBackup;
                m_isVirtualMouseBackup = false;
            }
        }

        public void CenterVirtualMouse()
        {
            VirtualMousePosition = m_camPixelRect.center;
        }

        private float m_allowedRadius;
        private Vector3 m_boundsCenter;
        public Vector3 BoundsCenter
        {
            get { return m_boundsCenter; }
        }

        public float BoundsRadius
        {
            get { return m_allowedRadius; }
        }

        public Ray Ray
        {
            get { return m_camera.ScreenPointToRay(VirtualMousePosition); }
        }

        private Vector3 m_cursor;
        public Vector3 Cursor
        {
            get { return m_cursor; }
        }

        private MapPos m_mapCursor;
        public MapPos MapCursor
        {
            get { return m_mapCursor; }
        }

        public bool InScreenBounds(Vector2 point)
        {
            Vector3 vmpos = point;

            const float cornerMargin = MoveViewportMargin;// * 1.5f;
            
            if (vmpos.x < m_camPixelRect.xMin + cornerMargin &&
               vmpos.y < m_camPixelRect.yMin + cornerMargin)
            {
                return false;
            }
            else if (vmpos.x < m_camPixelRect.xMin + cornerMargin &&
                vmpos.y > m_camPixelRect.yMax - cornerMargin)
            {
                return false;
            }
            else if (vmpos.x > m_camPixelRect.xMax - cornerMargin &&
               vmpos.y < m_camPixelRect.yMin + cornerMargin)
            {
                return false;
            }
            else if (vmpos.x > m_camPixelRect.xMax - cornerMargin &&
                vmpos.y > m_camPixelRect.yMax - cornerMargin)
            {
                return false;
            }
            else
            {
                if (vmpos.x < m_camPixelRect.xMin + MoveViewportMargin)
                {
                    return false;
                }
                if (vmpos.x > m_camPixelRect.xMax - MoveViewportMargin)
                {
                    return false;
                }
                if (vmpos.y < m_camPixelRect.yMin + MoveViewportMargin)
                {
                    return false;
                }
                if (vmpos.y > m_camPixelRect.yMax - MoveViewportMargin)
                {
                    return false;
                }
            }

            return true;
        }

        public Vector2 WorldToScreenPoint(Vector3 worldPoint)
        {
            return m_camera.WorldToScreenPoint(worldPoint);
        }

        public void SetVirtualMousePosition(Coordinate coordinate, bool lockMouse, bool animate)
        {
            Vector3 worldPosition = m_voxelMap.GetWorldPosition(coordinate);
            worldPosition.y = coordinate.Altitude * GameConstants.UnitSize;
            m_animateMousePosition = animate;
            m_lockMouseToWorld = lockMouse;

            if (m_lockMouseToWorld)
            {
                m_lockMousePosWorld = worldPosition;
                m_cursorIcon.sprite = m_spritePointer;
            }

            if (!m_animateMousePosition)
            {
                SetVirtualMousePosition(m_camera.WorldToScreenPoint(worldPosition));
            }
        }

        public void SetMapPivot(Vector3 offCenterDir, float normailzedDistance)
        {
            Vector3 position = m_boundsCenter + offCenterDir * m_allowedRadius * normailzedDistance;

            MapPivot = m_voxelMap.GetMapPosition(position, GameConstants.MinVoxelActorWeight);
            m_targetPivot.x = position.x;
            m_targetPivot.z = position.z;

            m_lockInputDuringPivotAnimation = false;
        }

        public bool IsVisible(MapPos pos, int weight)
        {
            return m_voxelMap.IsVisible(pos, weight, m_voxelCameraRef);
        }

        private void Awake()
        {
            m_gameState = Dependencies.GameState;
            m_settings = Dependencies.Settings;
            m_inputManager = Dependencies.InputManager;
            m_voxelMap = Dependencies.Map;

            MapRect rect = m_voxelMap.MapBounds;
            Vector3 p0 = m_voxelMap.GetWorldPosition(rect.P0, GameConstants.MinVoxelActorWeight);
            Vector3 p1 = m_voxelMap.GetWorldPosition(rect.P1, GameConstants.MinVoxelActorWeight);
            m_allowedRadius = (p1 - p0).magnitude / 2.0f;
            m_boundsCenter = p0 + (p1 - p0) / 2.0f;
        }

        private void Start()
        {
            LocalPlayerIndex = m_viewport.LocalPlayerIndex;

          
            m_boxSelector = Dependencies.GameView.GetBoxSelector(LocalPlayerIndex);
            
            

            GetViewportAndCamera();
            ReadPlayerCamSettings();
            SetCameraPosition();
            InitCameraPixelRect();
            CreateAndInitVoxelCamera();
            InitPivot();

            int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);
            var units = m_gameState.GetUnits(playerIndex);
            foreach (long unit in units)
            {
                IVoxelDataController dc = m_gameState.GetVoxelDataController(playerIndex, unit);
                if (VoxelData.IsControllableUnit(dc.ControlledData.Type))
                {
                    MapPivot = dc.Coordinate.ToWeight(GameConstants.MinVoxelActorWeight).MapPos;
                    break;
                }
            }
        }

        private void OnEnable()
        {
            if (m_viewport != null)
            {
                CreateAndInitVoxelCamera();
            }
        }

        private void OnDisable()
        {
            m_voxelMap.DestroyCamera(m_voxelCameraRef);
        }

        private void OnDestroy()
        {
            if (m_viewport != null)
            {
                m_viewport.ViewportChanged -= OnViewportChanged;
            }
        }

        private void OnViewportChanged()
        {
            InitCameraPixelRect();
        }

        private void GetViewportAndCamera()
        {
            if (m_viewport != null)
            {
                m_viewport.ViewportChanged -= OnViewportChanged;
            }

            if (m_viewport != null)
            {
                m_viewport.ViewportChanged += OnViewportChanged;
            }
            m_camera = m_viewport.Camera;
            m_pivot = m_viewport.Camera.transform.parent;
        }

        private void ReadPlayerCamSettings()
        {
            PlayerCamCtrlSettings settings = m_settings.PlayerCamCtrl[LocalPlayerIndex];
            m_camDistance = Mathf.Lerp(settings.MinCamDistance, settings.MaxCamDistance, 0.33f);
            m_camToVector = settings.ToCamVector.normalized;
        }

        private void SetCameraPosition()
        {
            Vector3 toCam = Quaternion.AngleAxis(m_camAngle, Vector3.up) * m_camToVector * m_camDistance;

            m_camera.transform.position = Pivot + toCam;

            m_camera.transform.LookAt(Pivot);

            UpdateCursors();
        }

        private void InitCameraPixelRect()
        {
            Rect rect = m_viewport.Camera.pixelRect;
            m_camPixelRect = rect;

            if(!m_gameState.IsMenuOpened(LocalPlayerIndex) && !m_gameState.IsActionsMenuOpened(LocalPlayerIndex) && !m_gameState.IsContextActionInProgress(LocalPlayerIndex))
            {
                VirtualMousePosition = m_camPixelRect.center;
            } 
        }

        private void CreateAndInitVoxelCamera()
        {
            m_voxelCameraRef = m_voxelMap.CreateCamera(GameConstants.VoxelCameraRadius, GameConstants.VoxelCameraWeight);
        }

        private void InitPivot()
        {
            int size = m_voxelMap.Map.GetMapSizeWith(GameConstants.MinVoxelActorWeight);

            int pow = (int)Mathf.Pow(2, GameConstants.VoxelCameraWeight - GameConstants.MinVoxelActorWeight);

            m_voxelMap.SetCameraPosition(size / (2 * pow), size / (2 * pow), m_voxelCameraRef);

            SetMapPivot(new MapPos(size / 2, size / 2));

            Pivot = m_targetPivot;
        }

        private Vector3 GetVirtualMouseOffset()
        {
            const float cornerMargin = MoveViewportMargin;// * 1.5f;

            Vector3 vmpos = VirtualMousePosition;
            Vector3 offset = Vector3.zero;
            if(vmpos.x < m_camPixelRect.xMin + cornerMargin &&
               vmpos.y < m_camPixelRect.yMin + cornerMargin)
            {
                offset.x = -1;
                offset.y = -1;
            }
            else if (vmpos.x < m_camPixelRect.xMin + cornerMargin &&
                vmpos.y > m_camPixelRect.yMax - cornerMargin)
            {
                offset.x = -1;
                offset.y = 1;
            }
            else if (vmpos.x > m_camPixelRect.xMax - cornerMargin &&
               vmpos.y < m_camPixelRect.yMin + cornerMargin)
            {
                offset.x = 1;
                offset.y = -1;
            }
            else if (vmpos.x > m_camPixelRect.xMax - cornerMargin &&
                vmpos.y > m_camPixelRect.yMax - cornerMargin)
            {
                offset.x = 1;
                offset.y = 1;
            }
            else
            {
                if (vmpos.x < m_camPixelRect.xMin + MoveViewportMargin)
                {
                    offset.x = -1;
                }
                if (vmpos.x > m_camPixelRect.xMax - MoveViewportMargin)
                {
                    offset.x = 1;
                }
                if (vmpos.y < m_camPixelRect.yMin + MoveViewportMargin)
                {
                    offset.y = -1;
                }
                if (vmpos.y > m_camPixelRect.yMax - MoveViewportMargin)
                {
                    offset.y = 1;
                }
            }
          
            return offset;
        }

        private void CursorIconFromMouseOffset(Vector3 mouseOffset)
        {
            if(m_lockMouseToWorld)
            {
                m_cursorIcon.sprite = m_spritePointer;
            }
            else
            {
                if (mouseOffset.x == 0 && mouseOffset.y == 1)
                    m_cursorIcon.sprite = m_spriteArrows[0];
                else if (mouseOffset.x == 1 && mouseOffset.y == 1)
                    m_cursorIcon.sprite = m_spriteArrows[1];
                else if (mouseOffset.x == 1 && mouseOffset.y == 0)
                    m_cursorIcon.sprite = m_spriteArrows[2];
                else if (mouseOffset.x == 1 && mouseOffset.y == -1)
                    m_cursorIcon.sprite = m_spriteArrows[3];
                else if (mouseOffset.x == 0 && mouseOffset.y == -1)
                    m_cursorIcon.sprite = m_spriteArrows[4];
                else if (mouseOffset.x == -1 && mouseOffset.y == -1)
                    m_cursorIcon.sprite = m_spriteArrows[5];
                else if (mouseOffset.x == -1 && mouseOffset.y == 0)
                    m_cursorIcon.sprite = m_spriteArrows[6];
                else if (mouseOffset.x == -1 && mouseOffset.y == 1)
                    m_cursorIcon.sprite = m_spriteArrows[7];
                else
                    m_cursorIcon.sprite = m_spritePointer;
            }
        }

        private void ClampVirtualMousePosition()
        {
            Rect r = m_cursorIconTransform.rect;
            float minX = m_camPixelRect.xMin + r.width / 2 + m_clampCursorPadding;
            float maxX = m_camPixelRect.xMax - r.width / 2 - m_clampCursorPadding;
            float minY = m_camPixelRect.yMin + r.height / 2 + m_clampCursorPadding;
            float maxY = m_camPixelRect.yMax - r.width / 2 - m_clampCursorPadding;

            Vector2 vmPos = m_virtualMousePosition;

            if(vmPos.x < minX)
            {
                vmPos = new Vector2(minX, vmPos.y);
            }
            else if(vmPos.x > maxX)
            {
                vmPos = new Vector2(maxX, vmPos.y);
            }

            if (vmPos.y < minY)
            {
                vmPos = new Vector2(vmPos.x, minY);
            }
            else if(vmPos.y > maxY)
            {
                vmPos = new Vector2(vmPos.x, maxY);
            }

            m_virtualMousePosition = vmPos;
        }


        private void MoveVirtualMouseToCenterOfScreen()
        {
            VirtualMousePosition = Vector3.Lerp(VirtualMousePosition, m_camPixelRect.center, Time.deltaTime * 20);
        }

        private float GetTotalHeight(MapCell cell)
        {
            if (cell != null)
            {
               return cell.GetTotalHeight((int)KnownVoxelTypes.Ground) * GameConstants.UnitSize;
            }
            else
            {
                return 0;
            }
        }

        private void AnimatePivotPoint(float xzSensitivity, float ySensitivity)
        {
            m_targetPivot.y = GetTotalHeight(m_targetPivotCell);

            if (m_targetPivot != Pivot)
            {
                Vector3 pivot = Pivot;
                Vector3 targetPivot = m_targetPivot;
                targetPivot.y  = pivot.y = 0;

                pivot = Vector3.Lerp(pivot, targetPivot, Time.deltaTime * xzSensitivity);
                pivot.y = Mathf.Lerp(Pivot.y, m_targetPivot.y, Time.deltaTime * ySensitivity);

                Pivot = pivot;
            }
           
            if(Vector3.Distance(m_targetPivot, Pivot) < GameConstants.UnitSize * 2)
            {
                m_lockInputDuringPivotAnimation = false;
            }
                
            SetCameraPosition();
        }

        private Vector3 Hit(Vector3 screenPoint)
        {
            Ray ray = m_camera.ScreenPointToRay(screenPoint);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {
                return hit.point;
            }

            float distance = 0;
            Debug.Assert(m_groundPlane.Raycast(ray, out distance));
            return ray.GetPoint(distance);
        }

        private Vector3 GetPointOnHitPlane(Vector3 screenPoint)
        {
            Ray ray = m_camera.ScreenPointToRay(screenPoint);

            float distance = 0;
            Debug.Assert(m_hitPlane.Raycast(ray, out distance));
            return ray.GetPoint(distance);
        }

        private void Update()
        {
            if (CursorHelper.lockState == CursorLockMode.None)
            {
                return;
            }

            PlayerCamCtrlSettings settings = m_settings.PlayerCamCtrl[LocalPlayerIndex];
            float cursorX = m_inputManager.GetAxisRaw(InputAction.CursorX, LocalPlayerIndex, false, false);
            float cursorY = m_inputManager.GetAxisRaw(InputAction.CursorY, LocalPlayerIndex, false, false);

            if (m_gameState.IsActionsMenuOpened(LocalPlayerIndex) ||
                m_gameState.IsMenuOpened(LocalPlayerIndex) ||
                m_gameState.IsPaused || m_gameState.IsPauseStateChanging ||
                m_camPixelRect.size == Vector2.zero ||
                m_boxSelector.IsActive)
            {
                if (m_prevMouseOffset != Vector3.zero)
                {
                    CursorIconFromMouseOffset(Vector3.zero);
                    m_prevMouseOffset = Vector3.zero;
                }

                if (m_isVirtualMouseEnabled)
                {
                    m_clampCursorPadding = 1;
                    VirtualMousePosition += new Vector2(cursorX, cursorY) * settings.CursorSensitivity;
                    m_clampCursorPadding = 0;
                }
                
                AnimatePivotPoint(settings.MoveSensitivity / 2, settings.MoveSensitivity / 8);
                return;
            }

            Vector3 mouseOffset = GetVirtualMouseOffset();
            if (IsInputEnabled && !m_lockInputDuringPivotAnimation)
            {
                float deltaY = m_inputManager.GetAxisRaw(InputAction.MoveForward, LocalPlayerIndex, true, false);
                float deltaX = m_inputManager.GetAxisRaw(InputAction.MoveSide, LocalPlayerIndex, true, false);
                bool rotateCamCW = m_inputManager.GetButton(InputAction.LT, LocalPlayerIndex, true, false);
                bool rotateCamCCW = m_inputManager.GetButton(InputAction.RT, LocalPlayerIndex, true, false);

                bool aPressed = m_inputManager.GetButton(InputAction.A, LocalPlayerIndex);
                bool pivotPreciseMode = aPressed | m_inputManager.GetButton(InputAction.RightStickButton, LocalPlayerIndex, true, false);
                bool cursorPreciseMode = aPressed | m_inputManager.GetButton(InputAction.LeftStickButton, LocalPlayerIndex, true, false);
                if (m_inputManager.IsAnyButtonDown(LocalPlayerIndex, false, false))
                {
                    m_lockMouseToWorld = false;
                }

                if (m_inputManager.GetButtonUp(InputAction.LT, LocalPlayerIndex, true, false) || m_inputManager.GetButtonUp(InputAction.RT, LocalPlayerIndex, true, false))
                {
                    if (!rotateCamCW && !rotateCamCCW)
                    {
                        m_cursorIcon.sprite = m_spritePointer;
                    }
                }

                if (m_inputManager.GetButtonUp(InputAction.MMB, LocalPlayerIndex))
                {
                    m_cursorIcon.sprite = m_spritePointer;
                }
                else if (m_inputManager.GetButton(InputAction.MMB, LocalPlayerIndex))
                {
                    m_lockMouseToWorld = false;

                    MoveVirtualMouseToCenterOfScreen();

                    m_camAngle += Time.deltaTime * cursorX * m_settings.PlayerCamCtrl[LocalPlayerIndex].RotateSensitivity;
                    SetCameraPosition();

                    if (cursorX != 0)
                    {
                        m_cursorIcon.sprite = cursorX > 0 ? m_spriteRotateCW : m_spriteRotateCCW;
                    }
                    else
                    {
                        m_cursorIcon.sprite = m_spritePointer;
                    }

                    cursorX = 0;
                    cursorY = 0;
                }
                else
                {
                    if (rotateCamCW && !rotateCamCCW)
                    {
                        m_cursorIcon.sprite = m_spriteRotateCW;
                        MoveVirtualMouseToCenterOfScreen();
                        m_camAngle += Time.deltaTime * settings.RotateSensitivity;
                        SetCameraPosition();
                    }
                    else if (rotateCamCCW && !rotateCamCW)
                    {
                        m_cursorIcon.sprite = m_spriteRotateCCW;
                        MoveVirtualMouseToCenterOfScreen();
                        m_camAngle -= Time.deltaTime * settings.RotateSensitivity;
                        SetCameraPosition();
                    }

                    if (cursorX != 0 && cursorY != 0)
                    {
                        float cursorSens = settings.CursorSensitivity;
                        if (cursorPreciseMode || pivotPreciseMode)
                        {
                            cursorSens /= 5.0f;
                        }
                        else
                        {
                            cursorSens *= VirtualMouseSensitivityScale;
                        }

                        if (m_isVirtualMouseEnabled)
                        {
                            VirtualMousePosition += new Vector2(cursorX, cursorY) * cursorSens;
                        }
                        m_lockMouseToWorld = false;
                    }
                }

                Vector3 deltaOffset = new Vector3(deltaX, deltaY, 0);
                Vector3 offset = Vector2.zero;
                if (deltaOffset != Vector3.zero)
                {
                    offset = deltaOffset;
                    mouseOffset = Vector2.zero;
                }
                else
                {
                    if (!m_lockMouseToWorld)
                    {
                        offset = mouseOffset;
                    }
                }

                MovePivot(settings, pivotPreciseMode, cursorPreciseMode, offset);

                if (m_prevMouseOffset != mouseOffset)
                {
                    CursorIconFromMouseOffset(mouseOffset);
                    m_prevMouseOffset = mouseOffset;
                }
            }

            AnimatePivotPoint(settings.MoveSensitivity / 2, settings.MoveSensitivity / 8);

            if (m_lockMouseToWorld)
            {
                Vector2 targetMousePos = m_camera.WorldToScreenPoint(m_lockMousePosWorld);

                if (m_animateMousePosition)
                {
                    SetVirtualMousePosition(Vector2.Lerp(VirtualMousePosition, targetMousePos, Time.deltaTime * 20));
                    CursorIconFromMouseOffset(mouseOffset);
                }
                else
                {
                    SetVirtualMousePosition(targetMousePos);
                    CursorIconFromMouseOffset(mouseOffset);
                }
            }
            Debug.DrawLine(Pivot, Pivot + Vector3.up, Color.red);
        }

        private void MovePivot(PlayerCamCtrlSettings settings, bool pivotPreciseMode, bool cursorPreciseMode, Vector3 offset)
        {
            if (offset != Vector3.zero)
            {
                m_lockMouseToWorld = false;

                Vector3 offsetW = m_camera.cameraToWorldMatrix.MultiplyVector(offset);
                offsetW.y = 0;

                Vector3 newPivot = offsetW * settings.MoveSensitivity * Time.deltaTime;
                if (pivotPreciseMode || cursorPreciseMode)
                {
                    newPivot /= 5.0f;
                }
                newPivot += m_targetPivot;

                MapPos newMapPivot = m_voxelMap.GetMapPosition(newPivot, Weight);
                SetMapPivot(newMapPivot);
                m_targetPivot = newPivot;
            }
            else
            {
                Vector3 toPivot = m_targetPivot - m_boundsCenter;
                toPivot.y = 0;
                if (toPivot.magnitude > m_allowedRadius)
                {
                    toPivot = toPivot.normalized * m_allowedRadius;
                    m_targetPivot = Vector3.up * m_targetPivot.y + m_boundsCenter + toPivot;
                }
            }
        }

        public void MovePivot(Vector2 offset)
        {
            PlayerCamCtrlSettings settings = m_settings.PlayerCamCtrl[LocalPlayerIndex];
            MovePivot(settings, false, false, offset);
        }
    }
}
