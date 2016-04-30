using UnityEngine;

public class NucleonSpawner : MonoBehaviour {

	public float spawnTime;
	public float spawnDistance;
	public Nucleon[] nucleonPrefabs;

	float timeSinceLastSpawn;

	void FixedUpdate() {
		timeSinceLastSpawn += Time.deltaTime;
		if (timeSinceLastSpawn >= spawnTime) {
			timeSinceLastSpawn -= spawnTime;
			SpawnNucleon ();
		}
	}

	void SpawnNucleon() {
		Nucleon prefab = nucleonPrefabs[Random.Range (0, nucleonPrefabs.Length)];
		Nucleon spawn = Instantiate<Nucleon> (prefab);
		spawn.transform.parent = this.transform;
		spawn.transform.localPosition = Random.onUnitSphere * spawnDistance;
	}
}
