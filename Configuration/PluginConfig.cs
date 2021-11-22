using IPA.Config.Stores;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace Shaffuru {
	internal class Config {
		public static Config Instance { get; set; }

		public virtual int queue_sizeLimit { get; set; } = 32;
		public virtual int queue_requeueLimit { get; set; } = 32;
		public virtual bool queue_pickRandomSongIfEmpty { get; set; } = true;



		public virtual bool chat_request_enabled { get; set; } = true;
		public virtual bool chat_currentmap_enabled { get; set; } = false;
		public virtual bool request_allowSpecificDiff { get; set; } = false;
		public virtual bool request_allowSpecificTime { get; set; } = false;
		public virtual int request_limitPerUser { get; set; } = 2;

		public virtual string filter_playlist { get; set; } = "None (All Songs)";
		public virtual bool filter_playlist_onlyHighlighted { get; set; } = true;
		public virtual int filter_minSeconds { get; set; } = 10;

		public virtual bool jumpcut_enabled { get; set; } = true;
		// Maybe at some point. I feel like this would be a massive pain
		//public virtual bool jumpcut_tryKeepParity { get; set; } = false;f
		public virtual float jumpcut_reactionTime { get; set; } = 0.5f;
		public virtual int jumpcut_minSeconds { get; set; } = 10;
		public virtual int jumpcut_maxSeconds { get; set; } = 30;
		public virtual float jumpcut_gracePeriod { get; set; } = 0.0f;

		public virtual bool random_prefer_top_diff { get; set; } = false;

		/// <summary>
		/// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
		/// </summary>
		public virtual void OnReload() {
			// Do stuff after config is read from disk.
		}

		/// <summary>
		/// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
		/// </summary>
		public virtual void Changed() {
			if(filter_minSeconds < 10)
				filter_minSeconds = 10;

			if(jumpcut_minSeconds < 5)
				jumpcut_minSeconds = 5;

			if(jumpcut_gracePeriod > 1)
				jumpcut_minSeconds = 1;

			if(jumpcut_reactionTime > 1f)
				jumpcut_reactionTime = 1f;
			else if(jumpcut_reactionTime < 0.3f)
				jumpcut_reactionTime = 0.3f;
		}

		/// <summary>
		/// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
		/// </summary>
		public virtual void CopyFrom(Config other) {
			// This instance's members populated from other
		}
	}
}
