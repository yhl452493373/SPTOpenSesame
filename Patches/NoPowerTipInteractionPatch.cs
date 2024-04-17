using System.Reflection;
using Aki.Reflection.Patching;
using EFT.Interactive;
using SPTOpenSesame.Helpers;

namespace SPTOpenSesame.Patches
{
    public class NoPowerTipInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return InteractionHelpers.TargetType.GetMethod("smethod_14", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref object __result, NoPowerTip noPowerTip)
        {
            if (!OpenSesamePlugin.AddNewActions.Value)
            {
                return;
            }

            // Try to add the "Turn On Power" action to the doors's context menu
            OpenSesamePlugin.PowerSwitch.AddTurnOnPowerToActionList(__result);
        }
    }
}
