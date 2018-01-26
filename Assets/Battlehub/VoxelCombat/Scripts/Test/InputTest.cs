using UnityEngine;

using Battlehub.VoxelCombat;
public class InputTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		if(Dependencies.InputManager.GetButton(InputAction.X, 0))
        {
            Debug.Log("Action4");
        }
	}
}
