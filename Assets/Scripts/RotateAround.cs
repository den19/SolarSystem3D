using UnityEngine;
using System.Collections;

public class RotateAround : MonoBehaviour {

    [Tooltip("This is the object that the script's game object will rotate around")]
	public Transform target; // the object to rotate around
    [Tooltip("This is the speed at which the object rotates")]
	public int speed; // the speed of rotation
	
	void Start() {
		if (target == null) 
		{
			target = this.gameObject.transform;
			Debug.Log ("RotateAround target not specified. Defaulting to this GameObject");
		}
	}

	// Update is called once per frame
	void Update () {
		// Time Machine drives orbital placement; keep self-spin (target == this) running.
		if (SolarSystemApp.SimulationClock.DrivesMotion
			&& target != null
			&& target != transform)
			return;

		// RotateAround takes three arguments, first is the Vector to rotate around
		// second is a vector that axis to rotate around
		// third is the degrees to rotate, in this case the speed per second
		transform.RotateAround(target.transform.position,target.transform.up,speed * Time.deltaTime);
	}
}
