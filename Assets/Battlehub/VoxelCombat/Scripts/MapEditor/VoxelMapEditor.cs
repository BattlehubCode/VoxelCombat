using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace Battlehub.VoxelCombat
{
    public class VoxelMapEditor : MonoBehaviour
    {
        private MouseOrbit m_editorCamera;
        [SerializeField]
        private GameObject m_pivot;
        private Plane m_groundPlane = new Plane(Vector3.up, Vector3.zero);
        private Vector3 m_lastMousePosition;
        private Vector3 m_prevPivotPosition;

        [SerializeField]
        private GameObject m_editorRoot;

        [SerializeField]
        private Button m_closeButton;

        [SerializeField]
        private Button m_topViewButton;

        [SerializeField]
        private Slider m_brushSizeSlider;

        [SerializeField]
        private Slider m_brushWeightSlider;

        [SerializeField]
        private InputField m_brushHeightInput;

        [SerializeField]
        private InputField m_ownerInput;

        private int BrushSize
        {
            get { return (int)m_brushSizeSlider.value; }
        }

        private int BrushWeight
        {
            get { return (int)m_brushWeightSlider.value; }
            set { m_brushWeightSlider.value = value; }
        }

        private int m_brushHeight;
        private int BrushHeight
        {
            get { return m_brushHeight; }
            set { m_brushHeight = value; }
        }

        private int m_owner;
        private int Owner
        {
            get { return m_owner; }
        }

        private const int m_mapEditorOwner = -1;

        [SerializeField]
        private MapEditorPrefab m_mapEditorPrefab;
        [SerializeField]
        private Transform m_prefabPanel;
        [SerializeField]
        private GridRenderer m_gridRenderer;

        private List<MapEditorPrefab> m_mapEditorPrefabs = new List<MapEditorPrefab>();
        private MapEditorPrefab m_selectedPrefab;
        private VoxelAbilities m_selectedAbilities;     

        private IConsole m_console;
        private IVoxelGame m_gameState;
        private IVoxelInputManager m_inputManager;
        private IVoxelMap m_voxelMap;
        private IVoxelFactory m_factory;
        private IGameView m_gameView;
        private IGameServer m_gameServer;
        private IGlobalSettings m_gSettings;
        private IProgressIndicator m_progress;

        private object m_voxelCameraRef;

        [SerializeField]
        private bool m_isOpened = false;

        private MapCell m_lastModifiedCell;
        private bool m_editorCreateButtonDown;
        private bool m_editorDestroyButtonDown;

        private bool IsOpened
        {
            get { return m_isOpened; }
            set
            {
                if(m_isOpened != value)
                {
                    m_isOpened = value;
                    OnIsOpenedChanged(value);
                }
            }
        }

        private void OnIsOpenedChanged(bool value)
        {
            enabled = value;
            if(m_editorRoot != null)
            {
                m_editorRoot.SetActive(value);
            }

       
            if(value)
            {             
                GetDependencies();

                m_gameState.IsPaused = true;

                m_voxelCameraRef = m_voxelMap.CreateCamera(GameConstants.VoxelCameraRadius, GameConstants.VoxelCameraWeight);

                IGameViewport zeroViewport = m_gameView.GetViewport(0);
                Camera zeroCamera = zeroViewport.Camera;
                Camera editorCamera = Instantiate(zeroViewport.Camera, zeroCamera.transform.parent);
                editorCamera.rect = new Rect(0, 0, 1, 1);

                m_editorCamera = editorCamera.GetComponent<MouseOrbit>();
                if (m_editorCamera == null)
                {
                    m_editorCamera = editorCamera.gameObject.AddComponent<MouseOrbit>();
                    m_editorCamera.name = "Editor Camera";
                }

                SetupEditorCamera();
            }
            else
            {
                if (m_editorCamera != null)
                {
                    Destroy(m_editorCamera.gameObject);
                }

                m_voxelMap.DestroyCamera(m_voxelCameraRef);

                m_gameView.IsOn = true;

                Dependencies.State.Clear();
                Dependencies.Navigation.Navigate("Game");
            }
        }

        private void SetupEditorCamera()
        {
            m_pivot.transform.position = Vector3.zero;
            m_prevPivotPosition = Vector3.zero;

            //Vector3 position = m_editorCamera.transform.position;
            //position.y = 0;
            //m_pivot.transform.position = position;

            m_gameView.IsOn = false;

            GLCamera glCamera = m_editorCamera.GetComponent<GLCamera>();
            glCamera.CullingMask = (int)RTLayer.SceneView;

            Component[] components = m_editorCamera.GetComponents<Component>();
            for (int i = 0; i < components.Length; ++i)
            {
                Component component = components[i];
                if (!(component is Camera || component is MouseOrbit || component is Transform || component is GLCamera))
                {
                    Destroy(component);
                }
            }

            m_gridRenderer.Target = m_pivot.transform;
            OnTopView();
        }

        private void Awake()
        {
            m_console = Dependencies.Console;
            m_console.Command += OnConsoleCommand;
        }

        private void Start()
        {
            GetDependencies();

            Voxel[] prefabs = m_factory.GetPrefabs();
            for (int i = 0; i < prefabs.Length; ++i)
            {
                MapEditorPrefab mapEditorPrefab = Instantiate(m_mapEditorPrefab);
                mapEditorPrefab.Prefab = prefabs[i];
                mapEditorPrefab.transform.SetParent(m_prefabPanel);
                if (prefabs[i].Type == (int)KnownVoxelTypes.Ground)
                {
                    VoxelAbilities abilities = new VoxelAbilities(prefabs[i].Type);

                    mapEditorPrefab.IsSelected = true;
                    mapEditorPrefab.AllowHeightEditing = true;
                    m_selectedPrefab = mapEditorPrefab;
                    m_selectedAbilities = abilities;

                    BrushHeight = abilities.MinHeight;
                    BrushWeight = abilities.MinWeight;// m_selectedPrefab.Prefab.Weight;
                }

                m_mapEditorPrefabs.Add(mapEditorPrefab);
                mapEditorPrefab.Selected += OnSelected;
                mapEditorPrefab.Unselected += OnUnselected;
            }

            //OnIsOpenedChanged(IsOpened);
            SetupTools();

            if (m_isOpened)
            {
                m_pivot.transform.position = Vector3.zero;
                OnTopView();
            }

            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnClose);
            }
            if (m_topViewButton != null)
            {
                m_topViewButton.onClick.AddListener(OnTopView);
            }
            if (m_brushSizeSlider != null)
            {
                m_brushSizeSlider.onValueChanged.AddListener(OnBrushSizeChanged);
            }
            if (m_brushWeightSlider != null)
            {
                m_brushWeightSlider.onValueChanged.AddListener(OnBrushWeightChanged);
            }
            if (m_brushHeightInput != null)
            {
                m_brushHeightInput.onValidateInput += OnBrushHeightValidateInput;
                m_brushHeightInput.onValueChanged.AddListener(OnBrushHeightChanged);
                m_brushHeightInput.onEndEdit.AddListener(OnBrushHeightEndEdit);
            }
            if (m_ownerInput != null)
            {
                m_ownerInput.onValidateInput += OnOwnerValidateInput;
                m_ownerInput.onValueChanged.AddListener(OnOwnerChanged);
            }

        }

        private void GetDependencies()
        {
            m_gameState = Dependencies.GameState;
            m_inputManager = Dependencies.InputManager;
            m_voxelMap = Dependencies.Map;
            m_factory = Dependencies.VoxelFactory;
            m_gameView = Dependencies.GameView;
            m_gameServer = Dependencies.GameServer;
            m_gSettings = Dependencies.Settings;
            m_progress = Dependencies.Progress;
        }


        private void SetupTools()
        {
            if(m_brushSizeSlider != null)
            {
                m_brushSizeSlider.minValue = 1;
                m_brushSizeSlider.maxValue = m_voxelMap.Map.Weight;
                m_brushSizeSlider.wholeNumbers = true;
            }
            
            if(m_brushWeightSlider != null)
            {
                if (m_selectedPrefab != null)
                {
                    m_brushWeightSlider.minValue = m_selectedPrefab.Prefab.MinWeight;
                }
                else
                {
                    m_brushWeightSlider.minValue = 0;
                }

                m_brushWeightSlider.maxValue = m_voxelMap.GetCameraWeight(m_voxelCameraRef);
                m_brushWeightSlider.wholeNumbers = true;
            }

            if(m_brushHeightInput != null)
            {
                if(m_selectedAbilities != null)
                {
                    m_brushHeightInput.text = (m_selectedAbilities.MinHeight).ToString();
                }
                else
                {
                    m_brushHeightInput.text = "0";
                }
                
            }

            if (m_ownerInput != null)
            {
                if(string.IsNullOrEmpty(m_ownerInput.text))
                {
                    m_ownerInput.text = "0";
                }
            }
        }

        private void OnDestroy()
        {
            if(m_console != null)
            {
                m_console.Command -= OnConsoleCommand;
            }

            for (int i = 0; i < m_mapEditorPrefabs.Count; i++)
            {
                MapEditorPrefab mapEditorPrefab = m_mapEditorPrefabs[i];
                if(mapEditorPrefab != null)
                {
                    mapEditorPrefab.Selected -= OnSelected;
                    mapEditorPrefab.Unselected -= OnUnselected;
                }
            }

            if(m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnClose);
            }

            if(m_topViewButton != null)
            {
                m_topViewButton.onClick.RemoveListener(OnTopView);
            }

            if(m_brushSizeSlider != null)
            {
                m_brushSizeSlider.onValueChanged.RemoveListener(OnBrushSizeChanged);
            }

            if(m_brushWeightSlider != null)
            {
                m_brushWeightSlider.onValueChanged.RemoveListener(OnBrushWeightChanged);
            }

            if(m_brushHeightInput)
            {
                m_brushHeightInput.onValidateInput -= OnBrushHeightValidateInput;
                m_brushHeightInput.onValueChanged.RemoveListener(OnBrushHeightChanged);
                m_brushHeightInput.onEndEdit.RemoveListener(OnBrushHeightEndEdit);
            }

            if (m_ownerInput != null)
            {
                m_ownerInput.onValidateInput -= OnOwnerValidateInput;
                m_ownerInput.onValueChanged.RemoveListener(OnOwnerChanged);
            }

            m_isOpened = false;
        }

        private void Update()
        {
            if(m_inputManager.GetButtonDown(InputAction.EditorCreate, m_mapEditorOwner))
            {
                m_lastModifiedCell = null;
                m_editorCreateButtonDown = true;
            }

            if (m_inputManager.GetButtonUp(InputAction.EditorCreate, m_mapEditorOwner))
            {
                m_lastModifiedCell = null;
                m_editorCreateButtonDown = false;
            }

            if(m_inputManager.GetButtonDown(InputAction.EditorDestroy, m_mapEditorOwner))
            {
                m_lastModifiedCell = null;
                m_editorDestroyButtonDown = true;
            }

            if (m_inputManager.GetButtonUp(InputAction.EditorDestroy, m_mapEditorOwner))
            {
                m_lastModifiedCell = null;
                m_editorDestroyButtonDown = false;
            }
            
            if (m_inputManager.GetButton(InputAction.EditorCreate, m_mapEditorOwner))
            {
                if(m_editorCreateButtonDown)
                {
                    CreateVoxel();
                }
            }
            else if(m_inputManager.GetButton(InputAction.EditorDestroy, m_mapEditorOwner))
            {
                if(m_editorDestroyButtonDown)
                {
                    DestroyVoxel();
                }
            }

            if(m_inputManager.GetButtonDown(InputAction.EditorPan, m_mapEditorOwner))
            {
                m_lastMousePosition = m_inputManager.MousePosition;
                m_prevPivotPosition = m_pivot.transform.position;
            }
            else if(m_inputManager.GetButtonUp(InputAction.EditorPan, m_mapEditorOwner))
            {
                m_editorCamera.enabled = false;
            }

            bool pan = m_inputManager.GetButton(InputAction.EditorPan, m_mapEditorOwner);
            bool rotate = m_inputManager.GetButton(InputAction.EditorRotate, m_mapEditorOwner);
            if(!pan && !rotate)
            {
                m_editorCamera.Zoom();
            }

            if (rotate)
            {
                if(pan)
                {
                    m_editorCamera.enabled = true;
                } 
               
            }
            else
            {
                m_editorCamera.enabled = false;
                if (pan)
                {
                    Pan();
                }
            }
        }


        private int GetTotalHeight(MapCell cell, int type)
        {
            int height = 0;

            VoxelData voxelData = cell.VoxelData;
            while (voxelData != null)
            {
                if (voxelData.Type == type)
                {
                    int newHeight = voxelData.Altitude + voxelData.Height;
                    if (newHeight < height)
                    {
                        Debug.LogError("Wrong height or altitude of VoxelData in VoxelData chain");
                    }
                    else
                    {
                        height = newHeight;
                    }
                }

                voxelData = voxelData.Next;
            }
            return height;
        }

        private bool CanSimplify(int type)
        {
            //TODO: Add other types which could be simplified (WATER for example)
            return type == (int)KnownVoxelTypes.Ground;
        }

        private void CreateVoxel()
        {
            if (m_selectedPrefab == null)
            {
                Debug.Log("Prefab is not selected");
                return;
            }

            Vector3 mousePosition = m_inputManager.MousePosition;
            Ray ray = m_editorCamera.Camera.ScreenPointToRay(mousePosition);

            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {
                Voxel voxel = hit.collider.GetComponentInParent<Voxel>();
                if(voxel != null)
                {
                    if(Vector3.Dot(hit.normal, Vector3.up) > 0.99)
                    {
                        CreateVoxel(hit.point);
                    }
                    else
                    {
                        CreateVoxel(hit.collider.transform.position);
                    }
                }
            }
            else
            {
                float enter;
                if (m_groundPlane.Raycast(ray, out enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);

                    CreateVoxel(hitPoint);
                }
            }
        }


        private void CreateVoxel(Vector3 hitPoint)
        {
            int weight = BrushWeight;
            int type = m_selectedPrefab.Prefab.Type;

            int selectedWeight = Mathf.Min(m_voxelMap.Map.Weight, weight + BrushSize - 1);

            MapPos mappos = m_voxelMap.GetMapPosition(hitPoint, selectedWeight);
            MapCell cell = m_voxelMap.GetCell(mappos, selectedWeight, m_voxelCameraRef);

            if (cell != null && m_lastModifiedCell != cell)
            {
                if(cell.HasDescendantsWithVoxelData())
                {
                    Debug.LogWarning("Unable to create in cell with child data available");
                    m_lastModifiedCell = cell;
                    return;
                }

                if(type == (int)KnownVoxelTypes.Ground)
                {
                    if (cell.VoxelData != null && cell.VoxelData.GetLast().Type != type)
                    {
                        Debug.LogWarning("Unable to create voxel of type GROUND attached to voxel of another type");
                        m_lastModifiedCell = cell;
                        return;
                    }
                    else if(cell.Parent != null)
                    {
                        MapCell parent = cell.Parent;
                        while(parent != null)
                        {
                            if(parent.VoxelData != null && parent.VoxelData.GetLast().Type != type)
                            {
                                Debug.LogWarning("Unable to create voxel of type GROUND attached to voxel of another type");
                                m_lastModifiedCell = cell;
                                return;
                            }
                                    
                            parent = parent.Parent;
                        }
                    }
                }
                

                if (BrushHeight == 0 && m_selectedAbilities.VariableHeight)
                {
                    //Following lines should prevent stacking of zero height voxels.

                    DestroyVoxels(cell, type);

                    MapCell parent = cell.Parent;
                    while(parent != null)
                    {
                        if(parent.VoxelData != null)
                        {
                            VoxelData data = parent.VoxelData.GetFirstOfType(type);
                            DestroyVoxel(parent, data);
                        }
                        parent = parent.Parent;
                    }
                }
                
                //if(CanSimplify(type))
                //{
                //    //If type is ground -> simplify (fill one big cell instead of multiple small cells);
                //    CreateVoxels(cell, type, selectedWeight, selectedWeight);
                //}
                //else
                //{
               CreateVoxels(cell, type, weight, selectedWeight);
                ///}
                
                m_lastModifiedCell = cell;
            }
        }


        private void CreateVoxels(MapCell cell, int type, int weight, int currentWeight)
        {
            if(weight == currentWeight)
            {
                Vector3 position = m_voxelMap.GetWorldPosition(cell.GetPosition(), weight);
                position.y = cell.GetTotalHeight() * GameConstants.UnitSize;

                VoxelData data = CreateVoxel(position, type, weight, BrushHeight);

                if (cell.VoxelData == null)
                {
                    cell.VoxelData = data;
                }
                else
                {
                    cell.VoxelData.Append(data);
                }
            }
            else
            {
                if(cell.Children != null)
                {
                    for(int i = 0; i < cell.Children.Length; ++i)
                    {
                        MapCell childCell = cell.Children[i];

                        CreateVoxels(childCell, type, weight, currentWeight - 1);
                    }
                }
            }
        }

        private VoxelData CreateVoxel(Vector3 position, int type, int weight, int height)
        {
            Voxel voxel = m_factory.Acquire(type);
            voxel.transform.position = position;

    
            VoxelData data = m_factory.InstantiateData(type);
            data.Type = type;

            data.Height = m_selectedAbilities.VariableHeight ? m_selectedAbilities.ClampHeight(height) : m_selectedAbilities.EvaluateHeight(weight);
            data.Altitude = Mathf.RoundToInt(position.y / GameConstants.UnitSize);
            data.Weight = weight;
            data.Owner = Owner;
            data.Health = m_selectedAbilities.DefaultHealth;
            data.VoxelRef = voxel;

            voxel.ReadFrom(data);
            return data;
        }

        private void DestroyVoxel()
        {
            Vector3 mousePosition = m_inputManager.MousePosition;
            Ray ray = m_editorCamera.Camera.ScreenPointToRay(mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Voxel voxel = hit.collider.GetComponentInParent<Voxel>();
                if (voxel != null)
                {
                    DestroyVoxel(hit.collider.transform.position);
                }
            }
            else
            {
                float enter;
                if (m_groundPlane.Raycast(ray, out enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    DestroyVoxel(hitPoint);
                }
            }
        }

        private void DestroyVoxel(Vector3 hitPoint)
        {
            int weight = BrushWeight;
            int type = m_selectedPrefab.Prefab.Type;

            int selectedWeight = Mathf.Min(m_voxelMap.Map.Weight, weight + BrushSize - 1);

            MapPos mappos = m_voxelMap.GetMapPosition(hitPoint, selectedWeight);

            MapCell cell = m_voxelMap.GetCell(mappos, selectedWeight, m_voxelCameraRef);
            if (cell != null && m_lastModifiedCell != cell)
            {
                DestroyVoxels(cell, type);
                //if (CanSimplify(type))
                //{
                //    //If type is ground -> simplify (fill one big cell instead of multiple small cells); 
                //    TryToSimplify(cell, type, selectedWeight);
                //}
               
                m_lastModifiedCell = cell;
            }
        }

        private void DestroyVoxels(MapCell cell, int type)
        {
            if (cell.VoxelData != null)
            {
                VoxelData data = cell.VoxelData.GetFirstOfType(type);
                DestroyVoxel(cell, data);
            }
            else
            {
                if (cell.Children != null)
                {
                    for (int i = 0; i < cell.Children.Length; ++i)
                    {
                        MapCell childCell = cell.Children[i];
                        DestroyVoxels(childCell, type);
                    }
                }
            }
        }

        private void DestroyVoxel(MapCell cell, VoxelData data)
        {
            if (data != null)
            {
                if (data.VoxelRef != null)
                {
                    m_factory.Release(data.VoxelRef);
                    int height = data.Height;
                    VoxelData next = data.Next;

                    DecreaseHeight(height, next);

                    //#warning Could be broken ...
                    Remove(cell.VoxelData, data.VoxelRef);

                    cell.ForEachDescendant(descendant =>
                    {
                        if (descendant.VoxelData != null)
                        {
                            DecreaseHeight(height, descendant.VoxelData);
                        }
                    });
                }

                if (cell.VoxelData == data)
                {
                    cell.VoxelData = data.Next;
                }
            }
        }

        public void Remove(VoxelData voxelData, Voxel voxel)
        {
            VoxelData data = voxelData;
            VoxelData prev = null;
            do
            {
                if (data.VoxelRef == voxel)
                {
                    if (prev != null)
                    {
                        prev.Next = data.Next;
                    }

                    data.VoxelRef = null;
                    break;
                }

                prev = data;
                data = data.Next;
            }
            while (data != null);
        }

        private static void DecreaseHeight(int height, VoxelData next)
        {
            while (next != null)
            {
                next.Altitude -= height;
                if (next.VoxelRef != null)
                {
                    Vector3 position = next.VoxelRef.transform.position;
                    next.VoxelRef.Altitude = next.Altitude;
                }
                next = next.Next;
            }
        }

        private void Pan()
        {
            Vector3 pointOnDragPlane;
            Vector3 prevPointOnDragPlane;

            bool gotPoint = GetPointOnDragPlane(m_inputManager.MousePosition, out pointOnDragPlane);
            bool gotLastPoint = GetPointOnDragPlane(m_lastMousePosition, out prevPointOnDragPlane);

            if (gotPoint && gotLastPoint)
            {
                Vector3 delta = (pointOnDragPlane - prevPointOnDragPlane);
                m_lastMousePosition = m_inputManager.MousePosition;
                m_editorCamera.Camera.transform.position -= delta;
                m_pivot.transform.position -= delta;
            }

            TryToMoveVoxelCamera();
        }

        private void TryToMoveVoxelCamera()
        {
            MapPos prevPivotPos = m_voxelMap.GetMapPosition(m_prevPivotPosition, m_voxelMap.GetCameraWeight(m_voxelCameraRef));
            MapPos pivotPos = m_voxelMap.GetMapPosition(m_pivot.transform.position, m_voxelMap.GetCameraWeight(m_voxelCameraRef));

            int rowOffset = pivotPos.Row - prevPivotPos.Row;
            int colOffset = pivotPos.Col - prevPivotPos.Col;
            if (rowOffset != 0 || colOffset != 0)
            {
                m_voxelMap.MoveCamera(rowOffset, colOffset, m_voxelCameraRef);
                m_prevPivotPosition = m_pivot.transform.position;
            }
        }

        private bool GetPointOnDragPlane(Vector3 mouse, out Vector3 point)
        {
            Ray ray = m_editorCamera.Camera.ScreenPointToRay(mouse);
            float distance;
            if (m_groundPlane.Raycast(ray, out distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        private void OnSelected(object sender, System.EventArgs e)
        {
            if(m_selectedPrefab != null)
            {
                m_selectedPrefab.IsSelected = false;
            }
            m_selectedPrefab = (MapEditorPrefab)sender;
            m_selectedAbilities = new VoxelAbilities(m_selectedPrefab.Prefab.Type);

            SetupTools();

            BrushHeight = m_selectedAbilities.MinHeight;
            BrushWeight = m_selectedAbilities.MinWeight;


        }

        private void OnUnselected(object sender, System.EventArgs e)
        {
            if (m_selectedPrefab != null)
            {
                m_selectedPrefab.IsSelected = false;
                m_selectedPrefab = null;
            }
        }

        private void OnConsoleCommand(IConsole console, string cmd, params string[] args)
        {
            cmd = cmd.ToLower();
            if(cmd == "mapeditor")
            {
                if(args.Length > 0)
                {
                    if(args[0].ToLower() == "close")
                    {
                        IsOpened = false;
                    }
                    else
                    {
                        IsOpened = true;
                    }
                }
                else
                {
                    IsOpened = true;
                }
            }
            

            if(!IsOpened)
            {
                return;
            }

            if (cmd == "close")
            {
                IsOpened = false;
            }
            else if (cmd == "save")
            {
                string name = "default";
                if (args.Length > 0)
                {
                    name = args[0];
                }

                OnSave(name);
            }
            else if (cmd == "load")
            {
                string name = "default";
                if (args.Length > 0)
                {
                    name = args[0];
                }

                OnLoad(name);
            }
            else if (cmd == "create")
            {
                if (args.Length == 1)
                {
                    int weight;
                        
                    if(int.TryParse(args[0].Trim(), out weight))
                    {
                        OnCreate(weight);
                    }
                    else
                    {
                        m_console.Echo("create <weight>");
                    }
                }
                else
                {
                    m_console.Echo("create <weight>");
                }
            }
            else if(cmd == "upload")
            {
                if(args.Length == 2)
                {
                    string name = args[0];
                    if(name != null)
                    {
                        name = name.Trim();
                    }

                    int maxPlayers;
                    if(string.IsNullOrEmpty(name) || !int.TryParse(args[1], out maxPlayers))
                    {
                        m_console.Echo("upload <mapname> <maxplayers>");
                    }
                    else
                    {
                        PlayerPrefs.SetString("lastmap", name);

                        OnUploadMap(name, maxPlayers);
                    }
                }
                else
                {
                    m_console.Echo("upload <mapname> <maxplayers>");
                }
            }
            else if(cmd == "download")
            {
                if(args.Length == 1)
                {
                    string name = args[0];
                    if (name != null)
                    {
                        name = name.Trim();
                    }

                    OnDownloadMap(name);
                }
                else
                {
                    m_console.Echo("download <mapname>");
                }
            }
            else if(cmd == "fill")
            {
                if (args.Length > 1)
                {
                    int weight;
                    int type;
                    if (int.TryParse(args[0], out weight) && int.TryParse(args[1], out type))
                    {
                        //Fill(weight, type); //Does not work properly                        
                    }
                    else
                    {
                        m_console.Echo("fill <weight> <type>");
                    }
                }
                else
                {
                    m_console.Echo("fill <weight> <type>");
                }
            }
            else if (cmd == "movecam")
            {
                if (args.Length > 1)
                {
                    int rowOffset;
                    int colOffset;
                    if (int.TryParse(args[0], out rowOffset) && int.TryParse(args[1], out colOffset))
                    {
                        m_voxelMap.MoveCamera(rowOffset, colOffset, 0);
                    }
                }
            }
            else if(cmd == "help")
            {
                m_console.Echo("movecam <int> <int>");
                m_console.Echo("save <string>");
                m_console.Echo("load <string>");
                m_console.Echo("create <weight>");
                m_console.Echo("fill <weight> <type>");
                m_console.Echo("download <mapname>");
                m_console.Echo("upload <mapname> <maxplayers>");
                m_console.Echo("close");
            }
            
        }

        private void OnTopView()
        {
            Vector3 position;
            const float height = 30;
            const float maxAngle = 90;
            const float minAngle = 45;

            position = m_pivot.transform.position;
            position.y = height;
            m_editorCamera.transform.position = position;
            
            m_editorCamera.Target = m_pivot.transform;
            m_editorCamera.transform.LookAt(m_editorCamera.Target);

            m_editorCamera.Distance = height;
            m_editorCamera.YMaxLimit = maxAngle;
            m_editorCamera.YMinLimit = minAngle;
            m_editorCamera.enabled = false;
            m_editorCamera.SyncAngles();

            MapPos mapPos = m_voxelMap.GetMapPosition(m_pivot.transform.position, m_voxelMap.GetCameraWeight(m_voxelCameraRef));
            m_voxelMap.SetCameraPosition(mapPos.Row, mapPos.Col, m_voxelCameraRef);
        }

        private void OnClose()
        {
            IsOpened = false;
        }

        private void OnBrushSizeChanged(float value)
        {
           
        }

        private void OnBrushWeightChanged(float value)
        {
            
        }

        private void OnBrushHeightChanged(string str)
        {
            int.TryParse(str, out m_brushHeight);
            
            if(m_selectedAbilities.MinHeight > BrushHeight)
            {
                BrushHeight = m_selectedAbilities.MinHeight;
            }
        }

        private void OnBrushHeightEndEdit(string str)
        {
            m_brushHeightInput.text = BrushHeight.ToString();
        }

        private char OnBrushHeightValidateInput(string text, int charIndex, char addedChar)
        {
            if (char.IsDigit(addedChar))
            {
                return addedChar;
            }
            return '\0';
        }

        private void OnOwnerChanged(string str)
        {
            int maxPlayers = m_gameState.MaxPlayersCount;
            if (int.TryParse(str, out m_owner))
            {
                if(m_owner > maxPlayers)
                {
                    m_owner = maxPlayers;
                    m_ownerInput.text = m_owner.ToString();
                }
                else if(m_owner < 0)
                {
                    m_owner = 0;
                    m_ownerInput.text = m_owner.ToString();
                }
                
            }
        }

        private char OnOwnerValidateInput(string text, int charIndex, char addedChar)
        {
            if (char.IsDigit(addedChar) || addedChar == '-')
            {
                return addedChar;
            }
            return '\0';
        }

        private void OnUploadMap(string mapName, int maxPlayers)
        {
            m_progress.IsVisible = true;
            m_gameServer.GetMaps(m_gSettings.ClientId, (error, mapInfos) =>
            {
                if (m_gameServer.HasError(error))
                {
                    m_progress.IsVisible = false;
                    OutputError(error);
                    return;
                }

                m_voxelMap.Save(bytes =>
                {
                    MapInfo mapInfo = mapInfos.Where(m => m.Name == mapName).FirstOrDefault();
                    if(mapInfo == null)
                    {
                        Guid mapId = Guid.NewGuid();

                        mapInfo = new MapInfo
                        {
                            Id = mapId,
                            Name = mapName,
                            MaxPlayers = maxPlayers,
                            SupportedModes = GameMode.All
                        };
                    }
                    else
                    {
                        mapInfo.MaxPlayers = maxPlayers;
                        mapInfo.SupportedModes = GameMode.All;
                    }

                    MapData mapData = new MapData
                    {
                        Id = mapInfo.Id,
                        Bytes = bytes,
                    };

                    m_gameServer.UploadMap(m_gSettings.ClientId, mapInfo, mapData, uploadError =>
                    {
                        m_progress.IsVisible = false;
                        if (m_gameServer.HasError(uploadError))
                        {
                            OutputError(uploadError);
                        }
                    });
                });
            });
        }

        private void OnDownloadMap(string mapName)
        {
            m_progress.IsVisible = true;
            m_gameServer.GetMaps(m_gSettings.ClientId, (error, mapInfos) =>
            {
                if (m_gameServer.HasError(error))
                {
                    m_progress.IsVisible = false;
                    OutputError(error);
                    return;
                }

                MapInfo mapInfo = mapInfos.Where(m => m.Name == mapName).FirstOrDefault();
                if(mapInfo == null)
                {
                    m_progress.IsVisible = false;
                    m_console.Echo(string.Format("map {0} not found", mapName));
                    return;
                }

                m_gameServer.DownloadMapData(m_gSettings.ClientId, mapInfo.Id, (downloadError, mapData) =>
                {
                    if (m_gameServer.HasError(downloadError))
                    {
                        m_progress.IsVisible = false;
                        OutputError(downloadError);
                        return;
                    }

                    Debug.Log("lastmap " + mapInfo.Name);
                    PlayerPrefs.SetString("lastmap", mapInfo.Name);
                    Debug.Log(PlayerPrefs.GetString("lastmap"));
                    m_voxelMap.Load(mapData.Bytes, () =>
                    {
                        m_progress.IsVisible = false;
                    });
                });
            });
        }

        private void OnSave(string mapName)
        {
            m_voxelMap.Save(bytes =>
            {
                string dataPath = Application.persistentDataPath + "/Maps/";
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }
                
                File.WriteAllBytes(dataPath + mapName, bytes);
            });
        }

        private void OnLoad(string mapName)
        {
            string dataPath = Application.persistentDataPath + "/Maps/";
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            byte[] bytes = File.ReadAllBytes(dataPath + mapName);

            m_voxelMap.Load(bytes, () =>
            {
               // MapPos mapPos = m_voxelMap.GetCellPosition(m_pivot.transform.position, m_voxelMap.GetCameraWeight());
               // m_voxelMap.SetCameraPosition(mapPos.Row, mapPos.Col);
            });
        }

        private void OnCreate(int weight)
        {
            m_voxelMap.Create(weight);
            SetupEditorCamera();
        }


        private void OutputError(Error error)
        {
            Debug.LogWarning(StatusCode.ToString(error.Code) + " " + error.Message);
           // m_errorNotification.Show(StatusCode.ToString(error.Code) + " " + error.Message);
        }
    }
}
