using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shaffuru.AppLogic;
using Shaffuru.HarmonyPatches;
using Zenject;

namespace Shaffuru.GameLogic {
	class BeatmapObjectDissolver : IInitializable, IDisposable {
		BeatmapObjectManager beatmapObjectManager;

		// I am very sorry for this misuse d̵̡͌ont u̴͕̽̇s̸̢̩͗e eveǹ̸̠͕͂̀t̷̻͆̌̚ś̷͈́ this w̴͔̦̥̆̈́̓͑̌a̷̡͊ỹ̴̢͍͚̘̺ nevè̷̙̒̈́̅̉͘͝r̴̨̧̜̳͖̥̓̿͋̒̀́̚ͅ ̸̗̩̣̈͋͌́͊̌̚ṕ̶͇̼͔̼͔̤͊͘l̵̢̺̻̻̪̟̽̓̾͌͠ease Ỏ̷͚͖̿̂̀̎Ḧ̶̤̮̫̱̖̭̟͔̰̖́̓̈́͆̍̚ ̶̞̤̪͕̱̻̳͍͒̑̽̿͂̂Ģ̶̧̛̱̯͊̑̍̓͒́̀̿͗͝O̴̘̙̔̌̓͗̀̂̓̎̉̋̀͝ͅD̴̲̼̯̿ ̸̧̙̖͙͔͔͖̹̈́̍̐̽͗̇͝͠T̸̡͎͉̠͖͈́͊̇Ĥ̷̢͂̅͐̊̈́̈́̏͋̌͘E̴̢̩̥͉͂͆̍̉̀́̌̌͒̃̀͠͝Y̶͖̲͚̿͋̐̃̇̋̈́̚͜ Ȃ̷͎̣̭̻̩̺̗̯̮̭̱͚̲̉̂̌ͅR̵̢̢̬͇̫͔̫̟̥̟̰̺͂̈́͜͝Ȩ̵̧͕̙̤̤͎̃͊̀̿̍̔̕͝ ̸̡̺̞̖̠̼̬̗͈͉̫̤̪͔̌̈́C̴̛̘͇͉̭̟͍̺̱̹͉̤̠̆̌̉̊̏̕͠͝O̶̡̝̰̗̗͔̿M̵̦͉̭̼̻̳̺̙̩̓̐̐̐̑̍͐̈͘͝I̵̦͉̟̝̺̳̙̬̭̱͚͖̥̣͕͓͍̒͌́̊̓N̸̡̛̰̞͍̩̝͖̠̠͈̂̀̐́̃̌̾͊̎͗̇̀̕͠Ğ̸̩͖̠͎̻́̇̾͌͑́̑̕̕͜
		static event Action<float> doDissolve;

		public static void DissolveAllAndEverything(float dissolveTime) {
			doDissolve?.Invoke(dissolveTime);
		}

		public void DissolveAll(float dissolveTime) {
			// The old maps objects which are dissolving keep moving with the NJS they have now
			freezeDissolvingNjs?.Freeze(beatmapObjectManager._allBeatmapObjects);

			foreach(var boc in beatmapObjectManager._allBeatmapObjects)
				boc.Dissolve(dissolveTime);
		}

		readonly FreezeDissolvingNjs freezeDissolvingNjs;

		public BeatmapObjectDissolver(BeatmapObjectManager beatmapObjectManager, [InjectOptional] FreezeDissolvingNjs freezeDissolvingNjs) {
			this.beatmapObjectManager = beatmapObjectManager;
			this.freezeDissolvingNjs = freezeDissolvingNjs;
		}

		public void Initialize() {
			doDissolve += this.DissolveAll;
		}

		public void Dispose() {
			doDissolve -= this.DissolveAll;
		}
	}
}
