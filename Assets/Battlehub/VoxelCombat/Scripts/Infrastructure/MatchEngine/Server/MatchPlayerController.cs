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

        bool Tick(out CommandsArray commands);

        IMatchUnitController GetUnitController(long index);

        void ConnectWith(IMatchPlayerController[] playerControllers);
     
        void CreateAssets(IList<VoxelDataCellPair> createAssets);

        void RemoveAssets(IList<VoxelData> removeAssets);

        void DestroyAllUnitsAndAssets();
    }


    public class MatchAsset : IMatchUnitAssetView
    {
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

        public MatchAsset(VoxelData data, MapCell cell)
        {
            m_voxelData = data;
            m_cell = cell;
            m_pos = cell.GetPosition();
        }
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

        private IMatchUnitController[] m_units;
        private CommandsArray m_commandBuffer;
        private long m_identity;

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

        IMatchUnitAssetView[] IMatchPlayerView.Units
        {
            get { return m_units; }
        }

        System.Collections.IEnumerable IMatchPlayerView.Assets
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

            m_identity++;

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

            m_identity++;

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


        public bool Tick(out CommandsArray commands)
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

                Cmd cmd = unitController.Tick();

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
    }

}

