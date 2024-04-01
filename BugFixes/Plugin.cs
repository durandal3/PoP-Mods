using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFixes
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = base.Logger;
            // Plugin startup logic
            Harmony.CreateAndPatchAll(typeof(Patches));
        }
    }
}
