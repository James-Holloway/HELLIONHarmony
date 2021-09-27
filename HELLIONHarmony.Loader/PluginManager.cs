extern alias HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib::HarmonyLib;
using HELLIONHarmony;

namespace HELLIONHarmony.Loader
{
    public delegate void PluginEvent(Plugin plugin);
    public delegate void CancellablePluginEvent(Plugin plugin, ref bool cancel);

    public static class PluginManager
    {
        public const string PluginDirectory = "HELLIONHarmonyPlugins";

        private const BindingFlags BindingDiscovery = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static PatchScope CurrentScope { get; private set; }

        private static HashSet<Plugin> allPlugins = new HashSet<Plugin>();
        private static HashSet<Plugin> enabledPlugins = new HashSet<Plugin>();
        private static Dictionary<Plugin, Harmony> pluginHarmonies = new Dictionary<Plugin, Harmony>();

        public static HashSet<Plugin> GetPlugins => allPlugins.ToHashSet();
        public static HashSet<Plugin> EnabledPlugins = enabledPlugins.ToHashSet();
        public static HashSet<Plugin> DisabledPlugins => allPlugins.Except(enabledPlugins).ToHashSet();

        public static Harmony GetPluginHarmony(Plugin plugin) => pluginHarmonies.GetValueSafe(plugin);

        /// <summary> Called after a plugin is loaded</summary>
        public static event PluginEvent PluginLoaded;
        /// <summary> Called after a plugin is enabled</summary>
        public static event PluginEvent PluginEnabled;
        /// <summary> Called just before a plugin is disabled</summary>
        public static event PluginEvent PluginDisabled;

        internal static void Init(PatchScope scope)
        {
            if (CurrentScope == default)
            {
                CurrentScope = scope;
                Logger.WriteLine("Initializing PluginManager with scope " + CurrentScope.ToString());
            }
            else
                Logger.WriteLine("Cannot re-init PluginManager");
        }

        internal static Harmony GetCreatePluginHarmony(Plugin plugin)
        {
            Harmony pluginHarmony = GetPluginHarmony(plugin) ?? new Harmony(plugin.Identifier);
            pluginHarmonies[plugin] = pluginHarmony;
            return pluginHarmony;
        }

        // Only supports PatchClassProcessors at the moment
        // TODO support further Harmony features such as methods
        internal static List<PatchClassProcessor> GetPatchesMatchingScope(Plugin plugin, PatchScope scope, bool allowBaseHarmony = false)
        {
            Assembly pluginAssembly = plugin.GetType().Assembly;
            Type[] pluginTypes = AccessTools.GetTypesFromAssembly(pluginAssembly);
            Harmony pluginHarmony = GetCreatePluginHarmony(plugin);

            List<PatchClassProcessor> patchClasses = new List<PatchClassProcessor>();

            foreach (Type type in pluginTypes)
            {
                // If the type has a HELLIONPatchAttribute that matches the provided scope then add it to the output list
                bool hasHELLIONAttribute = type.GetCustomAttributes<HELLIONPatchAttribute>(true)
                    .Where(att => att.PatchScope.HasFlag(scope))
                    .Count() > 0;
                if (hasHELLIONAttribute)
                {
                    patchClasses.Add(pluginHarmony.CreateClassProcessor(type));
                    continue;
                }
                if (allowBaseHarmony)
                {
                    bool hasHarmonyAttribute = (HarmonyMethodExtensions.GetFromType(type)?.Count() ?? 0) > 0;
                    if (hasHarmonyAttribute)
                    {
                        patchClasses.Add(pluginHarmony.CreateClassProcessor(type));
                        continue;
                    }
                }
            }

            return patchClasses;
        }

        /// <summary>
        /// Loads plugins from the plugin directory at the <paramref name="basePath"/>
        /// </summary>
        /// <param name="basePath">basePath\PluginDirectory is used to get plugin assemblies</param>
        /// <param name="searchUpwardsForExecutable">With Client & SP, it will search upwards for the executable named "HELLION.exe" and use the plugin directory there</param>
        internal static void LoadPlugins(string basePath, bool searchUpwardsForExecutable = false)
        {
            if (CurrentScope == default)
            {
                Logger.WriteLine("HELLIONHarmony's PluginManager was not initialized yet ");
                return;
            }
            if (searchUpwardsForExecutable)
            {
                for (int i = 0; i < 5; i++) // upwards search depth - how many parent directories to look up
                {
                    string checkPath = Path.Combine(basePath, string.Concat(Enumerable.Repeat(@"..\", i)));
                    if (File.Exists(Path.Combine(checkPath, "HELLION.exe")))
                    {
                        basePath = Path.GetFullPath(checkPath);
                        break;
                    }
                }
            }

            string pluginPath = Path.Combine(basePath, PluginDirectory);
            if (!Directory.Exists(pluginPath))
            {
                try
                {
                    Directory.CreateDirectory(pluginPath);
                }
                catch (IOException e)
                {
                    Logger.WriteLine("Could not create the plugin directory at " + pluginPath);
                    Logger.WriteLine(e);
                    return;
                }
            }
            Logger.WriteLine("Loading plugins from " + pluginPath);

            foreach (string filePath in Directory.GetFiles(pluginPath, "*.dll"))
            {
                try
                {
                    LoadPluginFromAssembly(filePath);
                }
                catch (Exception e)
                {
                    Logger.WriteLine($"Error caught while attempting to load {new FileInfo(filePath).Name}: {e}");
                }
            }

            EnableAllMenuPlugins();
        }
        internal static void LoadPluginFromAssembly(string path)
        {
            if (!File.Exists(path))
            {
                Logger.WriteLine($"Failed to load assembly from path {path}");
                // TODO throw?
                return;
            }
            Assembly pluginAssembly = Assembly.LoadFile(path);
            IEnumerable<Type> definedPluginTypes = pluginAssembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Plugin)));

            if (definedPluginTypes.Count() == 0)
            {
                Logger.WriteLine($"Skipping assembly, no Plugin found in {pluginAssembly.FullName}"); // TODO repalce with logger
                return;
            }

            HashSet<Plugin> plugins = new HashSet<Plugin>();
            foreach (Type type in definedPluginTypes)
            {
                Plugin plugin = null;
                try
                {
                    plugin = Activator.CreateInstance(type) as Plugin;
                    if (plugin != null)
                    {
                        if (plugin.OnlyScope.HasFlag(CurrentScope)) // only load plugins that fit the scope (e.g. Dedicated + SP when plugin scope is Server)
                        {
                            Logger.WriteLine($"Loading plugin {plugin.Identifier} from assembly {pluginAssembly.FullName}");
                            allPlugins.Add(plugin);
                            PluginLoaded?.Invoke(plugin);
                        }
                        else
                        {
                            Logger.WriteLine($"Skipping plugin {plugin.Identifier} in assembly {pluginAssembly.FullName}");
                        }
                    }
                }
                catch (Exception e)
                {
                    if (plugin != null)
                    {
                        Logger.WriteLine("Failed to load plugin - " + e); // TODO replace with file logger
                    }
                }
            }
        }

