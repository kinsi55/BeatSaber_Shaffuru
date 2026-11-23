using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SiraUtil.Affinity;
using UnityEngine;

namespace Shaffuru.HarmonyPatches {
	/*
	 * Notes, walls and arcs read the shared jump / NJS data every frame. When switching maps it gets re-initialized
	 * for the new map while the old maps objects are still dissolving, which would make them jump around, so while
	 * dissolving they get a frozen copy of the old data instead
	 */
	class FreezeDissolvingNjs : IAffinity {
		static readonly MethodInfo METHOD_object_MemberwiseClone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

		// The fields are readonly, so the publicizer alone doesnt allow writing them
		static readonly AccessTools.FieldRef<NoteJump, IVariableMovementDataProvider> FIELD_NoteJump_provider =
			AccessTools.FieldRefAccess<NoteJump, IVariableMovementDataProvider>(nameof(NoteJump._variableMovementDataProvider));
		static readonly AccessTools.FieldRef<NoteFloorMovement, IVariableMovementDataProvider> FIELD_NoteFloorMovement_provider =
			AccessTools.FieldRefAccess<NoteFloorMovement, IVariableMovementDataProvider>(nameof(NoteFloorMovement._variableMovementDataProvider));
		static readonly AccessTools.FieldRef<NoteWaiting, IVariableMovementDataProvider> FIELD_NoteWaiting_provider =
			AccessTools.FieldRefAccess<NoteWaiting, IVariableMovementDataProvider>(nameof(NoteWaiting._variableMovementDataProvider));
		static readonly AccessTools.FieldRef<ObstacleControllerBase, IVariableMovementDataProvider> FIELD_ObstacleControllerBase_provider =
			AccessTools.FieldRefAccess<ObstacleControllerBase, IVariableMovementDataProvider>(nameof(ObstacleControllerBase._variableMovementDataProvider));
		static readonly AccessTools.FieldRef<SliderController, IVariableMovementDataProvider> FIELD_SliderController_provider =
			AccessTools.FieldRefAccess<SliderController, IVariableMovementDataProvider>(nameof(SliderController._variableMovementDataProvider));
		static readonly AccessTools.FieldRef<SliderMovement, IVariableMovementDataProvider> FIELD_SliderMovement_provider =
			AccessTools.FieldRefAccess<SliderMovement, IVariableMovementDataProvider>(nameof(SliderMovement._variableMovementDataProvider));

		readonly ConditionalWeakTable<MonoBehaviour, IVariableMovementDataProvider> originalProviders = new ConditionalWeakTable<MonoBehaviour, IVariableMovementDataProvider>();
		IVariableMovementDataProvider frozenProvider;

		public void Freeze(IEnumerable<IBeatmapObjectController> dissolvingObjects) {
			frozenProvider = null;

			foreach(var x in dissolvingObjects) {
				switch(x) {
					case NoteController note:
						Freeze(note._noteMovement._jump, ref FIELD_NoteJump_provider(note._noteMovement._jump));
						Freeze(note._noteMovement._floorMovement, ref FIELD_NoteFloorMovement_provider(note._noteMovement._floorMovement));
						Freeze(note._noteMovement._waiting, ref FIELD_NoteWaiting_provider(note._noteMovement._waiting));
						break;
					case ObstacleController obstacle:
						Freeze(obstacle, ref FIELD_ObstacleControllerBase_provider(obstacle));
						break;
					case SliderController slider:
						Freeze(slider, ref FIELD_SliderController_provider(slider));
						Freeze(slider._sliderMovement, ref FIELD_SliderMovement_provider(slider._sliderMovement));
						break;
				}
			}
		}

		void Freeze(MonoBehaviour element, ref IVariableMovementDataProvider provider) {
			// Might still be frozen from a previous switch, in which case the field doesnt hold the original anymore
			if(!originalProviders.TryGetValue(element, out _))
				originalProviders.Add(element, provider);

			frozenProvider ??= (IVariableMovementDataProvider)METHOD_object_MemberwiseClone.Invoke(provider, null);
			provider = frozenProvider;
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
