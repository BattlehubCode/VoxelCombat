using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{

    public interface IVoxelMinimapRenderer
    {
        event EventHandler Loaded;
        event EventHandler TextureChanged;

        bool IsLoaded
        {
            get;
        }

        Texture2DArray FogOfWar
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
        public event EventHandler TextureChanged;
        public bool IsLoaded
        {
            get;
            private set;
        }

        [SerializeField]
        private int m_desiredResolution = 512;

        private Texture2D m_bgTexture;
        private Texture2D m_fgTexture;
        private Texture2DArray m_fogOfWarTextures;
        private Color32[] m_bgColors;
        private Color32[] m_fgColors;
        private Color32[][] m_fogOfWarColors = new Color32[GameConstants.MaxPlayers][];



        public Texture2DArray FogOfWar
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
        private Color m_fogOfWarVisitedColor;

        private int m_staticMapHeight;
        private MapRect m_bounds;
        private int m_scale;
        private bool m_updateRequired;

        private IVoxelMap m_voxelMap;
        private IMaterialsCache m_materialCache;
        private IVoxelGame m_gameState;
        private IGlobalSettings m_settings;

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
            m_groundBaseColor = m_materialCache.GetPrimaryColor(0);
            m_transparentColor = new Color(0, 0, 0, 0);

            m_settings = Dependencies.Settings;
            m_settings.DisableFogOfWarChanged += OnDisableFogOfWarChanged;
            UpdateColors();
        }

        private void OnDestroy()
        {
            if(m_settings != null)
            {
                m_settings.DisableFogOfWarChanged -= OnDisableFogOfWarChanged;
            }

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
                Destroy(m_fogOfWarTextures);
            }
        }


        private void UpdateColors()
        {
            if (m_settings.DisableFogOfWar)
            {
                m_fogOfWarColor = m_transparentColor;
                m_fogOfWarVisitedColor = m_transparentColor;
            }
            else
            {
                m_fogOfWarColor = new Color(0.05f, 0.05f, 0.05f, 1);
                m_fogOfWarVisitedColor = new Color(0, 0, 0, 0.3f);
            }
        }

        private void OnDisableFogOfWarChanged()
        {
            UpdateColors();

            if (m_bgTexture != null)
            {
                Destroy(m_bgTexture);
            }

            if (m_fgTexture != null)
            {
                Destroy(m_fgTexture);
            }

            if (m_fogOfWarTextures != null)
            {
                Destroy(m_fogOfWarTextures);
            }

            CreateAndSetTextures();
        }

        private void CreateAndSetTextures()
        {
            CreateTextures();

            Shader.SetGlobalTexture("_FogOfWarTex", FogOfWar);
            Shader.SetGlobalInt("_MapWeight", m_voxelMap.Map.Weight);

            if (TextureChanged != null)
            {
                TextureChanged(this, EventArgs.Empty);
            }
        }

        private void OnGameStarted()
        {
            CreateAndSetTextures();

            if (Loaded != null)
            {
                Loaded(this, EventArgs.Empty);
            }
            IsLoaded = true;
        }

        private void CreateTextures()
        {
            int size = m_voxelMap.Map.GetMapSizeWith(0);
            m_bounds = new MapRect(0, 0, m_voxelMap.Map.GetMapSizeWith(2), m_voxelMap.Map.GetMapSizeWith(2));
           
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
            m_bgTexture = CreateTexture(size, m_skyColor, out m_bgColors);
            m_fgTexture = CreateTexture(size, new Color(1, 1, 1, 0), out m_fgColors);

            int playersCount = m_gameState.PlayersCount;
            m_fogOfWarTextures = CreateTexture(size, m_fogOfWarColor, playersCount, out m_fogOfWarColors);

            BeginUpdate();
            m_updateRequired = true;

            Draw(m_voxelMap.Map.Root, new Coordinate(0, 0, 0, m_voxelMap.Map.Weight), m_bgColors, size, cell => cell.First.GetLastStatic(), data => m_groundBaseColor);
            Draw(m_voxelMap.Map.Root, new Coordinate(0, 0, 0, m_voxelMap.Map.Weight), m_fgColors, size,
                cell => GetLast(cell), 
                data => m_materialCache.GetPrimaryColor(data.Owner));

            m_voxelMap.Map.ForEach((cell, pos, weight) =>
            {
                for (int i = 0; i < cell.ObservedBy.Length; ++i)
                {
                    if(cell.ObservedBy[i] > 0)
                    {
                        Color32[] fogOfWarColors = m_fogOfWarColors[i];
                        if(fogOfWarColors != null)
                        {
                            Fill(fogOfWarColors, new Coordinate(pos, weight, 0), m_transparentColor);
                        }
                    }
                }
            });

            m_bgTexture.SetPixels32(m_bgColors);
            m_bgTexture.Apply();

            EndUpdate();
        }

        private static VoxelData GetLast(MapCell cell)
        {
            if(cell.First == null)
            {
                return null;
            }
            VoxelData last = cell.First.GetLast();
            if (last != null && (!VoxelData.IsStatic(last.Type) || !last.IsNeutral))
            {
                return last;
            }
            return null;
        }

        private Texture2D CreateTexture(int size, Color32 fill, out Color32[] colors)
        {
            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, true);
            result.wrapMode = TextureWrapMode.Clamp;
            result.filterMode = FilterMode.Bilinear;
            colors = new Color32[size * size];
            for (int i = 0; i < size; ++i)
            {
                for (int j = 0; j < size; ++j)
                {
                    colors[i * size + j] = fill;
                }
            }
            result.SetPixels32(colors);
            return result;
        }

        private Texture2DArray CreateTexture(int size, Color32 fill, int count, out Color32[][] colors)
        {
            Texture2DArray result = new Texture2DArray(size, size, count, TextureFormat.RGBA32, true);
            result.wrapMode = TextureWrapMode.Clamp;
            result.filterMode = FilterMode.Bilinear;
            colors = new Color32[count][];
            Color color = fill;
            for(int k = 0; k < count; ++k)
            {
                bool skip = !m_gameState.IsLocalPlayer(k);

#if UNITY_EDITOR
                if(k == 0)
                {
                    skip = false;
                    color = m_transparentColor;
                }
                else
                {
                    color = fill;
                }
#endif
                if (skip)
                {
                    continue;
                }
                colors[k] = new Color32[size * size];
                for (int i = 0; i < size; ++i)
                {
                    for (int j = 0; j < size; ++j)
                    {
                        colors[k][i * size + j] = color;
                    }
                }

                result.SetPixels32(colors[k], k);
            }
          
            return result;

        }
        private void CalculateStaticMapHeight()
        {
            m_voxelMap.Map.Root.ForEach(cell =>
            {
                if (cell.First != null)
                {
                    VoxelData lastStatic = cell.First.GetLastStatic();
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

        private void Draw(MapCell cell, Coordinate coord, Color32[] colors, int textureSize, Func<MapCell, VoxelData> voxelDataSelector, Func<VoxelData, Color> colorSelector)
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
            if (cell.First != null)
            {
                data = voxelDataSelector(cell);
            }

            if (data != null)
            {
                float height = data.Altitude + data.Height;
                float deltaColor = Mathf.Max(0, (1.0f - (height / m_staticMapHeight))) * 0.1f;
                Color32 color = colorSelector(data) - new Color(deltaColor, deltaColor, deltaColor, 0);
                for (int r = 0; r < rows; ++r)
                {
                    for (int c = 0; c < cols; ++c)
                    {
                        colors[(p0.Row + r) * textureSize + p0.Col + c] = color;
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

                    Draw(cell.Children[i], childCoord, colors, textureSize, voxelDataSelector, colorSelector);
                }
            }
        }

        public void BeginUpdate()
        {
            m_updateRequired = false;
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
                Fill(m_fgColors, from, m_transparentColor);
            }
            else
            {
                Fill(m_fgColors, from, GetColor(data));
            }

            Fill(m_fgColors, to, GetColor(voxelData));
        }

        public void Spawn(VoxelData voxelData, Coordinate coord)
        {
            if (coord.Weight < GameConstants.MinVoxelActorWeight)
            {
                return;
            }

            m_updateRequired = true;
            
            Fill(m_fgColors, coord, GetColor(voxelData));
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
                Fill(m_fgColors, coord, m_transparentColor);
            }
            else
            {
                Fill(m_fgColors, coord, GetColor(data));
            }
        }

        private Color GetColor(VoxelData data)
        {
            float height = data.Altitude + data.Height;
            float deltaColor = Mathf.Max(0, (1.0f - (height / m_staticMapHeight))) * 0.1f;
            Color color = m_materialCache.GetPrimaryColor(data.Owner) - new Color(deltaColor, deltaColor, deltaColor, 0);
            return color;
        }

        public void ObserveCell(int playerId, MapPos pos, int weight)
        {
            Color32[] colors = m_fogOfWarColors[playerId];
            if(colors == null)
            {
                return;
            }
            m_updateRequired = true;
            Fill(colors, new Coordinate(pos, weight, 0), m_transparentColor);
        }

        public void IgnoreCell(int playerId, MapPos pos, int weight)
        {
            Color32[] colors = m_fogOfWarColors[playerId];
            if (colors == null)
            {
                return;
            }
            m_updateRequired = true;

            Fill(colors, new Coordinate(pos, weight, 0), m_fogOfWarVisitedColor);
        }

        private void Fill(Color32[] colors, Coordinate coord, Color32 color)
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
                    colors[(coord.Row + r) * m_bgTexture.width + coord.Col + c] = color;
                }
            }
        }

        public void EndUpdate()
        {
            if(m_updateRequired)
            {
                m_fgTexture.SetPixels32(m_fgColors);
                m_fgTexture.Apply();
                for(int i = 0; i < m_fogOfWarTextures.depth; ++i)
                {
                    m_fogOfWarTextures.SetPixels32(m_fogOfWarColors[i], i);
                }
                m_fogOfWarTextures.Apply();
                m_updateRequired = false;
            }
        }
    }
}
