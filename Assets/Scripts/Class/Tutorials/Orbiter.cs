using UnityEngine;

public class Orbiter : MonoBehaviour {

	public Transform pivot;

	private Transform transform;
	private Quaternion destRot = Quaternion.identity;

	public float pivotDist = 5f;
	public float rotSpeed = 10f;

	private float rotZ = 0;

	// Use this for initialization
	void Awake () {
		transform = GetComponent<Transform> ();
	}
	
	// Update is called once per frame
	void Update () {
		rotZ += rotSpeed * Time.deltaTime;

		transform.rotation = Quaternion.Euler (0f, 0f, rotZ);

		transform.position = pivot.position + transform.rotation * Vector3.up * -pivotDist;
	}
}
