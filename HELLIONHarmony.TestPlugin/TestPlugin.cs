using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HELLIONHarmony;
using HELLIONHarmony.Loader;

namespace HELLIONHarmony.TestPlugin
{
    public class TestPlugin : Plugin
    {
        public override string Identifier => "HELLIONHarmony.TestPlugin";
        public override string Author => "James Holloway";
        public override string Description => "Prints when this plugin is loaded or unloaded";

        public override PatchScope OnlyScope => PatchScope.Shared;

        public void OnEnabled()
        {
            Logger.WriteLine($"{GetType().Name} has been enabled. CS: {PluginManager.CurrentScope} ST: {Environment.StackTrace}");
            HarmonyLib.FileLog.Log($"{GetType().Name} has been enabled. CS: {PluginManager.CurrentScope} ST: {Environment.StackTrace}\r\n");
        }
        public void OnDisabled()
        {
            Logger.WriteLine($"{GetType().Name} has been disabled. CS: {PluginManager.CurrentScope} ST: {Environment.StackTrace}");
            HarmonyLib.FileLog.Log($"{GetType().Name} has been disabled. CS: {PluginManager.CurrentScope} ST: {Environment.StackTrace}\r\n");
        }
    }
}
