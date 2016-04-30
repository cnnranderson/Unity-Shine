using UnityEngine;
using System.Collections;

public class RotateTo : MonoBehaviour {

	[Range(0f, 90f)]
	public float rotSpeed = 90f;
	public Transform target;

	private Transform transform;

	// Use this for initialization
	void Awake () {
		this.transform = GetComponent<Transform> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (target != null && Vector3.Distance(target.position, transform.position) < 6f) {
			Quaternion destRot = Quaternion.LookRotation (target.position - transform.position, Vector3.up);
			this.transform.rotation = Quaternion.RotateTowards (this.transform.rotation, destRot, rotSpeed * Time.deltaTime);
		} else {
			this.transform.rotation *= Quaternion.AngleAxis (rotSpeed * Time.deltaTime, Vector3.up);
			this.transform.rotation *= Quaternion.AngleAxis (rotSpeed * Time.deltaTime, Vector3.left);
		}
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.red;
		Gizmos.DrawLine (transform.position, transform.position + transform.forward * Mathf.Min(6f, Vector3.Distance(target.position, transform.position)));
	}
}
