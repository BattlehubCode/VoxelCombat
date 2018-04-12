using UnityEngine;
using UnityEngine.EventSystems;

namespace Battlehub.VoxelCombat
{
    public class VoxelUIBaseInputOverride : BaseInput
    {
        private IVoxelInputManager m_inputManager;

        private VoxelUIInputProvider m_inputProvider; 

        protected override void Awake()
        {
            m_inputProvider = GetComponent<VoxelUIInputProvider>();
            base.Awake();
        }

        public override float GetAxisRaw(string axisName)
        {
            return m_inputProvider.GetAxisRaw(axisName);
        }

        public override bool GetMouseButton(int button)
        {
            return m_inputProvider.GetMouseButton(button);
        }

        public override bool GetButtonDown(string buttonName)
        {
            return m_inputProvider.GetButtonDown(buttonName);
        }

        public override bool GetMouseButtonUp(int button)
        {
            return m_inputProvider.GetMouseButtonUp(button);
        }

        public override bool GetMouseButtonDown(int button)
        {
            return m_inputProvider.GetMouseButtonDown(button);
        }

        public override bool mousePresent
        {
            get { return m_inputProvider.IsMousePresent; }
        }

        public override Vector2 mousePosition
        {
            get { return m_inputProvider.MousePosition;  }
        }

    }
}

