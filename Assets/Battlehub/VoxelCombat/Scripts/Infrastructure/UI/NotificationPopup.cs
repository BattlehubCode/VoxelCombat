using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface INotification
    {
        void ShowError(string error, GameObject selectOnClose = null);
        void ShowError(Error error, GameObject selectOnClose = null);
        void Close();

        INotification GetChild(int index);
    }

    public class NotificationPopup : MonoBehaviour, INotification
    {
        private IConsole m_console;

        private void Awake()
        {
            m_console = Dependencies.Console;
        }

        public void ShowError(string error, GameObject selectOnClose = null)
        {
            Debug.LogError(error);

            m_console.Echo(error);

            //TODO: GUI
        }

        public void Close()
        {

        }

        public void ShowError(Error error, GameObject selectOnClose = null)
        {
            ShowError(StatusCode.ToString(error.Code) + " " + error.Message, selectOnClose);
        }

        public INotification GetChild(int index)
        {
            throw new System.NotImplementedException("Use Notification instead");
        }
    }

}
