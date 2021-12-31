using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.GridOperation.Model
{
    public class GridModel
    {
        public List<Curve> allLines = new List<Curve>();

        public List<Polyline> regions = new List<Polyline>();

        public Polyline GridPolygon { get; set; }

        public Point3d centerPt { get; set; }

        public Vector3d vector { get; set; }

        public GridType gridType { get; set; }
    }

    public enum GridType
    {
        LineGrid,

        ArcGrid,
    }
}
