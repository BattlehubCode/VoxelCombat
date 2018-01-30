using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface INotification
    {
        void Show(string text, GameObject selectOnClose = null);

        void ShowWithAction(string text, System.Action onClose = null);

        void Close();

        void ShowError(string error, GameObject selectOnClose = null);

        void ShowError(Error error, GameObject selectOnClose = null);

        void ShowErrorWithAction(string error, System.Action onClose = null);

        void ShowErrorWithAction(Error error, System.Action onClose = null);

        INotification GetChild(int index);
    }
}