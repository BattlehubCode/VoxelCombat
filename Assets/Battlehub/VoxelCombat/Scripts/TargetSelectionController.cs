using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface ITargetSelectionController
    {

    }

    public class TargetSelectionController : MonoBehaviour, ITargetSelectionController
    {
        [SerializeField]
        private GameViewport m_viewport;
        private IPlayerCameraController m_cameraController;

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
                        m_cameraController = Dependencies.GameView.GetCameraController(LocalPlayerIndex);
                    }
                }
            }
        }

        private int PlayerIndex
        {
            get
            {
                Guid playerId = m_gameState.GetLocalPlayerId(LocalPlayerIndex);
                return m_gameState.GetPlayerIndex(playerId);
            }
        }

        private IUnitSelection m_targetSelection;
        private IUnitSelection m_unitSelection;
        private IVoxelInputManager m_inputManager;
        private IVoxelGame m_gameState;
        private IVoxelMap m_map;

        private MapPos m_mapCursor = new MapPos(-1, -1);

        private VoxelData m_selectedTarget;
        private VoxelData m_previouslySelected;

        private struct SelectionDescriptor
        {
            public int Type;
            public int Weight;

            public SelectionDescriptor(int type, int weight)
            {
                Type = type;
                Weight = weight;
            }

            public override int GetHashCode()
            {
                return (Type << 16) | Weight;
            }

            public override bool Equals(object obj)
            {
                if(obj is SelectionDescriptor)
                {
                    SelectionDescriptor descriptor = (SelectionDescriptor)obj;

                    return descriptor.Type == Type && descriptor.Weight == Weight;
                }
                return false;
            }
        }

        private bool m_isTargetAutoSelectionMode;

        private SelectionDescriptor[] m_selectedUnitDescriptors;

        private void Awake()
        {
            m_targetSelection = Dependencies.TargetSelection;
            m_unitSelection = Dependencies.UnitSelection;
            m_unitSelection.SelectionChanged += OnUnitSelectionChanged;

            m_inputManager = Dependencies.InputManager;
            m_gameState = Dependencies.GameState;
            m_map = Dependencies.Map;
        }

        private void Start()
        {
            LocalPlayerIndex = m_viewport.LocalPlayerIndex;
            m_cameraController = Dependencies.GameView.GetCameraController(LocalPlayerIndex);
            BeginTargetSelection();
        }

        private void OnDestroy()
        {
            if (m_unitSelection != null)
            {
                m_unitSelection.SelectionChanged -= OnUnitSelectionChanged;
            }
        }

        private void Update()
        {
            if (m_gameState.IsContextActionInProgress(LocalPlayerIndex))
            {
                return;
            }

            if (m_gameState.IsMenuOpened(LocalPlayerIndex))
            {
                return;
            }

            if (m_gameState.IsPaused || m_gameState.IsPauseStateChanging)
            {
                return;
            }

            int playerIndex = PlayerIndex;

            if (m_mapCursor != m_cameraController.MapCursor)
            {

                m_mapCursor = m_cameraController.MapCursor;

                
                VoxelData newSelectedTarget = null;
                VoxelData target = null;
                int width = m_map.Map.GetMapSizeWith(GameConstants.MinVoxelActorWeight);
                if(m_mapCursor.Row >= 0 && m_mapCursor.Col >= 0 && m_mapCursor.Row < width && m_mapCursor.Col < width )
                {
                    MapCell cell = m_map.Map.Get(m_mapCursor.Row, m_mapCursor.Col, m_cameraController.Weight);

                    for (int i = 0; i < m_selectedUnitDescriptors.Length; ++i)
                    {
                        SelectionDescriptor descriptor = m_selectedUnitDescriptors[i];
                        bool lowestPossible = true;
                        cell.GetDefaultTargetFor(descriptor.Type, descriptor.Weight, playerIndex, lowestPossible, out target, playerIndex);
                        if (target != null)
                        {
                            break;
                        }
                    }
                }

                if (target != null && target.VoxelRef != null && target.UnitOrAssetIndex != -1)
                {
                    //Player could not destroy own units (and should not be able to select them as target)
                    if (!VoxelData.IsControllableUnit(target.Type) || target.Owner != playerIndex)
                    {
                        newSelectedTarget = target;
                    }
                }

                if(m_previouslySelected != newSelectedTarget)
                {
                    if(newSelectedTarget == null)
                    {
                        ClearSelection();
                        m_selectedTarget = null;
                        m_previouslySelected = null;
                    }
                    else
                    {
                        m_previouslySelected = newSelectedTarget;
                        TryEnableTargetAutoSelectionMode();
                        if (m_isTargetAutoSelectionMode)
                        {
                            m_selectedTarget = newSelectedTarget;
                            TargetSelectionSelect(playerIndex, target.Owner, m_selectedTarget.UnitOrAssetIndex);
                        }
                    }    
                }  
            }
            else
            {
                if (m_selectedTarget != null)
                {
                    IVoxelDataController dc = m_gameState.GetVoxelDataController(m_selectedTarget.Owner, m_selectedTarget.UnitOrAssetIndex);
                    if (dc != null && dc.IsAlive)
                    {
                        Coordinate cursorCoord = new Coordinate(m_cameraController.MapCursor, GameConstants.MinVoxelActorWeight, 0).ToWeight(dc.ControlledData.Weight);
                        if (cursorCoord.MapPos != dc.Coordinate.MapPos)
                        {
                            Coordinate coord = dc.Coordinate.ToWeight(GameConstants.MinVoxelActorWeight);
                            m_cameraController.MapPivot = coord.MapPos;
                            m_cameraController.SetVirtualMousePosition(coord, true, false);
                            m_mapCursor = m_cameraController.MapCursor;
                        }
                    }
                }
            }

            if (m_inputManager.GetButtonDown(InputAction.X, LocalPlayerIndex))
            {
                bool hasSelected = m_targetSelection.HasSelected(playerIndex);

                Select(playerIndex);

                if (!hasSelected)
                {
                    m_isTargetAutoSelectionMode = true;
                }

            }
            else if (m_inputManager.GetButtonUp(InputAction.RB, LocalPlayerIndex))
            {
                m_isTargetAutoSelectionMode = false;

                ClearSelection();
            }
        }

        private void ClearSelection()
        {
            m_targetSelection.ClearSelection(PlayerIndex);
        }

        private void BeginTargetSelection()
        {
            ReadUnitDescriptors();
        }

        private void FinishTargetSelection()
        {
            m_targetSelection.ClearSelection(PlayerIndex);
        }

        private void OnUnitSelectionChanged(int selectorIndex, int unitOwnerIndex, long[] selected, long[] unselected)
        {
            ReadUnitDescriptors();
            TryEnableTargetAutoSelectionMode();
        }

 
        private void ReadUnitDescriptors()
        {
            int playerIndex = PlayerIndex;

            m_selectedUnitDescriptors = m_unitSelection.GetSelection(playerIndex, playerIndex)
                .Select(unitIndex => m_gameState.GetVoxelDataController(playerIndex, unitIndex))
                .Where(dc => dc != null)
                .Select(dc => new SelectionDescriptor(dc.ControlledData.Type, dc.ControlledData.Weight))
                .Distinct().ToArray();
        }

        private void TryEnableTargetAutoSelectionMode()
        {
            m_isTargetAutoSelectionMode = m_selectedUnitDescriptors.Any(d => d.Type == (int)KnownVoxelTypes.Bomb);
        }

        private void Select(int myPlayerIndex)
        {
            if(m_selectedTarget != null)
            {
                do
                {
                    m_selectedTarget = m_selectedTarget.Next;
                }
                while (m_selectedTarget != null &&
                      (m_selectedTarget.VoxelRef == null && !IsTargetForAnyone(m_selectedTarget) || m_unitSelection.IsSelected(myPlayerIndex, myPlayerIndex, m_selectedTarget.UnitOrAssetIndex)));
 
                if (m_selectedTarget != null)
                {
                    m_previouslySelected = m_selectedTarget;
                    TargetSelectionSelect(myPlayerIndex, m_selectedTarget.Owner, m_selectedTarget.UnitOrAssetIndex);
                }
                else
                {
                    m_targetSelection.ClearSelection(myPlayerIndex);
                    m_isTargetAutoSelectionMode = false;
                }
                
                return;   
            }

            List<int> allPlayers = new List<int>();
            List<long[]> allUnitsAndAssets = new List<long[]>();
            
            for(int i = 0; i < m_gameState.PlayersCount; ++i)
            {                
                long[] unitsAndAssets = m_gameState.GetUnits(i).Union(m_gameState.GetAssets(i)).ToArray();
                allUnitsAndAssets.Add(unitsAndAssets);
                allPlayers.Add(i);
            }

            long selectedIndex = -1;
      

            long closestUnitOrAssetIndex;
            int closestPlayerIndex;
            FindClosestTo(selectedIndex, allPlayers.ToArray(), allUnitsAndAssets.ToArray(), out closestPlayerIndex, out closestUnitOrAssetIndex);

            if (closestUnitOrAssetIndex >= 0)
            {
                TargetSelectionSelect(myPlayerIndex, closestPlayerIndex, closestUnitOrAssetIndex);

                VoxelData closestData = null;
                MapPos closestMapPos = new MapPos();
                GetUnitOrAsset(closestPlayerIndex, closestUnitOrAssetIndex, ref closestData, ref closestMapPos);

                if (closestData == null)
                {
                    Debug.LogError("Wrong index " + closestUnitOrAssetIndex);
                    return;
                }

                const int dontCare = -1;
                Coordinate coord = new Coordinate(closestMapPos, closestData.Weight, dontCare);
                if(coord.MapPos != new Coordinate(m_mapCursor, m_cameraController.Weight, 0).ToWeight(closestData.Weight).MapPos)
                {
                    coord = coord.ToWeight(m_cameraController.Weight);
                    m_cameraController.MapPivot = coord.MapPos;
                    m_cameraController.SetVirtualMousePosition(coord, true, false);
                }

                m_mapCursor = m_cameraController.MapCursor;
                m_selectedTarget = closestData;
                m_previouslySelected = closestData;

            }
        }

        private void TargetSelectionSelect(int myPlayerIndex, int playerIndex, long unitIndex)
        {
            for (int i = 0; i < m_gameState.PlayersCount; ++i)
            {
                if (i == playerIndex)
                {
                    continue;
                }

                m_targetSelection.Select(myPlayerIndex, i, null);
            }

            m_targetSelection.Select(myPlayerIndex, playerIndex, new[] { unitIndex });
        }

       
        private void FindClosestTo(long selectedIndex, int[] allPlayers, long[][] allUnitsAndAssets, out int closestPlayerIndex, out long closesetUnitOrAssetIndex)
        {
            closestPlayerIndex = -1;
            closesetUnitOrAssetIndex = -1;

            Vector3 selectedPosition;
            if (selectedIndex == -1 || m_mapCursor != m_cameraController.MapCursor)
            {
                selectedPosition = m_cameraController.Cursor;
            }
            else
            {
                selectedPosition = GetUnitPosition(closestPlayerIndex, selectedIndex);
            }

            float minDistance = float.PositiveInfinity;
       
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(m_viewport.Camera);

            for(int p = 0; p < allPlayers.Length; ++p)
            {
                int playerIndex = allPlayers[p];
                long[] unitsAndAssets = allUnitsAndAssets[p];

                for (int i = 0; i < unitsAndAssets.Length; ++i)
                {
                    long unitOrAssetIndex = unitsAndAssets[i];

         
                    if (unitOrAssetIndex == selectedIndex && m_mapCursor != m_cameraController.MapCursor)
                    {
                        continue;
                    }

                    VoxelData targetData = null;
                    MapPos targetMapPos = new MapPos();
                    GetUnitOrAsset(playerIndex, unitOrAssetIndex, ref targetData, ref targetMapPos);

                    if (targetData == null)
                    {
                        Debug.LogError("Wrong index " + unitOrAssetIndex);
                        continue;
                    }

                    if (!IsTargetForAnyone(targetData))
                    {
                        continue;
                    }

                    Vector3 position = m_map.GetWorldPosition(targetMapPos, targetData.Weight);
                    if (IsVisible(planes, targetData.VoxelRef))
                    {
                        Vector3 toVector = (position - selectedPosition);

                        float distance = toVector.sqrMagnitude;
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closesetUnitOrAssetIndex = unitOrAssetIndex;
                            closestPlayerIndex = playerIndex;
                        }
                    }
                }
            }
        }

  
        private void GetUnitOrAsset(int playerIndex, long unitOrAssetIndex, ref VoxelData data, ref MapPos mapPos)
        {
            IVoxelDataController controller = m_gameState.GetVoxelDataController(playerIndex, unitOrAssetIndex);
            if (controller != null)
            {
                data = controller.ControlledData;
                mapPos = controller.Coordinate.MapPos;
            }
            else
            {
                MatchAssetCli asset = m_gameState.GetAsset(playerIndex, unitOrAssetIndex);
                if (asset != null)
                {
                    data = asset.VoxelData;
                    mapPos = asset.Cell.GetPosition();
                }
            }
        }


        private bool IsTargetForAnyone(VoxelData target)
        {
            if(target.Weight < GameConstants.MinVoxelActorWeight)
            {
                return false;
            }

            for(int i = 0; i < m_selectedUnitDescriptors.Length; ++i)
            {
                SelectionDescriptor descriptor = m_selectedUnitDescriptors[i];
                int playerIndex = PlayerIndex;

                if(target.IsTargetFor(descriptor.Type, descriptor.Weight, playerIndex))
                {
                    bool targetIsUnit = VoxelData.IsUnit(target.Type);
                    if (!targetIsUnit || target.Owner != PlayerIndex)
                    {
                        if(descriptor.Type == (int)KnownVoxelTypes.Bomb || targetIsUnit)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsVisible(Plane[] planes, Voxel voxel)
        {
            if (voxel == null)
            {
                return false;
            }

            Bounds bounds = voxel.Renderer.bounds;
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }

        private Vector3 GetUnitPosition(int playerIndex, long unitIndex)
        {
            IVoxelDataController controller = m_gameState.GetVoxelDataController(playerIndex, unitIndex);
            Vector3 position = m_map.GetWorldPosition(controller.Coordinate);
            return position;
        }
    }

}
    

