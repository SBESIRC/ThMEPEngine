using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.GridOperation.Model
{
    public class ArcGridModel : GridModel
    {
        public Point3d centerPt
        {
            get; set;
        }

        public List<Arc> arcLines = new List<Arc>();

        public List<Line> lines = new List<Line>();
    }
}
