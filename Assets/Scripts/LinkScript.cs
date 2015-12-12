using UnityEngine;
using System.Collections;

public class LinkScript : MonoBehaviour {

	private Animator anim;

	public Vector3 startPosition;
	public ArrayList keys = new ArrayList();
	public float speed = 3.35f;

	// Use this for initialization
	void Start () {
		startPosition = transform.position;
		anim = GetComponent<Animator> ();
	}

	void Update() {

		float inputX = Input.GetAxisRaw ("Horizontal");
		float inputY = Input.GetAxisRaw ("Vertical");

		if (inputX != 0 || inputY != 0) {
			anim.SetBool ("isWalking", true);
			if (inputX > 0) {
				anim.SetFloat("x", 1);
			} else if (inputX < 0) {
				anim.SetFloat ("x", -1);
			} else {
				anim.SetFloat ("x", 0);
			}

			if (inputY > 0) {
				anim.SetFloat("y", 1);
			} else if (inputY < 0) {
				anim.SetFloat("y", -1);
			} else {
				anim.SetFloat ("y", 0);
			}
		} else {
			anim.SetBool ("isWalking", false);
		}

		Vector3 movement = new Vector3 (
			inputX,
			inputY,
			0f);
		movement = movement.normalized;
		movement *= Time.deltaTime * speed;

		transform.position += (movement);
	}
}
