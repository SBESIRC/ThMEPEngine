using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThSpatialIndexService
    {
        public static ThCADCoreNTSSpatialIndex CreateTextSpatialIndex(DBObjectCollection dbTexts)
        {
            return new ThCADCoreNTSSpatialIndex(dbTexts);
        }
    }
}
