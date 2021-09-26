extern alias HarmonyLib;
extern alias HELLIONDedicated;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib::HarmonyLib;
using HELLIONDedicated::ZeroGravity;

namespace HELLIONHarmony.Loader.DedicatedPatches
{
    [HarmonyPatch(typeof(Server), MethodType.Constructor)]
    class PluginInit
    {
        private static bool pluginsLoaded = false;

        public static void Postfix()
        {
            if (!pluginsLoaded)
            {
                PluginManager.Init(PatchScope.Dedicated);
                string currentPath = Path.GetDirectoryName(typeof(Server).Assembly.Location);
                PluginManager.LoadPlugins(currentPath);
                pluginsLoaded = true;
            }
        }
    }
}
