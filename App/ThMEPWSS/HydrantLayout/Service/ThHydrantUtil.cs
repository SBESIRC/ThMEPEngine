using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.ExcelService;

namespace ThMEPWSS.HydrantLayout.Service
{
    internal class ThHydrantUtil
    {
        public static ThMEPOriginTransformer GetTransformer(Point3dCollection pts)
        {
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            return transformer;
        }
    }
}
