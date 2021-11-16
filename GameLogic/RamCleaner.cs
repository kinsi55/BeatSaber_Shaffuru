using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Shaffuru.GameLogic {
	class RamCleaner {
		int cleanSkips = 0;

		GameObject cleanInfoText;

		public RamCleaner() {
			cleanInfoText = new GameObject($"Label", new[] { typeof(Canvas), typeof(TextMeshPro) });

			var tmp = cleanInfoText.GetComponent<TextMeshPro>();
			tmp.text = "Clearing RAM so your PC\ndoesn't explode 😁";
			tmp.fontSize = 4f;
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.color = Color.magenta;

			((RectTransform)cleanInfoText.transform).position = new Vector3(0, 1.75f, 4f);

			cleanInfoText.SetActive(false);
		}

		public bool TrySkip() {
			return cleanSkips++ < 20;
		}

		public IEnumerator ClearRam() {
			yield return null;
			cleanSkips = 0;

			var oldMode = UnityEngine.Scripting.GarbageCollector.GCMode;
			cleanInfoText.SetActive(true);

			/*
			 * I have no idea why I need to do it 8 times, but It is always the 8th
			 * time when the memory is ACTUALLY cleared
			 */
			for(var i = 0; i < 8; i++) {
				yield return new WaitForSeconds(0.2f);
				UnityEngine.Scripting.GarbageCollector.GCMode = UnityEngine.Scripting.GarbageCollector.Mode.Enabled;
				GC.Collect();
				if(i == 7) {
					GC.WaitForPendingFinalizers();
					GC.Collect();
				}
			}
			UnityEngine.Scripting.GarbageCollector.GCMode = oldMode;
			cleanInfoText.SetActive(false);
		}
	}
}
