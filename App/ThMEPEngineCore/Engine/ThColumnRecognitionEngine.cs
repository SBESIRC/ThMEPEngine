using System;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThColumnRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var columnDbExtension = new ThStructureColumnDbExtension(database))
            {
                columnDbExtension.BuildElementCurves();
                List<Curve> curves = new List<Curve>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    columnDbExtension.ColumnCurves.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex columnCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    var pline = new Polyline()
                    {
                        Closed = true,
                    };
                    pline.CreatePolyline(polygon);
                    foreach (var filterObj in columnCurveSpatialIndex.SelectCrossingPolygon(pline))
                    {
                        curves.Add(filterObj as Curve);
                    }
                }
                else
                {
                    curves = columnDbExtension.ColumnCurves;
                }
                curves.ToCollection().UnionPolygons().Cast<Curve>()
                    .ForEach(o =>
                    {
                        if (o is Polyline polyline && polyline.Area > 0.0)
                        {
                            var bufferObjs = polyline.Buffer(ThMEPEngineCoreCommon.ColumnBufferDistance);
                            if (bufferObjs.Count == 1)
                            {
                                var outline = bufferObjs[0] as Polyline;
                                Elements.Add(ThIfcColumn.Create(outline));
                            }
                        }
                    });
            }
        }
        private List<Curve> PrecessColumn(List<Curve> curves)
        {
            List<Curve> columnOutlines = new List<Curve>();
            // 构成柱的几何图元包括：
            //  1. 圆
            //  2. 多段线构成的矩形
            //  3. 多条多段线构成的矩形（内部有图案）
            // 对于圆形柱，通过获取其外接矩形，将其转化成矩形柱
            // 对于矩形柱，直接保留
            // 对于内部有图案的矩形柱，获取其外轮廓矩形
            var arcs = new DBObjectCollection();
            var lines = new DBObjectCollection();
            var circles = new DBObjectCollection();
            foreach (Curve curve in curves)
            {
                if (curve is Line line)
                {
                    lines.Add(line);
                }
                else if (curve is Polyline polyline)
                {
                    if (JudgePolylineIsClosed(polyline))
                    {
                        columnOutlines.Add(polyline.Clone() as Curve);
                    }
                    else
                    {
                        lines.Add(polyline);
                    }
                }
                else if (curve is Circle)
                {
                    circles.Add(curve);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            // 对于线段，获取构成的轮廓，即为柱的矩形轮廓
            foreach (Polyline pline in lines.Outline())
            {
                columnOutlines.Add(pline);
            }
            // 对于圆，获取其外接矩形，转化成矩形柱
            foreach (Circle circle in circles)
            {
                Point3d pt1 = circle.GeometricExtents.MinPoint;
                Point3d pt3 = circle.GeometricExtents.MaxPoint;
                Point3d pt2 = new Point3d(pt3.X, pt1.Y, pt1.Z);
                Point3d pt4 = new Point3d(pt1.X, pt3.Y, pt1.Z);
                Point3dCollection pts = new Point3dCollection()
                {
                    pt1,pt2,pt3,pt4
                };
                columnOutlines.Add(ThDrawTool.CreatePolyline(pts));
            }
            return columnOutlines;
        }
        private bool JudgePolylineIsClosed(Polyline polyline, double tolerance = 1.0)
        {
            if (polyline.Closed)
            {
                return true;
            }
            Point3d firstPt = polyline.GetPoint3dAt(0);
            Point3d lastPt = polyline.GetPoint3dAt(polyline.NumberOfVertices - 1);
            if (firstPt.DistanceTo(lastPt) <= tolerance)
            {
                return true;
            }
            return false;
        }
    }
}
