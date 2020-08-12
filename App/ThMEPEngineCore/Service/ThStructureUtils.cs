using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ThMEPEngineCore.Service
{
    public class ThStructureUtils
    {
        public static bool IsColumnXref(string pathName)
        {
            return Path.GetFileName(pathName).ToUpper().Contains("COLU");
        }

        public static bool IsBeamXref(string pathName)
        {
            return Path.GetFileName(pathName).ToUpper().Contains("BEAM");
        }

        public static string OriginalFromXref(string xrefLayer)
        {
            int index = xrefLayer.LastIndexOf('|');
            return (index >= 0) ? xrefLayer.Substring(index + 1) : xrefLayer;
        }
    }
}
