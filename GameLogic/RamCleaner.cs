using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Zenject;

namespace Shaffuru.GameLogic {
	class RamCleaner {
		public static readonly RamCleaner instance = new RamCleaner();

		static int cleanSkips = 0;

		const string clearingText = "Clearing RAM so your PC\ndoesn't explode <color=#FC5>😁</color>\n\nLag right now is normal <color=#FC5>👍</color>\n";

		GameObject cleanInfoText;
		TextMeshPro cleanLabel;

		void CreateLabelIfNeededAndEnableIt() {
			if(cleanInfoText == null) {
				cleanInfoText = new GameObject($"Label", typeof(Canvas), typeof(TextMeshPro));

				cleanLabel = cleanInfoText.GetComponent<TextMeshPro>();
				cleanLabel.richText = true;
				cleanLabel.text = "";
				cleanLabel.fontSize = 3.5f;
				cleanLabel.alignment = TextAlignmentOptions.Center;
				cleanLabel.color = Color.magenta;

				((RectTransform)cleanInfoText.transform).position = new Vector3(0, 1.75f, 4f);
			}

			cleanInfoText.SetActive(true);
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
			CreateLabelIfNeededAndEnableIt();

			cleanLabel.text = clearingText + "0.00%";
			const int clearAmount = 2;
			for(var i = 1; i <= clearAmount; i++) {
				yield return new WaitForSeconds(0.2f);
				UnityEngine.Scripting.GarbageCollector.GCMode = UnityEngine.Scripting.GarbageCollector.Mode.Enabled;
				GC.Collect();
				if(i == clearAmount) {
					GC.WaitForPendingFinalizers();
					GC.Collect();
				}
				cleanLabel.text = clearingText + (i / (float)clearAmount).ToString("0.00%");
			}
			UnityEngine.Scripting.GarbageCollector.GCMode = oldMode;
			cleanInfoText.SetActive(false);
		}
	}
}
