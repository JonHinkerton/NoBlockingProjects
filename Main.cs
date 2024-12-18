using HarmonyLib;
using Kingmaker.Globalmap.Colonization.Requirements;
using Kingmaker.Globalmap.Colonization;
using System;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;
using Kingmaker.Globalmap.Blueprints.Colonization;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;

namespace NoBlockingProjects
{
    public static class Main
    {
        internal static Harmony HarmonyInstance;
        internal static UnityModManager.ModEntry.ModLogger log;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            log = modEntry.Logger;

            modEntry.OnGUI = OnGUI;
            HarmonyInstance = new Harmony(modEntry.Info.Id);
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            return true;

        }

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {

        }

        [HarmonyPatch(typeof(RequirementNotBuiltProjectInColony), nameof(RequirementNotBuiltProjectInColony.Check))]
        static class SuppressBlocks
        {
            static bool Postfix(bool _, RequirementNotBuiltProjectInColony __instance, Colony colony)
            {
                return __instance.NotBuiltProject == null || colony != null;
            }
        }

        [HarmonyPatch(typeof(StartChronicle), nameof(StartChronicle.RunAction))]
        static class DontRepeatDialog
        {
            static bool Prefix(StartChronicle __instance)
            {
                var colonies = Game.Instance.Player.ColoniesState.Colonies;
                var blueprintProject = __instance.Owner as BlueprintColonyProject;
                var onFinishActions = blueprintProject?.ActionsOnFinish?.Actions;
                
                if (onFinishActions == null)
                    return true;
                var chronicleGuids = onFinishActions
                    .OfType<Kingmaker.Designers.EventConditionActionSystem.Actions.StartChronicle>()
                    .Select(action => action.Chronicle.AssetGuid);
                foreach (var guid in chronicleGuids)
                {
                    foreach (var col in colonies)
                    {
                        foreach (var chr in col.Colony.Chronicles)
                        {
                            try
                            {
                                var blueprintChron = ResourcesLibrary.TryGetBlueprint<BlueprintColonyChronicle>(chr.Blueprint.AssetGuid);
                                log.Log("JHNoBlockingProjects: searching for: " + blueprintChron.AssetGuid + " " + chr.Blueprint.name);

                                var bpDialog = ResourcesLibrary.TryGetBlueprint<BlueprintDialog>(blueprintChron.BlueprintDialog.AssetGuid);

                                if (bpDialog.AssetGuid == guid)
                                {
                                    log.Log("JHNoBlockingProjects: Match found, skipping chronicle");
                                    return false;
                                }
                            }
                            catch (Exception e) { }
                        }
                    }
                }
                return true; ;
            }
        }
    }
}
