using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{

#warning PathFinder should accept area size as argument 
    public interface IPathFinder
    {
        bool IsRunning(long unitId);

        void Find(long unitId, long targetId, IVoxelDataController dataController, Coordinate[] waypoints, Action<long, Coordinate[]> callback, Action<long> terminateCallback);

        void FindEmptySpace(long unitId, IVoxelDataController dataController, int radius, Action<long, Coordinate[]> callback, Action<long> terminateCallback);

        void Terminate(long unitId);

        void Tick();

        void Update();

        void Destroy();
    }

    public class PathFinderTask
    { 
        private Action<long, Coordinate[]> m_callback;
        private Action<long> m_terminateCallback;
        private Coordinate[] m_result;
        private Coordinate[] m_waypoints;

        public class Data
        {
            public Coordinate Coordinate
            {
                get;
                private set;
            }

            public int Hops
            {
                get;
                private set;
            }

            public Data(Coordinate coordinate, int hops)
            {
                Coordinate = coordinate;
                Hops = hops;
            }
        }

        private readonly Queue<Data> m_dataQueue;
        public Queue<Data> DataQueue
        {
            get { return m_dataQueue; }
        }
        
        private int[,] m_hopsMatrix;
        public int[,] HopsMatrix
        {
            get { return m_hopsMatrix; }
        }

        public Coordinate ClosestToGoal
        {
            get;
            set;
        }

        private PathMatrixPool m_matrixPool;
        
        private long m_unitId;
        public long UnitId
        {
            get { return m_unitId; }
        }

        private int m_playerIndex;
        public int PlayerIndex
        {
            get { return m_playerIndex; }
        }

        private VoxelData m_controlledData;
        public VoxelData ControlledData
        {
            get { return m_controlledData; }
        }

        private Coordinate m_coordinate;
        public Coordinate Coordinate
        {
            get { return m_coordinate; }
        }

        private VoxelAbilities m_abilities;
        public VoxelAbilities Abilities
        {
            get { return m_abilities; }
        }

        public MapRoot Map
        {
            get;
            private set;
        }

        public int MapSize
        {
            get;
            private set;
        }


        private bool m_isTerminated;
        public bool IsTerminated
        {
            get { return m_isTerminated; }
        }

        public bool IsCompleted
        {
            get { return m_result != null; }
        }

        public Coordinate[] Waypoints
        {
            get { return m_waypoints; }
        }

        private long m_targetId;
        public bool HasTarget
        {
            get { return m_targetId > -1; }
        }

        public long TargetId
        {
            get { return m_targetId; }
        }

        public PathFinderTask(int playerIndex, long unitId, long targetId, MapRoot map, int mapSize, VoxelData controlledData, Coordinate coordinate, VoxelAbilities ablilities, Coordinate[] waypoints, PathMatrixPool[] pools, Action<long, Coordinate[]> callback, Action<long> terminateCallback)
        {
            m_waypoints = waypoints;
            m_callback = callback;
            m_terminateCallback = terminateCallback;

            m_playerIndex = playerIndex;
            m_unitId = unitId;
            m_targetId = targetId;

            Map = map;
            MapSize = mapSize;
            m_controlledData = new VoxelData(controlledData);
            m_controlledData.Unit.State = VoxelDataState.Idle;
            m_coordinate = coordinate;
            m_abilities = new VoxelAbilities(ablilities);

            m_matrixPool = pools[m_controlledData.Weight - ablilities.MinWeight];
            m_hopsMatrix =  m_matrixPool.Acquire();
            m_hopsMatrix[m_coordinate.Row, m_coordinate.Col] = 0;

            ClosestToGoal = m_coordinate;
            m_dataQueue = new Queue<Data>();
            m_dataQueue.Enqueue(new Data(m_coordinate, 0));
        }

        public void SetCompleted(Coordinate[] result)
        {
            m_result = result;

            m_dataQueue.Clear();

            if (m_hopsMatrix != null)
            {
                m_matrixPool.Release(m_hopsMatrix);
                m_hopsMatrix = null;
            }
        }

        public void Terminate()
        {
            m_isTerminated = true;

            m_dataQueue.Clear();

            if (m_hopsMatrix != null)
            {
                m_matrixPool.Release(m_hopsMatrix);
                m_hopsMatrix = null;
            }

            if(m_terminateCallback != null)
            {
                m_terminateCallback(m_unitId);
            }
        }

        public bool CallbackIfCompleted()
        {
            if (m_isTerminated)
            {
                return true;
            }

            if (m_result != null)
            {    
                if (m_callback != null)
                {
                    m_callback(m_unitId, m_result);
                }
                return true;
            }
            return false;
        }
        
    }

    public class PathMatrixPool : Pool<int[,]>
    {
        private int m_matrixSize;

        public PathMatrixPool(int size, int matrixSize)
        {
            m_matrixSize = matrixSize;
            Initialize(size);
        }

        protected override void Destroy(int[,] matrix)
        {
        }

        protected override int[,] Instantiate(int index)
        {
            return new int[m_matrixSize, m_matrixSize];
        }

        public override int[,] Acquire()
        {
            int[,] matrix = base.Acquire();

            for(int i = 0; i < m_matrixSize; ++i)
            {
                for(int j = 0; j < m_matrixSize; ++j)
                {
                    matrix[i, j] = int.MaxValue;
                }
            }

            return matrix;
        }
    }

    public class PathFinder2 : IPathFinder
    {
        private Dictionary<long, PathFinderTask> m_idToActiveTask;
        private readonly List<PathFinderTask> m_activeTasks = new List<PathFinderTask>();

        private readonly PathMatrixPool[] m_matrixPools;

        private System.Random m_rand;

        public PathFinder2(MapRoot map)
        {
            m_idToActiveTask = new Dictionary<long, PathFinderTask>();
            m_rand = new System.Random(Guid.NewGuid().GetHashCode());
            m_matrixPools = new PathMatrixPool[3];
            for (int i = 0; i < m_matrixPools.Length; ++i)
            {
                m_matrixPools[i] = new PathMatrixPool(100, map.GetMapSizeWith(GameConstants.MinVoxelActorWeight + i));
            }
        }

        public void Destroy()
        {
            for(int i = 0; i < m_activeTasks.Count; ++i)
            {
                m_activeTasks[i].Terminate();
            }

            m_idToActiveTask = null;
            m_activeTasks.Clear();
        }

        public bool IsRunning(long unitId)
        {
            return m_idToActiveTask.ContainsKey(unitId);
        }


        private int[,] m_offsets = { /*{ -1, -1 },*/  { 0,-1 }, /* { 1, -1 },*/
                                     { -1,  0 },/*{ 0, 0 },*/ { 1,  0 },
                                     /*{ -1,  1 },*/  { 0, 1 },  /* { 1,  1 }*/};
        private int[] m_offsetIndices = { 0, 1, 2, 3 };//, 4, 5, 6, 7 };
        public void FindEmptySpace(long unitId, IVoxelDataController dataController, int radius, Action<long, Coordinate[]> callback, Action<long> terminateCallback)
        {
            int[] offsetIndices = m_offsetIndices.OrderBy(x => m_rand.Next()).ToArray();
            for (int r = 1; r <= radius; ++r)
            {
                for (int o = 0; o < offsetIndices.Length; ++o)
                {
                    int offsetIndex = offsetIndices[o];

                    int deltaRow = m_offsets[offsetIndex, 0] * r;
                    int deltaCol = m_offsets[offsetIndex, 1] * r;

                    Coordinate coordinate = dataController.Coordinate;
                    coordinate = coordinate.Add(deltaRow, deltaCol);
                    if (dataController.IsValidAndEmpty(coordinate, false))
                    {
                        Find(unitId, -1, dataController, new[] { dataController.Coordinate, coordinate }, callback, terminateCallback);
                        return;
                    }
                }
            }

            if (callback != null)
            {
                //fail
                callback(unitId, new[] { dataController.Coordinate });
            }
        }

        public void Find(long unitId, long targetId, IVoxelDataController dataController,
              Coordinate[] waypoints, Action<long, Coordinate[]> callback, Action<long> terminateCallback)
        {
            if (waypoints == null)
            {
                throw new ArgumentNullException("waypoints");
            }
            PathFinderTask task;
            if (m_idToActiveTask.TryGetValue(unitId, out task))
            {
                task.Terminate();
                m_activeTasks.Remove(task);
            }

            task = new PathFinderTask(dataController.PlayerIndex, unitId, targetId, dataController.Map, dataController.MapSize, dataController.ControlledData, waypoints[0] /*dataController.Coordinate*/, dataController.Abilities, waypoints, m_matrixPools, callback, terminateCallback);
            m_idToActiveTask[unitId] = task;
            m_activeTasks.Add(task);
        }

        public void Terminate(long unitId)
        {
            PathFinderTask task;
            if (m_idToActiveTask.TryGetValue(unitId, out task))
            {
                task.Terminate();
                m_activeTasks.Remove(task);
                m_idToActiveTask.Remove(unitId);
            }
        }

        public void Tick() //This method should be called by MatchEngine
        {
            for (int i = m_activeTasks.Count - 1; i >= 0; --i)
            {
                PathFinderTask task = m_activeTasks[i];
                Debug.Assert(!task.IsTerminated);
                if (task.CallbackIfCompleted())
                {
                    if (!task.IsTerminated && m_activeTasks[i] == task)
                    {
                        m_activeTasks.RemoveAt(i);
                        m_idToActiveTask.Remove(task.UnitId);
                    }
                }
            }
        }

        public void Update()
        {
            int maxIterationsPerFrame = 100;
            
            while(m_activeTasks.Count > 0)
            {
                int completedTasksCount = 0;
                for (int i = 0; i < m_activeTasks.Count; ++i)
                {
                    PathFinderTask task = m_activeTasks[i];
                    if (task.IsCompleted)
                    {
                        completedTasksCount++;
                        if(m_activeTasks.Count == completedTasksCount)
                        {
                            return;
                        }

                        continue;
                    }

                    if (maxIterationsPerFrame == 0)
                    {
                        return;
                    }

                    maxIterationsPerFrame--;

                    Coordinate goal = task.Waypoints[task.Waypoints.Length - 1];
                    int[,] hopsMatrix = task.HopsMatrix;
                    int size = hopsMatrix.GetLength(0);

                    if (task.DataQueue.Count > 0)
                    {
                        PathFinderTask.Data current = task.DataQueue.Dequeue();
                        if (current.Coordinate.MapPos == goal.MapPos)
                        {
                            //path found
                            CompleteTask(task, hopsMatrix, size);
                        }
                        else
                        {
                            //searching for path
                            
                            for (int r = -1; r <= 1; r++)
                            {
                                for (int c = -1; c <= 1; c++)
                                {
                                    int s = r + c;
                                    if (s != -1 && s != 1)
                                    {
                                        continue;
                                    }

                                    int nextHops = current.Hops + 1;
                                    Coordinate next = current.Coordinate.Add(r, c);

                                    MapPos pos = next.MapPos;
                                    if (pos.Col < 0 || pos.Row < 0 || pos.Col >= size || pos.Row >= size)
                                    {
                                        continue;
                                    }

                                    if (hopsMatrix[pos.Row, pos.Col] <= nextHops)
                                    {
                                        continue;
                                    }

                                    VoxelData targetData;
                                    if (TryToMove(task.HasTarget, task, current.Coordinate, next, out next, out targetData))
                                    {
                                        hopsMatrix[pos.Row, pos.Col] = nextHops;

                                        if(targetData != null)
                                        {
                                            task.ClosestToGoal = next;
                                            task.Waypoints[task.Waypoints.Length - 1] = next;
                                            task.DataQueue.Clear();

                                            c = 2; //break inner for loop;
                                            r = 2; //break outer for loop
                                        }
                                        else
                                        {
                                            int closestDistance = task.ClosestToGoal.MapPos.SqDistanceTo(goal.MapPos);
                                            int distanceToGoal = pos.SqDistanceTo(goal.MapPos);
                                            if (distanceToGoal < closestDistance)
                                            {
                                                task.ClosestToGoal = next;
                                            }
                                        }
                                        task.DataQueue.Enqueue(new PathFinderTask.Data(next, nextHops));
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // path was not found

                        CompleteTask(task, hopsMatrix, size);
                    }
                }
            }
        }

        private void CompleteTask(PathFinderTask task, int[,] hopsMatrix, int size)
        {
            Coordinate coord = task.ClosestToGoal;
            int row = coord.Row;
            int col = coord.Col;

            List<Coordinate> path = new List<Coordinate>();
            path.Add(coord);

            int minHops = hopsMatrix[row, col];
            int minRow = 0;
            int minCol = 0;
            Coordinate minCoord = new Coordinate();

            while (minHops != 0) 
            {
                bool canMove = false;
                for (int r = -1; r <= 1; r++)
                {
                    for (int c = -1; c <= 1; c++)
                    {
                        int s = r + c;
                        if (s != -1 && s != 1)
                        {
                            continue;
                        }

                        int prevRow = row + r;
                        int prevCol = col + c;

                        if (prevCol < 0 || prevRow < 0 || prevRow >= size || prevCol >= size)
                        {
                            continue;
                        }

                        int hops = hopsMatrix[prevRow, prevCol];
                        if (hops == (minHops - 1))
                        {
                            Coordinate prevCoord = GetCoordinate(task, prevRow, prevCol);
                            Coordinate modifiedCoord;
                            VoxelData notUsed;
                            if(TryToMove(false, task, prevCoord, coord, out modifiedCoord, out notUsed)) //possible errors here (yes you were right... infinite loop if try to move failed?
                            {
                                minHops = hops;
                                minRow = prevRow;
                                minCol = prevCol;
                                minCoord = prevCoord;
                                canMove = true;
                            }
                        }
                    }
                }

                if(!canMove)
                {
                    //if can't move clear path
                    path.Clear();

                    for (int r = -1; r <= 1; r++)
                    {
                        for (int c = -1; c <= 1; c++)
                        {
                            int s = r + c;
                            if (s != -1 && s != 1)
                            {
                                continue;
                            }

                            int prevRow = row + r;
                            int prevCol = col + c;

                            if (prevCol < 0 || prevRow < 0 || prevRow >= size || prevCol >= size)
                            {
                                continue;
                            }

                            int hops = hopsMatrix[prevRow, prevCol];
                            if (hops == (minHops - 1))
                            {
                                //take first appropriate coordinate
                                Coordinate prevCoord = GetCoordinate(task, prevRow, prevCol);
                                minHops = hops;
                                minRow = prevRow;
                                minCol = prevCol;
                                minCoord = prevCoord;
                                canMove = true;
                                break;
                            }
                        }
                    }
                }

                if(canMove)
                {
                    row = minRow;
                    col = minCol;
                    coord = minCoord;

                    path.Add(coord);
                }
                else
                {
                    //if still can't move then something went wrong and infinite loop take place here
                    Debug.LogError("Infinite Loop");
                    break; //we break this loop
                }
            }
            
            path.Reverse();
            task.SetCompleted(path.ToArray());
        }

        private bool TryToMove(bool checkTarget, PathFinderTask task, Coordinate from, Coordinate next, out Coordinate result, out VoxelData resultData)
        {
            resultData = null;

            MapCell cell = task.Map.Get(next.Row, next.Col, next.Weight);
 
            int type = task.ControlledData.Type;
            int weight = task.ControlledData.Weight;
            int height = task.ControlledData.Height;

            MapCell targetCell;
            if(checkTarget)
            {
                VoxelData targetData = cell.GetById(task.TargetId);
                if(targetData != null)
                {
                    next.Altitude = targetData.Altitude;
                    CmdResultCode canMove = VoxelDataController.CanMove(task.ControlledData, task.Abilities, task.Map, task.MapSize, from, next, false, false, false, out targetCell);
                    if (canMove == CmdResultCode.Success)
                    {
                        resultData = targetData;
                        result = next;
                        return true;    
                    }
                }
            }

            //Change altitude if failed with target coordinate
            VoxelData target;
            VoxelData beneath = cell.GetDefaultTargetFor(type, weight, task.PlayerIndex, false, out target);
            if (beneath == null)
            {
                result = from;
                return false;
            }

            // This will allow bomb movement 
            bool isLastStep = task.Waypoints[task.Waypoints.Length - 1].MapPos == next.MapPos;
            if (isLastStep)
            {
                //Try target coordinate first
                next = task.Waypoints[task.Waypoints.Length - 1];

                //last step is param false -> force CanMove to check next coordinate as is
                CmdResultCode canMove = VoxelDataController.CanMove(task.ControlledData, task.Abilities, task.Map, task.MapSize, from, next, false, false, false, out targetCell);
                if (canMove == CmdResultCode.Success)
                {
                    result = next;
                    return true;
                }
            }

            if(target != beneath) //this will allow bombs to move over spawners
            {
                if (!isLastStep && target != null && !(target.IsCollapsableBy(type, weight) || target.IsAttackableBy(task.ControlledData)))
                {
                    result = from;
                    return false;
                }
            }
            
            next.Altitude = beneath.Altitude + beneath.Height;

            //last step param is false -> force CanMove to check next coordinate as is
            CmdResultCode canMoveResult = VoxelDataController.CanMove(task.ControlledData, task.Abilities, task.Map, task.MapSize, from, next, false, false, false, out targetCell);
            if (canMoveResult != CmdResultCode.Success)
            {
                result = from;
                return false;
            }

            result = next;
            return true;
        }

        private Coordinate GetCoordinate(PathFinderTask task, int row, int col)
        {
            int type = task.ControlledData.Type;
            int weight = task.ControlledData.Weight;
            int height = task.ControlledData.Height;

            MapCell cell = task.Map.Get(row, col, weight);

            VoxelData target;
            VoxelData beneath = cell.GetDefaultTargetFor(type, weight, task.PlayerIndex, false, out target);

            if (beneath == null)
            {
                return new Coordinate(row, col, 0, weight);
            }

            int altitude = beneath.Altitude + beneath.Height;
            return new Coordinate(row, col, altitude, weight);
        }
    }

}

