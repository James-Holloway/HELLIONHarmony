extern alias HarmonyLib;
extern alias HELLIONSP;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib::HarmonyLib;
using HELLIONSP;
using HELLIONSP::ZeroGravity;

namespace HELLIONHarmony.Loader.SPPatches
{
    [HarmonyPatch(typeof(HiResTime), "Stop")]
    internal class PluginStop
    {
        public static void Postfix()
        {
            PluginManager.DisableAllPlugins();
        }
    }
}
