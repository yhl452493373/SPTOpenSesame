using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;
using SPTOpenSesame.Helpers;

namespace SPTOpenSesame.Patches
{
    public class InteractiveObjectInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return InteractionHelpers.TargetType.GetMethod("smethod_3", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref object __result, GamePlayerOwner owner, WorldInteractiveObject worldInteractiveObject)
        {
            // Ignore interactions from bots
            if (InteractionHelpers.IsInteractorABot(owner))
            {
                return;
            }

            if (OpenSesamePlugin.WriteMessagesForAllDoors.Value)
            {
                LoggingUtil.LogInfo("Checking available actions for object " + worldInteractiveObject.Id + "...");
            }

            if (!OpenSesamePlugin.AddNewActions.Value)
            {
                return;
            }

            // Try to add the "Open Sesame" action to the door's context menu
            worldInteractiveObject.AddOpenSesameToActionList(__result, owner);
        }
    }
}
