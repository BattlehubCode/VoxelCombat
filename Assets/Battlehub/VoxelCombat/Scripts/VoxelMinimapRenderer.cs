using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{

    public interface IVoxelMinimapRenderer
    {
        event EventHandler Loaded;

        Texture2D[] FogOfWar
        {
            get;
        }

        Texture2D Foreground
        {
            get;
        }

        Texture2D Background
        {
            get;
        }

        void BeginUpdate();
        void Move(VoxelData data,Coordinate from, Coordinate to);
        void Spawn(VoxelData data,  Coordinate coord);
        void Die(VoxelData data, Coordinate coord);

        void ObserveCell(int playerId, MapPos pos, int weight);
        void IgnoreCell(int playerId, MapPos pos, int weight);

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
        private Texture2D[] m_fogOfWarTextures = new Texture2D[GameConstants.MaxPlayers];

        public Texture2D[] FogOfWar
        {
            get { return m_fogOfWarTextures; }
        }

        public Texture2D Foreground
        {
            get { return m_fgTexture; }
        }

        public Texture2D Background
        {
            get { return m_bgTexture; }
        }

        private Color m_skyColor;
        private Color m_groundBaseColor;
        private Color m_transparentColor;
        private Color m_fogOfWarColor;

        private int m_staticMapHeight;
        private MapRect m_bounds;
        private int m_scale;
        private bool m_updateRequired;
        private bool m_updating;

        private IVoxelMap m_voxelMap;
        private IMaterialsCache m_materialCache;
        private IVoxelGame m_gameState;

        private void Awake()
        {
            m_materialCache = Dependencies.MaterialsCache;
            m_voxelMap = Dependencies.Map;
            m_gameState = Dependencies.GameState;

            m_gameState.Started += OnGameStarted;
            if (m_gameState.IsStarted)
            {
                OnGameStarted();
            }

            m_skyColor = Camera.main.backgroundColor;
            m_skyColor.a = 1.0f;
            m_groundBaseColor = Color.white;
            m_transparentColor = new Color(0, 0, 0, 0);
            m_fogOfWarColor = new Color(0, 0, 0, 1);
        }

        private void OnDestroy()
        {
            if(m_gameState != null)
            {
                m_gameState.Started -= OnGameStarted;
            }

            if(m_bgTexture != null)
            {
                Destroy(m_bgTexture);
            }

            if(m_fgTexture != null)
            {
                Destroy(m_fgTexture);
            }

            if(m_fogOfWarTextures != null)
            {
                for(int i = 0; i < m_fogOfWarTextures.Length; ++i)
                {
                    Texture2D texture = m_fogOfWarTextures[i];
                    if(texture != null)
                    {
                        Destroy(texture);
                    }
                }
            }
        }

        private void OnGameStarted()
        {
            CreateTextures();
 
            if (Loaded != null)
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

            int playersCount = m_gameState.PlayersCount;
            m_fogOfWarTextures = new Texture2D[playersCount];
            for (int i = 0; i < m_fogOfWarTextures.Length; ++i)
            {
                if(m_gameState.IsLocalPlayer(i))
                {
                    m_fogOfWarTextures[i] = CreateTexture(size, m_fogOfWarColor);
                }
            }



            Draw(m_voxelMap.Map.Root, new Coordinate(0, 0, 0, m_voxelMap.Map.Weight), m_bgTexture, cell => cell.VoxelData.GetLastStatic(), data => m_groundBaseColor);
            Draw(m_voxelMap.Map.Root, new Coordinate(0, 0, 0, m_voxelMap.Map.Weight), m_fgTexture, 
                cell => GetLast(cell), 
                data => m_materialCache.GetPrimaryColor(data.Owner));

            m_voxelMap.Map.ForEach((cell, pos, weight) =>
            {
                for (int i = 0; i < cell.ObservedBy.Length; ++i)
                {
                    if(cell.ObservedBy[i] > 0)
                    {
                        Texture2D fogOfWarTexture = m_fogOfWarTextures[i];
                        if(fogOfWarTexture != null)
                        {
                            Fill(fogOfWarTexture, new Coordinate(pos, weight, 0), m_transparentColor);
                        }
                    }
                }
            });

            m_bgTexture.Apply();
            m_fgTexture.Apply();
            for (int i = 0; i < m_fogOfWarTextures.Length; ++i)
            {
                if (m_fogOfWarTextures[i] != null)
                {
                    m_fogOfWarTextures[i].Apply();
                }
            }
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
            result.wrapMode = TextureWrapMode.Clamp;
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
                Fill(m_fgTexture, from, m_transparentColor);
            }
            else
            {
                Fill(m_fgTexture, from, GetColor(data));
            }

            Fill(m_fgTexture, to, GetColor(voxelData));

            if (!m_updating && m_fgTexture != null)
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
            
            Fill(m_fgTexture, coord, GetColor(voxelData));

            if (!m_updating && m_fgTexture != null)
            {
                m_fgTexture.Apply();
            }
        }

        public void Die(VoxelData voxelData, Coordinate coord)
        {
            if (coord.Weight < GameConstants.MinVoxelActorWeight)
            {
                return;
            }

            m_updateRequired = true;

            VoxelData data = GetLast(m_voxelMap.GetCell(coord.MapPos, coord.Weight, null));
            if (data == null)
            {
                Fill(m_fgTexture, coord, m_transparentColor);
            }
            else
            {
                Fill(m_fgTexture, coord, GetColor(data));
            }

            if (!m_updating && m_fgTexture != null)
            {
                m_fgTexture.Apply();
            }
        }

        private Color GetColor(VoxelData data)
        {
            float height = data.Altitude + data.Height;
            float deltaColor = (1.0f - (height / m_staticMapHeight)) * 0.1f;
            Color color = m_materialCache.GetPrimaryColor(data.Owner) - new Color(deltaColor, deltaColor, deltaColor, 0);
            return color;
        }

        public void ObserveCell(int playerId, MapPos pos, int weight)
        {
            Texture2D texture = m_fogOfWarTextures[playerId];
            if(texture == null)
            {
                return;
            }
            m_updateRequired = true;

            Fill(texture, new Coordinate(pos, weight, 0), m_transparentColor);

            if (!m_updating && texture != null)
            {
                texture.Apply();
            }
        }

        public void IgnoreCell(int playerId, MapPos pos, int weight)
        {
            Texture2D texture = m_fogOfWarTextures[playerId];
            if (texture == null)
            {
                return;
            }
            m_updateRequired = true;

            Fill(texture, new Coordinate(pos, weight, 0), m_fogOfWarColor);

            if (!m_updating && texture != null)
            {
                texture.Apply();
            }
        }

        private void Fill(Texture2D texture, Coordinate coord, Color color)
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
                    texture.SetPixel(coord.Col + c, coord.Row + r, color);
                }
            }
        }

        public void EndUpdate()
        {
            m_updating = false;
            if(m_updateRequired)
            {
                m_fgTexture.Apply();
                for(int i = 0; i < m_fogOfWarTextures.Length; ++i)
                {
                    Texture2D fogOfWarTexuture = m_fogOfWarTextures[i];
                    if(fogOfWarTexuture != null)
                    {
                        fogOfWarTexuture.Apply();
                    }
                }
                m_updateRequired = false;
            }
        }
    }
}
