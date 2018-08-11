using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Battlehub.VoxelCombat.Tests
{
    public class VoxelMapMock : MonoBehaviour, IVoxelMap
    {
        public event EventHandler Loaded;
        public event EventHandler Saved;

        private IJob m_job;
        private IProgressIndicator m_progressIndicator;
        private bool m_isBusy;

        private class CameraRef
        {
            public MapCamera Camera;
            public CameraRef(MapCamera camera)
            {
                Camera = camera;
            }
        }

        private List<MapCamera> m_mapCameras;
        private MapRoot m_map;
        public MapRoot Map
        {
            get
            {
                return m_map;
            }
        }

        [SerializeField]
        private int m_mapBoundsPadding = 8;
        public int MapBoundsPadding
        {
            get { return m_mapBoundsPadding; }
        }

        [SerializeField]
        private int m_minMapBoundsSize = 16;
        public int MinMapBoundsSize
        {
            get { return m_minMapBoundsSize; }
        }

        private MapRect m_mapBounds;
        public MapRect MapBounds
        {
            get { return m_mapBounds; }
        }

        private bool m_isOn = false;
        public bool IsOn
        {
            get { return m_isOn; }
            set
            {
                if (m_isOn != value)
                {
                    m_isOn = value;

                    for (int i = 0; i < m_mapCameras.Count; ++i)
                    {
                        m_mapCameras[i].IsOn = m_isOn;
                    }
                }
            }
        }

        public bool IsLoaded
        {
            get { return m_map != null; }
        }

        private void Awake()
        {
            m_progressIndicator = Dependencies.Progress;
            m_job = Dependencies.Job;

            m_mapCameras = new List<MapCamera>();
        }

        private void Start()
        {
          
        }

        private void OnDestroy()
        {
         
        }

        public object CreateCamera(int radius, int weight)
        {
            MapCamera camera = new MapCamera(Map, radius, weight);
            camera.IsOn = m_isOn;

            m_mapCameras.Add(camera);
            return new CameraRef(camera);
        }

        public void DestroyCamera(object cRef)
        {
            CameraRef reference = (CameraRef)cRef;
            if (reference != null && reference.Camera != null)
            {
                reference.Camera.IsOn = false;
                m_mapCameras.Remove(reference.Camera);
                reference.Camera = null;
            }
        }

        public void SetCameraRadius(int radius, object cRef)
        {
            CameraRef reference = (CameraRef)cRef;
            reference.Camera.Radius = radius;
        }

        public MapPos GetCameraPosition(object cRef)
        {
            CameraRef reference = (CameraRef)cRef;
            return new MapPos(reference.Camera.Row, reference.Camera.Col);
        }

        public void SetCameraPosition(int row, int col, object cRef)
        {
            CameraRef reference = (CameraRef)cRef;
            reference.Camera.SetCamera(row, col, reference.Camera.Weight);
        }

        public void MoveCamera(int rowOffset, int colOffset, object cRef)
        {
            CameraRef reference = (CameraRef)cRef;
            reference.Camera.Move(rowOffset, colOffset);
        }

        public int GetCameraWeight(object cRef)
        {
            CameraRef reference = (CameraRef)cRef;
            return reference.Camera.Weight;
        }

        public void SetCameraWeight(int radius, object cRef)
        {
            CameraRef reference = (CameraRef)cRef;
            reference.Camera.Radius = radius;
        }

        public MapPos GetMapPosition(Vector3 position, int weight)
        {
            return Map.GetCellPosition(position, weight);
        }

        public bool IsVisible(MapPos mappos, int weight, object cRef = null)
        {
            int mapSize = m_map.GetMapSizeWith(weight);

            if (mappos.Row < 0 || mappos.Col < 0 || mappos.Row >= mapSize || mappos.Col >= mapSize)
            {
                return false;
            }

            if (cRef != null)
            {
                CameraRef reference = (CameraRef)cRef;
                return reference.Camera.IsVisible(mappos, weight);
            }

            return m_mapCameras.Any(cam => cam.IsVisible(mappos, weight));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mappos"></param>
        /// <param name="weight"></param>
        /// <param name="cRef">if cRef is null camera visiblity check is skipped</param>
        /// <returns></returns>
        public MapCell GetCell(MapPos mappos, int weight, object cRef)
        {
            int mapSize = m_map.GetMapSizeWith(weight);

            if (mappos.Row < 0 || mappos.Col < 0 || mappos.Row >= mapSize || mappos.Col >= mapSize)
            {
                return null;
            }

            if (cRef != null)
            {
                CameraRef reference = (CameraRef)cRef;
                if (!reference.Camera.IsVisible(mappos, weight))
                {
                    return null;
                }
            }

            return Map.Get(mappos.Row, mappos.Col, weight);
        }

        public Vector3 GetWorldPosition(MapPos mappos, int weight, MapPos.Align rowAlign = MapPos.Align.Center, MapPos.Align colAlign = MapPos.Align.Center)
        {
            return Map.GetWorldPosition(mappos, weight, rowAlign, colAlign);
        }

        public Vector3 GetWorldPosition(Coordinate coordinate)
        {
            return Map.GetWorldPosition(coordinate.MapPos, coordinate.Weight, MapPos.Align.Center, MapPos.Align.Center);
        }

        public void Load(byte[] bytes, Action done)
        {
            if (m_isBusy)
            {
                throw new InvalidOperationException("m_isBusy");
            }

            m_isBusy = true;
            m_progressIndicator.IsVisible = true;
            m_progressIndicator.SetText("LOADING...");

            for (int i = 0; i < m_mapCameras.Count; ++i)
            {
                m_mapCameras[i].IsOn = false;
            }

            m_job.Submit(
            () =>
            {
                ProtobufSerializer serializer = null;
                try
                {
                    var pool = Dependencies.Serializer;
                    if (pool != null)
                    {
                        serializer = pool.Acquire();
                        m_map = serializer.Deserialize<MapRoot>(bytes);
                    }
                    CalculateBounds();

                    //how to make sure than no one accessing cameras during background thread job ?
                    for (int i = 0; i < m_mapCameras.Count; ++i)
                    {
                        m_mapCameras[i].Map = m_map;
                    }
                }
                catch (Exception e)
                {
                    return e;
                }
                finally
                {
                    if(serializer != null)
                    {
                        var pool = Dependencies.Serializer;
                        if (pool != null)
                        {
                            pool.Release(serializer);
                        }
                    }
                }

                return null;
            },
            result =>
            {
                if (result is Exception)
                {
                    Debug.LogError(result.ToString());
                }

                m_isBusy = false;
                m_progressIndicator.IsVisible = false;

                if (Loaded != null)
                {
                    Loaded(this, EventArgs.Empty);
                }

                if (done != null)
                {
                    done();
                }

                for (int i = 0; i < m_mapCameras.Count; ++i)
                {
                    m_mapCameras[i].IsOn = m_isOn;
                }
            });
        }

        private static bool IsParentNonEmpty(MapCell cell)
        {
            bool nonEmpty = false;
            MapCell parentCell = cell;
            while (parentCell.Parent != null)
            {
                parentCell = parentCell.Parent;
                if (parentCell.First != null)
                {
                    nonEmpty = true;
                    break;
                }
            }

            return nonEmpty;
        }
        private static bool IsNonEmpty(MapCell cell)
        {
            bool nonEmpty = cell.First != null || cell.HasDescendantsWithVoxelData();
            if (!nonEmpty)
            {
                nonEmpty = IsParentNonEmpty(cell);
            }
            return nonEmpty;
        }

        private void CalculateBounds()
        {
            int weight = GameConstants.MinVoxelActorWeight;
            int size = m_map.GetMapSizeWith(weight);
            Debug.Assert(size >= m_minMapBoundsSize, "map size < m_minMapBoundsSize");

            MapPos min = new MapPos(0, 0);
            MapPos max = new MapPos(size - 1, size - 1);

            MapCell col0 = m_map.Get(0, 0, weight);
            for (int row = 0; row < size; ++row)
            {
                MapCell cell = col0;
                bool nonEmpty = false;
                for (int col = 0; col < size; ++col)
                {
                    nonEmpty = IsNonEmpty(cell);
                    if (nonEmpty)
                    {
                        break;
                    }
                    cell = cell.SiblingPCol;
                }

                if (nonEmpty)
                {
                    min.Row = row;
                    break;
                }
                else
                {
                    min.Row = row;
                }

                col0 = col0.SiblingPRow;
            }

            col0 = m_map.Get(size - 1, 0, weight);
            for (int row = size - 1; row >= 0; --row)
            {
                MapCell cell = col0;
                bool nonEmpty = false;
                for (int col = 0; col < size; ++col)
                {
                    nonEmpty = IsNonEmpty(cell);
                    if (nonEmpty)
                    {
                        break;
                    }
                    cell = cell.SiblingPCol;
                }

                if (nonEmpty)
                {
                    max.Row = row;
                    break;
                }
                else
                {
                    max.Row = row;
                }

                col0 = col0.SiblingMRow;
            }

            MapCell row0 = m_map.Get(0, 0, weight);
            for (int col = 0; col < size; ++col)
            {
                MapCell cell = row0;
                bool nonEmpty = false;
                for (int row = 0; row < size; ++row)
                {
                    nonEmpty = IsNonEmpty(cell);
                    if (nonEmpty)
                    {
                        break;
                    }
                    cell = cell.SiblingPRow;
                }

                if (nonEmpty)
                {
                    min.Col = col;
                    break;
                }
                else
                {
                    min.Col = col;
                }

                row0 = row0.SiblingPCol;
            }

            row0 = m_map.Get(0, size - 1, weight);
            for (int col = size - 1; col >= 0; --col)
            {
                MapCell cell = row0;
                bool nonEmpty = false;
                for (int row = 0; row < size; ++row)
                {
                    nonEmpty = IsNonEmpty(cell);
                    if (nonEmpty)
                    {
                        break;
                    }
                    cell = cell.SiblingPRow;
                }

                if (nonEmpty)
                {
                    max.Col = col;
                    break;
                }
                else
                {
                    max.Col = col;
                }

                row0 = row0.SiblingMCol;
            }

            if (min.Col > max.Col)
            {
                min.Col = max.Col;
            }

            if (min.Row > max.Row)
            {
                min.Row = max.Row;
            }

            int centerCol = min.Col + (max.Col - min.Col) / 2;
            int centerRow = min.Row + (max.Row - min.Row) / 2;

            int minCol = Mathf.Max(0, centerCol - m_minMapBoundsSize / 2);
            int minRow = Mathf.Max(0, centerRow - m_minMapBoundsSize / 2);

            int maxCol = minCol + m_minMapBoundsSize;
            int maxRow = minRow + m_minMapBoundsSize;
            if (maxCol >= size)
            {
                maxCol = size - 1;
                minCol = maxCol - m_minMapBoundsSize;
            }

            if (maxRow >= size)
            {
                maxRow = size - 1;
                minRow = maxRow - m_minMapBoundsSize;
            }

            if (minCol < min.Col)
            {
                min.Col = minCol;
            }
            if (minRow < min.Row)
            {
                min.Row = minRow;
            }
            if (maxCol > max.Col)
            {
                max.Col = maxCol;
            }
            if (maxRow > max.Row)
            {
                max.Row = maxRow;
            }


            m_mapBounds = new MapRect(min, max);
        }


        public void Save(Action<byte[]> done)
        {
            if (m_isBusy)
            {
                throw new InvalidOperationException("m_isBusy");
            }

            m_isBusy = true;
            m_progressIndicator.IsVisible = true;
            m_progressIndicator.SetText("Saving...");

            m_job.Submit(
            () =>
            {
                ProtobufSerializer serializer = null;
                byte[] bytes = null;
                try
                {
                    var pool = Dependencies.Serializer;
                    if (pool != null)
                    {
                        serializer = pool.Acquire();
                        bytes = serializer.Serialize(Map);
                    }
                }
                finally
                {
                    if (serializer != null)
                    {
                        var pool = Dependencies.Serializer;
                        if (pool != null)
                        {
                            pool.Release(serializer);
                        }
                    }
                }

                return bytes;
            },
            result =>
            {
                m_isBusy = false;
                m_progressIndicator.IsVisible = false;

                if (Saved != null)
                {
                    Saved(this, EventArgs.Empty);
                }

                if (done != null)
                {
                    done((byte[])result);
                }
            });
        }

        public void Create(int weight)
        {
            for (int i = 0; i < m_mapCameras.Count; ++i)
            {
                m_mapCameras[i].IsOn = false;
            }

            m_map = new MapRoot(weight);
            //how to make sure than no one accessing cameras during background thread job ?
            for (int i = 0; i < m_mapCameras.Count; ++i)
            {
                m_mapCameras[i].Map = m_map;

            }

            if (Loaded != null)
            {
                Loaded(this, EventArgs.Empty);
            }

            for (int i = 0; i < m_mapCameras.Count; ++i)
            {
                m_mapCameras[i].IsOn = m_isOn;
            }
        }
    }
}