using IPA.Config.Stores;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace Shaffuru {
	internal class Config {
		public static Config Instance { get; set; }

		public virtual int queue_sizeLimit { get; set; } = 32;
		public virtual int queue_requeueLimit { get; set; } = 32;


		public static readonly List<DateTime> hideOlderThanOptions = BuilDateTimeList();
		static List<DateTime> BuilDateTimeList() {
			var hideOlderThanOptions = new List<DateTime>();

			for(var x = new DateTime(2018, 5, 1); x < DateTime.Now; x = x.AddMonths(1))
				hideOlderThanOptions.Add(x);

			return hideOlderThanOptions;
		}


		public virtual bool chat_request_enabled { get; set; } = true;
		//public virtual bool chat_currentmap_enabled { get; set; } = false;
		public virtual bool request_allowSpecificDiff { get; set; } = false;
		public virtual bool request_allowSpecificTime { get; set; } = false;
		public virtual int request_limitPerUser { get; set; } = 2;

		public virtual string filter_playlist { get; set; } = "None (All Songs)";
		public virtual bool filter_playlist_onlyHighlighted { get; set; } = true;
		public virtual int filter_minSeconds { get; set; } = 15;

		public virtual bool filter_AllowME { get; set; } = true;

		public virtual bool filter_enableAdvancedFilters { get; set; } = false;
		public virtual float filter_advanced_njs_min { get; set; } = 0f;
		public virtual float filter_advanced_njs_max { get; set; } = 30f;
		public virtual float filter_advanced_nps_min { get; set; } = 0f;
		public virtual float filter_advanced_nps_max { get; set; } = 30f;
		public virtual int filter_advanced_bpm_min { get; set; } = 0;
		public virtual bool filter_advanced_only_ranked { get; set; } = false;
		public virtual int filter_advanced_uploadDate_min { get; set; } = 0;


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
			if(filter_minSeconds < 15)
				filter_minSeconds = 15;

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
