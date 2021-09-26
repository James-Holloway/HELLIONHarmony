extern alias HarmonyLib;
extern alias HELLIONClient;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib::HarmonyLib;
using HELLIONClient;

namespace HELLIONHarmony.Loader.ClientPatches
{
    [HarmonyPatch(typeof(SteamManager), "Initialized", MethodType.Getter)]
    internal class SteamManagerInit
    {
        static void Postfix(ref bool __result)
        {
            /* Logger.Write("Initialized is " + __result);
            if (!__result)
                __result = true;
            */
        }
    }
}
