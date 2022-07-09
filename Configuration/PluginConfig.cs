using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace Shaffuru {
	public class SongFilteringConfig {
		public int minSeconds = 15;

		public bool allowME = true;

		public bool enableAdvancedFilters = false;
		public float advanced_njs_min = 0f;
		public float advanced_njs_max = 30f;
		public float advanced_nps_min = 0f;
		public float advanced_nps_max = 30f;
		public int advanced_bpm_min = 0;
		public bool advanced_only_ranked = false;
		public int advanced_uploadDate_min = 0;
	}

	internal class Config {
		public static Config Instance;

		public static readonly List<DateTime> hideOlderThanOptions = BuilDateTimeList();
		static List<DateTime> BuilDateTimeList() {
			var hideOlderThanOptions = new List<DateTime>();

			for(var x = new DateTime(2018, 5, 1); x < DateTime.Now; x = x.AddMonths(1))
				hideOlderThanOptions.Add(x);

			return hideOlderThanOptions;
		}


		public virtual int queue_sizeLimit { get; set; } = 32;
		public virtual int queue_requeueLimit { get; set; } = 32;

		public virtual bool chat_request_enabled { get; set; } = true;
		public virtual bool chat_request_show_name { get; set; } = true;
		//public virtual bool chat_currentmap_enabled { get; set; } = false;
		public virtual bool request_allowDownloading { get; set; } = false;
		public virtual bool request_allowSpecificDiff { get; set; } = false;
		public virtual bool request_allowSpecificTime { get; set; } = false;
		public virtual int request_limitPerUser { get; set; } = 2;


		[NonNullable]
		public SongFilteringConfig songFilteringConfig = new SongFilteringConfig();

		public string filter_playlist = "None (All Songs)";
		public bool filter_playlist_onlyHighlighted = true;

		public virtual bool jumpcut_enabled { get; set; } = false;
		// Maybe at some point. I feel like this would be a massive pain
		//public virtual bool jumpcut_tryKeepParity { get; set; } = false;f
		public virtual float transition_reactionTime { get; set; } = 0.5f;
		public virtual float transition_gracePeriod { get; set; } = 0.4f;
		public virtual int jumpcut_minSeconds { get; set; } = 10;
		public virtual int jumpcut_maxSeconds { get; set; } = 30;

		public virtual bool random_prefer_top_diff { get; set; } = false;

		public virtual int ramclearer_frequency { get; set; } = 25;

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
			if(songFilteringConfig.minSeconds < 15)
				songFilteringConfig.minSeconds = 15;

			if(jumpcut_minSeconds < 5)
				jumpcut_minSeconds = 5;

			if(ramclearer_frequency > 50)
				ramclearer_frequency = 50;
			else if(ramclearer_frequency < 20)
				ramclearer_frequency = 20;

			if(transition_gracePeriod > 1.2f)
				transition_gracePeriod = 1.2f;

			if(transition_reactionTime > 1f)
				transition_reactionTime = 1f;
			else if(transition_reactionTime < 0.3f)
				transition_reactionTime = 0.3f;
		}

		/// <summary>
		/// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
		/// </summary>
		public virtual void CopyFrom(Config other) {
			// This instance's members populated from other
		}
	}
}
