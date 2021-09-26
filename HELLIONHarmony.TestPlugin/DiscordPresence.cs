extern alias HELLIONClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HELLIONHarmony;

using HELLIONClient;
using HELLIONClient::ZeroGravity;
using HELLIONClient::ZeroGravity.Discord;
using HELLIONClient::ZeroGravity.Objects;

namespace HELLIONHarmony.TestPlugin
{
    [HELLIONPatch(PatchScope.Client, typeof(DiscordController), "UpdateStatus")]
    public class DiscordPresence
    {
        public static void Postfix(DiscordController __instance)
        {
            try
            {
                if (Client.Instance.SinglePlayerMode)
                {
                    __instance.presence.details = "Cheating in a single player game";
                }
                else if (__instance.presence.state == "In Menus")
                {
                    __instance.presence.details = "Cheating in menus";
                }
                DiscordRpc.UpdatePresence(ref __instance.presence);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
