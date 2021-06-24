using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Linq2Acad;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.GeojsonExtractor;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDCommonService
    {
        public static ThExtractorBase getExtruactor(List<ThExtractorBase> extractors, Type extruactorName)

        {
            ThExtractorBase obj = null;
            foreach (var ex in extractors)
            {
                if (ex.GetType() == extruactorName)
                {
                    obj = ex;
                    break;
                }
            }
            return obj;
        }


    }
}
