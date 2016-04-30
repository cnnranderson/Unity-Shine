using UnityEngine;

public class FPSCounter : MonoBehaviour {

	public int frameRange = 60;
	public int MaxFPS { get; private set; }
	public int AvgFPS { get; private set; }
	public int MinFPS { get; private set; }

	int[] fpsBuffer;
	int fpsBufferIndex;

	void Update() {
		if (fpsBuffer == null || fpsBuffer.Length != frameRange) {
			InitializeBuffer ();
		}
		UpdateBuffer ();
		CalculateFPS ();
	}

	void InitializeBuffer() {
		if (frameRange <= 0) {
			frameRange = 1;
		}
		fpsBuffer = new int[frameRange];
		fpsBufferIndex = 0;
	}

	void UpdateBuffer() {
		fpsBuffer[fpsBufferIndex++] = (int)(1f / Time.unscaledDeltaTime);
		if (fpsBufferIndex >= frameRange) {
			fpsBufferIndex = 0;
		}
	}

	void CalculateFPS() {
		int sum = 0;
		int high = 0;
		int low = int.MaxValue;
		for (int i = 0; i < fpsBuffer.Length; i++) {
			int fps = fpsBuffer [i];
			sum += fps;
			if (fps > high) {
				high = fps;
			}
			if (fps < low) {
				low = fps;
			}
		}
		AvgFPS = (int)(sum / frameRange);
		MaxFPS = high;
		MinFPS = low;
	}
}
