extern alias HarmonyLib;
extern alias HELLIONDedicated;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib::HarmonyLib;
using HELLIONDedicated;
using HELLIONDedicated::ZeroGravity;

namespace HELLIONHarmony.Loader.DedicatedPatches
{
    [HarmonyPatch(typeof(Server), "Start")]
    internal class PluginStart
    {
        public static void Postfix()
        {
            PluginManager.EnableAllPlugins();
        }
    }
}
