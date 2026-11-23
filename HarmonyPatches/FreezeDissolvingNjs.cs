using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using SiraUtil.Affinity;
using UnityEngine;

namespace Shaffuru.HarmonyPatches {
	/*
	 * Notes, walls and arcs read the shared jump / NJS data every frame. When switching maps it gets re-initialized
	 * for the new map while the old maps objects are still dissolving, which would make them jump around, so while
	 * dissolving they get a frozen copy of the old data swapped in instead
	 */
	class FreezeDissolvingNjs : IAffinity {
		static readonly MethodInfo METHOD_object_MemberwiseClone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

		readonly ConditionalWeakTable<MonoBehaviour, IVariableMovementDataProvider> originalProviders = new ConditionalWeakTable<MonoBehaviour, IVariableMovementDataProvider>();
		IVariableMovementDataProvider frozenProvider;
		// Only so we dont have to check the table for every object every frame when nothing is dissolving
		float frozenUntil;

		public void Freeze(IEnumerable<IBeatmapObjectController> dissolvingObjects, float dissolveTime) {
			frozenProvider = null;
			frozenUntil = Time.time + dissolveTime + 0.5f;

			foreach(var x in dissolvingObjects) {
				switch(x) {
					case NoteController note:
						Add(note._noteMovement._jump, note._noteMovement._jump._variableMovementDataProvider);
						Add(note._noteMovement._floorMovement, note._noteMovement._floorMovement._variableMovementDataProvider);
						Add(note._noteMovement._waiting, note._noteMovement._waiting._variableMovementDataProvider);
						break;
					case ObstacleController obstacle:
						Add(obstacle, obstacle._variableMovementDataProvider);
						break;
					case SliderController slider:
						Add(slider, slider._variableMovementDataProvider);
						Add(slider._sliderMovement, slider._sliderMovement._variableMovementDataProvider);
						break;
				}
			}
		}

		void Add(MonoBehaviour element, IVariableMovementDataProvider provider) {
			originalProviders.Remove(element);
			originalProviders.Add(element, provider);
			frozenProvider ??= (IVariableMovementDataProvider)METHOD_object_MemberwiseClone.Invoke(provider, null);
		}

		[AffinityPatch(typeof(NoteJump), nameof(NoteJump.ManualUpdate))]
		[AffinityPatch(typeof(NoteFloorMovement), nameof(NoteFloorMovement.ManualUpdate))]
		[AffinityPatch(typeof(NoteFloorMovement), nameof(NoteFloorMovement.ShouldMove))]
		[AffinityPatch(typeof(NoteWaiting), nameof(NoteWaiting.ShouldWait))]
		[AffinityPatch(typeof(ObstacleController), nameof(ObstacleController.ManualUpdate))]
		[AffinityPatch(typeof(SliderController), nameof(SliderController.ManualUpdate))]
		[AffinityPatch(typeof(SliderMovement), nameof(SliderMovement.ManualUpdate))]
		[AffinityPrefix]
		void Prefix(MonoBehaviour __instance, ref IVariableMovementDataProvider ____variableMovementDataProvider) {
			// Once set it stays until the object gets re-used
			if(Time.time < frozenUntil && originalProviders.TryGetValue(__instance, out _))
				____variableMovementDataProvider = frozenProvider;
		}

		// Objects are pooled, once they get re-used they are not dissolving anymore
		[AffinityPatch(typeof(NoteJump), nameof(NoteJump.Init))]
		[AffinityPatch(typeof(NoteFloorMovement), nameof(NoteFloorMovement.Init))]
		[AffinityPatch(typeof(NoteWaiting), nameof(NoteWaiting.Init))]
		[AffinityPatch(typeof(ObstacleController), nameof(ObstacleController.Init))]
		[AffinityPatch(typeof(SliderController), nameof(SliderController.Init))]
		[AffinityPatch(typeof(SliderMovement), nameof(SliderMovement.Init))]
		[AffinityPrefix]
		void InitPrefix(MonoBehaviour __instance, ref IVariableMovementDataProvider ____variableMovementDataProvider) {
			if(!originalProviders.TryGetValue(__instance, out var original))
				return;

			____variableMovementDataProvider = original;
			originalProviders.Remove(__instance);
		}
	}
}
