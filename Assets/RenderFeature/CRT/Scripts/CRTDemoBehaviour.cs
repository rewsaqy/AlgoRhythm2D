using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrewedInk.CRT
{
	public class CRTDemoBehaviour : MonoBehaviour
	{
		[Header("Scene References")]
		public CRTCameraBehaviour crtCamera;
		public List<Transform> spin;
		
		[Header("Asset References")]
		public CRTDataObject[] demoValues;
		
		[Header("Runtime data")]
		public int currentDemoIndex;

		[ContextMenu("Next Demo")]
		public void GotoNextDemo()
		{
			var curr = demoValues[currentDemoIndex];
			currentDemoIndex = (currentDemoIndex + 1) % demoValues.Length;
			var next = demoValues[currentDemoIndex];
			
			float duration = 1;
			IEnumerator Animation()
			{
				crtCamera.data = curr.data;
				var startTime = Time.realtimeSinceStartup;
				var endTime = startTime + duration;

				while (Time.realtimeSinceStartup < endTime)
				{
					var t = 1 - ((endTime - Time.realtimeSinceStartup) / duration);
					var x = CRTData.Lerp(curr.data, next.data, t);
					crtCamera.data = x;
					yield return null;
				}

				crtCamera.data = next.data;
			}
			StartCoroutine(Animation());
		}
		
		[ContextMenu("Zoom in!")]
		public void ZoomIn()
		{

			float duration = 2;
			float startZoom = 2;
			float endZoom = 1.1f;
			IEnumerator Animation()
			{
				crtCamera.data.zoom = startZoom;
				var startTime = Time.realtimeSinceStartup;
				var endTime = startTime + duration;

				while (Time.realtimeSinceStartup < endTime)
				{
					var t = 1 - ((endTime - Time.realtimeSinceStartup) / duration);
					var x = Mathf.Lerp(startZoom, endZoom, t);
					crtCamera.data.zoom = x;
					yield return null;
				}

				crtCamera.data.zoom = endZoom;
			}
			StartCoroutine(Animation());
		}

		private void Start()
		{
			if (crtCamera == null)
			{
				Debug.LogWarning($"The {nameof(crtCamera)} field hasn't been assigned! Null references will likely abound in short order... :(" );
			}
			ZoomIn();
		}

		private void Update()
		{
#if ENABLE_INPUT_SYSTEM
			var keyboard = UnityEngine.InputSystem.Keyboard.current;
			if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
			{
				GotoNextDemo();
			}
#elif ENABLE_LEGACY_INPUT_MANAGER
			if (Input.GetKeyDown(KeyCode.Space))
			{
				GotoNextDemo();
			}
#endif

			foreach (var s in spin)
			{
				s.Rotate(10 * Time.deltaTime, 0, 0, Space.Self);
				// var curr = s.localRotation.eulerAngles;
				// s.localRotation = Quaternion.Euler(curr.x + 10 * Time.deltaTime, curr.y, curr.z);
			}
		}
	}
}
