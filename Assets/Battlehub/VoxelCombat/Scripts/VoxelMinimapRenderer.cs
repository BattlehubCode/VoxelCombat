using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{

    public interface IVoxelMinimapRenderer
    {
        event EventHandler Loaded;

        Texture2D Background
        {
            get;
        }
    }

    //This class writes minimap to single Texture2D, then this texture rendered using RawImage in all viewports
    //White ground blocks could not be destroyed or created by design, so pre-render them. 
    //All other objects should be rebndered to second "overlay" texture.
    //All objects should notify VoxelMinimapRenderer about changes of their state? (created, moved, destoroyed)
    public class VoxelMinimapRenderer : MonoBehaviour, IVoxelMinimapRenderer
    {
        public event EventHandler Loaded;

        private Texture2D m_bgTexture;
        private IVoxelMap m_voxelMap;

        public Texture2D Background
        {
            get { return m_bgTexture; }
        }

        private void Awake()
        {
            m_voxelMap = Dependencies.Map;
            m_voxelMap.Loaded += OnMapLoaded;
            if (m_voxelMap.IsLoaded)
            {
                CreateTextures();
            }

        }

        private void OnDestroy()
        {
            if(m_voxelMap != null)
            {
                m_voxelMap.Loaded -= OnMapLoaded;
            }
        }

        private void OnMapLoaded(object sender, System.EventArgs e)
        {
            CreateTextures();

            if(Loaded != null)
            {
                Loaded(this, EventArgs.Empty);
            }
        }

        private void CreateTextures()
        {
            MapRect bounds = m_voxelMap.MapBounds;
            
            int size = Mathf.Max(bounds.RowsCount, bounds.ColsCount);
            m_bgTexture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            m_bgTexture.filterMode = FilterMode.Point;

            for(int r = 0; r < size; ++r)
            {
                for(int c = 0; c < size; ++c)
                {
                    MapCell cell = m_voxelMap.Map.Get(bounds.Row + r, bounds.Col + c, GameConstants.MinVoxelActorWeight);
                    if(cell.VoxelData != null)
                    {
                       m_bgTexture.SetPixel(c,r, Color.white);
                    }
                    else
                    {
                       Color bgColor = Camera.main.backgroundColor;
                       bgColor.a = 1;
                       m_bgTexture.SetPixel(c, r, bgColor);
                    }
                }
            }
            m_bgTexture.Apply();
        }   
    }
}
