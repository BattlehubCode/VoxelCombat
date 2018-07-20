using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public static class MatchFactoryCli 
    {
        public static ITaskEngine CreateTaskEngine(IMatchView matchEngine, ITaskRunner taskRunner, IPathFinder pathFinder)
        {
            return new TaskEngine(matchEngine, taskRunner, pathFinder, true);
        }

        public static void DestroyTaskEngine(ITaskEngine taskEngine)
        {
            taskEngine.Destroy();
        }

        public static IVoxelDataController CreateVoxelDataController(MapRoot map, Coordinate coordinate, int type,  int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities)
        {
            IVoxelDataController dataController = new VoxelDataController(map, coordinate, type, playerIndex, allAbilities);
            return dataController;
        }

        public static IMatchUnitControllerCli CreateUnitController(MapRoot map, Coordinate coordinate, int type, int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities)
        {
            if(type == (int)KnownVoxelTypes.Eater)
            {
                return new VoxelActorUnitControllerCli(CreateVoxelDataController(map, coordinate, type, playerIndex, allAbilities));
            }
            else if (type == (int)KnownVoxelTypes.Bomb)
            {
                return new BombUnitControllerCli(CreateVoxelDataController(map, coordinate, type, playerIndex,  allAbilities));
            }
            else if(type == (int)KnownVoxelTypes.Spawner)
            {
                return new SpawnerUnitControllerCli(CreateVoxelDataController(map, coordinate, type, playerIndex, allAbilities));
            }
            else
            {
                throw new System.NotSupportedException(string.Format("type {0} is not supported"));
            }
        }

        public static void DestroyUnitController(IMatchUnitControllerCli controller)
        {
            controller.Destroy();
        }

        public static IMatchPlayerControllerCli CreatePlayerController(Transform parent, int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities)
        {
            GameObject go = new GameObject();
            go.transform.SetParent(parent);
            go.name = "MatchPlayerControllerCli" + playerIndex;

            IMatchPlayerControllerCli playerController = go.AddComponent<MatchPlayerControllerCli>();
            playerController.Init(playerIndex, allAbilities);

            return playerController;
        }

        public static void DestroyPlayerController(IMatchPlayerControllerCli controller)
        {
            MatchPlayerControllerCli component = (MatchPlayerControllerCli)controller;
            Object.Destroy(component.gameObject);
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

        public static IBotController CreateBotController(Player player, ITaskEngine taskEngine)
        {
            return new BotController(player, taskEngine);
        }

        public static void DestroyBotController(IBotController botController)
        {
            botController.Destroy();
        }
    }
}
