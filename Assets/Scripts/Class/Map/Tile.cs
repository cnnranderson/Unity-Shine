using UnityEngine;

public class Tile : MonoBehaviour {

	[SerializeField]
	public int x, y;
	public int layer = 0;

	public Color color;
	public int idx { get; private set; }
	public int idy { get; private set; }
}
