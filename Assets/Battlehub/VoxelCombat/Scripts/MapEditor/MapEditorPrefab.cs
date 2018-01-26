using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class MapEditorPrefab : MonoBehaviour
    {
        private Voxel m_prefab;
        public Voxel Prefab
        {
            get { return m_prefab; }
            set
            {
                m_prefab = value;
                if(m_prefab != null)
                {
                    m_text.text = m_prefab.Root.name;
                }
                else
                {
                    m_text.text = "[NULL]";
                }

            }
        }

        public bool AllowHeightEditing
        {
            get; set;
        }

        private Text m_text;
        private Toggle m_toggle;

        public event System.EventHandler Selected;
        public event System.EventHandler Unselected;    

        public bool IsSelected
        {
            get { return m_toggle.isOn; }
            set { m_toggle.isOn = value; }
        }

        private void Awake()
        {
            m_text = GetComponentInChildren<Text>();
            m_toggle = GetComponentInChildren<Toggle>();

            if (m_toggle != null)
            {
                m_toggle.onValueChanged.AddListener(OnValueChanged);
            }
        }

        private void OnDestroy()
        {
            if(m_toggle != null)
            {
                m_toggle.onValueChanged.RemoveListener(OnValueChanged);
            }
        }


        private void OnValueChanged(bool value)
        {
            if(value)
            {
                if(Selected != null)
                {
                    Selected(this, System.EventArgs.Empty);
                }
            }
            else
            {
                if(Unselected != null)
                {
                    Unselected(this, System.EventArgs.Empty);
                }
            }
        }

    }
}
