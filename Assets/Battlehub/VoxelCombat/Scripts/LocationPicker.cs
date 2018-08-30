using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface ILocationPicker
    {
        void Pick(VoxelData unit, int targetType, int targetWeight, Action<bool, Coordinate> callback);
    }

    public class LocationPicker : MonoBehaviour, ILocationPicker
    {
        public void Pick(VoxelData unit, int targetType, int targetWeight, Action<bool, Coordinate> callback)
        {
            
        }
    }

}

