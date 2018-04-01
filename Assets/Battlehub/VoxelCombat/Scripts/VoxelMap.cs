using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class VoxelMap :  MonoBehaviour, IVoxelMap, IGL
    {
        public event EventHandler Loaded;
        public event EventHandler Saved;

        [SerializeField]
        private bool m_drawDebugLines = true;

        private Material m_debugMaterial;

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
                if(m_isOn != value)
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
            m_debugMaterial = new Material(Shader.Find("GUI/Text Shader"));

            m_progressIndicator = Dependencies.Progress;
            m_job = Dependencies.Job;

            m_mapCameras = new List<MapCamera>();
        }

        private void Start()
        {
            if(GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Add(this);
            }
        }

        private void OnDestroy()
        {
            if (GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Remove(this);
            }
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
            if(reference != null && reference.Camera != null)
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

            if(cRef != null)
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
        
            for(int i = 0; i < m_mapCameras.Count; ++i)
            {
                m_mapCameras[i].IsOn = false;
            }
            
            m_job.Submit(
            () =>
            {
                try
                {
                    m_map = ProtobufSerializer.Deserialize<MapRoot>(bytes);
                    CalculateBounds();

                    //how to make sure than no one accessing cameras during background thread job ?
                    for (int i = 0; i < m_mapCameras.Count; ++i)
                    {
                        m_mapCameras[i].Map = m_map;
                    }
                }
                catch(Exception e)
                {
                    return e;
                }
               
                return null;
            },
            result =>
            {
                if(result is Exception)
                {
                    Debug.LogError(result.ToString());
                }

                m_isBusy = false;
                m_progressIndicator.IsVisible = false;

                if(Loaded != null)
                {
                    Loaded(this, EventArgs.Empty);
                }

                if(done != null)
                {
                    done();
                }

                for (int i = 0; i < m_mapCameras.Count; ++i)
                {
                    m_mapCameras[i].IsOn = m_isOn;
                }

                
            });
        }

        private void CalculateBounds()
        {
            int weight = GameConstants.MinVoxelActorWeight;
            int size = m_map.GetMapSizeWith(weight);
            MapPos min = new MapPos(size / 2, size / 2);
            MapPos max = new MapPos(size / 2, size / 2);
            for (int row = 0; row < size; ++row)
            {
                for(int col = 0; col < size; ++col)
                {
                    MapCell cell = m_map.Get(row, col, weight);
                    bool nonEmpty = cell.VoxelData != null || cell.HasDescendantsWithVoxelData();
                    if (!nonEmpty)
                    {
                        while (cell.Parent != null)
                        {
                            cell = cell.Parent;
                            if (cell.VoxelData != null)
                            {
                                nonEmpty = true;
                                break;
                            }
                        }
                    }

                    if(nonEmpty)
                    {
                        if(row < min.Row)
                        {
                            min.Row = row;
                        }

                        if(row > max.Row)
                        {
                            max.Row = row;
                        }

                        if(col < min.Col)
                        {
                            min.Col = col;
                        }

                        if(col > max.Col)
                        {
                            max.Col = col;
                        }
                    }
                }
            }

            m_mapBounds = new MapRect(min, max);
        }

        public void Save(Action<byte[]> done)
        {
            if(m_isBusy)
            {
                throw new InvalidOperationException("m_isBusy");
            }

            m_isBusy = true;
            m_progressIndicator.IsVisible = true;
            m_progressIndicator.SetText("Saving...");

            m_job.Submit(
            () =>
            {
                byte[] bytes = ProtobufSerializer.Serialize(Map);
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

            if(Loaded != null)
            {
                Loaded(this, EventArgs.Empty);
            }

            for (int i = 0; i < m_mapCameras.Count; ++i)
            {
                m_mapCameras[i].IsOn = m_isOn;
            }
        }

        private Color[] m_debugColors = new[]
        {
            Color.yellow,
            Color.red,
            Color.green,
            Color.blue
        };
        public void Draw(int cullingMask)
        {
            if(!m_drawDebugLines)
            {
                return;
            }

            for(int i = 0; i < m_mapCameras.Count; ++i)
            {
                MapCamera camera = m_mapCameras[i];
                //Drag camera bounds here
                int radius = camera.Radius;
                int weight = camera.Weight;
                int row = camera.Row;
                int col = camera.Col;

                Vector3 p1 = GetWorldPosition(new MapPos(row - radius, col - radius), weight, MapPos.Align.Minus, MapPos.Align.Minus);
                Vector3 p2 = GetWorldPosition(new MapPos(row - radius, col + radius), weight, MapPos.Align.Minus, MapPos.Align.Plus);
                Vector3 p3 = GetWorldPosition(new MapPos(row + radius, col + radius), weight, MapPos.Align.Plus, MapPos.Align.Plus);
                Vector3 p4 = GetWorldPosition(new MapPos(row + radius, col - radius), weight, MapPos.Align.Plus, MapPos.Align.Minus);

                p1.y = p2.y = p3.y = p4.y = p4.y + GameConstants.UnitSize + i / 4.0f;

                GL.PushMatrix();

                m_debugMaterial.SetPass(0);

                GL.Begin(GL.LINES);

                GL.Color(m_debugColors[i % m_debugColors.Length]);

                GL.Vertex(p1);
                GL.Vertex(p2);

                GL.Vertex(p2);
                GL.Vertex(p3);

                GL.Vertex(p3);
                GL.Vertex(p4);

                GL.Vertex(p4);
                GL.Vertex(p1);

                GL.End();

                GL.PopMatrix();
            }
            
        }
    }
}

