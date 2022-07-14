using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shaffuru.AppLogic;
using Zenject;

namespace Shaffuru.GameLogic {
	class BeatmapObjectDissolver : IInitializable, IDisposable {
		BeatmapObjectManager beatmapObjectManager;

		static readonly IPA.Utilities.FieldAccessor<BeatmapObjectManager, List<IBeatmapObjectController>>.Accessor BeatmapObjectManager_allBeatmapObjects =
			IPA.Utilities.FieldAccessor<BeatmapObjectManager, List<IBeatmapObjectController>>.GetAccessor("_allBeatmapObjects");

		// I am very sorry for this misuse d̵̡͌ont u̴͕̽̇s̸̢̩͗e eveǹ̸̠͕͂̀t̷̻͆̌̚ś̷͈́ this w̴͔̦̥̆̈́̓͑̌a̷̡͊ỹ̴̢͍͚̘̺ nevè̷̙̒̈́̅̉͘͝r̴̨̧̜̳͖̥̓̿͋̒̀́̚ͅ ̸̗̩̣̈͋͌́͊̌̚ṕ̶͇̼͔̼͔̤͊͘l̵̢̺̻̻̪̟̽̓̾͌͠ease Ỏ̷͚͖̿̂̀̎Ḧ̶̤̮̫̱̖̭̟͔̰̖́̓̈́͆̍̚ ̶̞̤̪͕̱̻̳͍͒̑̽̿͂̂Ģ̶̧̛̱̯͊̑̍̓͒́̀̿͗͝O̴̘̙̔̌̓͗̀̂̓̎̉̋̀͝ͅD̴̲̼̯̿ ̸̧̙̖͙͔͔͖̹̈́̍̐̽͗̇͝͠T̸̡͎͉̠͖͈́͊̇Ĥ̷̢͂̅͐̊̈́̈́̏͋̌͘E̴̢̩̥͉͂͆̍̉̀́̌̌͒̃̀͠͝Y̶͖̲͚̿͋̐̃̇̋̈́̚͜ Ȃ̷͎̣̭̻̩̺̗̯̮̭̱͚̲̉̂̌ͅR̵̢̢̬͇̫͔̫̟̥̟̰̺͂̈́͜͝Ȩ̵̧͕̙̤̤͎̃͊̀̿̍̔̕͝ ̸̡̺̞̖̠̼̬̗͈͉̫̤̪͔̌̈́C̴̛̘͇͉̭̟͍̺̱̹͉̤̠̆̌̉̊̏̕͠͝O̶̡̝̰̗̗͔̿M̵̦͉̭̼̻̳̺̙̩̓̐̐̐̑̍͐̈͘͝I̵̦͉̟̝̺̳̙̬̭̱͚͖̥̣͕͓͍̒͌́̊̓N̸̡̛̰̞͍̩̝͖̠̠͈̂̀̐́̃̌̾͊̎͗̇̀̕͠Ğ̸̩͖̠͎̻́̇̾͌͑́̑̕̕͜
		static event Action<float> doDissolve;

		public static void DissolveAllAndEverything(float dissolveTime) {
			doDissolve?.Invoke(dissolveTime);
		}

		public void DissolveAll(float dissolveTime) {
			foreach(var boc in BeatmapObjectManager_allBeatmapObjects(ref beatmapObjectManager))
				boc.Dissolve(dissolveTime);
		}

		public BeatmapObjectDissolver(BeatmapObjectManager beatmapObjectManager) {
			this.beatmapObjectManager = beatmapObjectManager;
		}

		public void Initialize() {
			doDissolve += this.DissolveAll;
		}

		public void Dispose() {
			doDissolve -= this.DissolveAll;
		}
	}
}
