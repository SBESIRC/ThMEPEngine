using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;

namespace ThMEPLighting.IlluminationLighting.Model
{
    class ThLayoutPt
    {
        public Point3d Pt = new Point3d();
        public Vector3d Dir = new Vector3d();
        public string BlkName = "";
        public double Angle = 0;

        public void transformBack(ThMEPOriginTransformer transformer)
        {
           Pt= transformer.Reset(Pt);
        }
    }
}
