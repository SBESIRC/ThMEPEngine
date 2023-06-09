﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.Broadcast.Service
{
    public class CheckService
    {
        public List<Polyline> FilterColumns(List<Polyline> columns, Line line, Polyline frame, Point3d sPt, Point3d ePt)
        {
            Vector3d xDir = (line.EndPoint - line.StartPoint).GetNormal();
            Vector3d yDir = Vector3d.ZAxis.CrossProduct(xDir);
            Vector3d zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d( 
                new double[] {
                    xDir.X, yDir.X, zDir.X, line.StartPoint.X,
                    xDir.Y, yDir.Y, zDir.Y, line.StartPoint.Y,
                    xDir.Z, yDir.Z, zDir.Z, line.StartPoint.Z,
                    0.0, 0.0, 0.0, 1.0
                });

            if (columns.Count > 0)
            {
                var orderColumns = columns.OrderBy(x => StructUtils.GetStructCenter(x).TransformBy(matrix).X).ToList();
                var firMoveColumns = IsUsefulColumn(frame, orderColumns, sPt, xDir);
                columns = columns.Except(firMoveColumns).ToList();
                orderColumns.Reverse();
                var lastMoveColumns = IsUsefulColumn(frame, orderColumns, sPt, xDir);
                columns = columns.Except(lastMoveColumns).ToList();
            }
            
            return columns;
        }

        /// <summary>
        /// 判断是否是可用柱
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="polyline"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private List<Polyline> IsUsefulColumn(Polyline frame, List<Polyline> colomns, Point3d pt, Vector3d dir)
        {
            var moveColumns = new List<Polyline>();
            foreach (var polyline in colomns)
            {
                var newPoly = polyline.Buffer(200)[0] as Polyline;
                Line layoutLine = IsLayoutColumn(newPoly, pt, dir);
                bool isIntersect = layoutLine.IsIntersects(frame);
                if (!isIntersect)
                {
                    return moveColumns;
                }
                moveColumns.Add(polyline);
            }
            return moveColumns;
        }

        private Line IsLayoutColumn(Polyline polyline, Point3d pt, Vector3d dir)
        {
            var closetPt = polyline.GetClosestPointTo(pt, false);
            List<Line> lines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                lines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }
           
            Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);
            var layoutLine = lines.Where(x => x.GetClosestPointTo(closetPt, false).DistanceTo(closetPt) < 1)
                .Where(x =>
                {
                    var xDir = (x.EndPoint - x.StartPoint).GetNormal();
                    return Math.Abs(otherDir.DotProduct(xDir)) < Math.Abs(dir.DotProduct(xDir));
                }).FirstOrDefault();

            if (layoutLine == null)
            {
                layoutLine = lines.First();
            }
            return layoutLine;
        }

        public List<Polyline> FilterWalls(List<Polyline> walls, List<Line> lines, double expandLength, double tol)
        {
            if (walls.Count <= 0)
            {
                return walls;
            }
            var wallCollections = walls.ToCollection();
            var resPolys = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, expandLength, tol);
                return linePoly.Intersection(wallCollections).Cast<Polyline>().ToList();
            }).ToList();

            return resPolys;
        }
    }
}
