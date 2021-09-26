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
    [HarmonyPatch(typeof(Client), "Start")]
    internal class PluginStart
    {
        public static void Postfix()
        {
            PluginManager.EnableAllPlugins();
        }
    }
}
