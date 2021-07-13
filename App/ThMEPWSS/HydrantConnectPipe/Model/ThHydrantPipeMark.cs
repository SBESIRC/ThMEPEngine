using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrantPipeMark
    {
        public Point3d StartPoint { set; get; }
        public Point3d EndPoint { set; get; }
        public ThHydrantPipeMark()
        {
            StartPoint = new Point3d();
            EndPoint = new Point3d();
        }
    }
}
