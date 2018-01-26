using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class TxtSlider : MonoBehaviour
    {
        [SerializeField]
        private Text m_text;

        [SerializeField]
        private Slider m_slider;

        private void Start()
        {
            m_slider.onValueChanged.AddListener(OnValueChanged);
            m_text.text = m_slider.value.ToString();
        }

        private void OnDestroy()
        {
            if(m_slider != null)
            {
                m_slider.onValueChanged.RemoveListener(OnValueChanged);
            }
        }

        private void OnValueChanged(float value)
        {
            m_text.text = value.ToString();
        }

    }

}

