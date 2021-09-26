extern alias HarmonyLib;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib::HarmonyLib;
using UnityEngine.SceneManagement;
using HELLIONHarmony.Loader.ClientPatches;
using HELLIONHarmony.Loader.DedicatedPatches;
using HELLIONHarmony.Loader.SPPatches;
using System.IO;

namespace HELLIONHarmony.Loader
{
    public static class Patches
    {
        // We can't harmony.PatchAll(assembly) as we can't patch Client only functions on a server and vice versa

        internal static Harmony harmony = new Harmony("HELLIONHarmony.InjectHarmony");

        private static IEnumerable<Type> GetPatchUnderNamespace(string @namespace)
        {
            return typeof(Patches).Assembly.GetTypes().Where(t => t.Namespace == @namespace);
        }

        private static bool pluginsLoaded = false;
        private static void LoadPlugins(bool searchUpwards = false)
        {
            if (!pluginsLoaded)
            {
                PluginManager.Init(PatchScope.Client);
                string currentPath = Path.GetDirectoryName(typeof(Patches).Assembly.Location);
                PluginManager.LoadPlugins(currentPath, searchUpwards);
                pluginsLoaded = true;
            }
        }

        private static void PatchAllUnderNamespace(string @namespace)
        {
            Logger.WriteLine($"Checking for patches under {@namespace}");
            IEnumerable<Type> types = GetPatchUnderNamespace(@namespace);
            foreach(Type type in types)
            {
                Logger.WriteLine($"Found potential patch type {type.FullName}");
                PatchClassProcessor patchClassProcessor = harmony.CreateClassProcessor(type);
                if (patchClassProcessor != null)
                {
                    patchClassProcessor.Patch();
                }
            }
        }

        public static void StartHarmonyClient()
        {
            PatchAllUnderNamespace("HELLIONHarmony.Loader.ClientPatches");
            LoadPlugins(true);
        }
        public static void StartHarmonySP()
        {
            PatchAllUnderNamespace("HELLIONHarmony.Loader.SPPatches");
            LoadPlugins(true);
        }
        public static void StartHarmonyDedicated()
        {
            PatchAllUnderNamespace("HELLIONHarmony.Loader.DedicatedPatches");
            LoadPlugins(false);
        }

        public static void SteamCheckerBypass()
        {
            SceneManager.LoadScene("Client", LoadSceneMode.Single);
        }
    }
}
