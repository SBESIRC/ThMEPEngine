using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;

namespace ThMEPLighting.IlluminationLighting
{
    public class ThAFASIlluminateCmd
    {
        [CommandMethod("TIANHUACAD", "THFAIlluminationNoUI", CommandFlags.Modal)]
        public void THFAIlluminationNoUI()
        {
            using (var cmd = new IlluminationLightingCmd(null))
            {
                cmd.Execute();
            }
        }
    }
}
