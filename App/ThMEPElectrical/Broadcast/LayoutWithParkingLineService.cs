using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.Broadcast.Service;

namespace ThMEPElectrical.Broadcast
{
    public class LayoutWithParkingLineService
    {
        readonly double protectRange = 27000;
        readonly double oneProtect = 21000;
        readonly double tol = 5000;

        /// <summary>
        /// 计算布置信息
        /// </summary>
        /// <param name="mainLines"></param>
        /// <param name="otherLines"></param>
        /// <param name="roomPoly"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> LayoutBraodcast(List<List<Line>> mainLines, List<Polyline> columns, List<Polyline> walls)
        {
            Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> layoutInfo = new Dictionary<List<Line>, Dictionary<Point3d, Vector3d>>();
            foreach (var lines in mainLines)
            {
                //计算车道线上布置点
                var lineLayoutPts = GetLayoutLinePoint(lines);

                //获取该车道线上的构建
                StructureService structureService = new StructureService();
                var lineColumn = structureService.GetStruct(lines, columns, tol);
                var lineWall = structureService.GetStruct(lines, walls, tol);

                //将构建分为上下部分
                var usefulColumns = structureService.SeparateColumnsByLine(lineColumn, lines.First());
                var usefulWalls = structureService.SeparateColumnsByLine(lineWall, lines.First());

                //计算布置信息
                var dir = (lines.First().EndPoint - lines.First().StartPoint).GetNormal();
                StructureLayoutService structureLayoutService = new StructureLayoutService();
                var lInfo = structureLayoutService.GetLayoutStructPt(lineLayoutPts, usefulColumns[1], usefulWalls[1], dir);

                if (lInfo != null && lInfo.Count > 0)
                {
                    layoutInfo.Add(lines, lInfo);
                }
            }

            return layoutInfo;
        }

        /// <summary>
        /// 获取车道线上布置点
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        private List<Point3d> GetLayoutLinePoint(List<Line> lines)
        {
            ParkingLinesService parkingLinesService = new ParkingLinesService();
            var handleLines = parkingLinesService.HandleParkingLines(lines, out Point3d sPt, out Point3d ePt);

            List<Point3d> layoutPts = new List<Point3d>();
            double lineLength = lines.Sum(x => x.Length);
            if (lineLength < oneProtect)
            {
                layoutPts.Add(new Point3d((sPt.X + ePt.X) / 2, (sPt.Y + ePt.Y) / 2, 0));
            }
            else
            {
                if (lineLength > protectRange)
                {
                    var num = Math.Ceiling(lineLength / protectRange) - 1;
                    double moveLength = lineLength / num;
                    layoutPts.AddRange(GetLayoutPoint(handleLines, moveLength, sPt, ePt));
                }
                else
                {
                    layoutPts.AddRange(new List<Point3d>() { sPt, ePt });
                }
            }

            return layoutPts;
        }

        /// <summary>
        /// 计算线上的布置点
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="moveLength"></param>
        /// <param name="sPt"></param>
        /// <param name="ePt"></param>
        /// <returns></returns>
        private List<Point3d> GetLayoutPoint(List<Line> lines, double moveLength, Point3d sPt, Point3d ePt)
        {
            List<Point3d> allPts = new List<Point3d>() { sPt };
            double excessLength = 0;
            foreach (var line in lines)
            {
                double lineLength = line.Length;
                Vector3d dir = (line.EndPoint - line.StartPoint).GetNormal();
                Vector3d compareDir = (ePt - sPt).GetNormal();
                if (dir.DotProduct(compareDir) < 0)
                {
                    dir = -dir;
                }

                while (lineLength >= moveLength || (excessLength > 0 && lineLength > excessLength))
                {
                    if (excessLength > 0)
                    {
                        lineLength = lineLength - excessLength;
                        Point3d movePt = sPt + dir * excessLength;
                        sPt = movePt;
                        allPts.Add(movePt);
                        excessLength = 0;
                    }
                    else
                    {
                        lineLength = lineLength - moveLength;
                        Point3d movePt = sPt + dir * moveLength;
                        sPt = movePt;
                        allPts.Add(movePt);
                    }
                }

                if (excessLength > 0)
                {
                    excessLength = excessLength - lineLength;
                }
                else
                {
                    excessLength = moveLength - lineLength;
                }
            }

            allPts.Add(ePt);
            return allPts; 
        }
    }
}
