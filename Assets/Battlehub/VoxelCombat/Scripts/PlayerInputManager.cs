using UnityEngine;


namespace Battlehub.VoxelCombat
{
    public interface IPlayerInputManager
    {

    }

    //This is wrapper around input manager. This class will have extra state which will depend on current selection, current active region (minimap, cmd buttons) and input method keyboard/mouse, touch, gamepad
    public class PlayerInputManager : MonoBehaviour, IPlayerInputManager
    {
        
    }

}
