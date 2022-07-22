using System;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace Shaffuru.GameLogic {
	class IntroPlayer : IInitializable {
		readonly QueueProcessor queueProcessor;
		readonly BeatmapSwitcher beatmapSwitcher;

		static bool didPlayThisSession = false;
		public IntroPlayer(QueueProcessor queueProcessor, BeatmapSwitcher beatmapSwitcher) {
			this.queueProcessor = queueProcessor;
			this.beatmapSwitcher = beatmapSwitcher;
		}

		public void Initialize() {
			if(didPlayThisSession)
				return;

			if(DateTime.Now.Day != 1 || DateTime.Now.Month != 4)
				return;

			didPlayThisSession = true;

			using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Shaffuru.Assets.triangle")) {
				var bundle = AssetBundle.LoadFromStream(stream);

				var triangle = bundle.LoadAsset<AudioClip>("triangle");
				beatmapSwitcher.customAudioSource.SetClip(triangle);

				queueProcessor.switchToNextBeatmapAt += triangle.length;
				bundle.Unload(false);
			}
		}
	}
}
