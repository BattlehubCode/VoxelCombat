using UnityEngine;
using UnityEngine.PostProcessing;

namespace Battlehub.VoxelCombat
{
    public delegate void GameViewportEventHandler();
    public interface IGameViewport
    {
        event GameViewportEventHandler ViewportChanged;

        Camera Camera
        {
            get;
        }  

        int LocalPlayerIndex
        {
            get;
            set;
        }
    }


    
    public class GameViewport : MonoBehaviour, IGameViewport
    {
        public event GameViewportEventHandler ViewportChanged;
        
        private RectTransform m_viewport;
        private Vector3 m_viewportPosition;
        private float m_viewportWidth;
        private float m_viewportHeight;

        [SerializeField]
        private Camera m_camera;

        [SerializeField]
        private PostProcessingProfile m_postrocessingProfile;

        public Camera Camera
        {
            get { return m_camera; }
        }

        private int m_localPlayerIndex = -1;
        public int LocalPlayerIndex 
        {
            get { return m_localPlayerIndex; }
            set
            {
                if(m_localPlayerIndex != value)
                {
                    m_localPlayerIndex = value;

                    GLCamera glCamera = m_camera.GetComponent<GLCamera>();

                    int player0Layer = GameConstants.Player0LayerMask;
                    int player1Layer = GameConstants.Player1LayerMask;
                    int player2Layer = GameConstants.Player2LayerMask;
                    int player3Layer = GameConstants.Player3LayerMask;

                    if (m_localPlayerIndex == 0)
                    {
                        glCamera.CullingMask = (int)RTLayer.Viewport0;

                        m_camera.cullingMask &= ~player1Layer;
                        m_camera.cullingMask &= ~player2Layer;
                        m_camera.cullingMask &= ~player3Layer;

                    }
                    else if (m_localPlayerIndex == 1)
                    {
                        glCamera.CullingMask = (int)RTLayer.Viewport1;

                        m_camera.cullingMask &= ~player0Layer;
                        m_camera.cullingMask &= ~player2Layer;
                        m_camera.cullingMask &= ~player3Layer;
                    }
                    else if (m_localPlayerIndex == 2)
                    {
                        glCamera.CullingMask = (int)RTLayer.Viewport2;

                        m_camera.cullingMask &= ~player0Layer;
                        m_camera.cullingMask &= ~player1Layer;
                        m_camera.cullingMask &= ~player3Layer;
                    }
                    else if (m_localPlayerIndex == 3)
                    {
                        glCamera.CullingMask = (int)RTLayer.Viewport3;

                        m_camera.cullingMask &= ~player0Layer;
                        m_camera.cullingMask &= ~player1Layer;
                        m_camera.cullingMask &= ~player2Layer;
                    }
                    else
                    {
                        glCamera.CullingMask = (int)RTLayer.GameView;
                        Debug.LogWarning("CullingMask = RTLayer.GameView");
                    }

                    GridRenderer gridRenderer = GetComponentInChildren<GridRenderer>(true);
                    if(gridRenderer != null)
                    {
                        gridRenderer.CullingMask = glCamera.CullingMask;
                    }

                    CameraFogOfWar fogOfWar = m_camera.GetComponent<CameraFogOfWar>();
                    if(fogOfWar != null)
                    {
                        fogOfWar.PlayerIndex = Dependencies.GameState.LocalToPlayerIndex(m_localPlayerIndex);
                    }
                }
            }
        }


        private void Awake()
        {
            GameObject pivot = new GameObject();
            pivot.name = "ViewportPivot";
            
            GameObject camera = new GameObject();
            camera.transform.SetParent(pivot.transform, true);
            m_camera = camera.AddComponent<Camera>();                
            m_camera.fieldOfView = 50;
            m_camera.clearFlags = CameraClearFlags.SolidColor;
            m_camera.backgroundColor = new Color32(0x4F, 0xC6, 0xFF, 0x00);

            GLCamera glCam = camera.AddComponent<GLCamera>();
            glCam.CullingMask = (int)RTLayer.GameView;

            GridRenderer gridRenderer = GetComponentInChildren<GridRenderer>(true);
            if (gridRenderer != null)
            {
                gridRenderer.Target = pivot.transform;
                gridRenderer.CullingMask = glCam.CullingMask;
            }

            if(m_postrocessingProfile != null)
            {
                PostProcessingBehaviour postprocessing = camera.AddComponent<PostProcessingBehaviour>();
                postprocessing.profile = m_postrocessingProfile;

                m_camera.allowMSAA = false;
            }

            camera.AddComponent<CameraFogOfWar>();
            
            m_viewport = GetComponent<RectTransform>();

            Canvas canvas = m_viewport.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceCamera && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                gameObject.SetActive(false);
                Debug.LogWarning("ViewportFitter requires canvas.renderMode -> RenderMode.ScreenSpaceOverlay or RenderMode.ScreenSpaceCamera");
                return;
            }
            
            m_camera.pixelRect = new Rect(new Vector2(0, 0), new Vector2(Screen.width, Screen.height));
        }

        private void OnDestroy()
        {
            if(m_camera != null)
            {
                Destroy(m_camera.gameObject);
            }
        }

        private void OnEnable()
        {
            Rect rect = m_viewport.rect;
            UpdateViewport();
            m_viewportHeight = rect.height;
            m_viewportWidth = rect.width;
            m_viewportPosition = m_viewport.position;
        }

        private void Start()
        {
            m_camera.name = name + "Camera";

            Rect rect = m_viewport.rect;
            UpdateViewport();
            m_viewportHeight = rect.height;
            m_viewportWidth = rect.width;
            m_viewportPosition = m_viewport.position;
        }

        private void OnDisable()
        {
            if (m_camera == null)
            {
                return;
            }

            m_camera.pixelRect = new Rect(new Vector2(0, 0), new Vector2(Screen.width, Screen.height));
            
        }

        private void OnGUI()
        {
            if (m_viewport != null)
            {
                Rect rect = m_viewport.rect;
                if (m_viewportHeight != rect.height || m_viewportWidth != rect.width || m_viewportPosition != m_viewport.position)
                {
                    UpdateViewport();
                    m_viewportHeight = rect.height;
                    m_viewportWidth = rect.width;
                    m_viewportPosition = m_viewport.position;
                }
            }
        }

        private void UpdateViewport()
        {
            if (m_camera == null)
            {
                return;
            }
         
            Vector3[] corners = new Vector3[4];
            m_viewport.GetWorldCorners(corners);
            m_camera.pixelRect = new Rect(corners[0], new Vector2(corners[2].x - corners[0].x, corners[1].y - corners[0].y));

            if(ViewportChanged != null)
            {
                ViewportChanged();
            }
        }

    }
}

