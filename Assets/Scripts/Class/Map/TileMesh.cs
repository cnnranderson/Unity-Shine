using UnityEngine;
using System.Collections.Generic;

[RequireComponent (typeof (MeshFilter))]
[RequireComponent (typeof (MeshRenderer))]
public class TileMesh : MonoBehaviour {

	TileMap map;
	Mesh tileMesh;
	MeshCollider meshCollider;
	List<Vector3> vertices;
	List<int> triangles;
	List<Vector2> uvs;
	List<Color> colors;

	private float tUnit = 1f / 16f;

	void Awake () {
		map = GetComponentInParent<TileMap> ();
		GetComponent<MeshFilter> ().mesh = tileMesh = new Mesh ();
		meshCollider = gameObject.AddComponent<MeshCollider> ();
		tileMesh.name = "Tile Mesh";
		vertices = new List<Vector3> ();
		triangles = new List<int> ();
		uvs = new List<Vector2> ();
		colors = new List<Color> ();
	}

	public void Triangulate(Tile[] tiles) {
		tileMesh.Clear ();
		vertices.Clear ();
		triangles.Clear ();
		uvs.Clear ();
		colors.Clear ();

		for (int i = 0; i < tiles.Length; i++) {
			Triangulate (tiles [i]);
		}
		tileMesh.vertices = vertices.ToArray ();
		tileMesh.triangles = triangles.ToArray ();
		tileMesh.colors = colors.ToArray ();
		tileMesh.uv = uvs.ToArray ();
		tileMesh.RecalculateNormals ();

		meshCollider.sharedMesh = tileMesh;
	}

	void Triangulate(Tile tile) {
		Vector3 v1 = tile.transform.localPosition;
		Vector3 v2 = tile.transform.localPosition;
		Vector3 v3 = tile.transform.localPosition;

		// Bottom Triangle
		v2.y += map.tileSize;
		v2.x += map.tileSize;
		v3.x += map.tileSize;
		AddTriangle (v1, v2, v3);
		AddTriangleColor (tile.color);

		// Top Triangle
		v2 = tile.transform.localPosition;
		v2.x = v1.x;
		v2.y += map.tileSize;
		v3.y += map.tileSize;
		AddTriangle (v1, v2, v3);
		AddTriangleColor (tile.color);

		AddUV (tile);
	}

	void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = vertices.Count;
		vertices.Add (v1);
		vertices.Add (v2);
		vertices.Add (v3);
		triangles.Add (vertexIndex);
		triangles.Add (vertexIndex + 1);
		triangles.Add (vertexIndex + 2);
	}

	void AddTriangleColor (Color color) {
		colors.Add (color);
		colors.Add (color);
		colors.Add (color);
	}

	void AddUV(Tile tile) {
		uvs.Add(new Vector2 (tUnit * tile.idx, tUnit * tile.idy));
		uvs.Add(new Vector2 (tUnit * tile.idx + tUnit, tUnit * tile.idy + tUnit));
		uvs.Add(new Vector2 (tUnit * tile.idx + tUnit, tUnit * tile.idy));

		uvs.Add(new Vector2 (tUnit * tile.idx, tUnit * tile.idy));
		uvs.Add(new Vector2 (tUnit * tile.idx, tUnit * tile.idy + tUnit));
		uvs.Add(new Vector2 (tUnit * tile.idx + tUnit, tUnit * tile.idy + tUnit));
	}
}