        internal static void EnableAllMenuPlugins()
        {
            Logger.WriteLine("Enabling all menu plugins");
            Type typeIMenuPlugin = typeof(IMenuPlugin);

            foreach (Plugin plugin in allPlugins)
            {
                Type pluginType = plugin.GetType();
                if (pluginType.GetInterfaces().Contains(typeIMenuPlugin))
                {
                    EnablePlugin(plugin);
                }
            }
        }

        /// <remarks>
        /// Doesn't enable menu plugins
        /// </remarks>
        internal static void EnableAllPlugins()
        {
            Logger.WriteLine("Enabling all plugins");
            Type typeIMenuPlugin = typeof(IMenuPlugin);

            foreach (Plugin plugin in allPlugins)
            {
                Type pluginType = plugin.GetType();
                if (!pluginType.GetInterfaces().Contains(typeIMenuPlugin))
                {
                    EnablePlugin(plugin);
                }
            }
        }

        /// <remarks>
        /// Doesn't disable menu plugins
        /// </remarks>
        internal static void DisableAllPlugins()
        {
            Logger.WriteLine("Disabiling all plugins");
            Type typeIMenuPlugin = typeof(IMenuPlugin);

            foreach (Plugin plugin in new HashSet<Plugin>(enabledPlugins))
            {
                Type pluginType = plugin.GetType();
                if (!pluginType.GetInterfaces().Contains(typeIMenuPlugin))
                {
                    DisablePlugin(plugin);
                }
            }
        }

        /// <summary>
        /// Enables a plugin
        /// </summary>
        /// <returns>Whether the plugin enabled without any errors</returns>
        public static bool EnablePlugin(Plugin plugin)
        {
            if (enabledPlugins.Contains(plugin))
            {
                Logger.WriteLine($"Plugin {plugin.Identifier} already enabled");
                return false;
            }
            try
            {
                Logger.WriteLine($"Enabling plugin {plugin.Identifier}");
                try
                {
                    MethodInfo onEnabledMethod = plugin.GetType().GetMethod("OnEnabled", BindingDiscovery);
                    onEnabledMethod?.Invoke(plugin, null);
                }
                catch { }

                MethodInfo setEnabledMethod = typeof(Plugin).GetMethod("SetEnabled", BindingDiscovery);
                setEnabledMethod.Invoke(plugin, new object[] { true });

                enabledPlugins.Add(plugin);

                PatchPlugin(plugin);

                PluginEnabled?.Invoke(plugin);

                return true;
            }
            catch { }
            return false;
        }

        internal static void PatchPlugin(Plugin plugin)
        {
            List<PatchClassProcessor> patchClassProcessors = GetPatchesMatchingScope(plugin, CurrentScope, true);
            foreach(PatchClassProcessor patchClass in patchClassProcessors)
            {
                patchClass.Patch();
            }
        }

        /// <summary>
        /// Disables a plugin
        /// </summary>
        /// <returns>Whether the plugin disabled without any errors</returns>
        public static bool DisablePlugin(Plugin plugin)
        {
            if (!enabledPlugins.Contains(plugin))
            {
                Logger.WriteLine($"Plugin {plugin.Identifier} was not enabled");
                return false;
            }
            try
            {
                Logger.WriteLine($"Disabling plugin {plugin.Identifier}");
                PluginDisabled?.Invoke(plugin);
                try
                {
                    MethodInfo onDisabledMethod = plugin.GetType().GetMethod("OnDisabled", BindingDiscovery);
                    onDisabledMethod?.Invoke(plugin, null);
                }
                catch { }

                UnpatchPlugin(plugin);

                MethodInfo setEnabledMethod = typeof(Plugin).GetMethod("SetEnabled", BindingDiscovery);
                setEnabledMethod.Invoke(plugin, new object[] { false });

                enabledPlugins.Remove(plugin);
                return true;
            }
            catch { }
            return false;
        }

        internal static void UnpatchPlugin(Plugin plugin)
        {
            GetPluginHarmony(plugin).UnpatchAll(plugin.Identifier);
        }
    }
}
