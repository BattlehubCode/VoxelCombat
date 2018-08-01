namespace Battlehub.VoxelCombat
{
    public static class GameConstants
    {
        public const float UnitSize = 0.5f;

        public const int VoxelCameraRadius = 16;// 2; //= 6;
        public const int VoxelCameraWeight = 4;
        


        public const int MinVoxelActorWeight = VoxelCameraWeight - 2;
        public const int MaxVoxelActorWeight = 4;

        public const int MaxPlayersIncludingNeutral = 9;
        public const int MaxPlayers = 8;
        public const int MaxLocalPlayers = 4;

        /// <summary>
        /// Smallest fraction of time used by MatchEngine measured in milliseconds
        /// </summary>
        public const float MatchEngineTick = 50.0f / 1000.0f; //seconds
        public const long PingTimeout = 10;
        // public const long PingTimeout = 8; //8 * MatchEngineTick milliseconds
        public const int PathFinderBatchSize = 200; 
        public const int TaskRunnerBatchSize = 200;
        public const int TaskEngineBatchSize = 200;
        public const int TaskEngineClientTimeout = 1200; //rougly equal to 1 minute;

        public const float BotHardThinkInterval = 0.15f;
        public const float BotMedumThinkInterval = 0.15f;// BotHardThinkInterval * 2;
        public const float BotEasyThinkInterval = 0.15f;//BotMedumThinkInterval * 2;
        public const float BotNeutralThinkInterval = 5.0f;

        //Layers
        public const int VoxelActorsLayerMask = 1 << 8;

        public const int BottomLayer = 26;
        public const int BottomLayerMask = 1 << BottomLayer;

        public const int Player0Layer = 28;
        public const int Player1Layer = 29;
        public const int Player2Layer = 30;
        public const int Player3Layer = 31;

        public static readonly int[] PlayerLayers = new int[]
        {
            Player0Layer,
            Player1Layer,
            Player2Layer,
            Player3Layer,
        };

        public const int Player0LayerMask = 1 << Player0Layer;
        public const int Player1LayerMask = 1 << Player1Layer;
        public const int Player2LayerMask = 1 << Player2Layer;
        public const int Player3LayerMask = 1 << Player3Layer;

        public static readonly int[] PlayerLayerMasks = new int[]
        {
            Player0LayerMask,
            Player1LayerMask,
            Player2LayerMask,
            Player3LayerMask,
        };

        public const int ExplodableWeightDelta = 1;

#if !SERVER
        public static readonly int[] GLViewports = new int[]
        {
            (int)RTLayer.Viewport0,
            (int)RTLayer.Viewport1,
            (int)RTLayer.Viewport2,
            (int)RTLayer.Viewport3,
        };
#endif

        public const int DefaultTestInitPlayersCount = 1;
        public const int DefaultTestInitBotsCount = 1;
    }

}

