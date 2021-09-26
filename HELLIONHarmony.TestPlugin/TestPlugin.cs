using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HELLIONHarmony;

namespace HELLIONHarmony.TestPlugin
{
    public class TestPlugin : Plugin, IMenuPlugin
    {
        public override string Identifier => "HELLIONHarmony.TestPlugin";
        public override string Author => "James Holloway";
        public override string Description => "A test plugin to only work on the client and client menus";

        public override PatchScope OnlyScope => PatchScope.Client;
    }
}
