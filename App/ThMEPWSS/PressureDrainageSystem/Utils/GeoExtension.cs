using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.PressureDrainageSystem.Model;
using ThMEPWSS.Uitl;
using ThMEPWSS.WaterSupplyPipeSystem;

namespace ThMEPWSS.PressureDrainageSystem.Utils
{
    public static class GeoExtension
    {
        public static Point3d Project(this Point3d point)
        {
            return point.ToPoint2D().ToPoint3d();
        }
    }
}
