using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Shaffuru.GameLogic {
	class RamCleaner {
		public static readonly RamCleaner instance = new RamCleaner();

		static int cleanSkips = 0;

		const string clearingText = "Clearing RAM so your PC\ndoesn't explode <color=#FC5>😁</color>\n\nLag right now is normal <color=#FC5>👍</color>\n";

		readonly GameObject cleanInfoText;
		readonly TextMeshPro cleanLabel;

		public RamCleaner() {
			cleanInfoText = new GameObject($"Label", typeof(Canvas), typeof(TextMeshPro));

			cleanLabel = cleanInfoText.GetComponent<TextMeshPro>();
			cleanLabel.richText = true;
			cleanLabel.text = "";
			cleanLabel.fontSize = 3.5f;
			cleanLabel.alignment = TextAlignmentOptions.Center;
			cleanLabel.color = Color.magenta;

			((RectTransform)cleanInfoText.transform).position = new Vector3(0, 1.75f, 4f);

			cleanInfoText.SetActive(false);
		}

		public bool TrySkip() {
			// TODO: Maybe in addition to requiring at least N maps to have been played, check if free system memory is actually low
			return cleanSkips++ < Config.Instance.ramclearer_frequency * 2;
		}

		internal static void AddWeight(int weight) {
			cleanSkips += weight;
		}

		public IEnumerator ClearRam() {
			yield return null;
			cleanSkips = 0;

			var oldMode = UnityEngine.Scripting.GarbageCollector.GCMode;
			cleanInfoText.SetActive(true);

			cleanLabel.text = clearingText + "0.00%";
			/*
			 * I have no idea why I need to do it 8 times, but It is always the 7-8th
			 * time when the memory is ACTUALLY freed
			 */
			for(var i = 1; i <= 8; i++) {
				yield return new WaitForSeconds(0.2f);
				UnityEngine.Scripting.GarbageCollector.GCMode = UnityEngine.Scripting.GarbageCollector.Mode.Enabled;
				GC.Collect();
				if(i == 8) {
					GC.WaitForPendingFinalizers();
					GC.Collect();
				}
				cleanLabel.text = clearingText + (i / 8f).ToString("0.00%");
			}
			UnityEngine.Scripting.GarbageCollector.GCMode = oldMode;
			cleanInfoText.SetActive(false);
		}
	}
}
