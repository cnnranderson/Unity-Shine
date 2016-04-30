using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class TileMap : MonoBehaviour {

	public float tileSize = 1.0f;

	public int width = 4;
	public int height = 4;

	public Color defaultColor = Color.white;
	public Color selectionColor = Color.magenta;

	public Tile tilePrefab;
	public Text tileLabelPrefab;

	Canvas mapCanvas;
	TileMesh tileMesh;
	Tile[] tiles;

	void Start() {
		tileMesh.Triangulate (tiles);
	}

	void Awake () {
		//mapCanvas = GetComponentInChildren<Canvas> ();
		tileMesh = GetComponentInChildren<TileMesh> ();

		tiles = new Tile[width * height];

		for (int y = 0, i = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				CreateTile (x, y, i++);
			}
		}
	}

	void CreateTile(int x, int y, int i) {
		Vector3 pos;
		pos.x = x * tileSize;
		pos.y = y * tileSize;
		pos.z = 2f;

		Tile tile = tiles [i] = Instantiate<Tile> (tilePrefab);
		tile.color = defaultColor;
		tile.x = x;
		tile.y = y;
		tile.transform.SetParent (transform, false);
		tile.transform.localPosition = pos;

//		Text label = Instantiate<Text> (tileLabelPrefab);
//		label.rectTransform.SetParent (mapCanvas.transform, false);
//		label.rectTransform.anchoredPosition = new Vector2 (pos.x, pos.y);
//		label.text = x.ToString () + "\n" + y.ToString ();
	}

	void Update() {
		if (Input.GetMouseButton (0)) {
			HandleInput ();
		} 
	}

	void HandleInput() {
		Ray inputRay = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast (inputRay, out hit)) {
			TouchTile (hit.point);
		}
	}

	void TouchTile(Vector3 position) {
		position = FromPosition (position);
		int index = (int)(position.x + position.y * width);
		Tile tile = tiles [index];
		tile.color = selectionColor;
		tileMesh.Triangulate (tiles);
		Debug.Log ("Touched at: (" + position.x.ToString() + ", " + position.y.ToString() + ")");
	}

	public static Vector3 FromPosition(Vector3 position) {
		position.x = Mathf.FloorToInt(position.x);
		position.y = Mathf.FloorToInt(position.y);
		return position;
	}

	public void Regenerate() {
		//Start ();
	}
}
