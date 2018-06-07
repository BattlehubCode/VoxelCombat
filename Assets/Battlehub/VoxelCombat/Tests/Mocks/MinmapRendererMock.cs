using System;
using UnityEngine;

namespace Battlehub.VoxelCombat.Tests
{
    public class MinmapRendererMock : MonoBehaviour, IVoxelMinimapRenderer
    {
        public Texture2D Background
        {
            get { return null; }
        }

        public Texture2DArray FogOfWar
        {
           get { return null; }
        }

        public Texture2D Foreground
        {
            get { return null; }
        }

        public bool IsLoaded
        {
            get { return true; }
        }

        public event EventHandler Loaded;
        public void RaiseLoaded()
        {
            if(Loaded != null)
            {
                Loaded(this, EventArgs.Empty);
            }   
        }

        public void BeginUpdate()
        {
        }

        public void Die(VoxelData data, Coordinate coord)
        {
        }

        public void EndUpdate()
        {
        }

        public void IgnoreCell(int playerId, MapPos pos, int weight)
        {
        }

        public void Move(VoxelData data, Coordinate from, Coordinate to)
        {
        }

        public void ObserveCell(int playerId, MapPos pos, int weight)
        {
        }

        public void Spawn(VoxelData data, Coordinate coord)
        {
        }
    }
}