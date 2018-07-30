using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class SettingsPanel : ButtonsPanel
    {
        private IGlobalSettings m_settings;

        protected override void Awake()
        {
            base.Awake();
            m_settings = Dependencies.Settings;
            UpdateButtonState();
        }

        protected override void OnAction(int index)
        {
            base.OnAction(index);
            if(index == 0)
            {
                m_settings.DebugMode = !m_settings.DebugMode;
            }
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            Text text = m_sequence[0].GetComponentInChildren<Text>();
          
            if (m_settings.DebugMode)
            {
                text.text = "Debug Mode On";
            }
            else
            {
                text.text = "Debug Mode Off";
            }
        }
    }
}
