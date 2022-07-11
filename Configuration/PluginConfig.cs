using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Utilities;
using Newtonsoft.Json;
using ProtoBuf;
using Zenject;

namespace Shaffuru {
	[ProtoContract(SkipConstructor = true)]
	class SongFilteringConfig {
		[ProtoMember(1)] public int minSeconds = 15;

		[ProtoMember(2)] public bool allowME = true;

		[ProtoMember(3)] public bool enableAdvancedFilters = false;
		[ProtoMember(4)] public float advanced_njs_min = 0f;
		[ProtoMember(5)] public float advanced_njs_max = 30f;
		[ProtoMember(6)] public float advanced_nps_min = 0f;
		[ProtoMember(7)] public float advanced_nps_max = 30f;
		[ProtoMember(8)] public int advanced_bpm_min = 0;
		[ProtoMember(9)] public bool advanced_only_ranked = false;
		[ProtoMember(10)] public int advanced_uploadDate_min = 0;
	}

	class Config {
		public static Config Instance;

		public static readonly List<DateTime> hideOlderThanOptions = BuilDateTimeList();
		static List<DateTime> BuilDateTimeList() {
			var hideOlderThanOptions = new List<DateTime>();

			for(var x = new DateTime(2018, 5, 1); x < DateTime.Now; x = x.AddMonths(1))
				hideOlderThanOptions.Add(x);

			return hideOlderThanOptions;
		}


		public int queue_sizeLimit = 32;
		public int queue_requeueLimit = 32;

		public bool chat_request_enabled = true;
		public bool chat_request_show_name = true;
		//public bool chat_currentmap_enabled = false;
		public bool request_allowDownloading = false;
		public bool request_allowSpecificDiff = false;
		public bool request_allowSpecificTime = false;
		public int request_limitPerUser = 2;


		public SongFilteringConfig songFilteringConfig = new SongFilteringConfig();

		public string filter_playlist = "None (All Songs)";
		public bool filter_playlist_onlyHighlighted = true;

		public bool jumpcut_enabled = false;
		// Maybe at some point. I feel like this would be a massive pain
		//public bool jumpcut_tryKeepParity = false;f
		public float transition_reactionTime = 0.5f;
		public float transition_gracePeriod = 0.4f;
		public int jumpcut_minSeconds = 10;
		public int jumpcut_maxSeconds = 30;

		public bool random_prefer_top_diff = false;

		public int ramclearer_frequency = 25;


		void Sanitize() {
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

		static readonly string configPath = Path.Combine(UnityGame.UserDataPath, $"{typeof(Config).Namespace}.json");

		static readonly JsonSerializerSettings leanDeserializeSettings = new JsonSerializerSettings {
			NullValueHandling = NullValueHandling.Ignore,
			Error = (se, ev) => {
#if DEBUG
				Plugin.Log.Warn("Failed JSON deserialize:");
				Plugin.Log.Warn(ev.ErrorContext.Error);
#endif
				ev.ErrorContext.Handled = true;
			}
		};

		public void Load() {
			if(File.Exists(configPath)) {
				JsonConvert.PopulateObject(File.ReadAllText(configPath), this, leanDeserializeSettings);

				songFilteringConfig ??= new SongFilteringConfig();

				Sanitize();
			} else {
				songFilteringConfig ??= new SongFilteringConfig();

				Save();
			}
		}

		public void Save() {
			File.WriteAllText(configPath, JsonConvert.SerializeObject(this, Formatting.Indented));
		}

		public Config() {
			Instance = this;
			Load();
		}
	}
}
