using UnityEngine;
using System.Collections;

public class FollowCam : MonoBehaviour {

	private GameObject target;

	public float cameraSpeed = 4.0f;

	// Use this for initialization
	void Start () {
		target = GameObject.FindWithTag("Player");
	}
	
	// Update is called once per frame
	void Update () {
		if (target != null) {
			Vector3 targetLocation = target.transform.position;
			targetLocation.z = -10;
			transform.position = Vector3.Lerp (transform.position, targetLocation, cameraSpeed * Time.deltaTime);
		}
	}
}
