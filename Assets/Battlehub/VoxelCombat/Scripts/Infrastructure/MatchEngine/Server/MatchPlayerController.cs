using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface IMatchPlayerController : IMatchPlayerView
    {
        bool IsPlayerInRoom
        {
            get;
        }

        bool HasControllableUnits
        {
            get;
        }


        void Submit(Cmd command);

        bool Tick(long tick, out CommandsArray commands);

        IMatchUnitController GetUnitController(long index);

        void ConnectWith(IMatchPlayerController[] playerControllers);
     
        void CreateAssets(IList<VoxelDataCellPair> createAssets);

        void RemoveAssets(IList<VoxelData> removeAssets);

        void DestroyAllUnitsAndAssets();

        void AddAssignment(Guid groupId, long unitId, SerializedTaskLaunchInfo taskLaunchInfo, bool hasTarget = false, int targetPlayerIndex = -1, long targetId = 0);

        /// <summary>
        /// Remove unit from assignment. Do not remove if assignment has target
        /// </summary>
        /// <param name="unitId"></param>
        void RemoveUnitFromAssignment(IMatchUnitAssetView unit);


        /// <summary>
        /// Remove target from assignment. Do not remove if assignment has unit
        /// </summary>
        /// <param name="targetId"></param>
        void RemoveTargetFromAssignments(IMatchUnitAssetView unitOrAsset);

        /// <summary>
        /// Remove assignment. Unconditional
        /// </summary>
        /// <param name="unitId"></param>
        void RemoveAssignment(IMatchUnitAssetView unit);

        void RemoveAssignmentGroup(Guid groupId);
    }


    public class MatchAsset : IMatchUnitAssetView
    {
        public event Action<CmdResultCode> CmdExecuted;
        private void NeverUsed()
        {
            CmdExecuted(CmdResultCode.Fail);
        }
        
        private VoxelData m_voxelData;
        private MapCell m_cell;
        private MapPos m_pos;
        
        public VoxelData VoxelData
        {
            get { return m_voxelData; }
        }

        public MapCell Cell
        {
            get { return m_cell; }
        }

        public MapPos Position
        {
            get { return m_pos; }
        }

        public long AssetIndex
        {
            get { return m_voxelData.UnitOrAssetIndex; }
        }

        long IMatchUnitAssetView.Id
        {
            get { return m_voxelData.UnitOrAssetIndex; }
        }

        VoxelData IMatchUnitAssetView.Data
        {
            get { return m_voxelData;  }
        }

        bool IMatchUnitAssetView.IsAlive
        {
            get { return m_voxelData.IsAlive; }
        }

        public IVoxelDataController DataController
        {
            get { return null; }
        }

        public Assignment Assignment
        {
            get;
            set;
        }

        public List<Assignment> TargetForAssignments
        {
            get;
            set;
        }

        public MatchAsset(VoxelData data, MapCell cell)
        {
            m_voxelData = data;
            m_cell = cell;
            m_pos = cell.GetPosition();
        }
    }

    public class Assignment
    {
        public Guid GroupId;
        public long UnitId;
        public bool HasUnit;
        public int  TargetPlayerIndex;
        public long TargetId;
        public bool HasTarget;
        public SerializedTaskLaunchInfo TaskLaunchInfo;
    }

    public class MatchPlayerController : IMatchPlayerController
    {
        public event MatchPlayerEventHandler<IMatchUnitAssetView> UnitCreated;
        public event MatchPlayerEventHandler<IMatchUnitAssetView> UnitRemoved;
        public event MatchPlayerEventHandler<IMatchUnitAssetView> AssetCreated;
        public event MatchPlayerEventHandler<IMatchUnitAssetView> AssetRemoved;

        private IMatchEngine m_engine;

        private readonly int m_playerIndex;

        private readonly Dictionary<int, VoxelAbilities>[] m_allAbilities;
        private readonly Dictionary<long, IMatchUnitController> m_idToUnit = new Dictionary<long, IMatchUnitController>();

        private IMatchPlayerController[] m_otherPlayerControllers;
        private readonly Dictionary<VoxelData, long> m_voxelDataToId = new Dictionary<VoxelData, long>();
        private readonly Dictionary<long, MatchAsset> m_idToAsset = new Dictionary<long, MatchAsset>(); //Walls build by player and possible other types of VoxelData. Assets are passive elements and does not have corresponding controller 

        private long m_identity;
        private IMatchUnitController[] m_units;
        private CommandsArray m_commandBuffer;

        private readonly Dictionary<Guid, List<Assignment>> m_groupIdToAssignments = new Dictionary<Guid, List<Assignment>>();
  
        private bool m_isPlayerLeftRoom = false;
        private bool m_isPlayerInRoom = true;
        public bool IsPlayerInRoom
        {
            get { return m_isPlayerInRoom; }
        }

        private int m_controllableUnitsCount;
        public bool HasControllableUnits
        {
            get { return m_controllableUnitsCount > 0; }
        }

        int IMatchPlayerView.Index
        {
            get { return m_playerIndex; }
        }

        int IMatchPlayerView.ControllableUnitsCount
        {
            get { return m_controllableUnitsCount; }
        }

        int IMatchPlayerView.UnitsCount
        {
            get { return m_units.Length; }
        }

        int IMatchPlayerView.AssetsCount
        {
            get { return m_idToAsset.Count; }
        }

        IEnumerable IMatchPlayerView.Units
        {
            get { return m_units; }
        }

        IEnumerable IMatchPlayerView.Assets
        {
            get { return m_idToAsset.Values; }
        }

        public MatchPlayerController(IMatchEngine engine, int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities)
        {
            m_playerIndex = playerIndex;
            m_engine = engine;
            m_allAbilities = allAbilities;

            m_engine.Map.Root.ForEach(cell =>
            {
                cell.ForEach(voxelData =>
                {
                    int owner = voxelData.Owner;
                    if (owner == m_playerIndex)
                    {
                        if (VoxelData.IsUnit(voxelData.Type))
                        {
                            Coordinate coordinate = new Coordinate(cell, voxelData);
                            CreateUnitController(voxelData, coordinate);
                        }
                        else
                        {
                            if (!voxelData.IsNeutral)
                            {
                                CreateAsset(voxelData, cell);
                            }
                        }
                    }
                });
            });

            m_units = m_idToUnit.Values.ToArray();
            m_commandBuffer = new CommandsArray(new Cmd[m_idToUnit.Count]);
        }

        private void CreateAsset(VoxelData data, MapCell cell)
        {
            if (data.IsNeutral)
            {
                return;
            }

            data.UnitOrAssetIndex = m_identity;
            MatchAsset asset = new MatchAsset(data, cell);

            m_voxelDataToId.Add(data, m_identity);
            m_idToAsset.Add(m_identity, asset);

            unchecked
            {
                m_identity++;
            }
           
            if (AssetCreated != null)
            {
                AssetCreated(asset);
            }
        }

        private void RemoveAsset(VoxelData data)
        {
            if (data.IsNeutral)
            {
                return;
            }

            long id; 

            if(m_voxelDataToId.TryGetValue(data, out id))
            {
                m_voxelDataToId.Remove(data);

                IMatchUnitAssetView asset = m_idToAsset[id];

                m_idToAsset.Remove(id);

                if (AssetRemoved != null)
                {
                    AssetRemoved(asset);
                }
            }
        }

        private void CreateUnitController(VoxelData voxelData, Coordinate coordinate)
        {
            voxelData.UnitOrAssetIndex = m_identity;
            IMatchUnitController unit = MatchFactory.CreateUnitController(m_engine, coordinate, voxelData.Type, m_playerIndex, m_allAbilities);
            m_idToUnit.Add(m_identity, unit);

            if (VoxelData.IsControllableUnit(unit.DataController.ControlledData.Type))
            {
                m_controllableUnitsCount++;
            }

            unchecked
            {
                m_identity++;
            }
            
            if (UnitCreated != null)
            {
                UnitCreated(unit);
            }
        }

        private bool RemoveUnitController(bool unitsChanged, IMatchUnitController unitController)
        {
            if (m_idToUnit.ContainsKey(unitController.Id))
            {
                if (VoxelData.IsControllableUnit(unitController.DataController.ControlledData.Type))
                {
                    m_controllableUnitsCount--;
                }

                MatchFactory.DestroyUnitController(unitController);
                m_idToUnit.Remove(unitController.Id);
                unitsChanged = true;

                if(UnitRemoved != null)
                {
                    UnitRemoved(unitController);
                }
            }

            return unitsChanged;
        }

        public void Submit(Cmd command)
        {
            if (m_isPlayerLeftRoom)
            {
                return; //it means that player has left the room;
            }

            if (command.Code == CmdCode.Composite)
            {
                CompositeCmd composite = (CompositeCmd)command;
                if (composite.Commands != null)
                {
                    for (int i = 0; i < composite.Commands.Length; ++i)
                    {
                        SubmitForUnit(composite.Commands[i]);
                    }
                }
            }
            else
            {
                SubmitForUnit(command);
            }


            if (command.Code == CmdCode.LeaveRoom)
            {

                //Changes player color to neutral??

                m_isPlayerLeftRoom = true;
            }

        }

        private void SubmitForUnit(Cmd command)
        {
            IMatchUnitController unit;
            if (m_idToUnit.TryGetValue(command.UnitIndex, out unit))
            {
                unit.SetCommand(command);
            }
            else
            {
                Debug.LogWarningFormat("Unit {0} not found", command.UnitIndex);
            }
        }


        public bool Tick(long tick, out CommandsArray commands)
        {
            m_isPlayerInRoom = !m_isPlayerLeftRoom;

            bool isChanged = false;

            if (m_units.Length != m_commandBuffer.Commands.Length)
            {
                m_commandBuffer = new CommandsArray(new Cmd[m_idToUnit.Count]);
            }

            bool unitsChanged = false;
            for (int i = 0; i < m_units.Length; ++i)
            {
                IMatchUnitController unitController = m_units[i];

                Cmd cmd;
                unitController.Tick(tick, out cmd);

                IList<VoxelDataCellPair> createdVoxels = unitController.CreatedVoxels;
                if (createdVoxels.Count != 0)
                {
                    CreateAssets(createdVoxels);
                    for (int j = 0; j < m_otherPlayerControllers.Length; ++j)
                    {
                        m_otherPlayerControllers[j].CreateAssets(createdVoxels);
                    }
                }

                IList<VoxelData> eatenOrDestroyed = unitController.EatenOrDestroyedVoxels;
                if (eatenOrDestroyed.Count != 0)
                {
                    RemoveAssets(eatenOrDestroyed);
                    for (int j = 0; j < m_otherPlayerControllers.Length; ++j)
                    {
                        m_otherPlayerControllers[j].RemoveAssets(eatenOrDestroyed);
                    }
                }

                m_commandBuffer.Commands[i] = cmd;

                if (cmd != null)
                {
                    isChanged = true;

                    if (cmd.Code == CmdCode.Composite)
                    {
                        CompositeCmd composite = (CompositeCmd)cmd;
                        for (int c = 0; c < composite.Commands.Length; ++c)
                        {
                            unitsChanged = PostprocessCmd(unitsChanged, unitController, composite.Commands[c]);
                        }
                    }
                    else
                    {
                        unitsChanged = PostprocessCmd(unitsChanged, unitController, cmd);
                    }
                }
              

                if (!unitController.IsAlive)
                {
                    unitsChanged = RemoveUnitController(unitsChanged, unitController);
                }
            }


            if (unitsChanged)
            {
                m_units = m_idToUnit.Values.ToArray();
            }

            commands = m_commandBuffer;
                        
            return isChanged;
        }


        private bool PostprocessCmd(bool unitsChanged, IMatchUnitController unitController, Cmd cmd)
        {
            if(cmd.IsFailed)
            {
                return unitsChanged;
            }
            if (cmd.Code == CmdCode.Split || cmd.Code == CmdCode.Split4)
            {
                CoordinateCmd splitCmd = (CoordinateCmd)cmd;
                for (int c = 0; c < splitCmd.Coordinates.Length; ++c)
                {
                    Coordinate coordinate = splitCmd.Coordinates[c];
                    VoxelData voxelData = m_engine.Map.Get(coordinate);

                    if (voxelData != null)
                    {
                        CreateUnitController(voxelData, coordinate);
                        unitsChanged = true;
                    }
                }
            }
            else if (cmd.Code == CmdCode.Convert)
            {
                Coordinate coord = unitController.DataController.Coordinate;
                MapCell cell = m_engine.Map.Get(coord.Row, coord.Col, coord.Weight);
                VoxelData voxelData = cell.GetVoxelDataAt(coord.Altitude);

                if (voxelData != null)
                {
                    if(VoxelData.IsUnit(voxelData.Type))
                    {
                        CreateUnitController(voxelData, unitController.DataController.Coordinate);
                        unitsChanged = true;
                    }
                    else
                    {
                        CreateAsset(voxelData, cell);
                    }
                }
            }


            return unitsChanged;
        }

        public IMatchUnitController GetUnitController(long index)
        {
            IMatchUnitController result;
            if(m_idToUnit.TryGetValue(index, out result))
            {
                return result;
            }
            return null;
        }

        public MatchAsset GetAsset(long index)
        {
            MatchAsset result;
            if(m_idToAsset.TryGetValue(index, out result))
            {
                return result;
            }
            return null;
        }


        public void ConnectWith(IMatchPlayerController[] playerControllers)
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
                    CreateAsset(pair.Data, pair.Cell);
                }
            }
        }

        public void RemoveAssets(IList<VoxelData> removeAssets)
        {
            for (int i = 0; i < removeAssets.Count; ++i)
            {
                VoxelData data = removeAssets[i];
                if (data.Owner == m_playerIndex)
                {
                    RemoveAsset(data);
                }
            }
        }

        IMatchUnitAssetView IMatchPlayerView.GetUnit(long id)
        {
            IMatchUnitController unit;
            if(m_idToUnit.TryGetValue(id, out unit))
            {
                return unit;
            }
            return null;
        }
        IMatchUnitAssetView IMatchPlayerView.GetAsset(long id)
        {
            MatchAsset asset;
            if(m_idToAsset.TryGetValue(id, out asset))
            {
                return asset;
            }
            return null;
        }
        IMatchUnitAssetView IMatchPlayerView.GetUnitOrAsset(long id)
        {
            IMatchUnitController unit;
            if (m_idToUnit.TryGetValue(id, out unit))
            {
                return unit;
            }
            MatchAsset asset;
            if (m_idToAsset.TryGetValue(id, out asset))
            {
                return asset;
            }
            return null;
        }

        public void DestroyAllUnitsAndAssets()
        {
            for(int i = 0; i < m_units.Length; ++i)
            {
                IMatchUnitController unit = m_units[i];
                if(!unit.IsAlive)
                {
                    continue;
                }
                Coordinate c = unit.DataController.Coordinate;
                MapCell cell = m_engine.Map.Get(c.Row, c.Col, c.Weight);
                cell.RemoveVoxelDataAndDecreaseHeight(unit.Data);
                RemoveUnitController(true, unit);
            }

            m_units = new IMatchUnitController[0];

            long[] assetIds = m_idToAsset.Keys.ToArray();
            for(int i = 0; i < assetIds.Length; ++i)
            {
                long assetId = assetIds[i];
                MatchAsset asset = m_idToAsset[assetId];
                asset.Cell.RemoveVoxelDataAndDecreaseHeight(asset.VoxelData);
                RemoveAsset(asset.VoxelData);
            }
        }

        public void AddAssignment(Guid groupId, long unitId, SerializedTaskLaunchInfo taskLaunchInfo, bool hasTarget = false, int targetPlayerIndex = -1, long targetId = 0)
        {
            IMatchPlayerView targetPlayerView = null;
            IMatchUnitAssetView targetUnitOrAsset = null;
            if (hasTarget)
            {
                targetPlayerView = m_engine.GetPlayerView(targetPlayerIndex);
                targetUnitOrAsset = targetPlayerView.GetUnitOrAsset(targetId);
                if(targetUnitOrAsset == null || !targetUnitOrAsset.IsAlive)
                {
                    hasTarget = false;
                    targetId = 0;
                    targetPlayerIndex = -1;
                }
            }

            IMatchUnitAssetView unit = m_idToUnit[unitId];

            Assignment assignment = new Assignment
            {
                GroupId = groupId,
                UnitId = unitId,
                HasUnit = true,
                TaskLaunchInfo = taskLaunchInfo,
                TargetPlayerIndex = targetPlayerIndex,
                TargetId = targetId,
                HasTarget = hasTarget
            };

            if(unit.Assignment != null)
            {
                RemoveUnitFromAssignment(unit);
            }

            unit.Assignment = assignment;
            if(hasTarget)
            {
                if(targetUnitOrAsset.TargetForAssignments == null)
                {
                    targetUnitOrAsset.TargetForAssignments = new List<Assignment>();
                }
                targetUnitOrAsset.TargetForAssignments.Add(assignment);
            }

            List<Assignment> group;
            if(!m_groupIdToAssignments.TryGetValue(groupId, out group))
            {
                group = new List<Assignment>();
                m_groupIdToAssignments.Add(groupId, group);
            }

            group.Add(assignment);
        }

        public void RemoveUnitFromAssignment(IMatchUnitAssetView unit)
        {
            Assignment assignment = unit.Assignment;
            if (assignment == null)
            {
                return;
            }
            unit.Assignment = null;

            assignment.HasUnit = false;
            assignment.UnitId = 0;

            if(!assignment.HasTarget)
            {
                List<Assignment> groupAssignments;
                if(m_groupIdToAssignments.TryGetValue(assignment.GroupId, out groupAssignments))
                {
                    groupAssignments.Remove(assignment);
                    if(groupAssignments.Count == 0)
                    {
                        m_groupIdToAssignments.Remove(assignment.GroupId);
                    }
                }
            }
        }

        public void RemoveTargetFromAssignments(IMatchUnitAssetView unitOrAsset)
        {
            List<Assignment> targetForAssignments = unitOrAsset.TargetForAssignments;
            if(targetForAssignments == null)
            {
                return;
            }
            unitOrAsset.TargetForAssignments = null;

            for(int i = 0; i < targetForAssignments.Count; ++i)
            {
                Assignment assignment = targetForAssignments[i];
                assignment.HasTarget = false;
                assignment.TargetId = 0;
                assignment.TargetPlayerIndex = -1;

                if(!assignment.HasUnit)
                {
                    List<Assignment> groupAssignments;
                    if (m_groupIdToAssignments.TryGetValue(assignment.GroupId, out groupAssignments))
                    {
                        groupAssignments.Remove(assignment);
                        if (groupAssignments.Count == 0)
                        {
                            m_groupIdToAssignments.Remove(assignment.GroupId);
                        }
                    }
                }
            }
        }

        public void RemoveAssignment(IMatchUnitAssetView unit)
        {
            Assignment assignment = unit.Assignment;
            if (assignment == null)
            {
                return;
            }
            RemoveAssignment(assignment);
        }

        private void RemoveAssignment(Assignment assignment)
        {
            if(assignment.HasUnit)
            {
                IMatchPlayerView playerView = m_engine.GetPlayerView(m_playerIndex);
                IMatchUnitAssetView unitOrAsset = playerView.GetUnitOrAsset(assignment.UnitId);
                if(unitOrAsset != null)
                {
                    unitOrAsset.Assignment = null;
                }
            }

            assignment.HasUnit = false;
            assignment.UnitId = 0;

            if (assignment.HasTarget)
            {
                IMatchPlayerView targetPlayerView = m_engine.GetPlayerView(assignment.TargetPlayerIndex);
                IMatchUnitAssetView targetUnitOrAsset = targetPlayerView.GetUnitOrAsset(assignment.TargetId);
                if (targetUnitOrAsset != null && targetUnitOrAsset.TargetForAssignments != null)
                {
                    targetUnitOrAsset.TargetForAssignments.Remove(assignment);
                    if(targetUnitOrAsset.TargetForAssignments.Count == 0)
                    {
                        targetUnitOrAsset.TargetForAssignments = null;
                    }
                }

                assignment.HasTarget = false;
                assignment.TargetId = 0;
                assignment.TargetPlayerIndex = -1;
            }

            List<Assignment> groupAssignments;
            if (m_groupIdToAssignments.TryGetValue(assignment.GroupId, out groupAssignments))
            {
                groupAssignments.Remove(assignment);
                if (groupAssignments.Count == 0)
                {
                    m_groupIdToAssignments.Remove(assignment.GroupId);
                }
            }
        }

        public void RemoveAssignmentGroup(Guid groupId)
        {
            List<Assignment> assignments;
            if(!m_groupIdToAssignments.TryGetValue(groupId, out assignments))
            {
                return;
            }

            assignments = assignments.ToList();
            for(int i = assignments.Count - 1; i >= 0; --i)
            {
                Assignment assignment = assignments[i];
                RemoveAssignment(assignment);
            }
        }
    }

}

