using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HELLIONHarmony
{
    public enum PatchScope
    {
        Client = 1,
        Dedicated = 2,
        SP = 4,

        Server = Dedicated | SP,
        Shared = Client | Server,
    }
}
