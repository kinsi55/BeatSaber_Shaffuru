using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shaffuru.AppLogic;
using Zenject;

namespace Shaffuru.GameLogic {
	class BeatmapSwitchSubscriber : IInitializable, IDisposable {
		[Inject] readonly QueueProcessor queueProcessor = null;
		[Inject] readonly BeatmapSwitcher beatmapSwitcher = null;
		int delayMs = 0;

		public BeatmapSwitchSubscriber(int delayMs = 0) {
			this.delayMs = delayMs;
		}

		public void Initialize() {
			queueProcessor.switchedToNewSong += QueueProcessor_switchedToNewSong;
		}

		public void Dispose() {
			queueProcessor.switchedToNewSong -= QueueProcessor_switchedToNewSong;
		}

		private void QueueProcessor_switchedToNewSong(ShaffuruSong song, IDifficultyBeatmap beatmap, IReadonlyBeatmapData beatmapData) {
			void cont() {
				beatmapSwitcher.SwitchToDifferentBeatmap(beatmap, beatmapData, (float)song.startTime, (float)song.length);
			}

			if(delayMs == 0) {
				cont();
				return;
			}

			Task.Delay(delayMs).ContinueWith(_ => cont(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}
	}
}
