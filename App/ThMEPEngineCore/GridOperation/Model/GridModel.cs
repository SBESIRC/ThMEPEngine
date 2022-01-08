using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GridOperation.Model
{
    /// <summary>
    /// 轴网类型
    /// </summary>
    public enum GridType
    {
        LineGrid,
        ArcGrid,
    }

    /// <summary>
    /// 轴网模型
    /// </summary>
    public class GridModel
    {
        public List<Curve> allLines = new List<Curve>();

        public List<Polyline> regions = new List<Polyline>();

        public Polyline GridPolygon { get; set; }

        public Point3d centerPt { get; set; }

        public Vector3d vector { get; set; }
    }
}
