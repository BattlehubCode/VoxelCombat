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
    }
}



