using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface INotification
    {
        void Show(string text, GameObject selectOnClose = null);

        void Close();

        void ShowError(string error, GameObject selectOnClose = null);

        void ShowError(Error error, GameObject selectOnClose = null);

        INotification GetChild(int index);

    }
}