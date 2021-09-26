extern alias HarmonyLib;
extern alias HELLIONSP;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib::HarmonyLib;
using HELLIONSP::ZeroGravity;

namespace HELLIONHarmony.Loader.SPPatches
{
    [HarmonyPatch(typeof(Server), MethodType.Constructor)]
    class PluginInit
    {
        private static bool pluginsLoaded = false;

        public static void Postfix()
        {
            if (!pluginsLoaded)
            {
                PluginManager.Init(PatchScope.SP);
                string currentPath = Path.GetDirectoryName(typeof(Server).Assembly.Location);
                PluginManager.LoadPlugins(currentPath, true);
                pluginsLoaded = true;
            }
        }
    }
}
