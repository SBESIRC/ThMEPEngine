using Linq2Acad;
using System.Linq;
using ThCADExtension;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertDiffEngine
    {
        public Database SourceDb { get; set; }

        public Database TargetDb { get; set; }

        public List<ThBConvertRule> Rules { get; set; }
    }
}