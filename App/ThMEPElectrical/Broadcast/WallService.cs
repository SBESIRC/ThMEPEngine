using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Broadcast
{
    public class WallService
    {
        readonly double tol = 800;
        readonly double usefulWallTol = 200;

        public void HandleWalls(List<List<Line>> mainLines, List<List<Line>> otherLines, List<Polyline> wallPolys,
            out Dictionary<List<Line>, List<WallModel>> mainWalls, out Dictionary<List<Line>, List<WallModel>> otherWalls)
        {
            mainWalls = null;
            otherWalls = null;
        }

        public Dictionary<List<Line>, List<WallModel>> GetParkingLineColumn(List<List<Line>> parkingLines, List<Polyline> wallPoly)
        {
            Dictionary<List<Line>, List<WallModel>> resWalls = new Dictionary<List<Line>, List<WallModel>>();
            foreach (var pLines in parkingLines)
            {
                List<WallModel> pLineWalls = new List<WallModel>();
                List<WallModel> upWalls = new List<WallModel>();
                List<WallModel> downWalls = new List<WallModel>();
                foreach (var line in pLines)
                {
                    //创建坐标系
                    var matrix = CreateMatrix(line);

                    //获取上下部分剪力墙
                    var sepWallPolys = SeparateWallsByLine(wallPoly, matrix);

                }
            }
            return null;
        }

        private void GetWalls(List<Polyline> wallPolys, Polyline pline, Matrix3d matrix)
        {
            var usefulWalls = wallPolys.Where(x => x.Intersects(pline)).ToList();
            
        }

        private void GetUsefulWallInfo(Polyline polyline)
        {
            List<Line> allLines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                allLines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }
        }

        /// <summary>
        /// 沿着线将柱分隔成上下两部分
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private List<List<Polyline>> SeparateWallsByLine(List<Polyline> columns, Matrix3d matrix)
        {
            List<Polyline> upWall = new List<Polyline>();
            List<Polyline> downWall = new List<Polyline>();
            foreach (var col in columns)
            {
                var colModel = new ColumnModel(col);
                var transPt = colModel.columnCenterPt.TransformBy(matrix.Inverse());
                if (transPt.Y < 0)
                {
                    downWall.Add(col);
                }
                else
                {
                    upWall.Add(col);
                }
            }

            return new List<List<Polyline>>() { upWall, downWall };
        }

        /// <summary>
        /// 扩张line成polyline
        /// </summary>
        /// <param name="line"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private Polyline expandLine(Line line, double distance)
        {
            Vector3d lineDir = line.Delta.GetNormal();
            Vector3d moveDir = Vector3d.ZAxis.CrossProduct(lineDir);
            Point3d p1 = line.StartPoint - lineDir * tol + moveDir * distance;
            Point3d p2 = line.EndPoint + lineDir * tol + moveDir * distance;
            Point3d p3 = line.EndPoint + lineDir * tol - moveDir * distance;
            Point3d p4 = line.StartPoint - lineDir * tol - moveDir * distance;

            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, p1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p4.ToPoint2D(), 0, 0, 0);
            return polyline;
        }

        /// <summary>
        /// 创建坐标系
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private Matrix3d CreateMatrix(Line line)
        {
            Vector3d xDir = CalLineDirection(line);
            Vector3d yDir = Vector3d.ZAxis.CrossProduct(xDir);
            Vector3d zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, line.StartPoint.X,
                    xDir.Y, yDir.Y, zDir.Y, line.StartPoint.Y,
                    xDir.Z, yDir.Z, zDir.Z, line.StartPoint.Z,
                    0.0, 0.0, 0.0, 1.0
                });

            return matrix;
        }

        /// <summary>
        /// 让线指向x或者y轴正方向
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private Vector3d CalLineDirection(Line line)
        {
            Vector3d lineDir = (line.EndPoint - line.StartPoint).GetNormal();

            double xDotValue = Vector3d.XAxis.DotProduct(lineDir);
            double yDotValue = Vector3d.YAxis.DotProduct(lineDir);
            if (Math.Abs(xDotValue) > Math.Abs(yDotValue))
            {
                return xDotValue > 0 ? lineDir : -lineDir;
            }
            else
            {
                return yDotValue > 0 ? lineDir : -lineDir;
            }
        }
    }
}
