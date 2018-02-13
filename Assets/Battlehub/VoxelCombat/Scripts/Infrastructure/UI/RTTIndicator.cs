using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class RTTIndicator : MonoBehaviour
    {
        [SerializeField]
        private Text m_rtt;
        [SerializeField]
        private Text m_rttMax;
        private IMatchEngineCli m_matchEngine;

        private void Awake()
        {
            m_matchEngine = Dependencies.MatchEngine;
        }

        private void OnEnable()
        {
            m_matchEngine.Ping += OnPing;
        }

        private void OnDisable()
        {
            if(m_matchEngine != null)
            {
                m_matchEngine.Ping -= OnPing;
            }
        }

        private void OnPing(Error error, RTTInfo payload)
        {
            m_rtt.text = "RTT: " + Mathf.RoundToInt(payload.RTT / 1000.0f) + " ms";
            if(m_rttMax != null)
            {
                m_rttMax.text = "RTT MAX: " + Mathf.RoundToInt(payload.RTTMax / 1000.0f) + " ms";
            }
        }
    }

}
