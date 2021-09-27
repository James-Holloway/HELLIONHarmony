using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HELLIONHarmony;

namespace HELLIONHarmony.HideDisclaimer
{
    [HELLIONPatch(PatchScope.Client, typeof(ZeroGravity.CanvasManager), "Start")]
    public class SkipDisclaimer
    {
        public static void Prefix()
        {
            ZeroGravity.CanvasManager.ShowDisclamer = false;
        }
    }
}
