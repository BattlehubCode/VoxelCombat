using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class VoxelUI : MonoBehaviour
    {
        public int LocalPlayerIndex
        {
            get;
            set;
        }

        private IGameViewport m_viewport;
        
        [SerializeField]
        private ProgressBar m_progress;

        [SerializeField]
        private Transform m_pivot;
        
        private void OnEnable()
        {
            m_viewport = Dependencies.GameView.GetViewport(LocalPlayerIndex);

            foreach (SpriteRenderer sr in m_progress.GetComponentsInChildren<SpriteRenderer>(true))
            {
                cakeslice.Outline outline = sr.gameObject.AddComponent<cakeslice.Outline>();
                outline.eraseRenderer = true;
                outline.layerMask = GameConstants.PlayerLayerMasks[LocalPlayerIndex];
            }

            foreach (Transform trans in gameObject.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = gameObject.layer;
            }

            Transform cameraTransform = m_viewport.Camera.transform;
            m_pivot.LookAt(
                transform.position + cameraTransform.rotation * Vector3.forward,
                cameraTransform.rotation * Vector3.up);
        }

        private void Update()
        {
            Transform cameraTransform = m_viewport.Camera.transform;
            m_pivot.LookAt(
                transform.position + cameraTransform.rotation * Vector3.forward,
                cameraTransform.rotation * Vector3.up);
        }

        public virtual void UpdateProgress(bool animate, float progress)
        {
            bool prevAnimate = m_progress.Animate;
            m_progress.Animate = animate;
            m_progress.Progress = progress;
            m_progress.Animate = prevAnimate;
        }

     
    }
}


