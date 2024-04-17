using System.Reflection;
using Aki.Reflection.Patching;
using EFT;

namespace SPTOpenSesame.Patches
{
    public class GameWorldOnDestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnDestroy), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {
            OpenSesamePlugin.PowerSwitch = null;
        }
    }
}
