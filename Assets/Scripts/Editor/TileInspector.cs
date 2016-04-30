using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (Tile))]
public class TileInspector : Editor {

	public override void OnInspectorGUI () {
		Tile tile = (Tile)target;

		EditorGUILayout.LabelField ("Coordinates", 
			"(" + tile.x + ", " + tile.y + ")");
		
	}
}
