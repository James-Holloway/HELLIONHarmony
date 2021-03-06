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

namespace HELLIONHarmony.Loader.ClientPatches
{
    [HarmonyPatch(typeof(Client), "QuitApplication")]
    [HarmonyPatch(typeof(Client), "OnDestroy")]
    internal class PluginStop
    {
        public static void Postfix()
        {
            PluginManager.DisableAllPlugins();
        }
    }
}
