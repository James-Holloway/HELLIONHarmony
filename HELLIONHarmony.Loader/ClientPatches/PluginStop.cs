using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HELLIONHarmony.Loader.ClientPatches
{
    internal class PluginStop
    {
        public static void Postfix()
        {
            PluginManager.DisableAllPlugins();
        }
    }
}
