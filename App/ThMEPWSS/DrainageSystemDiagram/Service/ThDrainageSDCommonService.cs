using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.GeojsonExtractor;


namespace ThMEPWSS.DrainageSystemDiagram.Service
{
    public class ThDrainageSDCommonService
    {
        public static ThExtractorBase getExtruactor(List<ThExtractorBase> extractors, Type extruactorName)
        {
            ThExtractorBase obj = null;
            foreach (var ex in extractors)
            {
                if (ex.GetType() == extruactorName)
                {
                    obj = ex;
                    break;
                }
            }
            return obj;
        }

        public static List<Point3d> orderPtInStrightLine(List<Point3d> pts)
        {
            if (pts.Count > 1)
            {
                var matrix = getGroupMatrix(pts);

                var ptsDict = pts.ToDictionary(x => x, x => x.TransformBy(matrix.Inverse()));
                var ptsOrder = ptsDict.OrderBy(x => x.Value.X).Select(x => x.Key).ToList();

                pts = ptsOrder;
            }
            return pts;
        }

        public static Matrix3d getGroupMatrix(List<Point3d> pts)
        {
            var pt = pts.First();
            var otherPtInGroup = pts.Where(x => x.IsEqualTo(pt, new Tolerance(10, 10)) == false).First();
            var dirGroup = (otherPtInGroup - pt).GetNormal();

            var rotationangle = Vector3d.XAxis.GetAngleTo(dirGroup, Vector3d.ZAxis);
            var matrix = Matrix3d.Displacement(pt.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));

            return matrix;
        }

        public static List<Point3d> getPT(Polyline pl)
        {
            var ptList = new List<Point3d>();
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                var pt = pl.GetPoint3dAt(i % pl.NumberOfVertices);
                ptList.Add(pt);
            }

            return ptList;
        }

        public static List<Line> GetLines(Polyline pl)
        {
            var lines = new List<Line>();
            for (int j = 0; j < pl.NumberOfVertices; j++)
            {
                var pt = pl.GetPoint3dAt(j % pl.NumberOfVertices);
                var ptNext = pl.GetPoint3dAt((j + 1) % pl.NumberOfVertices);

                var line = new Line(pt, ptNext);
                lines.Add(line);
            }
            
            return lines;

        }

        public static Polyline turnBoundary(Polyline boundary, int turn)
        {
            Polyline boundaryNew = boundary.Clone() as Polyline;
            if (turn != 0)
            {
                for (int i = 0; i < boundary.NumberOfVertices; i++)
                {
                    boundaryNew.SetPointAt(i, boundary.GetPoint3dAt((i + turn) % boundary.NumberOfVertices).ToPoint2D());
                }
            }
            return boundaryNew;
        }
    }
}
