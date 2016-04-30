using UnityEngine;
using System.Collections;

public class Fractal : MonoBehaviour {

	public Mesh mesh;
	public Material material;

	public float childScale;

	public int maxDepth;
	private int depth;

	private static Quaternion[] childOrientations = {
		Quaternion.identity,
		Quaternion.Euler(0f, 0f, -90f),
		Quaternion.Euler(0f, 0f,  90f),
		Quaternion.Euler( 90f, 0f, 0f),
		Quaternion.Euler(-90f, 0f, 0f)
	};

	private static Vector3[] childDirections = {
		Vector3.up,
		Vector3.right,
		Vector3.left,
		Vector3.forward,
		Vector3.back
	};

	private void Start() {
		gameObject.AddComponent<MeshFilter> ().mesh = mesh;
		gameObject.AddComponent<MeshRenderer> ().material = material;

		if (depth < maxDepth) {
			StartCoroutine (CreateChildren ());
		}
	}

	private void Update() {
		transform.Rotate (0, 37f * Time.deltaTime, 0f);
	}

	private IEnumerator CreateChildren () {
		for (int i = 0; i < childDirections.Length; i++) {
			yield return new WaitForSeconds (Random.Range (0.1f, 0.5f));
			new GameObject ("Fractal Child").AddComponent<Fractal> ()
				.Initialize (this, i);
		}
	}

	private void Initialize(Fractal parent, int childIndex) {
		this.mesh = parent.mesh;
		this.material = parent.material;
		this.maxDepth = parent.maxDepth;
		this.depth = parent.depth + 1;

		childScale = parent.childScale * .8f;

		transform.parent = parent.transform;
		transform.localScale = Vector3.one * childScale;
		transform.localPosition = childDirections[childIndex] * (0.5f + 0.5f * childScale);
		transform.localRotation = childOrientations[childIndex];
	}
}
