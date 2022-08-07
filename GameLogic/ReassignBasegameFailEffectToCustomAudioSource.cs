using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace Shaffuru.GameLogic {
	class ReassignBasegameFailEffectToCustomAudioSource : IInitializable {
		readonly CustomAudioSource customAudioSource = null;
		readonly GameSongController gameSongController = null;

		public ReassignBasegameFailEffectToCustomAudioSource(CustomAudioSource customAudioSource, GameSongController gameSongController) {
			this.customAudioSource = customAudioSource;
			this.gameSongController = gameSongController;
		}

		public void Initialize() => customAudioSource.YoinkFailEffect(gameSongController);
	}
}
