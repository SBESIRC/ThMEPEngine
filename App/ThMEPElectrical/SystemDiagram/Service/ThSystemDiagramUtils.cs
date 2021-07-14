using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Service
{
    public class ThSystemDiagramUtils
    {
        public static Point3d Extend(Point3d sp,Point3d ep, double length)
        {
            var vec = sp.GetVectorTo(ep).GetNormal();
            return sp + vec.MultiplyBy(length);
        }
    }
}
