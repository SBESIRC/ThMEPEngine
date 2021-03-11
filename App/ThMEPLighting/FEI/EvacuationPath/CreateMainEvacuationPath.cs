using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.FEI.Service;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public class CreateMainEvacuationPath
    {
        readonly double moveDistance = 800;
        public void CreatePath(KeyValuePair<Polyline, List<Polyline>> polyInfo, List<Polyline> columns, List<Line> lanes, List<BlockReference> enterBlocks)
        {
            //分类车道线，将车道分为主车道和副车道
            ParkingLinesService parkingLinesService = new ParkingLinesService();
            var xLines = parkingLinesService.CreateNodedPLineToPolyByConnect(polyInfo.Key, lanes, out List<List<Line>> yLines);



        }

        private void CreateAuxiliaryPath(Dictionary<Polyline, List<Polyline>> polyInfo, List<Polyline> columns, List<BlockReference> startBlocks, List<Line> lanes)
        {
            foreach (var block in startBlocks)
            {
                var dir = GetExtendsDirection(block, lanes);
                var sPt = block.Position;


            }
        }

        private void GetExtendLines(Point3d pt, Vector3d dir, List<Line> lanes, List<Polyline> holes, List<Polyline> columns)
        {
            Ray ray = new Ray();
            ray.UnitDir = dir;
            Point3d spt = pt;
            while (true)
            {
                ray.BasePoint = spt;
                var intersectPts = lanes.SelectMany(x =>
                {
                    Point3dCollection collection = new Point3dCollection();
                    x.IntersectWith(ray, Intersect.OnBothOperands, collection, (IntPtr)0, (IntPtr)0);
                    return collection.Cast<Point3d>().ToList();
                })
                .OrderBy(x => x.DistanceTo(spt))
                .ToList();

                List<Line> exLines = new List<Line>();
                Line maxLenghthLine = new Line(spt, intersectPts.Last());
                List<Polyline> allHoles = new List<Polyline>(holes);
                allHoles.AddRange(columns);
                if (!CheckService.CheckIntersectWithHols(maxLenghthLine, holes, out List<Polyline> interHoles))
                {
                    exLines.Add(maxLenghthLine);
                    break;
                }
                else
                {
                    foreach (var iPt in intersectPts)
                    {
                        Line line = new Line(spt, iPt);

                    }
                }
            }
        }

        private bool AdjustExtendLines(List<Point3d> pts, List<Polyline> holes, bool isFirst, ref Point3d spt, out List<Line> exLines)
        {
            exLines = new List<Line>();
            foreach (var pt in pts)
            {
                Line line = new Line(spt, pt);
                if (!CheckService.CheckIntersectWithHols(line, holes, out List<Polyline> interHoles))
                {
                    exLines.Add(line);
                    spt = pt;
                }
                else
                {
                    if (isFirst)
                    {

                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 计算偏移起始点
        /// </summary>
        /// <param name="spt"></param>
        /// <param name="dir"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private Point3d MoveStartPoint(Point3d spt, Vector3d dir, List<Polyline> holes)
        {
            var zDir = Vector3d.ZAxis;
            var xDir = zDir.CrossProduct(dir);
            Matrix3d matrix = new Matrix3d(
                new double[]{
                    xDir.X, dir.X, zDir.X, 0,
                    xDir.Y, dir.Y, zDir.Y, 0,
                    xDir.Z, dir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
            });

            var allPts = holes.SelectMany(x =>
            {
                var pts = new List<Point3d>();
                for (int i = 0; i < x.NumberOfVertices; i++)
                {
                    pts.Add(x.GetPoint3dAt(i).TransformBy(matrix.Inverse()));
                }
                return pts;
            }).OrderBy(x => x.X).ToList();

            var transPt = spt.TransformBy(matrix.Inverse());
            var moveDir = xDir;
            var moveLength = allPts.Last().X - transPt.X + moveDistance;
            if (transPt.DistanceTo(allPts.Last()) > transPt.DistanceTo(allPts.First()))
            {
                moveLength = allPts.First().X - transPt.X + moveDistance;
                moveDir = -moveDir;
            }

            return spt + moveDir * moveLength;
        }

        /// <summary>
        /// 计算出入口起点延申方向
        /// </summary>
        /// <param name="block"></param>
        /// <param name="lanes"></param>
        /// <returns></returns>
        private Vector3d GetExtendsDirection(BlockReference block, List<Line> lanes)
        {
            var closetPt = lanes.Select(x => x.GetClosestPointTo(block.Position, false))
                .OrderBy(x => x.DistanceTo(block.Position))
                .First();
            var dir = (closetPt - block.Position).GetNormal();
            var blockDir = block.BlockTransform.CoordinateSystem3d.Yaxis;
            if (blockDir.DotProduct(dir) < 0)
            {
                blockDir = -blockDir;
            }

            return blockDir;
        }
    }
}
