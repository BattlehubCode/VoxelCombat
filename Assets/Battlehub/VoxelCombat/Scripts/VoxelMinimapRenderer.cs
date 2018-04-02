using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{

    public interface IVoxelMinimapRenderer
    {
        event EventHandler Loaded;

        Texture2D Foreground
        {
            get;
        }

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
        private Texture2D m_fgTexture;
        private IVoxelMap m_voxelMap;

        public Texture2D Foreground
        {
            get { return m_fgTexture; }
        }

        public Texture2D Background
        {
            get { return m_bgTexture; }
        }

        private IMaterialsCache m_materialCache;
        private Color m_skyColor;
        private Color m_groundBaseColor;
        private int m_staticMapHeight;

        private void Awake()
        {
            m_materialCache = Dependencies.MaterialsCache;
            m_voxelMap = Dependencies.Map;
            m_voxelMap.Loaded += OnMapLoaded;
            if (m_voxelMap.IsLoaded)
            {
                CreateTextures();
            }

            m_skyColor = Camera.main.backgroundColor;
            m_groundBaseColor = Color.white;
        }

        private void OnDestroy()
        {
            if(m_voxelMap != null)
            {
                m_voxelMap.Loaded -= OnMapLoaded;
            }

            if(m_bgTexture != null)
            {
                Destroy(m_bgTexture);
            }

            if(m_fgTexture != null)
            {
                Destroy(m_fgTexture);
            }
            
        }

        private void OnMapLoaded(object sender, EventArgs e)
        {
            CreateTextures();

            if(Loaded != null)
            {
                Loaded(this, EventArgs.Empty);
            }
        }

        private void CreateTextures()
        {
            CalculateStaticMapHeight();

            MapRect bounds = m_voxelMap.MapBounds;
            int size = m_voxelMap.Map.GetMapSizeWith(0);
            m_bgTexture = CreateTexture(size, m_skyColor);
            m_fgTexture = CreateTexture(size, new Color(1, 1, 1, 0));

            Draw(m_voxelMap.Map.Root, new Coordinate(0, 0, 0, m_voxelMap.Map.Weight), bounds, m_bgTexture, cell => cell.VoxelData.GetLastStatic(), data => m_groundBaseColor);
            Draw(m_voxelMap.Map.Root, new Coordinate(0, 0, 0, m_voxelMap.Map.Weight), bounds, m_fgTexture, cell =>
            {
                VoxelData last = cell.VoxelData.GetLast();
                if(last != null && !VoxelData.IsStatic(last.Type))
                {
                    return last;
                }
                return null;
            },
            data => m_materialCache.GetPrimaryColor(data.Owner));

            m_bgTexture.Apply();
            m_fgTexture.Apply();
        }

        private Texture2D CreateTexture(int size, Color fill)
        {
            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, true);
            result.filterMode = FilterMode.Bilinear;
            for (int i = 0; i < result.width; ++i)
            {
                for (int j = 0; j < result.height; ++j)
                {
                    result.SetPixel(i, j, fill);
                }
            }
            return result;
        }

        private void CalculateStaticMapHeight()
        {
            m_voxelMap.Map.Root.ForEach(cell =>
            {
                if (cell.VoxelData != null)
                {
                    VoxelData lastStatic = cell.VoxelData.GetLastStatic();
                    if (lastStatic != null)
                    {
                        if (lastStatic.Height + lastStatic.Altitude > m_staticMapHeight)
                        {
                            m_staticMapHeight = lastStatic.Height + lastStatic.Altitude;
                        }
                    }
                }
            });
        }

        private void Draw(MapCell cell, Coordinate coord, MapRect bounds, Texture2D texture, Func<MapCell, VoxelData> voxelDataSelector, Func<VoxelData, Color> colorSelector)
        {
            //Coordinate boundsWeightCoord = coord.ToWeight(GameConstants.MinVoxelActorWeight);
            VoxelData data = null;
            if (cell.VoxelData != null)
            {
                data = voxelDataSelector(cell);
            }

            if (data != null)
            {
                Coordinate zeroWeightCoord = coord.ToWeight(0);
                int size = 1 << coord.Weight;
                float height = data.Altitude + data.Height;
                float deltaColor = (1.0f - (height / m_staticMapHeight)) * 0.1f;
                Color color = colorSelector(data) - new Color(deltaColor, deltaColor, deltaColor, 0);
                for (int x = 0; x < size; ++x)
                {
                    for (int y = 0; y < size; ++y)
                    {
                        texture.SetPixel(zeroWeightCoord.Col + y, zeroWeightCoord.Row + x, color);
                    }
                }
            }

            if (cell.Children != null && cell.Children.Length > 0)
            {
                coord.Weight--;
                coord.Col *= 2;
                coord.Row *= 2;
                for (int i = 0; i < 4; i++)
                {
                    Coordinate childCoord = coord;
                    childCoord.Row += i / 2;
                    childCoord.Col += i % 2;

                    Draw(cell.Children[i], childCoord, bounds, texture, voxelDataSelector, colorSelector);
                }
            }

        }

        /*
        private void DrawGround(MapCell cell, Coordinate coord, MapRect bounds)
        {
            //Coordinate boundsWeightCoord = coord.ToWeight(GameConstants.MinVoxelActorWeight);
            //if(!bounds.Contains(boundsWeightCoord.MapPos))
            //{
            //    return;
            //}

            VoxelData lastStatic = null;
            if(cell.VoxelData != null)
            {
                lastStatic = cell.VoxelData.GetLastStatic();
            }

            if(lastStatic != null)
            {
                Coordinate zeroWeightCoord = coord.ToWeight(0);
                int size = 1 << coord.Weight;
                float height = lastStatic.Altitude + lastStatic.Height;
                float deltaColor = (1.0f - (height / m_staticMapHeight)) * 0.1f;
                Color color = m_groundBaseColor - new Color(deltaColor, deltaColor, deltaColor, 0);
                for (int x = 0; x < size; ++x)
                {
                    for (int y = 0; y < size; ++y)
                    {    
                        m_bgTexture.SetPixel(zeroWeightCoord.Col + y, zeroWeightCoord.Row + x, color);
                    }
                }
            }

            if (cell.Children != null && cell.Children.Length > 0)
            {
                coord.Weight--;
                coord.Col *= 2;
                coord.Row *= 2;
                for (int i = 0; i < 4; i++)
                {
                    Coordinate childCoord = coord;
                    childCoord.Row += i / 2;
                    childCoord.Col += i % 2;

                    DrawGround(cell.Children[i], childCoord, bounds);
                }
            }
        }*/
    }
}
