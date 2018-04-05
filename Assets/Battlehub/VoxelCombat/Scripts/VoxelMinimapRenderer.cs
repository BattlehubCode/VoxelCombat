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

        void BeginUpdate();
        void Move(VoxelData data, Coordinate from, Coordinate to);
        void Spawn(VoxelData data, Coordinate coord);
        void Kill(VoxelData data, Coordinate coord);
        void EndUpdate();
    }

    //This class writes minimap to single Texture2D, then this texture rendered using RawImage in all viewports
    //White ground blocks could not be destroyed or created by design, so pre-render them. 
    //All other objects should be rebndered to second "overlay" texture.
    //All objects should notify VoxelMinimapRenderer about changes of their state? (created, moved, destoroyed)
    public class VoxelMinimapRenderer : MonoBehaviour, IVoxelMinimapRenderer
    {
        public event EventHandler Loaded;
   
        [SerializeField]
        private int m_desiredResolution = 512;

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
        private Color m_transparentColor;
        private int m_staticMapHeight;
        private MapRect m_bounds;
        private int m_scale;
        private bool m_updateRequired;
        private bool m_updating;

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
            m_transparentColor = new Color(0, 0, 0, 0);
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
            m_bounds = m_voxelMap.MapBounds;

            int size = m_voxelMap.Map.GetMapSizeWith(0);
           
            int toZeroWeight = (1 << GameConstants.MinVoxelActorWeight);
            m_bounds.ColsCount *= toZeroWeight;
            m_bounds.RowsCount *= toZeroWeight;
            m_bounds.Col *= toZeroWeight;
            m_bounds.Row *= toZeroWeight;

            float centerRow = m_bounds.Row + m_bounds.RowsCount / 2.0f;
            float centerCol = m_bounds.Col + m_bounds.ColsCount / 2.0f;
            int boundsSize = Mathf.Max(m_bounds.ColsCount, m_bounds.RowsCount);

            Vector2 p0 = new Vector2(Mathf.Max(0, centerRow - boundsSize / 2.0f), Mathf.Max(0, centerCol - boundsSize / 2.0f));
            Vector2 p1 = new Vector2(Mathf.Min(size, centerRow + boundsSize / 2.0f), Mathf.Min(size, centerCol + boundsSize / 2.0f));

            p0.x = Mathf.FloorToInt((p0.x) / toZeroWeight) * toZeroWeight;
            p0.y = Mathf.FloorToInt((p0.y) / toZeroWeight) * toZeroWeight;

            p1.x = Mathf.CeilToInt((p1.x) / toZeroWeight) * toZeroWeight;
            p1.y = Mathf.CeilToInt((p1.y) / toZeroWeight) * toZeroWeight;

            m_bounds.Row = (int)p0.x;
            m_bounds.Col = (int)p0.y;

            m_bounds.RowsCount = (int)(p1.x - p0.x);
            m_bounds.ColsCount = (int)(p1.y - p0.y);
            boundsSize = Mathf.Max(m_bounds.ColsCount, m_bounds.RowsCount);
            m_bounds.RowsCount = boundsSize;
            m_bounds.ColsCount = boundsSize;

            size = boundsSize;
            m_scale = Mathf.Max(m_desiredResolution / size, 1);
            size *= m_scale;

            CalculateStaticMapHeight();
            m_bgTexture = CreateTexture(size, m_skyColor);
            m_fgTexture = CreateTexture(size, new Color(1, 1, 1, 0));

            Draw(m_voxelMap.Map.Root, new Coordinate(0, 0, 0, m_voxelMap.Map.Weight), m_bgTexture, cell => cell.VoxelData.GetLastStatic(), data => m_groundBaseColor);
            Draw(m_voxelMap.Map.Root, new Coordinate(0, 0, 0, m_voxelMap.Map.Weight), m_fgTexture, 
                cell => GetLast(cell), 
                data => m_materialCache.GetPrimaryColor(data.Owner));

            m_bgTexture.Apply();
            m_fgTexture.Apply();
        }

        private static VoxelData GetLast(MapCell cell)
        {
            VoxelData last = cell.VoxelData.GetLast();
            if (last != null && (!VoxelData.IsStatic(last.Type) || !last.IsNeutral))
            {
                return last;
            }
            return null;
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

        private void Draw(MapCell cell, Coordinate coord, Texture2D texture, Func<MapCell, VoxelData> voxelDataSelector, Func<VoxelData, Color> colorSelector)
        {
            Coordinate zeroCoord = coord.ToWeight(0);
            MapPos p0 = zeroCoord.MapPos;
            int size = (1 << coord.Weight);
            MapPos p1 = p0;
            p1.Add(size, size);

            p0.Row = Mathf.Max(p0.Row, m_bounds.Row);
            p0.Col = Math.Max(p0.Col, m_bounds.Col);
            p1.Row = Mathf.Min(p1.Row, m_bounds.Row + m_bounds.RowsCount);
            p1.Col = Mathf.Min(p1.Col, m_bounds.Col + m_bounds.ColsCount);

            int cols = p1.Col - p0.Col;
            int rows = p1.Row - p0.Row; 
            if(cols <= 0 || rows <= 0)
            {
                return;
            }
            
            cols *= m_scale;
            rows *= m_scale;

            p0.Row -= m_bounds.Row;
            p0.Col -= m_bounds.Col;

            p0.Row *= m_scale;
            p0.Col *= m_scale;

            VoxelData data = null;
            if (cell.VoxelData != null)
            {
                data = voxelDataSelector(cell);
            }

            if (data != null)
            {
                float height = data.Altitude + data.Height;
                float deltaColor = (1.0f - (height / m_staticMapHeight)) * 0.1f;
                Color color = colorSelector(data) - new Color(deltaColor, deltaColor, deltaColor, 0);
                for (int r = 0; r < rows; ++r)
                {
                    for (int c = 0; c < cols; ++c)
                    {
                        texture.SetPixel(p0.Col + c, p0.Row + r, color);
                    }
                }
            }

            if (cell.Children != null && cell.Children.Length > 0 && coord.Weight > GameConstants.MinVoxelActorWeight)
            {
                coord.Weight--;
                coord.Col *= 2;
                coord.Row *= 2;

                for (int i = 0; i < 4; i++)
                {
                    Coordinate childCoord = coord;
                    childCoord.Row += i / 2;
                    childCoord.Col += i % 2;

                    Draw(cell.Children[i], childCoord, texture, voxelDataSelector, colorSelector);
                }
            }
        }

        public void BeginUpdate()
        {
            m_updateRequired = false;
            m_updating = true;
        }

        public void Move(VoxelData voxelData, Coordinate from, Coordinate to)
        {
            if (from.Weight < GameConstants.MinVoxelActorWeight && to.Weight < GameConstants.MinVoxelActorWeight)
            {
                return;
            }

            m_updateRequired = true;

            VoxelData data = GetLast(m_voxelMap.GetCell(from.MapPos, from.Weight, null));
            if(data == null)
            {
                Fill(from, m_transparentColor);
            }
            else
            {
                Fill(from, m_materialCache.GetPrimaryColor(data.Owner));
            }

            Fill(to, m_materialCache.GetPrimaryColor(voxelData.Owner));

            if (!m_updating)
            {
                m_fgTexture.Apply();
            }
        }

        public void Spawn(VoxelData voxelData, Coordinate coord)
        {
            if (coord.Weight < GameConstants.MinVoxelActorWeight)
            {
                return;
            }

            m_updateRequired = true;

            Color color = m_materialCache.GetPrimaryColor(voxelData.Owner);
            Fill(coord, color);

            if (!m_updating)
            {
                m_fgTexture.Apply();
            }
        }

        public void Kill(VoxelData voxelData, Coordinate coord)
        {
            if (coord.Weight < GameConstants.MinVoxelActorWeight)
            {
                return;
            }

            m_updateRequired = true;

            VoxelData data = GetLast(m_voxelMap.GetCell(coord.MapPos, coord.Weight, null));
            if (data == null)
            {
                Fill(coord, m_transparentColor);
            }
            else
            {
                Fill(coord, m_materialCache.GetPrimaryColor(data.Owner));
            }

            if (!m_updating)
            {
                m_fgTexture.Apply();
            }
        }

        private void Fill(Coordinate coord, Color color)
        {
            int size = (1 << coord.Weight) * m_scale;
            coord = coord.ToWeight(0);
            coord.Row -= m_bounds.Row;
            coord.Col -= m_bounds.Col;
            coord.Row *= m_scale;
            coord.Col *= m_scale;

            for (int r = 0; r < size; ++r)
            {
                for (int c = 0; c < size; ++c)
                {
                    m_fgTexture.SetPixel(coord.Col + c, coord.Row + r, color);
                }
            }
        }

        public void EndUpdate()
        {
            m_updating = false;
            if(m_updateRequired)
            {
                m_fgTexture.Apply();
                m_updateRequired = false;
            }
        }
    }
}
