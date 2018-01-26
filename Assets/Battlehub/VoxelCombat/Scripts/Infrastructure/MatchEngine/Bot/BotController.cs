using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public interface IMatchView
    {
        int PlayersCount
        {
            get;
        }

        MapRoot Map
        {
            get;
        }

        IMatchPlayerView GetPlayerView(int index);
        IMatchPlayerView GetPlayerView(Guid guid);

        bool IsSuitableCmdFor(Guid playerId, long unitIndex, int cmdCode);
        void Submit(Guid playerId, Cmd cmd);
    }

    public delegate void MatchPlayerEventHandler<T>(T arg);

    public interface IMatchPlayerView
    {
        event MatchPlayerEventHandler<IMatchUnitAssetView> UnitCreated;
        event MatchPlayerEventHandler<IMatchUnitAssetView> UnitRemoved;
        event MatchPlayerEventHandler<IMatchUnitAssetView> AssetCreated;
        event MatchPlayerEventHandler<IMatchUnitAssetView> AssetRemoved;

        int Index
        {
            get;
        }

        int UnitsCount
        {
            get;
        }

        int ControllableUnitsCount
        {
            get;
        }

        int AssetsCount
        {
            get;
        }

        IMatchUnitAssetView[] Units
        {
            get;
        }

  
        System.Collections.IEnumerable Assets
        {
            get;
        }

        IMatchUnitAssetView GetUnit(long id);
        IMatchUnitAssetView GetAsset(long id);
        IMatchUnitAssetView GetUnitOrAsset(long id);
    }

    public interface IMatchUnitAssetView
    {

        long Id
        {
            get;
        }

        VoxelData Data
        {
            get;
        }

        IVoxelDataController DataController
        {
            get;
        }

        MapPos Position
        {
            get;
        }

        bool IsDead
        {
            get;
        }
    }


    public interface IBotController
    {
        void Update(float time);
    }


    public class BotController : IBotController
    {
        private IMatchView m_matchView;

        private Player m_player;
        private IPathFinder m_pathFinder;
        private ITaskRunner m_taskRunner;

        private float m_thinkInterval;
        private float m_timeToThink;

        private IBotStrategy m_strategy;

        public BotController(Player player, IMatchView matchView, IPathFinder pathFinder, ITaskRunner taskRunner)
        {

            if (!player.IsBot)
            {
                throw new ArgumentException("player is not bot");
            }

            m_player = player;
            m_matchView = matchView;
            m_pathFinder = pathFinder;
            m_taskRunner = taskRunner;

            switch (player.BotType)
            {
                case BotType.Hard:
                    m_thinkInterval = GameConstants.BotHardThinkInterval;
                    break;
                case BotType.Medium:
                    m_thinkInterval = GameConstants.BotMedumThinkInterval;
                    break;
                case BotType.Easy:
                    m_thinkInterval = GameConstants.BotEasyThinkInterval;
                    break;
                case BotType.Neutral:
                    m_thinkInterval = GameConstants.BotNeutralThinkInterval;
                    break;
                default:
                    throw new NotImplementedException();
            }

            m_strategy = new DefaultBotStrategy(m_player, m_matchView, m_pathFinder, m_taskRunner);
        }

        public void Update(float time)
        {
            if (m_timeToThink <= time)
            {
                CompositeCmd cmd = m_strategy.Think();

                if (cmd != null && cmd.Commands != null && cmd.Commands.Length > 0)
                {
                    m_matchView.Submit(m_player.Id, cmd);
                }
                m_timeToThink = time + m_thinkInterval;
            }
        }
    }
  
}

