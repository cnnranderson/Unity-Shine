using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(FPSCounter))]
public class FPSDisplay : MonoBehaviour {

	public Text fpsMaxLabel;
	public Text fpsAvgLabel;
	public Text fpsMinLabel;

	FPSCounter fpsCounter;

	void Awake() {
		fpsCounter = GetComponent<FPSCounter> ();
	}

	void Update() {
		fpsMaxLabel.text = Mathf.Clamp(fpsCounter.MaxFPS, 0, 99).ToString ();
		fpsAvgLabel.text = Mathf.Clamp(fpsCounter.AvgFPS, 0, 99).ToString ();
		fpsMinLabel.text = Mathf.Clamp(fpsCounter.MinFPS, 0, 99).ToString ();
	}
}
