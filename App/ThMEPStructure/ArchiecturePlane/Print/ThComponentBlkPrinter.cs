using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.ArchiecturePlane.Print
{
    internal abstract class ThComponentBlkPrinter
    {
        public ThComponentBlkPrinter()
        {
        }
        public abstract ObjectIdCollection Print(Database db, List<ThComponentInfo> infos, double scale = 1.0);
    }
}
