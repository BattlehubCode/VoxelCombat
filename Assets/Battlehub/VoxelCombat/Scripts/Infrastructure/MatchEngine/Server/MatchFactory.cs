

using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public static class MatchFactory 
    {
        public static ITaskEngine CreateTaskEngine(IMatchView matchEngine, ITaskRunner taskRunner, IPathFinder pathFinder)
        {
            return new TaskEngine(matchEngine, taskRunner, pathFinder, false);
        }

        public static void DestroyTaskEngine(ITaskEngine taskEngine)
        {
            taskEngine.Destroy();
        }

        public static IBotController CreateBotController(Player player, ITaskEngine taskEngine)
        {
            return new BotController(player, taskEngine);
        }

        public static void DestroyBotController(IBotController botController)
        {
            botController.Destroy();
        }

        public static IReplaySystem CreateReplayRecorder()
        {
            return new ReplayRecorder();
        }

        public static IReplaySystem CreateReplayPlayer()
        {
            return new ReplayPlayer();
        }

        public static IMatchEngine CreateMatchEngine(MapRoot map, int playersCount)
        {
            return new MatchEngine(map, playersCount);
        }

        public static void DestroyMatchEngine(IMatchEngine engine)
        {
            engine.Destroy();
        }

        public static ITaskRunner CreateTaskRunner()
        {
            return new TaskRunner();
        }

        public static void DestroyTaskRunner(ITaskRunner taskRunner)
        {
            taskRunner.Destroy();
        }

        public static IPathFinder CreatePathFinder(MapRoot map)
        {
            return new PathFinder2(map);
        }

        public static void DestroyPathFinder(IPathFinder pathFinder)
        {
            pathFinder.Destroy();
        }

        public static IMatchPlayerController CreatePlayerController(IMatchEngine engine, int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities)
        {
            return new MatchPlayerController(engine, playerIndex, allAbilities);
        }

        public static IMatchUnitController CreateUnitController(IMatchEngine engine, Coordinate coordinate, int type, int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities)
        {
            IVoxelDataController dataController = CreateVoxelDataController(engine.Map, coordinate, type, playerIndex, allAbilities);   
            if (type == (int)KnownVoxelTypes.Eater)
            {
                return new VoxelActorUnitController(dataController, engine);
            }
            else if (type == (int)KnownVoxelTypes.Bomb)
            {
                return new VoxelBombUnitController(dataController, engine);
            }
            else if(type == (int)KnownVoxelTypes.Spawner)
            {
                return new SpawnerUnitController(dataController);
            }
            else if((type & (int)KnownVoxelTypes.Preview) != 0)
            {
                return new PreviewUnitController(dataController);
            }
            else
            {
                throw new System.NotSupportedException(string.Format("Type {0} is not supported", type));
            }  
        }

        public static void DestroyUnitController(IMatchUnitController unitController)
        {
            unitController.Destroy();
        }

        public static IVoxelDataController CreateVoxelDataController(MapRoot map, Coordinate coordinate, int type, int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities)
        {
            IVoxelDataController dataController = new VoxelDataController(map, coordinate, type, playerIndex, allAbilities);
            return dataController;
        } 
    }

}
