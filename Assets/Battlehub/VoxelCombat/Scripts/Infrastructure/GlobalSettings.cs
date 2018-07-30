using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    [Serializable]
    public class PlayerCamCtrlSettings
    {
        public float RotateSensitivity = 100.0f;
        public float MoveSensitivity = 1.0f;
        public float CursorSensitivity = 15.0f;
        public float ZoomSensitivity = 1.0f;

        public float MinCamDistance = 10.0f;
        public float MaxCamDistance = 20.0f;
        public Vector3 ToCamVector = new Vector3(1, 2, 1);// Vector3.one;
    }

    public interface IGlobalSettings
    {
        event Action DisableFogOfWarChanged;
        event Action DebugModeChanged;

        string GameServerUrl
        {
            get;
        }

        string ServerUrl
        {
            get;
        }

        string MatchServerUrl
        {
            get;
            set;
        }

        bool DebugMode
        {
            get;
            set;
        }

        bool DisableFogOfWar
        {
            get;
            set;
        }

        Guid ClientId
        {
            get; 
        }

        PlayerCamCtrlSettings[] PlayerCamCtrl
        {
            get;
        }
    }

    public class GlobalSettings : MonoBehaviour, IGlobalSettings
    {
        public event Action DisableFogOfWarChanged;
        public event Action DebugModeChanged;

        [SerializeField]
        private string m_serverUrl = "ws://localhost:7777";

        public string GameServerUrl
        {
            get { return m_serverUrl + "/GameServer.ashx"; }
        }

        public string ServerUrl
        {
            get { return m_serverUrl; }
        }

        public string MatchServerUrl
        {
            get { return Dependencies.State.GetValue<string>("Battlehub.VoxelCombat.MatchServerUrl"); }
            set { Dependencies.State.SetValue("Battlehub.VoxelCombat.MatchServerUrl", value); }
        }

        public bool DebugMode
        {
            get { return PlayerPrefs.GetInt("Battlehub.VoxelCombat.DebugMode", 0) == 1; }
            set
            {
                PlayerPrefs.SetInt("Battlehub.VoxelCombat.DebugMode", value ? 1 : 0);
                DisableFogOfWar = value;
                if(DebugModeChanged != null)
                {
                    DebugModeChanged();
                }
            }
        }

        private bool m_disableFogOfWar = true;
        public bool DisableFogOfWar
        {
            get { return m_disableFogOfWar; }
            set
            {
                m_disableFogOfWar = value;
                if(DisableFogOfWarChanged != null)
                {
                    DisableFogOfWarChanged();
                }
            }
        }

        private static Guid m_clientId = Guid.NewGuid();
        public Guid ClientId
        {
            get { return m_clientId; }
        }

        [SerializeField]
        private PlayerCamCtrlSettings[] m_playerCamCtrl;
        public PlayerCamCtrlSettings[] PlayerCamCtrl
        {
            get { return m_playerCamCtrl; }
        }

        protected void Awake()
        {
            DebugMode = DebugMode;
        }
    }
}



