using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface IMatchPlayerControllerCli
    {
        int ControllableUnitsCount
        {
            get;
        }
        int UnitsCount
        {
            get;
        }
        IEnumerable<long> Units
        {
            get;
        }

        IEnumerable<long> Assets
        {
            get;
        }

        bool IsInRoom
        {
            get;
        }

        int PlayerIndex
        {
            get;
        }

        void Init(int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities);
        void Execute(Cmd[] commands, long tick, long lagTicks);

        IVoxelDataController GetVoxelDataController(long unitIndex);
        MatchAssetCli GetAsset(long assetIndex);

        long GetAssetIndex(VoxelData data);
        bool ContainsUnit(long unitIndex);
        bool ContainsAsset(long asetIndex);
        void ConnectWith(IMatchPlayerControllerCli[] playerControllers);

        void CreateAssets(IList<VoxelDataCellPair> data);
        void RemoveAssets(IList<VoxelData> data);

        void DestroyAllUnitsAndAssets();
        
    }

    public class MatchAssetCli
    {
        private VoxelData m_voxelData;
        private VoxelAbilities m_abilities;
        private MapCell m_cell;
        
        private Voxel m_voxel;
        private ulong m_targetSelection;
        private ulong m_selection;

        public VoxelData VoxelData
        {
            get { return m_voxelData; }
        }

        public VoxelAbilities Abilities
        {
            get { return m_abilities; }
        }

        public MapCell Cell
        {
            get { return m_cell; }
        }

        public MatchAssetCli(VoxelData data, VoxelAbilities abilities, MapCell cell)
        {
            m_voxelData = data;
            m_abilities = abilities;
            m_cell = cell;

            if(m_voxelData.VoxelRef != null)
            {
                m_voxel = m_voxelData.VoxelRef;
            }
            m_voxelData.VoxelRefSet += OnVoxelRefSet;
            m_voxelData.VoxelRefReset += OnVoxelRefReset;
        }

        public void Destroy()
        {
            m_voxelData.VoxelRefSet -= OnVoxelRefSet;
            m_voxelData.VoxelRefReset -= OnVoxelRefReset;
        }

        private void OnVoxelRefSet(Voxel voxel)
        {
            m_voxel = voxel;
            if (m_voxel != null)
            {
                for (int i = 1; i <= GameConstants.MaxLocalPlayers; ++i)
                {
                    if (IsSelectedAsTarget(i))
                    {
                        m_voxel.UnselectAsTarget(i);
                    }
                }
            }
        }

        private void OnVoxelRefReset(Voxel voxel)
        {
            bool wasVisible = m_voxel != null;
            if (wasVisible)
            {
                for (int i = 1; i <= GameConstants.MaxLocalPlayers; ++i)
                {
                    m_voxel.UnselectAsTarget(i);
                }
            }

            m_voxel = null;
        }

        public void SelectAsTarget(int playerIndex) //this is player index (not owner index)
        {
            if (!IsSelectedAsTarget(playerIndex))
            {
                m_targetSelection |= (1ul << playerIndex);
                if (m_voxel != null)
                {
                    m_voxel.SelectAsTarget(playerIndex);
                }
            }
        }
        public void UnselectAsTarget(int playerIndex) ///this is player index (not owner index)
        {
            if (IsSelectedAsTarget(playerIndex))
            {
                m_targetSelection &= ~(1ul << playerIndex);
                if (m_voxel != null)
                {
                    m_voxel.UnselectAsTarget(playerIndex);
                }
            }
        }
        public bool IsSelectedAsTarget(int playerIndex)
        {
            return (m_targetSelection & (1ul << playerIndex)) != 0;
        }

        public void Select(int playerIndex) //this is player index (not owner index)
        {
            if (!IsSelected(playerIndex))
            {
                m_selection |= (1ul << playerIndex);
                if (m_voxelData != null)
                {
                    m_voxel.Select(playerIndex);
                }
            }
        }
        public void Unselect(int playerIndex) ///this is player index (not owner index)
        {
            if (IsSelected(playerIndex))
            {
                m_selection &= ~(1ul << playerIndex);
                if (m_voxel != null)
                {
                    m_voxel.Unselect(playerIndex);
                }
            }
        }
        public bool IsSelected(int playerIndex)
        {
            return (m_selection & (1ul << playerIndex)) != 0;
        }
    }

    public class MatchPlayerControllerCli : MonoBehaviour, IMatchPlayerControllerCli
    {
        private IMatchPlayerControllerCli[] m_otherPlayerControllers;

        private readonly Dictionary<VoxelData, long> m_voxelDataToId = new Dictionary<VoxelData, long>();
        private readonly Dictionary<long, MatchAssetCli> m_idToAsset = new Dictionary<long, MatchAssetCli>(); //Walls build by player and possible other types of VoxelData. Assets are passive elements and does not have corresponding controller 
        private readonly Dictionary<long, IMatchUnitControllerCli> m_idToUnit = new Dictionary<long, IMatchUnitControllerCli>();

        private Dictionary<int, VoxelAbilities>[] m_allAbilities;

        private IVoxelMap m_voxelMap;

        private IUnitSelection m_selection;
        private IUnitSelection m_targetSelection;
        private IVoxelMinimapRenderer m_minimap;
        private IVoxelGame m_gameState;

        private int m_playerIndex = -1;
        private bool m_isLocalPlayer;

        private long m_identity;

        private int m_controllableUnitsCount;
        public int ControllableUnitsCount
        {
            get { return m_controllableUnitsCount; }
        }
        public int UnitsCount
        {
            get { return m_idToUnit.Count; }
        }

        public IEnumerable<long> Units
        {
            get { return m_idToUnit.Keys; }
        }

        public IEnumerable<long> Assets
        {
            get { return m_idToAsset.Keys; }
        }

        private bool m_isInRoom = true;
        public bool IsInRoom
        {
            get { return m_isInRoom; }
        }

        public int PlayerIndex
        {
            get { return m_playerIndex; }
        }

        private void Awake()
        {
            m_gameState = Dependencies.GameState;
            m_voxelMap = Dependencies.Map;
            m_selection = Dependencies.UnitSelection;
            m_targetSelection = Dependencies.TargetSelection;
            m_minimap = Dependencies.Minimap;
            
            m_selection.SelectionChanged += OnUnitSelectionChanged;
            m_targetSelection.SelectionChanged += OnTargetSelectionChanged;
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<long, IMatchUnitControllerCli> kvp in m_idToUnit)
            {
                MatchFactoryCli.DestroyUnitController(kvp.Value);
            }

            if(m_selection != null)
            {
                m_selection.SelectionChanged -= OnUnitSelectionChanged;
            }

            if(m_targetSelection != null)
            {
                m_targetSelection.SelectionChanged -= OnTargetSelectionChanged;
            }
        }

        public void Init(int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities)
        {
            m_allAbilities = allAbilities;

            m_idToUnit.Clear();
            m_idToAsset.Clear();
            m_voxelDataToId.Clear();

            m_playerIndex = playerIndex;
            m_isLocalPlayer = m_gameState.IsLocalPlayer(m_playerIndex);

            Dictionary<int, VoxelAbilities> abilities = m_allAbilities[m_playerIndex];

            m_voxelMap.Map.Root.ForEach(cell =>
            {
                cell.ForEach(voxelData =>
                {
                    int owner = voxelData.Owner;
                    if (owner == m_playerIndex)
                    {
                        if(VoxelData.IsUnit(voxelData.Type))
                        {
                            Coordinate coordinate = new Coordinate(cell, voxelData);
                            CreateUnitController(voxelData, coordinate);
                        }
                        else
                        {
                            if(!voxelData.IsNeutral)
                            {
                                CreateAsset(voxelData, abilities[voxelData.Type], cell);
                            }
                        }   
                    }
                });
            });
        }

        private void CreateAsset(VoxelData voxelData, VoxelAbilities abilities, MapCell cell)
        {
            if(voxelData.IsNeutral)
            {
                return;
            }

            voxelData.UnitOrAssetIndex = m_identity;
            MatchAssetCli asset = new MatchAssetCli(voxelData, abilities, cell);

            m_voxelDataToId.Add(voxelData, m_identity);
            m_idToAsset.Add(m_identity, asset);

            unchecked
            {
                m_identity++;
            }
           
            if (voxelData.Weight >= GameConstants.MinVoxelActorWeight)
            {
                Coordinate coordinate = new Coordinate(cell, voxelData);
                int radius = abilities.VisionRadius;
                m_voxelMap.Map.ForEachInRadius(coordinate, radius, (observedCell, pos) =>
                {
                    ObserveCell(asset.VoxelData.Owner, observedCell, pos, coordinate.Weight);
                });

                m_minimap.Spawn(voxelData, new Coordinate(cell, voxelData));
            }
        }

        private void RemoveAsset(VoxelData voxelData)
        {
            if(voxelData.IsNeutral)
            {
                return;
            }

            long id;
            if(m_voxelDataToId.TryGetValue(voxelData, out id))
            {
                m_voxelDataToId.Remove(voxelData);

                MatchAssetCli asset = m_idToAsset[id];

                if (asset.VoxelData.Weight >= GameConstants.MinVoxelActorWeight)
                {
                    Coordinate coordinate = new Coordinate(asset.Cell, asset.VoxelData);
                    int radius = asset.Abilities.VisionRadius;
                    m_voxelMap.Map.ForEachInRadius(coordinate, radius, (ignoredCell, pos) =>
                    {
                        IgnoreCell(asset.VoxelData.Owner, ignoredCell, pos, coordinate.Weight);
                    });

                    m_minimap.Die(voxelData, new Coordinate(asset.Cell, voxelData));
                }
                
                asset.Destroy();
                m_idToAsset.Remove(id);
            }
        }

        private void CreateUnitController(VoxelData voxelData, Coordinate coordinate)
        {
            voxelData.UnitOrAssetIndex = m_identity;

            IMatchUnitControllerCli unit = MatchFactoryCli.CreateUnitController(m_voxelMap.Map, coordinate, voxelData.Type, m_playerIndex, m_allAbilities);
            
            if(VoxelData.IsControllableUnit(voxelData.Type))
            {
                m_controllableUnitsCount++;
            }

            m_idToUnit.Add(m_identity, unit);

            int radius = unit.DataController.Abilities.VisionRadius;
            m_voxelMap.Map.ForEachInRadius(coordinate, radius, (observedCell, pos) =>
            {
                ObserveCell(unit.DataController.ControlledData.Owner, observedCell, pos, coordinate.Weight);
            });

            m_minimap.Spawn(voxelData, coordinate);

            unchecked
            {
                m_identity++;
            }
        }

        private void RemoveUnitController(long unitId)
        {
            IMatchUnitControllerCli unitController = m_idToUnit[unitId];
            if (VoxelData.IsControllableUnit(unitController.Type))
            {
                m_controllableUnitsCount--;
            }

            Coordinate coordinate = unitController.DataController.Coordinate;
            int radius = unitController.DataController.Abilities.VisionRadius;
            m_voxelMap.Map.ForEachInRadius(coordinate, radius, (ignoredCell, pos) =>
            {
                IgnoreCell(unitController.DataController.ControlledData.Owner, ignoredCell, pos, coordinate.Weight);
            });

            m_minimap.Die(unitController.DataController.ControlledData, unitController.DataController.Coordinate);
            m_idToUnit.Remove(unitId);

            //Does not needed because all nessesary actions and event unsubscription performed in OnVoxelRefReset event handler
            //MatchFactoryCli.DestroyUnitController(unitController);
        }



        public void Execute(Cmd[] commands, long tick, long lagTicks)
        {
            HashSet<long> deadUnitsHs = null;
            List<long> spawnedUnitsList = null;
            for (int c = 0; c < commands.Length; ++c)
            {
                Cmd cmd = commands[c];
                if(cmd == null)
                {
                    continue;
                }

                if(cmd.Code == CmdCode.LeaveRoom)
                {
                    m_isInRoom = false;
                }
                else
                {
                    IMatchUnitControllerCli unitController = m_idToUnit[cmd.UnitIndex];

                    long duration = cmd.Duration;
                    duration -= lagTicks;
                    duration = Math.Max(0, duration);

                    cmd.Duration = (int)duration;

                    IVoxelDataController dc = unitController.DataController;
                    Coordinate prevCoord = dc.Coordinate;

                    int radius = unitController.DataController.Abilities.VisionRadius;
                    
                    unitController.ExecuteCommand(cmd, tick);
                    if(prevCoord != dc.Coordinate)
                    {
                        HandleCoordinateChange(dc, prevCoord, radius);
                    }

                    IList<VoxelDataCellPair> createdVoxels = unitController.CreatedVoxels;
                    if (createdVoxels.Count != 0)
                    {
                        CreateAssets(createdVoxels);
                        for (int i = 0; i < m_otherPlayerControllers.Length; ++i)
                        {
                            m_otherPlayerControllers[i].CreateAssets(createdVoxels);
                        }
                    }

                    IList<VoxelData> eatenOrDestroyed = unitController.EatenOrDestroyedVoxels;
                    if(eatenOrDestroyed.Count != 0)
                    {
                        RemoveAssets(eatenOrDestroyed);
                        for(int i = 0; i < m_otherPlayerControllers.Length; ++i)
                        {
                            m_otherPlayerControllers[i].RemoveAssets(eatenOrDestroyed);
                        }
                    }

                    if (cmd.Code == CmdCode.Composite)
                    {
                        CompositeCmd compositeCmd = (CompositeCmd)cmd;
                        for(int i = 0; i < compositeCmd.Commands.Length; ++i)
                        {
                            spawnedUnitsList = PostprocessCommand(spawnedUnitsList, compositeCmd.Commands[i], unitController);
                        }
                    }
                    else
                    {
                        spawnedUnitsList = PostprocessCommand(spawnedUnitsList, cmd, unitController);
                    }

                    //if (unitController.DataController.ControlledData.Unit.State == VoxelDataState.Dead)
                    if (!unitController.DataController.IsAlive)
                    {
                        //Voxel voxel = unitController.DataController.ControlledData.VoxelRef; 
                        //Debug.Assert(voxel == null);//
                        
                        if (deadUnitsHs == null)
                        {
                            deadUnitsHs = new HashSet<long>();
                        }

                        if (deadUnitsHs.Contains(cmd.UnitIndex))
                        {
                            Debug.LogError("Dead unit could not execute commands");
                        }
                        else
                        {
                            deadUnitsHs.Add(cmd.UnitIndex);
                        }
                    }
                }
            }


            if(deadUnitsHs != null)
            {
                long[] deadUnits = deadUnitsHs.ToArray();
                for (int i = 0; i < m_gameState.PlayersCount; ++i)
                {
                    m_selection.Unselect(i, m_playerIndex, deadUnits);
                    m_targetSelection.Unselect(i, m_playerIndex, deadUnits);
                }
                for(int i = 0; i < deadUnits.Length; ++i)
                {
                    long unitId = deadUnits[i];

                    RemoveUnitController(unitId);
                }
            }

            if(m_isLocalPlayer)
            {
                if (spawnedUnitsList != null)
                {
                    long[] spawnedUnits = spawnedUnitsList.ToArray();
                    spawnedUnits = spawnedUnits.Where(
                        u => m_gameState.GetVoxelDataController(m_playerIndex, u) != null &&
                        VoxelData.IsControllableUnit(m_gameState.GetVoxelDataController(m_playerIndex, u).ControlledData.Type)).ToArray();

                    m_selection.AddToSelection(m_playerIndex, m_playerIndex, spawnedUnits);
                }
            }
        }

        private void HandleCoordinateChange(IVoxelDataController dc, Coordinate prevCoord, int radius)
        {
            if (prevCoord.Weight != dc.Coordinate.Weight)
            {
                m_voxelMap.Map.ForEachInRadius(prevCoord, radius, (ignoredCell, pos) =>
                {
                    IgnoreCell(dc.ControlledData.Owner, ignoredCell, pos, prevCoord.Weight);
                });

                m_voxelMap.Map.ForEachInRadius(dc.Coordinate, radius, (observedCell, pos) =>
                {
                    ObserveCell(dc.ControlledData.Owner, observedCell, pos, dc.Coordinate.Weight);
                });
            }
            else if (prevCoord.Row != dc.Coordinate.Row)
            {
                Debug.Assert(Mathf.Abs(prevCoord.Row - dc.Coordinate.Row) == 1);
                m_voxelMap.Map.ForEachColInRadius(prevCoord, radius * (int)Mathf.Sign(dc.Coordinate.Row - prevCoord.Row), (ignoredCell, pos) =>
                {
                    IgnoreCell(dc.ControlledData.Owner, ignoredCell, pos, prevCoord.Weight);
                });

                m_voxelMap.Map.ForEachColInRadius(dc.Coordinate, -radius * (int)Mathf.Sign(dc.Coordinate.Row - prevCoord.Row), (observedCell, pos) =>
                {
                    ObserveCell(dc.ControlledData.Owner, observedCell, pos, dc.Coordinate.Weight);
                });
            }
            else if (prevCoord.Col != dc.Coordinate.Col)
            {
                Debug.Assert(Mathf.Abs(prevCoord.Col - dc.Coordinate.Col) == 1);
                m_voxelMap.Map.ForEachRowInRadius(prevCoord, radius * (int)Mathf.Sign(dc.Coordinate.Col - prevCoord.Col), (ignoredCell, pos) =>
                {
                    IgnoreCell(dc.ControlledData.Owner, ignoredCell, pos, prevCoord.Weight);
                });

                m_voxelMap.Map.ForEachRowInRadius(dc.Coordinate, -radius * (int)Mathf.Sign(dc.Coordinate.Col - prevCoord.Col), (observedCell, pos) =>
                {
                    ObserveCell(dc.ControlledData.Owner, observedCell, pos, dc.Coordinate.Weight);
                });
            }

            m_minimap.Move(dc.ControlledData, prevCoord, dc.Coordinate);
        }

        private void ObserveCell(int owner, MapCell observedCell, MapPos pos, int weight)
        {
            ObserveRecursive(owner, observedCell, pos,  weight);
        }

        private void IgnoreCell(int owner, MapCell ignoredCell, MapPos pos, int weight)
        {
            IgnoreRecursive(owner, ignoredCell, pos, weight);
        }

        private void ObserveRecursive(int owner, MapCell cell, MapPos pos, int weight)
        {
            Debug.Assert(weight >= GameConstants.MinVoxelActorWeight);

            int counter = cell.ObservedBy[owner];
            counter++;
            cell.ObservedBy[owner] = counter;

            if (weight > GameConstants.MinVoxelActorWeight)
            {
                if (cell.Children != null)
                {
                    for (int i = 0; i < cell.Children.Length; ++i)
                    {
                        MapPos childPos = pos;
                        childPos.Row *= 2;
                        childPos.Row += i / 2;
                        childPos.Col *= 2;
                        childPos.Col += i % 2;
                        ObserveRecursive(owner, cell.Children[i], childPos, weight - 1);
                    }
                }
            }
            else
            {
                if (counter == 1)
                {
                    m_minimap.ObserveCell(owner, pos, weight);
                }
            }
        }

        private void IgnoreRecursive(int owner, MapCell cell, MapPos pos, int weight)
        {
            Debug.Assert(weight >= GameConstants.MinVoxelActorWeight);

            int counter = cell.ObservedBy[owner];
            counter--;
            cell.ObservedBy[owner] = counter;

            if (weight > GameConstants.MinVoxelActorWeight)
            {
                if (cell.Children != null)
                {
                    for (int i = 0; i < cell.Children.Length; ++i)
                    {
                        MapPos childPos = pos;
                        childPos.Row *= 2;
                        childPos.Row += i / 2;
                        childPos.Col *= 2;
                        childPos.Col += i % 2;
                        IgnoreRecursive(owner, cell.Children[i], childPos, weight - 1);
                    }
                }
            }
            else 
            {
                if (counter == 0)
                {
                    m_minimap.IgnoreCell(owner, pos, weight);
                }
            }

            Debug.Assert(counter >= 0);
        }

        private List<long> PostprocessCommand(List<long> spawnedUnitsList, Cmd cmd, IMatchUnitControllerCli unitController)
        {
            if (cmd.Code == CmdCode.Split || cmd.Code == CmdCode.Split4)
            {
                CoordinateCmd splitCmd = (CoordinateCmd)cmd;
                for (int i = 0; i < splitCmd.Coordinates.Length; ++i)
                {
                    Coordinate coordinate = splitCmd.Coordinates[i];
                    VoxelData voxelData = m_voxelMap.Map.Get(coordinate);
                    if (voxelData != null)
                    {
                        if (spawnedUnitsList == null)
                        {
                            spawnedUnitsList = new List<long>();
                        }

                        spawnedUnitsList.Add(m_identity);
                        CreateUnitController(voxelData, coordinate);
                    }
                }
            }
            else if (cmd.Code == CmdCode.Convert)
            {
                Coordinate coordinate = unitController.DataController.Coordinate;
                MapCell cell = m_voxelMap.Map.Get(coordinate.Row, coordinate.Col, coordinate.Weight);
                VoxelData voxelData = cell.GetVoxelDataAt(coordinate.Altitude);
                if (voxelData != null)
                {
                    if(VoxelData.IsUnit(voxelData.Type))
                    {
                        if (spawnedUnitsList == null)
                        {
                            spawnedUnitsList = new List<long>();
                        }

                        spawnedUnitsList.Add(m_identity);
                        CreateUnitController(voxelData, coordinate);
                    }
                    else
                    {
                        CreateAsset(voxelData, m_allAbilities[voxelData.Owner][voxelData.Type], cell);
                    }                   
                }
            }

            return spawnedUnitsList;
        }

        private void OnTargetSelectionChanged(int selectorIndex, int unitOwnerIndex, long[] selected, long[] unselected)
        {
            if (unitOwnerIndex == m_playerIndex)
            {
                Debug.Assert(m_gameState.IsLocalPlayer(selectorIndex));

                UnselectAsTarget(selectorIndex, unselected);
                SelectAsTarget(selectorIndex, selected);
            }
        }

        private void OnUnitSelectionChanged(int selectorIndex, int unitOwnerIndex, long[] selected, long[] unselected)
        {
            if (unitOwnerIndex == m_playerIndex)
            {
                Debug.Assert(m_gameState.IsLocalPlayer(selectorIndex));

                Unselect(selectorIndex, unselected);
                Select(selectorIndex, selected);
            }
        }

        private void Select(int selectorIndex, long[] ids)
        {
            for(int i = 0; i < ids.Length; ++i)
            {
                long id = ids[i];

                IMatchUnitControllerCli unitController;
                if(m_idToUnit.TryGetValue(id, out unitController))
                {
                    unitController.Select(selectorIndex);
                }
                else
                {
                    MatchAssetCli asset = m_idToAsset[id];
                    asset.Select(selectorIndex);
                }       
            }
        }

        private void Unselect(int selectorIndex, long[] ids)
        {
            for (int i = 0; i < ids.Length; ++i)
            {
                long id = ids[i];

                IMatchUnitControllerCli unitController;
                if (m_idToUnit.TryGetValue(id, out unitController))
                {
                    unitController.Unselect(selectorIndex);
                }
                else
                {
                    MatchAssetCli asset = m_idToAsset[id];
                    asset.Unselect(selectorIndex);
                }    
            }
        }

        private void SelectAsTarget(int selectorIndex, long[] ids)
        {
            for (int i = 0; i < ids.Length; ++i)
            {
                long id = ids[i];

                IMatchUnitControllerCli unitController;
                if (m_idToUnit.TryGetValue(id, out unitController))
                {
                    unitController.SelectAsTarget(selectorIndex);
                }
                else
                {
                    MatchAssetCli asset = m_idToAsset[id];
                    asset.SelectAsTarget(selectorIndex);
                }
            }
        }

        private void UnselectAsTarget(int selectorIndex, long[] ids)
        {
            for (int i = 0; i < ids.Length; ++i)
            {
                long id = ids[i];

                IMatchUnitControllerCli unitController;
                if (m_idToUnit.TryGetValue(id, out unitController))
                {
                    unitController.UnselectAsTarget(selectorIndex);
                }
                else
                {
                    MatchAssetCli asset = m_idToAsset[id];
                    asset.UnselectAsTarget(selectorIndex);
                }
            }
        }


        public IVoxelDataController GetVoxelDataController(long unitIndex)
        {
            IMatchUnitControllerCli unitController;
            if(!m_idToUnit.TryGetValue(unitIndex, out unitController))
            {
                return null;
            }
            return unitController.DataController;
        }

        public MatchAssetCli GetAsset(long assetIndex)
        {
            MatchAssetCli asset;
            if(!m_idToAsset.TryGetValue(assetIndex, out asset))
            {
                return null;
            }
            return asset;
        }

        public long GetAssetIndex(VoxelData data)
        {
            long assetIndex;
            if (!m_voxelDataToId.TryGetValue(data, out assetIndex))
            {
                return -1;
            }
            return assetIndex;
        }


        public bool ContainsUnit(long unitIndex)
        {
            return m_idToUnit.ContainsKey(unitIndex);
        }

        public bool ContainsAsset(long assetIndex)
        {
            return m_idToAsset.ContainsKey(assetIndex);
        }

        public void ConnectWith(IMatchPlayerControllerCli[] playerControllers)
        {
            m_otherPlayerControllers = playerControllers.Where(ctrl => ((object)ctrl) != this).ToArray();
        }

        public void CreateAssets(IList<VoxelDataCellPair> createAssets)
        {
            for (int i = 0; i < createAssets.Count; ++i)
            {
                VoxelDataCellPair pair = createAssets[i];
                if (pair.Data.Owner == m_playerIndex)
                {
                    CreateAsset(pair.Data, m_allAbilities[pair.Data.Owner][pair.Data.Type], pair.Cell);
                }
            }
        }

        public void RemoveAssets(IList<VoxelData> removeAssets)
        {
            for(int i = 0; i < removeAssets.Count; ++i)
            {
                VoxelData data = removeAssets[i];
                if(data.Owner == m_playerIndex)
                {
                    RemoveAsset(data);
                }
            }
        }

        public void DestroyAllUnitsAndAssets()
        {
            long[] unitIds = m_idToUnit.Keys.ToArray();

            for (int i = 0; i < unitIds.Length; ++i)
            {
                long unitId = unitIds[i];

                IMatchUnitControllerCli unit = m_idToUnit[unitId];
                if (!unit.DataController.IsAlive)
                {
                    continue;
                }
                Coordinate c = unit.DataController.Coordinate;
                MapCell cell = m_voxelMap.Map.Get(c.Row, c.Col, c.Weight);

                cell.RemoveVoxelDataAndDecreaseHeight(unit.DataController.ControlledData);

                if(unit.DataController.ControlledData.VoxelRef != null)
                {
                    unit.DataController.ControlledData.VoxelRef.Explode(1.0f, unit.DataController.ControlledData.Health);
                }

                RemoveUnitController(unitId);
            }

            long[] assetIds = m_idToAsset.Keys.ToArray();
            for (int i = 0; i < assetIds.Length; ++i)
            {
                long assetId = assetIds[i];
                MatchAssetCli asset = m_idToAsset[assetId];

                asset.Cell.RemoveVoxelDataAndDecreaseHeight(asset.VoxelData);

                if(asset.VoxelData.VoxelRef != null)
                {
                    asset.VoxelData.VoxelRef.Explode(0, asset.VoxelData.Health);
                }
                
                RemoveAsset(asset.VoxelData);
            }
        }
    }

}
