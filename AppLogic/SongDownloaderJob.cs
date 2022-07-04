﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Shaffuru.Util;
using SongCore.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Networking;

namespace Shaffuru.AppLogic {
	public struct SongDownloaderJob : IJob {
		public static readonly HashSet<uint> downloadingMaps = new HashSet<uint>();

		uint beatsaverId;
		public bool succeeded;

		public SongDownloaderJob(uint beatsaverId) {
			this.beatsaverId = beatsaverId;
			succeeded = false;

			downloadingMaps.Add(beatsaverId);
		}

		static readonly IPA.Utilities.FieldAccessor<BeatmapLevelsModel, Dictionary<string, IPreviewBeatmapLevel>>.Accessor BeatmapLevelsModel_loadedPreviewBeatmapLevels =
			IPA.Utilities.FieldAccessor<BeatmapLevelsModel, Dictionary<string, IPreviewBeatmapLevel>>.GetAccessor("_loadedPreviewBeatmapLevels");

		static readonly MethodInfo SongCore_LoadSongAndAddToDictionaries = IPA.Loader.PluginManager.GetPluginFromId("SongCore")?
			.Assembly.GetType("SongCore.Loader")?
			.GetMethod("LoadSongAndAddToDictionaries", BindingFlags.NonPublic | BindingFlags.Instance);

		static readonly string UserAgent = "Shaffuru/" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
		public unsafe void Execute() {
			try {
				if(!SongDetailsUtil.instance.songs.FindByMapId(beatsaverId, out var song))
					return;

				var downloadUrl = $"https://r2cdn.beatsaver.com/{song.hash.ToLowerInvariant()}.zip";
				var folderName = string.Concat($"{song.key} ({song.songName} - {song.levelAuthorName})".Split(Path.GetInvalidFileNameChars())).Trim();

				using(var m = DownloadMap(downloadUrl)) {
					if(m == null)
						return;

					var p = SaveMap(m, folderName);

					if(p == null)
						return;

					var data = SongCore.Loader.Instance.LoadCustomLevelSongData(p);

					var preview = (CustomPreviewBeatmapLevel)SongCore_LoadSongAndAddToDictionaries.Invoke(SongCore.Loader.Instance, new object[] {
						CancellationToken.None, data, p, null
					});

					var hash = MapPool.GetHashOfPreview(preview);

					var x = SongCore.Loader.BeatmapLevelsModelSO;
					// Adding it to this Dict so that beatmapLevelsModel.GetBeatmapLevelAsync can load this custom beatmap
					BeatmapLevelsModel_loadedPreviewBeatmapLevels(ref x)[$"custom_level_{hash}"] = preview;

					succeeded = MapPool.instance.AddRequestableLevel(preview, true);
				}
			} finally {
				downloadingMaps.Remove(beatsaverId);
			}
		}

		DownloadHandlerBuffer DownloadMap(string downloadUrl) {
			using(var www = UnityWebRequest.Get(downloadUrl)) {
				var b = new DownloadHandlerBuffer();

				www.SetRequestHeader("User-Agent", UserAgent);
				www.timeout = 20;
				www.downloadHandler = b;
				www.disposeDownloadHandlerOnDispose = false;

				www.SendWebRequest();

				while(!www.isDone)
					Thread.Sleep(50);

				if(www.isHttpError || www.isNetworkError)
					return null;

				return b;
			}
		}

		unsafe string SaveMap(DownloadHandlerBuffer dlh, string folderName) {
			var files = new Dictionary<string, (NativeArray<byte> ptr, UnmanagedMemoryStream stream)>(4);

			try {
				var longestFileNameLength = 0;

				using(var zipStream = new MemoryStream(dlh.data)) {
					// BetterSongSearch 🙏
					using(var archive = new ZipArchive(zipStream, ZipArchiveMode.Read)) {
						foreach(var entry in archive.Entries) {
							var len = (int)entry.Length;

							// If a file, supposedly, is bigger than that we can assume its malicious
							if(len > 200_000_000)
								throw new InvalidDataException();

							// Dont extract directories / sub-files
							if(entry.FullName.IndexOf("/", StringComparison.Ordinal) == -1) {
								using(var str = entry.Open()) {
									var file = new NativeArray<byte>(len, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
									var x = new UnmanagedMemoryStream((byte*)NativeArrayUnsafeUtility.GetUnsafePtr(file), len, len, FileAccess.ReadWrite);

									str.CopyTo(x);

									x.Position = 0;

									files.Add(entry.Name, (file, x));

									if(entry.Name.Length > longestFileNameLength)
										longestFileNameLength = entry.Name.Length;
								}
							}
						}
					}
				}

				// Failsafe so we dont break songcore. Info.dat, a diff and the song itself - not sure if the cover is needed
				if(files.Count < 3 || !files.Keys.Any(x => x.Equals("info.dat", StringComparison.OrdinalIgnoreCase)))
					throw new InvalidDataException();

				var path = Path.Combine(Directory.GetCurrentDirectory(), CustomLevelPathHelper.customLevelsDirectoryPath, folderName);

				if(path.Length > 253 - longestFileNameLength)
					path = $"{path.Substring(0, 253 - longestFileNameLength - 7)}..";

				if(Directory.Exists(path)) {
					var pathNum = 1;
					while(Directory.Exists(path + $" ({pathNum})"))
						pathNum++;

					path += $" ({pathNum})";
				}

				if(!Directory.Exists(path))
					Directory.CreateDirectory(path);

				foreach(var e in files) {
					var entryPath = Path.Combine(path, e.Key);
					using(var s = File.OpenWrite(entryPath))
						e.Value.stream.CopyTo(s);
				}

				return path;
			} finally {
				foreach(var item in files)
					item.Value.ptr.Dispose();
			}
			return null;
		}
	}
}
