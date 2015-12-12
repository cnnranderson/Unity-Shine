using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TileMap))]
public class TileMap_Mouse : MonoBehaviour {

	TileMap _tileMap;
	MeshCollider _collider;
	Vector2 _currentSelection;
	MeshRenderer selectorRenderer;
	Color _selectorColor;

	public GameObject selector;

	void Start() {
		_tileMap = GetComponent<TileMap> ();
		_collider = GetComponent<MeshCollider> ();
		selector = GameObject.Find ("Selector");
		selectorRenderer = GameObject.Find ("Highlighter").GetComponent<MeshRenderer> ();
		selector.transform.localScale = new Vector3 (_tileMap.tileSize, _tileMap.tileSize, .1f);
		_selectorColor = selectorRenderer.material.color;
	}

	// Update is called once per frame
	void Update () {
		Ray cameraRay = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hitInfo;

		if (_collider.Raycast (cameraRay, out hitInfo, Mathf.Infinity)) {
			_currentSelection.x = Mathf.FloorToInt (hitInfo.point.x / _tileMap.tileSize);
			_currentSelection.y = Mathf.FloorToInt (hitInfo.point.y / _tileMap.tileSize);
			//Debug.Log ("Tile highlighted: (" + _currentSelection.x + ", " + _currentSelection.y + ")"); 

			selector.transform.position = _currentSelection;
			selector.transform.localScale = new Vector3 (_tileMap.tileSize, _tileMap.tileSize, .1f);
			selectorRenderer.enabled = true;
		} else {
			selectorRenderer.enabled = false;
		}
	}

	public void ResizeSelector() {
		selector.transform.localScale = new Vector3 (_tileMap.tileSize, _tileMap.tileSize, .1f);
		Debug.Log ("Resized! : " + _tileMap.tileSize);
	}
}