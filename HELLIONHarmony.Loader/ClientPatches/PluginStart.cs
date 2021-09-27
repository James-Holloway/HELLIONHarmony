extern alias HarmonyLib;
extern alias HELLIONClient;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib::HarmonyLib;
using HELLIONClient;
using HELLIONClient::ZeroGravity;
using HELLIONClient::ZeroGravity.Network;

namespace HELLIONHarmony.Loader.ClientPatches
{
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(ConnectionThread), "FinalizeConnecting")]
    internal class PluginStart
    {
        public static void Prefix(ConnectionThread __instance, bool ___gameSocketReady)
        {
            if (___gameSocketReady)
                PluginManager.EnableAllPlugins();
        }
    }
}
