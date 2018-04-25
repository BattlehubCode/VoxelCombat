
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface IMatchUnitControllerCli
    {
        IVoxelDataController DataController
        {
            get;
        }

        int Type
        {
            get;
        }



        //Execute Command side effect
        IList<VoxelDataCellPair> CreatedVoxels
        {
            get;
        }

        //ExecuteCommand side effect
        IList<VoxelData> EatenOrDestroyedVoxels
        {
            get;
        }

        void ExecuteCommand(Cmd cmd, long tick);

        void Select(int playerIndex);
        void Unselect(int playerIndex);

        void SelectAsTarget(int playerIndex);

        void UnselectAsTarget(int playerIndex);
        
        void Destroy();


    }

    public abstract class MatchUnitControllerBaseCli : IMatchUnitControllerCli
    {
        protected readonly IVoxelMap m_voxelMap;
        protected readonly IVoxelFactory m_voxelFactory;
       
        protected readonly IVoxelDataController m_dataController;
        protected Voxel m_controlledVoxel;
        private ulong m_selection;
        private ulong m_targetSelection;

        protected float m_currentCmdDuration;
        protected long m_currentTick;

        public IVoxelDataController DataController
        {
            get { return m_dataController; }
        }

        public int Type
        {
            get { return m_dataController.ControlledData.Type; }
        }


        private static readonly VoxelData[] m_emptyData = new VoxelData[0];
        private static readonly VoxelDataCellPair[] m_emptyPairs = new VoxelDataCellPair[0];
        public virtual IList<VoxelDataCellPair> CreatedVoxels
        {
            get { return m_emptyPairs; }
        }

        public virtual IList<VoxelData> EatenOrDestroyedVoxels
        {
            get { return m_emptyData; }
        }
      
        public MatchUnitControllerBaseCli(IVoxelDataController dataController)
        {
            m_dataController = dataController;

            m_controlledVoxel = m_dataController.ControlledData.VoxelRef;

            OnSubscribe();

            m_dataController.ControlledData.VoxelRefSet += OnVoxelRefSet;
            m_dataController.ControlledData.VoxelRefReset += OnVoxelRefReset;

            m_voxelMap = Dependencies.Map;
            m_voxelFactory = Dependencies.VoxelFactory;
        }

        public void Destroy()
        {
            //if (m_controlledVoxel != null)
            //{
            //    OnUnsubscribe();

            //    for (int i = 1; i <= GameConstants.MaxLocalPlayers; ++i)
            //    {
            //        m_controlledVoxel.Unselect(i);
            //        m_controlledVoxel.UnselectAsTarget(i);
            //    }
            //}

            if(m_controlledVoxel != null)
            {
                m_controlledVoxel.Kill();
            }

            m_dataController.ControlledData.VoxelRefSet -= OnVoxelRefSet;
            m_dataController.ControlledData.VoxelRefReset -= OnVoxelRefReset;
        }

        private void OnVoxelRefSet(Voxel voxel)
        {
            m_controlledVoxel = voxel;
            if (m_controlledVoxel != null)
            {
                for (int i = 1; i <= GameConstants.MaxLocalPlayers; ++i)
                {
                    if (IsSelected(i))
                    {
                        m_controlledVoxel.Select(i);
                    }

                    if(IsSelectedAsTarget(i))
                    {
                        m_controlledVoxel.SelectAsTarget(i);
                    }
                }

                OnSubscribe();
            }
            OnVoxelRefSetOverride(voxel);
        }

        private void OnVoxelRefReset(Voxel voxel)
        {
            bool wasVisible = m_controlledVoxel != null;
            if (wasVisible)
            {
                for (int i = 1; i <= GameConstants.MaxLocalPlayers; ++i)
                {
                    m_controlledVoxel.Unselect(i);
                    m_controlledVoxel.UnselectAsTarget(i);
                }

                OnUnsubscribe();
            }

            m_controlledVoxel = null;

            OnVoxelRefResetOverride(voxel);

            if(m_dataController.ControlledData.Unit.State == VoxelDataState.Dead)
            {
                m_dataController.ControlledData.VoxelRefSet -= OnVoxelRefSet;
                m_dataController.ControlledData.VoxelRefReset -= OnVoxelRefReset;
            }
        }

        protected virtual void OnSubscribe()
        {

        }

        protected virtual void OnUnsubscribe()
        {

        }

        protected virtual void OnVoxelRefSetOverride(Voxel voxel)
        {

        }

        protected virtual void OnVoxelRefResetOverride(Voxel voxel)
        {

        }

        public void ExecuteCommand(Cmd cmd, long tick)
        {
            float duration = (cmd.Duration * GameConstants.MatchEngineTick); //seconds

            m_currentCmdDuration = Mathf.Max(0.01f, duration);

            m_currentTick = tick;

            OnExecuteCommand(cmd);

        }
        protected abstract void OnExecuteCommand(Cmd cmd);

        public void Select(int playerIndex) //this is player index (not owner index)
        {
            if (!IsSelected(playerIndex))
            {
                m_selection |= (1ul << playerIndex);
                if (m_controlledVoxel != null)
                {
                    m_controlledVoxel.Select(playerIndex);
                }
            }
        }
        public void Unselect(int playerIndex) ///this is player index (not owner index)
        {
            if (IsSelected(playerIndex))
            {
                m_selection &= ~(1ul << playerIndex);
                if (m_controlledVoxel != null)
                {
                    m_controlledVoxel.Unselect(playerIndex);
                }
            }
        }
        private bool IsSelected(int playerIndex)
        {
            return (m_selection & (1ul << playerIndex)) != 0;
        }

        public void SelectAsTarget(int playerIndex) //this is player index (not owner index)
        {
            if (!IsSelectedAsTarget(playerIndex))
            {
                m_targetSelection |= (1ul << playerIndex);
                if (m_controlledVoxel != null)
                {
                    m_controlledVoxel.SelectAsTarget(playerIndex);
                }
            }
        }
        public void UnselectAsTarget(int playerIndex) ///this is player index (not owner index)
        {
            if (IsSelectedAsTarget(playerIndex))
            {
                m_targetSelection &= ~(1ul << playerIndex);
                if (m_controlledVoxel != null)
                {
                    m_controlledVoxel.UnselectAsTarget(playerIndex);
                }
            }
        }
        private bool IsSelectedAsTarget(int playerIndex)
        {
            return (m_targetSelection & (1ul << playerIndex)) != 0;
        }


        protected Voxel AcquireVoxel(VoxelData voxelData, MapPos mapPos, int weight)
        {
            Voxel voxel = m_voxelFactory.Acquire(voxelData.Type);
            voxel.transform.position = m_voxelMap.GetWorldPosition(mapPos, weight);
            voxel.ReadFrom(voxelData);
            voxelData.VoxelRef = voxel;
            return voxel;
        }

    }

    public class SpawnerUnitControllerCli : MatchUnitControllerBaseCli
    {
        
        public SpawnerUnitControllerCli(IVoxelDataController dataController)
            :base(dataController)
        {
           
        }

        private readonly List<VoxelDataCellPair> m_createdVoxels = new List<VoxelDataCellPair>();
        public override IList<VoxelDataCellPair> CreatedVoxels
        {
            get { return m_createdVoxels; }
        }

        protected override void OnExecuteCommand(Cmd cmd)
        {
            if(m_createdVoxels.Count != 0)
            {
                m_createdVoxels.Clear();
            }
            
            if (cmd.Code == CmdCode.Composite)
            {
                CompositeCmd compositeCmd = (CompositeCmd)cmd;
                for (int i = 0; i < compositeCmd.Commands.Length; ++i)
                {
                    ExecuteCommand(compositeCmd.Commands[i]);
                }
            }
            else
            {
                ExecuteCommand(cmd);
            }
        }

        private void ExecuteCommand(Cmd cmd)
        {
            if (cmd.Code == CmdCode.Spawn)
            {
                Coordinate[] coordinates;
                bool noFail = m_dataController.PerformSpawnAction(out coordinates);
                Debug.Assert(noFail);

                for (int i = 0; i < coordinates.Length; ++i)
                {
                    Coordinate coordinate = coordinates[i];
                    MapPos mapPos = coordinate.MapPos;

                    MapCell cell = m_voxelMap.Map.Get(coordinate.Row, coordinate.Col, coordinate.Weight);
                    VoxelData data = cell.GetVoxelDataAt(coordinate.Altitude);
                    
                    m_createdVoxels.Add(new VoxelDataCellPair(data, cell));

                    int weight = coordinate.Weight;
                    if (m_voxelMap.IsVisible(mapPos, weight))
                    {
                        AcquireVoxel(data, mapPos, weight);
                    }
                }
            }
        }
    }

    public class BombUnitControllerCli : VoxelActorUnitControllerCli
    {
        private IVoxelGame m_game;

        public BombUnitControllerCli(IVoxelDataController dataController)
            :base(dataController)
        {
            m_game = Dependencies.GameState;
        }

        protected override void OnCommand(Cmd cmd)
        {
            if(cmd.Code == CmdCode.Explode)
            {
                if(cmd is MovementCmd)
                {
                    MovementCmd explodeCmd = (MovementCmd)cmd;
                    Debug.Assert(explodeCmd.HasTarget);

                    Coordinate to = explodeCmd.Coordinates[1];
                    int targetPlayerIndex = explodeCmd.TargetPlayerIndex;
                    long targetIndex = explodeCmd.TargetIndex;

                    Explode(targetPlayerIndex, targetIndex, to);

                }
                else
                {
                    TargetCmd explodeCmd = (TargetCmd)cmd;
                    if(explodeCmd.HasTarget)
                    {
                        Coordinate to = m_dataController.Coordinate;

                        int targetPlayerIndex = explodeCmd.TargetPlayerIndex;
                        long targetIndex = explodeCmd.TargetIndex;

                        Explode(targetPlayerIndex, targetIndex, to);
                    }
                }
            }
        }

        private void Explode(int targetPlayerIndex, long targetIndex, Coordinate to)
        {
            VoxelData explodeData;
            IVoxelDataController dataController = m_game.GetVoxelDataController(targetPlayerIndex, targetIndex);
            if (dataController != null)
            {
                explodeData = dataController.ControlledData;
            }
            else
            {
                MatchAssetCli asset = m_game.GetAsset(targetPlayerIndex, targetIndex);
                explodeData = asset.VoxelData;
            }

            bool noFail = m_dataController.Explode(to, explodeData, EatOrDestroyCallback, ExpandCallback, ExplodeCallback);
            Debug.Assert(noFail);
            VoxelData voxelData = m_dataController.ControlledData;
            MapPos mapPos = m_dataController.Coordinate.MapPos;
            int weight = m_dataController.Coordinate.Weight;

            Debug.Assert(weight == m_dataController.ControlledData.Weight);

           // AcquireReleaseVisibility(voxelData, mapPos, weight);

            if (m_controlledVoxel == null)
            {
                CollapseEatExpandClear(m_currentTick);
            }
            else
            {
                Collapse(m_currentTick, 0);
                //this code moved above m_controlldeVoxel.Kill to prevent cleanup of eatables
                EatAndExpand(m_currentTick, 0); 
                
                //if (m_controlledVoxel != null)
                //{
                //    m_controlledVoxel.Explode(0);
                //}

                if (explodeData.VoxelRef != null)
                {
                    //explodeData.VoxelRef.Explode(0);

                    VoxelData next = explodeData.Next;
                    while (next != null)
                    {
                        if (next.VoxelRef != null)
                        {
                            next.VoxelRef.ChangeAltitude(next.VoxelRef.Altitude, next.Altitude, m_currentCmdDuration);
                        }

                        next = next.Next;
                    }
                }
            }

            Explode(0);
        }
    }

    public class VoxelActorUnitControllerCli : MatchUnitControllerBaseCli
    {
        protected class EatCmd
        {
            public VoxelData Eater;
            public VoxelData Voxel;
            public int EaterDeltaHealth;
            public int VoxelHealth;

            public EatCmd(VoxelData eater, VoxelData voxel, int eaterDeltaHealth, int voxelHealth)
            {
                Eater = eater;
                Voxel = voxel;
                EaterDeltaHealth = eaterDeltaHealth;
                VoxelHealth = voxelHealth;
            }
        }

        protected class CollapseCmd
        {
            public VoxelData VoxelData;
            public int DeltaAltitude;

            public CollapseCmd(VoxelData voxelData, int deltaAltitude)
            {
                VoxelData = voxelData;
                DeltaAltitude = deltaAltitude;
            }
        }

        protected class ExplodeCmd
        {
            public VoxelData VoxelData;
            public int Health;

            public ExplodeCmd(VoxelData data, int health)
            {
                VoxelData = data;
                Health = health;
            }
        }

        protected readonly List<VoxelData> m_expandedVoxels = new List<VoxelData>();
        protected readonly List<CollapseCmd> m_collapseCommands = new List<CollapseCmd>();
        protected readonly List<EatCmd> m_eatCommands = new List<EatCmd>();
        protected readonly List<VoxelData> m_eatenOrDestroyedVoxels = new List<VoxelData>();
        protected readonly List<ExplodeCmd> m_explodeVoxels = new List<ExplodeCmd>();

        public override IList<VoxelData> EatenOrDestroyedVoxels
        {
            get { return m_eatenOrDestroyedVoxels; }
        }



        public VoxelActorUnitControllerCli(IVoxelDataController dataController)
            :base(dataController)
        {
        }


        protected override void OnSubscribe()
        {
            base.OnSubscribe();
            if(m_controlledVoxel != null)
            {
                m_controlledVoxel.BeginMove += OnBeginMove;
                m_controlledVoxel.BeforeMoveCompleted += OnBeforeMoveCompleted;
                m_controlledVoxel.MoveCompleted += OnMoveCompleted;
                m_controlledVoxel.RotateCompleted += OnRotateCompleted;
                m_controlledVoxel.BeforeGrowCompleted += OnBeforeGrowCompleted;
                m_controlledVoxel.BeforeDiminishCompleted += OnBeforeDiminishCompleted;
                m_controlledVoxel.ResizeCompleted += OnResizeCompleted;
            }
        }

        protected override void OnUnsubscribe()
        {
            base.OnUnsubscribe();
            if(m_controlledVoxel != null)
            {
                m_controlledVoxel.BeginMove -= OnBeginMove;
                m_controlledVoxel.BeforeMoveCompleted -= OnBeforeMoveCompleted;
                m_controlledVoxel.MoveCompleted -= OnMoveCompleted;
                m_controlledVoxel.RotateCompleted -= OnRotateCompleted;
                m_controlledVoxel.BeforeGrowCompleted -= OnBeforeGrowCompleted;
                m_controlledVoxel.BeforeDiminishCompleted -= OnBeforeDiminishCompleted;
                m_controlledVoxel.ResizeCompleted -= OnResizeCompleted;
            }
        }

        protected override void OnVoxelRefResetOverride(Voxel voxel)
        {
            base.OnVoxelRefResetOverride(voxel);

            for(int i = 0; i < m_eatCommands.Count; ++i)
            {
                EatCmd cmd = m_eatCommands[i];
                if(cmd.Voxel != null && cmd.Voxel.VoxelRef != null)
                {
                    cmd.Voxel.VoxelRef.Kill();
                }

            }
            m_eatCommands.Clear(); //not cleared eat commands may lead to spawing of new objects
            CollapseEatExpandClear(m_currentTick);

            for (int i = m_explodeVoxels.Count - 1; i >= 0; --i)
            {
                ExplodeCmd explodeCmd = m_explodeVoxels[i];

                if(explodeCmd.VoxelData != m_controlledVoxel.VoxelData)
                {
                    if (explodeCmd.VoxelData.VoxelRef != null)
                    {
                        if(explodeCmd.VoxelData.IsAlive)
                        {
                            explodeCmd.VoxelData.VoxelRef.Health = explodeCmd.VoxelData.Health;
                        }
                        else
                        {
                            explodeCmd.VoxelData.VoxelRef.Kill();
                        }   
                    }
                }
            }

            m_explodeVoxels.Clear();
        }

        protected void CollapseEatExpandClear(long tick)
        {
            Collapse(tick, 0);
            EatAndExpand(tick, 0);

            m_eatCommands.Clear();
            m_expandedVoxels.Clear();
            m_collapseCommands.Clear();
        }

        protected override void OnExecuteCommand(Cmd cmd)
        {
            if(m_eatenOrDestroyedVoxels.Count != 0)
            {
                m_eatenOrDestroyedVoxels.Clear();
            }
            
            CollapseEatExpandClear(m_currentTick - 1); //clear previous side effect commands if remaining

            if(cmd.Code == CmdCode.Composite)
            {
                CompositeCmd compositeCmd = (CompositeCmd)cmd;
                for(int i = 0; i < compositeCmd.Commands.Length; ++i)
                {
                    ExecuteCommand(compositeCmd.Commands[i]);
                }
            }
            else
            {
                ExecuteCommand(cmd);
            }
        }

        private void ExecuteCommand(Cmd cmd)
        {
            if (cmd.Code == CmdCode.StateChanged)
            {
                OnStateChanged(cmd);
            }
            else if (cmd.Code == CmdCode.Move)
            {
                OnMove(cmd);
            }
            else if (cmd.Code == CmdCode.RotateLeft)
            {
                OnRotateLeft();
            }
            else if (cmd.Code == CmdCode.RotateRight)
            {
                OnRotateRight();
            }
            else if (cmd.Code == CmdCode.Split)
            {
                OnSplit();
            }
            else if (cmd.Code == CmdCode.Split4)
            {
                OnSplit4();
            }
            else if (cmd.Code == CmdCode.Grow)
            {
                OnGrow();
            }
            else if (cmd.Code == CmdCode.Diminish)
            {
                OnDiminish();
            }
            else if (cmd.Code == CmdCode.Convert)
            {
                OnConvert(cmd);
            }
            else if (cmd.Code == CmdCode.SetHealth)
            {
                OnSetHealth(cmd);
            }
            else if (cmd.Code == CmdCode.Failed)
            {
                if (m_controlledVoxel != null)
                {
                    //Last command was failed go to idle state 
                }
            }
            else
            {
                OnCommand(cmd);
            }
        }

        protected virtual void OnCommand(Cmd cmd)
        {

        }

        protected void AcquireReleaseVisibility(VoxelData voxelData, MapPos mapPos, int weight)
        {
            if (m_voxelMap.IsVisible(mapPos, weight))
            {
                if (voxelData.VoxelRef == null)
                {
                    AcquireVoxel(voxelData, mapPos, weight);
                }
            }
            else
            {
                if (voxelData.VoxelRef != null)
                {
                    m_voxelFactory.Release(voxelData.VoxelRef);
                    voxelData.VoxelRef = null;
                }
            }
        }

        protected virtual void OnStateChanged(Cmd cmd)
        {
            ChangeParamsCmd changeParamsCmd = (ChangeParamsCmd)cmd;
            bool noFail = m_dataController.SetVoxelDataState((VoxelDataState)changeParamsCmd.IntParams[1]);
            Debug.Assert(noFail);
        }

        protected virtual void OnMove(Cmd cmd)
        {
            MovementCmd coordinateCmd = (MovementCmd)cmd;
            Coordinate to = coordinateCmd.Coordinates[1];

            bool isLastCmd = coordinateCmd.IsLastCmdInSequence;
            bool noFail = m_dataController.Move(to, isLastCmd,  EatOrDestroyCallback, CollapseCallback, ExpandCallback, ExplodeCallback);
            Debug.Assert(noFail);
           
            VoxelData voxelData = m_dataController.ControlledData;
            MapPos mapPos = m_dataController.Coordinate.MapPos;
            int weight = m_dataController.Coordinate.Weight;

            Debug.Assert(weight == m_dataController.ControlledData.Weight);

            bool wasVisible = m_controlledVoxel != null;

            AcquireReleaseVisibility(voxelData, mapPos, weight);
            
            bool isVisible = m_controlledVoxel != null;

            if (wasVisible && isVisible)
            {
                m_controlledVoxel.Move(voxelData.Altitude, m_currentTick, m_currentCmdDuration);
            }
            else
            {
                m_eatCommands.Clear();
                //clear needed because voxel already reflected current Health value during AcquireReleaseVisibility method call
                //no execution of eat commands required in this case

                CollapseEatExpandClear(m_currentTick);
                Explode(0);
            }
        }

 
        protected virtual void OnRotateLeft()
        {
            m_dataController.RotateLeft();

            if (m_controlledVoxel != null)
            {
                m_controlledVoxel.RotateLeft(m_currentTick, m_currentCmdDuration);
            }
        }

        protected virtual void OnRotateRight()
        {
            m_dataController.RotateRight();

            if (m_controlledVoxel != null)
            {
                m_controlledVoxel.RotateRight(m_currentTick, m_currentCmdDuration);
            }
        }

        protected virtual void OnSplit()
        {
            Coordinate[] coordinates;
            bool noFail = m_dataController.Split(out coordinates, EatOrDestroyCallback, CollapseCallback, DieCallback);
            Debug.Assert(noFail);

            int visibleCount = 0;
            for (int i = 0; i < coordinates.Length; ++i)
            {
                Coordinate coordinate = coordinates[i];
                //Voxel cloneActor = null;
                if (m_voxelMap.IsVisible(coordinate.MapPos, coordinate.Weight))
                {
                    VoxelData voxelData = m_voxelMap.Map.Get(coordinate);
                    // cloneActor =
                    AcquireVoxel(voxelData, coordinate.MapPos, coordinate.Weight);
                    visibleCount++;
                }
            }

            m_eatCommands.Clear();
            //clear needed because voxel already reflected current Health value during AcquireReleaseVisibility method call
            //no execution of eat commands required in this case

            if (visibleCount == 0)
            {
                CollapseEatExpandClear(m_currentTick);
            }
            else
            {
                /*This code should be moved to Split Animation OnBeforeCompleted event handler*/
                Collapse(m_currentTick, 0);
                EatAndExpand(m_currentTick, 0);
            }

        }

        protected virtual void OnSplit4()
        {
            Coordinate[] coordinates;
            bool noFail = m_dataController.Split4(out coordinates, ExpandCallback, DieCallback);
            Debug.Assert(noFail);


            int visibleCount = 0;
            for(int i = 0; i < coordinates.Length; ++i)
            {
                Coordinate coordinate = coordinates[i];
                //Voxel cloneActor = null;
                if (m_voxelMap.IsVisible(coordinate.MapPos, coordinate.Weight))
                {
                    VoxelData voxelData = m_voxelMap.Map.Get(coordinate);
                   // cloneActor =
                        AcquireVoxel(voxelData, coordinate.MapPos, coordinate.Weight);
                    visibleCount++;
                }
            }

            m_eatCommands.Clear();
            //clear needed because voxel already reflected current Health value during AcquireReleaseVisibility method call
            //no execution of eat commands required in this case

            if (visibleCount == 0)
            {
                CollapseEatExpandClear(m_currentTick); 
            }
            else
            {
                /*This code should be moved to Split4 Animation OnBeforeCompleted event handler*/
                Collapse(m_currentTick, 0);
                EatAndExpand(m_currentTick, 0);
            }
        }

        protected virtual void OnGrow()
        {
            int previousAltitude = m_dataController.ControlledData.Altitude;
            bool noFail = m_dataController.Grow(EatOrDestroyCallback, CollapseCallback);
            Debug.Assert(noFail);
            if (m_controlledVoxel == null)
            {
                CollapseEatExpandClear(m_currentTick);
            }
            else
            {
                m_controlledVoxel.Health = m_dataController.Abilities.DefaultHealth;//  // m_dataController.ControlledData.Health;
                Vector3 position = m_voxelMap.GetWorldPosition(m_dataController.Coordinate);
                m_controlledVoxel.Grow(position, m_currentTick, m_currentCmdDuration);

                int newAltitude = m_dataController.ControlledData.Altitude;
                m_controlledVoxel.ChangeAltitude(previousAltitude, newAltitude, m_currentCmdDuration, 0);

                float duration = (m_dataController.Abilities.GrowDuration) * GameConstants.MatchEngineTick;
                Collapse(m_currentTick, duration);
            }
        }

        protected virtual void OnDiminish()
        {
            int previousAltitude = m_dataController.ControlledData.Altitude;
            bool noFail = m_dataController.Diminish(ExpandCallback);
            Debug.Assert(noFail);
            if (m_controlledVoxel == null)
            {
                CollapseEatExpandClear(m_currentTick);
            }
            else
            {
                Vector3 position = m_voxelMap.GetWorldPosition(m_dataController.Coordinate);
                m_controlledVoxel.Diminish(position, m_currentTick, m_currentCmdDuration);

                int newAltitude = m_dataController.ControlledData.Altitude;
                m_controlledVoxel.ChangeAltitude(previousAltitude, newAltitude, m_currentCmdDuration, 0);

                float duration = (m_dataController.Abilities.DiminishDuration) * GameConstants.MatchEngineTick;
                EatAndExpand(m_currentTick, duration);
            }
        }

        protected virtual void OnSetHealth(Cmd cmd)
        {
            ChangeParamsCmd changeCmd = (ChangeParamsCmd)cmd;

            m_dataController.SetHealth(changeCmd.IntParams[0], DieCallback);

            if (m_controlledVoxel != null)
            {
                m_controlledVoxel.Health = changeCmd.IntParams[0];
            }
        }

        protected virtual void OnConvert(Cmd cmd)
        {
            ChangeParamsCmd changeParams = (ChangeParamsCmd)cmd;
            int type = changeParams.IntParams[0];

            bool isVisible = m_controlledVoxel != null;
            if (isVisible)
            {
                m_controlledVoxel.Kill();
            }
          
            bool noFail = m_dataController.Convert(type, DieCallback);
            Debug.Assert(noFail);

            if (isVisible)
            {
                VoxelData voxelData = m_voxelMap.Map.Get(m_dataController.Coordinate);
                MapPos mapPos = m_dataController.Coordinate.MapPos;
                int weight = m_dataController.Coordinate.Weight;
                AcquireVoxel(voxelData, mapPos, weight);
            }
        }

        /// <summary>
        /// If delta Health is 0 then voxel will be smashed otherwise it will be eaten 
        /// </summary>
        /// <param name="voxelData"></param>
        /// <param name="eaterDeltaHealth"></param>
        protected void EatOrDestroyCallback(VoxelData eater, VoxelData voxelData, int eaterDeltaHealth, int voxelHealth)
        {
            //if(voxelData.VoxelRef != null)
            //{
            //    voxelData.VoxelRef.WillBeEaten = deltaHealth > 0;
            //}

            m_eatCommands.Add(new EatCmd(eater, voxelData, eaterDeltaHealth, voxelHealth));
            m_eatenOrDestroyedVoxels.Add(voxelData);
        }

        protected void CollapseCallback(VoxelData voxelData, int altitudeChange)
        {
            m_collapseCommands.Add(new CollapseCmd(voxelData, altitudeChange));
        }

        protected void ExpandCallback(VoxelData voxelData)
        {
            m_expandedVoxels.Add(voxelData);
        }

        protected void ExplodeCallback(VoxelData voxelData, int health)
        {
            m_explodeVoxels.Add(new ExplodeCmd(voxelData, health));
        }

        protected void DieCallback(VoxelData voxelData)
        {
            if(voxelData.VoxelRef != null)
            {
                voxelData.VoxelRef.Kill();
            }
        }

        protected virtual void OnBeginMove(long tick)
        {
           // if(tick == m_currentTick) <-- this line prevent execution of required actions. it will lead to argument out of range exceptions in voxel actor
            {
                float duration = (m_dataController.Abilities.MovementDuration / 1.5f) * GameConstants.MatchEngineTick;
                Collapse(tick, duration, true, m_dataController.ControlledData.Weight); //collapse voxels with higher weight (to make animation started in proper time)
            }
        }

        protected virtual void OnBeforeMoveCompleted(long tick)
        {
            // if(tick == m_currentTick) <-- this line prevent execution of required actions. it will lead to argument out of range exceptions in voxel actor
            {
                float duration = (m_dataController.Abilities.MovementDuration / 5) * GameConstants.MatchEngineTick;
                EatAndExpand(tick, duration);
                Collapse(tick, duration, false, 0, m_dataController.ControlledData.Weight); //collapse voxels with lower weight
                Explode(0);
            }
        }

        protected virtual void OnMoveCompleted(long tick)
        {
        }

        protected virtual void OnRotateCompleted(long tick)
        {
        }

        protected virtual void OnBeforeGrowCompleted(long tick)
        {
            //if(tick == m_currentTick) <-- this line prevent execution of required actions. it will lead to argument out of range exceptions in voxel actor
            {
                float duration = (m_dataController.Abilities.GrowDuration / 5) * GameConstants.MatchEngineTick;
                EatAndExpand(tick, duration);
            }
        }

        protected virtual void OnBeforeDiminishCompleted(long tick)
        {
        }

        protected virtual void OnResizeCompleted(long tick)
        {
        }

        protected void Explode(float delay)
        {
            for (int i = m_explodeVoxels.Count - 1; i >= 0; --i)
            {
                ExplodeCmd explodeCmd = m_explodeVoxels[i];
                m_explodeVoxels.RemoveAt(i);

                if (explodeCmd.VoxelData.VoxelRef != null)
                {
                    if(explodeCmd.VoxelData.IsAlive)
                    {
                        explodeCmd.VoxelData.VoxelRef.Health = explodeCmd.VoxelData.Health;
                    }
                    else
                    {
                        explodeCmd.VoxelData.VoxelRef.Explode(delay, explodeCmd.Health);
                    }
                    
                }
            }
        }

        protected void Collapse(long tick, float duration, bool skipDeltaAltitude = false, int minWeight = 0, int maxWeight = int.MaxValue)
        {
            for (int i = m_collapseCommands.Count - 1; i >= 0; --i)
            {
                CollapseCmd collapseCmd = m_collapseCommands[i];
                VoxelData collapsedVoxel = collapseCmd.VoxelData;
                if (collapsedVoxel.VoxelRef != null)
                {
                    if(skipDeltaAltitude)
                    {
                        if(Math.Abs(collapseCmd.DeltaAltitude) >= collapsedVoxel.RealHeight / 2)
                        {
                            continue;
                        }
                    }

                    if(minWeight <= collapsedVoxel.Weight && collapsedVoxel.Weight <= maxWeight)
                    {   
                        collapsedVoxel.VoxelRef.ChangeAltitude(collapsedVoxel.VoxelRef.Altitude, collapsedVoxel.Altitude, duration);
                        collapsedVoxel.VoxelRef.Collapse(duration);
                        m_collapseCommands.RemoveAt(i);
                    }
                }
                else
                {
                    m_collapseCommands.RemoveAt(i);
                }
            }
        }

        protected void EatAndExpand(long tick, float duration)
        {
            for (int i = 0; i < m_eatCommands.Count; ++i)
            {
                EatCmd eatCmd = m_eatCommands[i];
                ExecuteEatCommand(tick, eatCmd);
            }
            m_eatCommands.Clear();

            for (int i = 0; i < m_expandedVoxels.Count; ++i)
            {
                VoxelData expandedVoxel = m_expandedVoxels[i];
                if (expandedVoxel.VoxelRef != null)
                {
                    int height = expandedVoxel.Height;

                    expandedVoxel.VoxelRef.ChangeAltitude(expandedVoxel.VoxelRef.Altitude, expandedVoxel.Altitude, duration);
                    expandedVoxel.VoxelRef.Expand(height, duration);
                }
            }
            m_expandedVoxels.Clear();
        }

        private void ExecuteEatCommand(long tick, EatCmd eatCmd)
        {
            Voxel eater = eatCmd.Eater.VoxelRef;
            if(eater == null || !eater.IsAcquired)
            {
                if(eatCmd.Voxel.VoxelRef != null)
                {
                    eatCmd.Voxel.VoxelRef.Kill();
                }
                return;
            }

            int deltaHealth = eatCmd.EaterDeltaHealth;
            Voxel voxel = eatCmd.Voxel.VoxelRef; //this change may break eat process. Possible that VoxelRef set to null previously
            if(voxel == null)
            {
                Debug.Assert(deltaHealth == 0);
                //if deltaHealth > 0 set texture directly 
                return;
            }

            if(!voxel.IsAcquired)
            {
                Debug.Assert(deltaHealth == 0);
                //if deltaHealth > 0 set texture directly 
                return;
            }

            if (deltaHealth == 0)
            {
                VoxelAbilities abilities = m_dataController.AllAbilities[eater.Owner][eater.Type];
                voxel.Smash(abilities.MovementDuration * GameConstants.MatchEngineTick / 3, eatCmd.VoxelHealth);
            }
            else if (deltaHealth == 1)
            {
                if(voxel.Type == (int)KnownVoxelTypes.Eater)
                {
                    SplitAndEat(tick, eater, deltaHealth, voxel);
                }
                else
                {
                    eater.BeginEat(voxel, tick);
                }
                
            }
            else if (deltaHealth > 1 && deltaHealth <= 8)
            {
                SplitAndEat(tick, eater, deltaHealth, voxel);
            }
            else
            {
                throw new NotImplementedException("deltaHealth " + deltaHealth);
            }
        }

        private void SplitAndEat(long tick, Voxel eater, int deltaHealth, Voxel voxel)
        {
            Debug.Assert(voxel.Weight > 0);

            int weight = voxel.Weight;
            int owner = voxel.Owner;
            Vector3 position = voxel.transform.position;

            m_voxelFactory.Release(voxel); 

            for (int i = 0; i < deltaHealth; i++)
            {
                Voxel part = m_voxelFactory.Acquire((int)KnownVoxelTypes.Eatable);

                VoxelAbilities abilities = m_dataController.AllAbilities[m_dataController.PlayerIndex][part.Type];

                part.Owner = owner;
                part.Weight = weight - 1;
                part.Height = abilities.EvaluateHeight(part.Weight);

                float voxelSideSize = Mathf.Pow(2, weight) * GameConstants.UnitSize;

                Vector3 partPostion = position;

                int imod4 = i % 4;
                int imod2 = i % 2;
                float vssDiv4 = voxelSideSize / 4.0f;
                float vssDiv2 = voxelSideSize / 2.0f;
                bool bx = (imod4 == 0 || imod4 == 1);
                bool bz = (imod2 == 1);
                partPostion.x += bx ? -vssDiv4 : vssDiv4;
                partPostion.z += bz ? -vssDiv4 : vssDiv4;
                partPostion.y += i < 4 ? 0 : vssDiv2; // (bx || !bz) && (!bx || bz) ? 0 : vssDiv2;

                part.transform.position = partPostion;

                eater.BeginEat(part, tick);
            }

            
        }
    }
}

