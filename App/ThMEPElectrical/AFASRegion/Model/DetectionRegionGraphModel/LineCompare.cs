using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.SecurityPlaneSystem.Utls;

namespace ThMEPElectrical.AFASRegion.Model.DetectionRegionGraphModel
{
    public class LineCompare : IEqualityComparer<Line>
    {
        public bool Equals(Line x, Line y)
        {
            return x.CheckLineIsEqual( y, Tolerance.Global);
        }

        public int GetHashCode(Line obj)
        {
            return base.GetHashCode();
        }
    }
}
