using UnityEngine;
using System;

public class ClockAnimator : MonoBehaviour {

	private const float 
		hoursToDeg = 360f / 12f,
		minsToDeg = 360f / 60f,
		secToDeg = 360f / 60f;

	public Transform hour, min, sec;

	private void Update() {
		DateTime time = DateTime.Now;
		hour.localRotation =
			Quaternion.Euler(0f, 0f, time.Hour * -hoursToDeg);
		min.localRotation =
			Quaternion.Euler(0f, 0f, time.Minute * -minsToDeg);
		sec.localRotation =
			Quaternion.Euler(0f, 0f, time.Second * -secToDeg);
	}
		
}
