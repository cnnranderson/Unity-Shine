using UnityEngine;
using System.Collections;

public class FollowCam : MonoBehaviour {

	private GameObject target;

	public float cameraSpeed = 10.0f;

	void Start () {
		target = GameObject.FindWithTag("Player");
	}

	void Update () {
		if (target != null) {
			Vector3 targetLocation = target.transform.position;
			targetLocation.z = -5; 
			transform.position = Vector3.Lerp (transform.position, targetLocation, cameraSpeed * Time.deltaTime);
		}
	}
}
