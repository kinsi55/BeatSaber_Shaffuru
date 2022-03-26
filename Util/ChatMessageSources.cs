using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatCore;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Twitch.Interfaces;
using ChatCore;
using ChatCore.Interfaces;
using ChatCore.Services.Twitch;

namespace Shaffuru.Util {
	public interface IChatMessageSource : IDisposable {
		delegate void IncomingChatMessage(string name, string message, object channel = null);

		public event IncomingChatMessage OnTextMessageReceived;

		public void SendChatMessage(string message, object channel = null);
	}


	class ChatCoreSource : IChatMessageSource {
		public event IChatMessageSource.IncomingChatMessage OnTextMessageReceived;

		static ChatCoreInstance chatCore;
		static TwitchService twitch;

		ChatCoreSource() {
			chatCore ??= ChatCoreInstance.Create();

			twitch ??= chatCore.RunTwitchServices();

			twitch.OnTextMessageReceived += Twitch_OnTextMessageReceived;
		}

		private void Twitch_OnTextMessageReceived(IChatService _, IChatMessage msg) {
			OnTextMessageReceived?.Invoke(msg.Sender.UserName, msg.Message, msg.Channel);
		}

		public void SendChatMessage(string message, object channel) {
			twitch.SendTextMessage(message, (IChatChannel)channel);
		}

		~ChatCoreSource() => Dispose();
		public void Dispose() => twitch.OnTextMessageReceived -= Twitch_OnTextMessageReceived;
	}

	class CatCoreSource : IChatMessageSource {
		public event IChatMessageSource.IncomingChatMessage OnTextMessageReceived;

		static CatCoreInstance catCore;
		static ITwitchService twitch;

		CatCoreSource() {
			catCore ??= CatCoreInstance.Create();

			twitch ??= catCore.RunTwitchServices();

			twitch.OnTextMessageReceived += Twitch_OnTextMessageReceived;
		}

		private void Twitch_OnTextMessageReceived(ITwitchService _, TwitchMessage msg) {
			OnTextMessageReceived?.Invoke(msg.Sender.UserName, msg.Message, msg.Channel);
		}

		public void SendChatMessage(string message, object channel) {
			((TwitchChannel)channel).SendMessage(message);
		}

		~CatCoreSource() => Dispose();
		public void Dispose() => twitch.OnTextMessageReceived -= Twitch_OnTextMessageReceived;
	}

	class BeatSaberPlusSource : IChatMessageSource {
		public event IChatMessageSource.IncomingChatMessage OnTextMessageReceived;

		private BeatSaberPlus.SDK.Chat.Services.ChatServiceMultiplexer multiplexer;

		BeatSaberPlusSource() {
			BeatSaberPlus.SDK.Chat.Service.Acquire();
			multiplexer = BeatSaberPlus.SDK.Chat.Service.Multiplexer;
			multiplexer.OnTextMessageReceived += Twitch_OnTextMessageReceived;
		}

		private void Twitch_OnTextMessageReceived(BeatSaberPlus.SDK.Chat.Interfaces.IChatService _, BeatSaberPlus.SDK.Chat.Interfaces.IChatMessage msg) {
			OnTextMessageReceived?.Invoke(msg.Sender.UserName, msg.Message, msg.Channel);
		}

		public void SendChatMessage(string message, object channel) {
			multiplexer.SendTextMessage((BeatSaberPlus.SDK.Chat.Interfaces.IChatChannel)channel, message);
		}

		~BeatSaberPlusSource() => Dispose();
		public void Dispose() => multiplexer.OnTextMessageReceived -= Twitch_OnTextMessageReceived;
	}
}
