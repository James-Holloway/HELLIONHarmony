using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace HELLIONHarmony
{
    public abstract class Plugin
    {
        protected Harmony PluginHarmony;
        public bool Enabled { get; private set; }

        // Called via reflection
        private void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        public Plugin() { }

        /// <summary>
        /// <see cref="HELLIONPatchAttribute"/> that fall outside this scope won't be run. 
        /// </summary>
        public virtual PatchScope OnlyScope => PatchScope.Client;

        public abstract string Identifier { get; }
        public abstract string Author { get; }
        public abstract string Description { get; }

        /// <summary>
        /// Used for setting up the plugin before the patches are applied
        /// </summary>
        /// <remarks>
        /// Called by the PluginManager. Not required. Calling will not enable
        /// </remarks>
        void OnEnable() { }
        /// <summary>
        /// Used for clearing up events just before patches are unapplied<br/>
        /// </summary>
        /// <remarks>
        /// Called by the PluginManager. Not required. Calling will not disable
        /// </remarks>

        void OnDisable() { }
    }

    /// <summary>
    /// Implement this on your <see cref="Plugin"/> to have patches applied before entering a game
    /// </summary>
    public interface IMenuPlugin
    {
    }
}
