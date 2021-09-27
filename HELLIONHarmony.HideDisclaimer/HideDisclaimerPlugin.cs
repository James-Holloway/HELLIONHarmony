using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HELLIONHarmony;

namespace HELLIONHarmony.HideDisclaimer
{
    public class HideDisclaimerPlugin : Plugin, IMenuPlugin
    {
        public override string Identifier => "HELLIONHarmony.HideDisclaimer";

        public override string Author => "James Holloway";

        public override string Description => "A simple plugin to never show the disclaimer on startup";

        public override PatchScope OnlyScope => PatchScope.Client;
    }
}
