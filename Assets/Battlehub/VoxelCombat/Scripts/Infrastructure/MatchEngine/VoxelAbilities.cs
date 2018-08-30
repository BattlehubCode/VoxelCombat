using ProtoBuf;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    [ProtoContract]
    public class VoxelAbilities
    {
        [ProtoMember(1)]
        public int Type;

        [ProtoMember(10)]
        public bool CanMove;

        //[ProtoMember(11)]
        //public bool CanFly;
        //[ProtoMember(12)]
        //public bool CanSwim;

        [ProtoMember(20)]
        public float MaxJumpHeight; //fractions of weight
        [ProtoMember(21)]
        public float MaxFallHeight; //fractions of height;
        [ProtoMember(22)]
        public int JumpDuration; //How much ticks jump for 4 units should take 

        //Moved to gameConstants
        //  [ProtoMember(23)]
        //  public int FallDuration; //How much ticks fall for 4 units should take

        [ProtoMember(30)]
        public int MinMoveDistance;
        [ProtoMember(31)]
        public int MaxMoveDistance;
        [ProtoMember(32)]
        public int MovementDuration; //How much ticks voxel movement between 2 coordinates should take
        [ProtoMember(33)]
        public int RotationDuration; //How much tick rotation should take
        [ProtoMember(34)]
        public int SplitDuration;
        [ProtoMember(35)]
        public int GrowDuration;
        [ProtoMember(36)]
        public int DiminishDuration;
        [ProtoMember(37)]
        public int ConvertDuration;
        [ProtoMember(38)]
        public int ActionInterval;
        [ProtoMember(39)]
        public int TargetCheckInterval;
        [ProtoMember(40)]
        public int SplitDelay;
        [ProtoMember(41)]
        public int GrowDelay;
        [ProtoMember(42)]
        public int DiminishDelay;
        [ProtoMember(43)]
        public int ConvertDelay;

        [ProtoMember(70)]
        public int MinWeight;
        [ProtoMember(71)]
        public int MaxWeight;

        [ProtoMember(80)]
        public int MinHealth; //MinHelath to stay alive;
        [ProtoMember(81)]
        public int MaxHealth; //Max health
        [ProtoMember(82)]
        public int DefaultHealth; //Health given to unit by default


        [ProtoMember(90)]
        public bool VariableHeight; //If true then height does not depened on Voxel Weight
        [ProtoMember(91)]
        public int MinHeight;
        [ProtoMember(92)]
        public int MaxHeight;
        [ProtoMember(93)]
        public float HeightMultiplier; //Used if variable height = false

        [ProtoMember(100)]
        public int VisionRadius;

        public int EvaluateHeight(int weight, bool clamp = true)
        {
            int height = (int)(Mathf.Pow(2, weight) * HeightMultiplier);
            if(clamp)
            {
                height = ClampHeight(height);
            }
            return height;
        }

        public int ClampHeight(int height)
        {
            if(height > MaxHeight)
            {
                Debug.LogError("height > MaxHeight");
                height = MaxHeight;
            }

            if(height < MinHeight)
            {
                Debug.LogError("height < MaxHeight");
                height = MinHeight;
            }
            return height;
        }

  
        public VoxelAbilities()
        {

        }

        public VoxelAbilities(VoxelAbilities abilities)
        {
            Type = abilities.Type;

            CanMove = abilities.CanMove;

            MaxJumpHeight = abilities.MaxJumpHeight;
            MaxFallHeight = abilities.MaxFallHeight;
            JumpDuration = abilities.JumpDuration;
            //FallDuration = abilities.FallDuration;

            MinMoveDistance = abilities.MinMoveDistance;
            MaxMoveDistance = abilities.MaxMoveDistance;
            MovementDuration = abilities.MovementDuration;
            RotationDuration = abilities.RotationDuration;
            SplitDuration = abilities.SplitDuration;
            GrowDuration = abilities.GrowDuration;
            DiminishDuration = abilities.DiminishDuration;
            ActionInterval = abilities.ActionInterval;
            TargetCheckInterval = abilities.TargetCheckInterval;

            SplitDelay = abilities.SplitDelay;
            GrowDelay = abilities.GrowDelay;
            DiminishDelay = abilities.DiminishDelay;
            ConvertDelay = abilities.ConvertDelay;

            MinWeight = abilities.MinWeight;
            MaxWeight = abilities.MaxWeight;

            MinHealth = abilities.MinHealth;
            MaxHealth = abilities.MaxHealth;
            DefaultHealth = abilities.DefaultHealth;

            MinHeight = abilities.MinHeight;
            MaxHeight = abilities.MaxHeight;
            VariableHeight = abilities.VariableHeight;

            VisionRadius = abilities.VisionRadius;
        }

        public VoxelAbilities(int type)
        {
            Type = type;
            switch (type)
            {
                case (int)KnownVoxelTypes.Eater:
                    CanMove = true;
                    MaxJumpHeight = 1.0f;
                    MaxFallHeight = -100.0f;
                    MinMoveDistance = 1;
                    MaxMoveDistance = 1;

                    MovementDuration = 10;
                    RotationDuration = 10;
                    SplitDuration = 10;
                    GrowDuration = 10;
                    DiminishDuration = 10;
                    ConvertDuration = 10;
                    TargetCheckInterval = 10;

                    SplitDelay = 50;
                    GrowDelay = 50;
                    DiminishDelay = 50;
                    ConvertDelay = 200;

                    MinWeight = 2;
                    MaxWeight = 4;

                    MaxHealth = 64;
                    MinHealth = 1;
                    DefaultHealth = 8;

                    HeightMultiplier = 1.0f;
                    VariableHeight = false;

                    MaxHeight = EvaluateHeight(MaxWeight, false);
                    MinHeight = EvaluateHeight(MinWeight, false);

                    VisionRadius = 2;

                    break;
                case (int)KnownVoxelTypes.Bomb:
                    CanMove = true;
                    MaxJumpHeight = 1.0f;
                    MaxFallHeight = -100.0f;
                    MinMoveDistance = 1;
                    MaxMoveDistance = 1;

                    MovementDuration = 10;
                    RotationDuration = 10;
                    TargetCheckInterval = 10;

                    MinWeight = 2;
                    MaxWeight = 4;

                    MaxHealth = 64;
                    MinHealth = 64;
                    DefaultHealth = 64;

                    HeightMultiplier = 1.0f;
                    VariableHeight = false;

                    MaxHeight = EvaluateHeight(MaxWeight, false);
                    MinHeight = EvaluateHeight(MinWeight, false);

                    VisionRadius = 2;

                    break;
                case (int)KnownVoxelTypes.Ground:
                    CanMove = false;

                    VariableHeight = true;
                    MinHeight = 1;
                    MaxHeight = 1024;
                    MinWeight = 0;
                    MaxWeight = 6;
                    HeightMultiplier = 1.0f;

                    MinHealth = 1;
                    DefaultHealth = 3;
                    MaxHealth = 3;

                    VisionRadius = 2;

                    //TBD
                    break;
                case (int)KnownVoxelTypes.Eatable:
                    CanMove = false;

                    MaxHealth = 1;
                    MinHealth = 1;
                    DefaultHealth = 1;
                    HeightMultiplier = 1.0f;
                    VariableHeight = false;
                    MinWeight = 0;
                    MaxWeight = 3;
                    MaxHeight = EvaluateHeight(MaxWeight, false);
                    MinHeight = EvaluateHeight(MinWeight, false);

                    VisionRadius = 2;
                    //TBD
                    break;
                case (int)KnownVoxelTypes.Spawner:
                    CanMove = false;
                    MaxHealth = 1;
                    MinHealth = 1;
                    DefaultHealth = 1;

                    MinWeight = 2;
                    MaxWeight = 4;

                    HeightMultiplier = 0.25f;
                    VariableHeight = false;
                    MaxHeight = EvaluateHeight(MaxWeight, false);
                    MinHeight = EvaluateHeight(MinWeight, false);


                    ActionInterval = 30;

                    VisionRadius = 2;

                    break;
            }
        }
    }
}
