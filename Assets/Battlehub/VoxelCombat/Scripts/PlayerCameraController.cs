using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface IPlayerCameraController
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

        Vector3 Cursor
        {
            get;
        }

        MapPos MapPivot
        {
            get;
            set;
        }

        MapPos MapCursor
        {
            get;
            set;
        }

        int Weight
        {
            get;
        }

        bool IsVisible(MapPos pos, int weight);
    }

#warning Mouse Is Not Currently Supported
    public class PlayerCameraController : MonoBehaviour, IPlayerCameraController
    {
        [SerializeField]
        private Transform m_screenCursor;
        [SerializeField]
        private Camera m_camera;
        [SerializeField]
        private int m_localPlayerIndex;

        [SerializeField]
        private Transform m_pivot;
        private Vector3 m_targetPivot;
        private MapCell m_targetPivotCell;

        private MapPos m_mapPivot;
        private MapPos m_mapCursor;
        private MapCell m_targetCursorCell;

        [SerializeField]
        private TargetHighlight TargetHiglightPrefab;
        private TargetHighlight m_targetHighlight;

        [SerializeField]
        private GameObject CursorPrefab;
        private Transform m_cursor;
        private Vector3 TargetCursor
        {
            get;
            set;
        }

        private Vector2 m_virtualMousePosition;
        private Vector2 VirtualMousePosition
        {
            get { return m_virtualMousePosition; }
            set
            {
                m_virtualMousePosition = value;
                if(m_screenCursor != null)
                {
                    m_screenCursor.transform.position = m_virtualMousePosition;
                }
            }
        }


        //private Vector2 m_virtualMoseOffset;
        //private int m_virtualMouseDeltaRow;
        //private int m_virtualMouseDeltaCol;

        private float m_prevX;
        private float m_prevY;
        private float m_prevCursorX;
        private float m_prevCursorY;

        private float m_movePivotDelay;
        private bool m_isMovingPivot;
        private float m_moveCursorDelay;
        private bool m_isMovingCursor;
        private float m_cursorSensitivity;

        private float m_camDistance;
        private Vector3 m_camToVector;

        private bool m_isInputEnabled = true;
        public bool IsInputEnabled
        {
            get { return m_isInputEnabled; }
            set { m_isInputEnabled = value; }
        }

        public Vector3 Pivot
        {
            get { return m_pivot.position; }
            private set { m_pivot.position = value; }
        }

        public MapPos MapPivot
        {
            get { return m_mapPivot; }
            set
            {
                SetMapPivot(value);
                UpdateVirtualMousePosition();
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
        }

        public MapPos MapCursor
        {
            get { return m_mapCursor; }
            set
            {
                SetMapCursor(value);
                UpdateVirtualMousePosition();
            }
        }

        private void SetMapCursor(MapPos value)
        {
            m_mapCursor = value;

            m_targetCursorCell = m_voxelMap.GetCell(m_mapCursor, GameConstants.MinVoxelActorWeight, null);

            TargetCursor = m_voxelMap.GetWorldPosition(m_mapCursor, GameConstants.MinVoxelActorWeight);

            Vector3 targetCursor = TargetCursor;

            if (m_targetCursorCell != null)
            {
                targetCursor.y = m_targetCursorCell.GetTotalHeight() * GameConstants.UnitSize;
            }
            else
            {
                targetCursor.y = 0;
            }
            TargetCursor = targetCursor;

            m_targetHighlight.CursorPos = m_mapCursor;

            targetCursor.y = 0;
        }

        public int Weight
        {
            get { return GameConstants.MinVoxelActorWeight; }
        }

        public int LocalPlayerIndex 
        {
            get { return m_localPlayerIndex; }
            set
            {
                if(m_localPlayerIndex != value)
                {
                    m_localPlayerIndex = value;
                    if (m_viewport != null) //if start method was called
                    {
                        GetViewportAndCamera();
                        ReadPlayerCamSettings();
                        SetCameraPosition();
                    }   

                    if(m_targetHighlight != null)
                    {
                        m_targetHighlight.LocalPlayerIndex = m_localPlayerIndex;
                    }
                }
            }
        }

        public Vector3 Cursor
        {
            get { return m_cursor.position; }
            private set { m_cursor.position = value; }
        }

        private IVoxelInputManager m_inputManager;
        private IGameViewport m_viewport;
        private IGlobalSettings m_settings;
        private IVoxelMap m_voxelMap;
        private IVoxelGame m_gameState;

        private object m_voxelCameraRef;
        private Rect m_camPixelRect;
        private const int JoystickMargin = 75;
        private const int MouseMargin = 10;
        
        private void Awake()
        {
            m_gameState = Dependencies.GameState;
            m_settings = Dependencies.Settings;
            m_inputManager = Dependencies.InputManager;
            m_voxelMap = Dependencies.Map;
        }

        private void Start()
        {
            GetViewportAndCamera();
            InitCamerPixelRect();
            ReadPlayerCamSettings();
            SetCameraPosition();
            CreateAndInitVoxelCamera();
            InitPivot();
            CreateCursor();

            m_screenCursor.gameObject.SetActive(false);
            //m_cursor.gameObject.SetActive(false);

            int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);
            var units = m_gameState.GetUnits(playerIndex);
            foreach(long unit in units)
            {
                IVoxelDataController dc = m_gameState.GetVoxelDataController(playerIndex, unit);
                if(VoxelData.IsControllableUnit(dc.ControlledData.Type))
                {
                    MapCursor = dc.Coordinate.ToWeight(GameConstants.MinVoxelActorWeight).MapPos;
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

        public bool IsVisible(MapPos pos, int weight)
        {
            return m_voxelMap.IsVisible(pos, weight, m_voxelCameraRef);
        }

        private void GetViewportAndCamera()
        {
            if(m_viewport != null)
            {
                m_viewport.ViewportChanged -= OnViewportChanged;
            }

            m_viewport = Dependencies.GameView.GetViewport(LocalPlayerIndex);
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
            m_cursorSensitivity = settings.CursorSensitivity;
        }

        private void SetCameraPosition()
        {
            Vector3 toCam = m_camToVector * m_camDistance;

            m_camera.transform.position = Pivot + toCam;

            m_camera.transform.LookAt(m_camera.transform.position - toCam);
        }

        private void InitCamerPixelRect()
        {
            Rect rect = m_viewport.Camera.pixelRect;
            m_camPixelRect = rect;
           // m_camPixelRect.x = 0;
           // m_camPixelRect.y = 0;

            UpdateVirtualMousePosition();
        }

        private void UpdateVirtualMousePosition()
        {
            Vector2 position = new Vector2(
                (m_camPixelRect.xMax - m_camPixelRect.xMin) / 2,
                (m_camPixelRect.yMax - m_camPixelRect.yMin) / 2);

            Ray ray = m_camera.ScreenPointToRay(position);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (groundPlane.Raycast(ray, out distance))
            {
                Vector3 wCenter = ray.GetPoint(distance);

                Vector3 wCursor = m_voxelMap.GetWorldPosition(m_mapCursor, GameConstants.MinVoxelActorWeight);
                Vector3 wPivot = m_voxelMap.GetWorldPosition(m_mapPivot, GameConstants.MinVoxelActorWeight);

                wCenter += (wCursor - wPivot);

                position = m_camera.WorldToScreenPoint(wCenter);

                VirtualMousePosition = m_camPixelRect.position + position;
            }
        }

        private bool IsVirtualMouseOutOfScreen()
        {
            return (VirtualMousePosition.x < m_camPixelRect.xMin + MouseMargin ||
                VirtualMousePosition.x > m_camPixelRect.xMax - MouseMargin ||
                VirtualMousePosition.y < m_camPixelRect.yMin + MouseMargin ||
                VirtualMousePosition.y > m_camPixelRect.yMax - MouseMargin);
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

        private void CreateCursor()
        {
            m_cursor = Instantiate(CursorPrefab).transform;
            m_cursor.name = "Cursor " + LocalPlayerIndex;
            m_cursor.gameObject.layer =  GameConstants.PlayerLayers[LocalPlayerIndex];

            m_targetHighlight = Instantiate(TargetHiglightPrefab);
            m_targetHighlight.LocalPlayerIndex = LocalPlayerIndex;

            SetMapCursor(MapPivot);

            cakeslice.Outline outline = m_cursor.GetComponent<cakeslice.Outline>();
            if(outline != null)
            {
                outline.layerMask = GameConstants.PlayerLayerMasks[LocalPlayerIndex];
            }

            Renderer renderer = m_cursor.GetComponentInChildren<Renderer>();

            if(m_gameState.IsReplay)
            {
                renderer.sharedMaterial = Dependencies.MaterialsCache.GetPrimaryMaterial(0);
            }
            else
            {
                int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);
                renderer.sharedMaterial = Dependencies.MaterialsCache.GetPrimaryMaterial(playerIndex);
            }
            

            Cursor = TargetCursor;
        }


        private void Update()
        {
            if(m_gameState.IsContextActionInProgress(LocalPlayerIndex))
            {
                return;
            }

            if (m_gameState.IsMenuOpened(LocalPlayerIndex))
            {
                return;
            }

            
            if(m_gameState.IsPaused || m_gameState.IsPauseStateChanging)
            {
                return;
            }

            if (m_camPixelRect.size == Vector2.zero)
            {
                return;
            }

            PlayerCamCtrlSettings settings = m_settings.PlayerCamCtrl[LocalPlayerIndex];

            float yAxis = 0;
            float xAxis = 0;

            if(IsInputEnabled)
            {
                xAxis = -m_inputManager.GetAxisRaw(InputAction.MoveForward, LocalPlayerIndex);
                yAxis = m_inputManager.GetAxisRaw(InputAction.MoveSide, LocalPlayerIndex);
                if (Mathf.Abs(xAxis) > Mathf.Abs(yAxis))
                {
                    yAxis = 0;
                }
                else
                {
                    xAxis = 0;
                }
            }
            

            bool yAxisChanged = m_prevY > 0 && yAxis < 0 || m_prevY < 0 && yAxis > 0;
            bool xAxisChanged = m_prevX > 0 && xAxis < 0 || m_prevX < 0 && xAxis > 0;

            if (yAxisChanged || xAxisChanged)
            {
                m_movePivotDelay = 0;
            }

            m_prevY = yAxis;
            m_prevX = xAxis;

            float zAxis = 0;
            float cursorYAxis = 0;
            float cursorXAxis = 0;

            float mouseYAxis = 0;
            float mouseXAxis = 0;

            if(IsInputEnabled)
            {
                zAxis = m_inputManager.GetAxisRaw(InputAction.Zoom, LocalPlayerIndex);
                cursorXAxis = -m_inputManager.GetAxisRaw(InputAction.CursorY, LocalPlayerIndex);
                cursorYAxis = m_inputManager.GetAxisRaw(InputAction.CursorX, LocalPlayerIndex);

                mouseXAxis = 0;// m_inputManager.GetAxisRaw(InputAction.MouseX, LocalPlayerIndex);
                mouseYAxis = 0;// m_inputManager.GetAxisRaw(InputAction.MouseY, LocalPlayerIndex);

                if(cursorXAxis != 0 || cursorYAxis != 0)
                {
                    m_screenCursor.gameObject.SetActive(false);
                    m_cursor.gameObject.SetActive(true);
                }
                else if(mouseXAxis != 0 || mouseYAxis != 0)
                {
                    m_screenCursor.gameObject.SetActive(true);
                    m_cursor.gameObject.SetActive(false);
                }

                if(xAxis != 0 || yAxis != 0)
                {
                    mouseXAxis = 0;
                    mouseYAxis = 0;
                }

                if (UnityEngine.Cursor.lockState == CursorLockMode.Locked)
                {
                    bool left = VirtualMousePosition.x < m_camPixelRect.xMin + MouseMargin;
                    bool right = VirtualMousePosition.x > m_camPixelRect.xMax - MouseMargin;
                    bool top = VirtualMousePosition.y < m_camPixelRect.yMin + MouseMargin;
                    bool bottom = VirtualMousePosition.y > m_camPixelRect.yMax - MouseMargin;
                    if(left && mouseXAxis < 0)
                    {
                        mouseXAxis = 0;
                    }
                    if(right && mouseXAxis > 0)
                    {
                        mouseXAxis = 0;
                    }
                    if(bottom && mouseYAxis > 0)
                    {
                        mouseYAxis = 0;
                    }
                    if(top && mouseYAxis < 0)
                    {
                        mouseYAxis = 0;
                    }

                    VirtualMousePosition += new Vector2(mouseXAxis, mouseYAxis);
                }

                if (mouseXAxis != 0 || mouseYAxis != 0)
                {
                    cursorXAxis = -mouseYAxis;
                    cursorYAxis = mouseXAxis;
                }
            }
            
            bool yCursorChanged = m_prevCursorY > 0 && cursorYAxis < 0 || m_prevCursorY < 0 && cursorYAxis > 0;
            bool xCursorChanged = m_prevCursorX > 0 && cursorXAxis < 0 || m_prevCursorX < 0 && cursorXAxis > 0;

            if (yCursorChanged || xCursorChanged)
            {
                m_moveCursorDelay = 0;
            }

            m_prevCursorY = cursorYAxis;
            m_prevCursorX = cursorXAxis;

            if (cursorYAxis != 0 || cursorXAxis != 0)
            {
                if(mouseXAxis != 0 || mouseYAxis != 0)
                {
                    if(UnityEngine.Cursor.lockState == CursorLockMode.Locked)
                    {
                        MoveCursorUsingMouse(settings);
                        //MoveCursor(settings);
                    }
                }
                else
                {
                    MoveCursor(cursorXAxis, cursorYAxis, settings);
                }
            }
            else
            {
                m_isMovingCursor = false;
                m_moveCursorDelay = 0;
            }

            if(IsVirtualMouseOutOfScreen())
            {
                MovePivotPoint(settings);
               // MoveCursor(settings);
                AnimatePivotPoint(settings.MoveSensitivity);

                Ray ray = m_camera.ScreenPointToRay(VirtualMousePosition);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                float distance;
                if (groundPlane.Raycast(ray, out distance))
                {
                    Cursor = ray.GetPoint(distance);
                }
            }
            else if(mouseXAxis == 0 && mouseYAxis == 0)
            {
                bool outOfScreen = false;
                if ((cursorXAxis != 0 || cursorYAxis != 0) && WillBeOutOfScreen(TargetCursor))
                {
                    xAxis = cursorXAxis;
                    yAxis = cursorYAxis;
                    outOfScreen = true;
                }

                if (yAxis != 0 || xAxis != 0)
                {
                    MovePivotPoint(xAxis, yAxis, outOfScreen, settings);
                }
                else
                {
                    m_isMovingPivot = false;
                    m_movePivotDelay = 0;

                    if (zAxis != 0)
                    {
                        m_camDistance -= zAxis * settings.ZoomSensitivity;
                        m_camDistance = Mathf.Clamp(m_camDistance, settings.MinCamDistance, settings.MaxCamDistance);
                    }
                }

                AnimatePivotPoint(settings.MoveSensitivity / 4);
                AnimateCursor();
            }
            else
            {
                AnimatePivotPoint(settings.MoveSensitivity / 4);
                AnimateCursor();
            }

   

            Debug.DrawLine(Pivot, Pivot + Vector3.up, Color.red);
        }

        private bool WillBeOutOfScreen(Vector3 cursor)
        {
            Vector3 screenCursorPosition = WorldToScreenPoint(m_viewport.Camera, cursor);// // m_viewport.Camera.WorldToScreenPoint(cursor);

            Rect camPixelRect = m_camPixelRect;
            camPixelRect.x = 0;
            camPixelRect.y = 0;

            return (screenCursorPosition.x < camPixelRect.xMin + JoystickMargin ||
                screenCursorPosition.x > camPixelRect.xMax - JoystickMargin ||
                screenCursorPosition.y < camPixelRect.yMin + JoystickMargin ||
                screenCursorPosition.y > camPixelRect.yMax - JoystickMargin);

        }
        private Vector3 WorldToScreenPoint(Camera cam, Vector3 wp)
        {
            Vector3 toCam = m_camToVector * m_camDistance;
            Matrix4x4 worldToCamera = Matrix4x4.TRS(m_targetPivot + toCam, Quaternion.LookRotation(-toCam) , Vector3.one); //no scale...
            worldToCamera = Matrix4x4.Inverse(worldToCamera);
            worldToCamera.m20 *= -1f;
            worldToCamera.m21 *= -1f;
            worldToCamera.m22 *= -1f;
            worldToCamera.m23 *= -1f;

            // calculate view-projection matrix
            Matrix4x4 mat = cam.projectionMatrix * worldToCamera;// cam.worldToCameraMatrix;//worldToCamera;

            // multiply world point by VP matrix
            Vector4 temp = mat * new Vector4(wp.x, wp.y, wp.z, 1f);

            if (temp.w == 0f)
            {
                // point is exactly on camera focus point, screen point is undefined
                // unity handles this by returning 0,0,0
                return Vector3.zero;
            }
            else
            {
                // convert x and y from clip space to window coordinates
                temp.x = (temp.x / temp.w + 1f) * .5f * cam.pixelWidth;
                temp.y = (temp.y / temp.w + 1f) * .5f * cam.pixelHeight;
                return new Vector3(temp.x, temp.y, wp.z);
            }
        }

        private void MoveCursor(float xAxis, float yAxis, PlayerCamCtrlSettings settings)
        {
            m_cursorSensitivity = settings.CursorSensitivity;

            if (Mathf.Abs(xAxis) > Mathf.Abs(yAxis))
            {
                yAxis = 0;
            }
            else
            {
                xAxis = 0;
            }


            m_moveCursorDelay -= Time.deltaTime;
            if(m_moveCursorDelay <= 0)
            {
                Vector3 offset;
       
                offset = new Vector3(xAxis, 0, -yAxis);

                offset.y = 0;
                offset.Normalize();

                int deltaRow = Mathf.RoundToInt(offset.x);
                int deltaCol = Mathf.RoundToInt(offset.z);

                SetMapCursor(new MapPos(MapCursor.Row + deltaRow, MapCursor.Col + deltaCol));

                if (m_isMovingCursor)
                {
                    m_moveCursorDelay = 0.75f / m_cursorSensitivity;
                }
                else
                {
                    m_moveCursorDelay = 1.5f / m_cursorSensitivity;
                }

                m_isMovingCursor = true;
            }
        }

        private void MoveCursorUsingMouse(PlayerCamCtrlSettings settings)
        {
            m_cursorSensitivity = settings.CursorSensitivity;

            Ray ray = m_camera.ScreenPointToRay(VirtualMousePosition);

            Vector3 worldPoint = Vector3.zero;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                worldPoint = hit.point;
            }
            else
            {
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                float distance;
                if (groundPlane.Raycast(ray, out distance))
                {
                    worldPoint = ray.GetPoint(distance);
                }
            }


            MapPos cursorPos = m_voxelMap.GetMapPosition(worldPoint, GameConstants.MinVoxelActorWeight);

            if (cursorPos != MapCursor)
            {
                SetMapCursor(cursorPos);
                m_isMovingCursor = true;
            }
        }

        private void MovePivotPoint(PlayerCamCtrlSettings settings)
        {
            m_movePivotDelay -= Time.deltaTime;
            if (m_movePivotDelay <= 0)
            { 
                Vector3 offset = Vector3.zero;
                bool left = VirtualMousePosition.x < m_camPixelRect.xMin + MouseMargin;
                bool right = VirtualMousePosition.x > m_camPixelRect.xMax - MouseMargin;
                bool top = VirtualMousePosition.y < m_camPixelRect.yMin + MouseMargin;
                bool bottom = VirtualMousePosition.y > m_camPixelRect.yMax - MouseMargin;

                if(left)
                {
                    offset.x += 1;
                }
                if(right)
                {
                    offset.x -= 1;
                }
                if(top)
                {
                    offset.y -= 1;
                }
                if(bottom)
                {
                    offset.y += 1;
                }
                
                offset = m_camera.transform.TransformVector(offset);

                offset.y = 0;
                offset.Normalize();

                int deltaRow = Mathf.RoundToInt(offset.x);
                int deltaCol = Mathf.RoundToInt(offset.z);

                SetMapPivot(new MapPos(MapPivot.Row + deltaRow, MapPivot.Col + deltaCol));

                m_movePivotDelay = 0.76f / settings.MoveSensitivity;
                m_isMovingPivot = true;
            }
        }

        private void MovePivotPoint(float xAxis, float yAxis, bool outOfScreen, PlayerCamCtrlSettings settings)
        {
            m_movePivotDelay -= Time.deltaTime;
            if (m_movePivotDelay <= 0)
            {
                Vector3 offset = new Vector3(xAxis, 0, -yAxis);

                offset.y = 0;
                offset.Normalize();

                int deltaRow = Mathf.RoundToInt(offset.x);
                int deltaCol = Mathf.RoundToInt(offset.z);

                SetMapPivot(new MapPos(MapPivot.Row + deltaRow, MapPivot.Col + deltaCol));

                if (!outOfScreen)
                {
                    SetMapCursor(new MapPos(MapCursor.Row + deltaRow, MapCursor.Col + deltaCol));

                    if (WillBeOutOfScreen(TargetCursor))
                    {
                        SetMapCursor(new MapPos(MapCursor.Row - deltaRow, MapCursor.Col - deltaCol));
                        FixMapCursor();
                    }
                }
                else
                {
                    if (WillBeOutOfScreen(TargetCursor))
                    {
                        FixMapCursor();
                    }
                }

                if (m_isMovingPivot)
                {
                    m_movePivotDelay = 0.75f / settings.MoveSensitivity;
                }
                else
                {
                    m_movePivotDelay = 1.5f / settings.MoveSensitivity;
                }

                m_isMovingPivot = true;
            }
        }

   

        private void FixMapCursor()
        {
            for (int r = 0; r <= 50; ++r)
            {
                for (int i = -1; i <= 1; ++i)
                {
                    for (int j = -1; j <= 1; ++j)
                    {
                        SetMapCursor(new MapPos(MapCursor.Row + i * r, MapCursor.Col + j * r));
                        if (WillBeOutOfScreen(TargetCursor))
                        {
                            SetMapCursor(new MapPos(MapCursor.Row - i * r, MapCursor.Col - j * r));
                        }
                        else
                        {
                            Debug.Log("Map Cursor Fixed");
                            return;
                        }
                    }
                }
            }
        }

        private void AnimateCursor()
        {
            Vector3 targetCursor = TargetCursor;

            MapPos cursorPos = m_voxelMap.GetMapPosition(targetCursor, GameConstants.MinVoxelActorWeight);
            MapCell cursorCell = m_voxelMap.GetCell(cursorPos, GameConstants.MinVoxelActorWeight, null);

            if (cursorCell != null)
            {
                float totalHeight = GetTotalHeight(cursorCell);
                if(totalHeight < Cursor.y)
                {
                    cursorPos = m_voxelMap.GetMapPosition(Cursor, GameConstants.MinVoxelActorWeight);
                    cursorCell = m_voxelMap.GetCell(cursorPos, GameConstants.MinVoxelActorWeight, null);
                    if(cursorCell != null)
                    {
                        totalHeight = GetTotalHeight(cursorCell);
                    }
                }

                targetCursor.y = totalHeight * GameConstants.UnitSize;
            }
            else
            {
                targetCursor.y = 0;
            }
            TargetCursor = targetCursor;

            Vector3 cursor;
            if ((TargetCursor - Cursor).magnitude > 0.1f)
            {
                cursor = Cursor + (TargetCursor - Cursor) * Time.deltaTime * m_cursorSensitivity;
            }
            else
            {
                cursor = Vector3.Lerp(Cursor, TargetCursor, Time.deltaTime * m_cursorSensitivity);
            }

            if(TargetCursor.y > cursor.y)
            {
                cursor.y = TargetCursor.y;
            }
            

            Cursor = cursor;
        }

        private static float GetTotalHeight(MapCell cursorCell)
        {
            float totalHeight = 0;
            for (int i = 0; i < cursorCell.Children.Length; ++i)
            {
                for (int j = 0; j < cursorCell.Children[i].Children.Length; ++j)
                {
                    MapCell childOfChild = cursorCell.Children[i].Children[j];
                    float childHeight = childOfChild.GetTotalHeight();
                    if (childHeight > totalHeight)
                    {
                        totalHeight = childHeight;
                    }
                }
            }

            return totalHeight;
        }

        private void AnimatePivotPoint(float sensitivity)
        {
            if (m_targetPivotCell != null)
            {
                m_targetPivot.y = m_targetPivotCell.GetTotalHeight((int)KnownVoxelTypes.Ground) * GameConstants.UnitSize;
            }
            else
            {
                m_targetPivot.y = 0;
            }

            if(m_targetPivot != Pivot)
            {
                if ((m_targetPivot - Pivot).magnitude > 0.1f)
                {
                    Pivot = Pivot + (m_targetPivot - Pivot) * Time.deltaTime * sensitivity;
                }
                else
                {
                    Pivot = Vector3.Lerp(Pivot, m_targetPivot, Time.deltaTime * sensitivity);
                }

                Vector3 toCam = m_camToVector * m_camDistance;

                m_camera.transform.position = Pivot + toCam;

                m_camera.transform.LookAt(Pivot);
            }
        }

        private void OnViewportChanged()
        {
            InitCamerPixelRect();
        }

    }
}

