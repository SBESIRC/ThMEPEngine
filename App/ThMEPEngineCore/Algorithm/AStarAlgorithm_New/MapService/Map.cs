using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm.AStarAlgorithm_New.Model;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm_New.MapService
{
    public class Map
    {
        Polyline originPoly = null; //原始框线
        double avoidHoleDistance = 800;
        double avoidFrameDistance = 200;
        Matrix3d ucsMatrix = Matrix3d.Identity;
        public Polyline polyline = null; //外包框
        public Point3d startPt;
        public Line endLine;
        public List<Polyline> holes = null;
        public List<Line> cellLines = new List<Line>();

        public Map(Polyline _polyline, Vector3d xDir, double _avoidFrameDistance, double _avoidHoleDistance)
        {
            avoidHoleDistance = _avoidHoleDistance;
            avoidFrameDistance = _avoidFrameDistance;

            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = zDir.CrossProduct(xDir);
            ucsMatrix = new Matrix3d(new double[] {
                xDir.X, xDir.Y, xDir.Z, 0,
                yDir.X, yDir.Y, yDir.Z, 0,
                zDir.X, zDir.Y, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0
            });

            var clonePoly = _polyline.Clone() as Polyline;
            clonePoly.TransformBy(ucsMatrix);
            originPoly = clonePoly;
            this.polyline = clonePoly.Buffer(-avoidFrameDistance)[0] as Polyline;
        }

        /// <summary>
        /// 设置障碍
        /// </summary>
        /// <param name="holes"></param>
        public void SetObstacle(List<Polyline> _holes)
        {
            var cloneHoles = _holes.Select(x => x.Clone() as Polyline).ToList();
            cloneHoles.ForEach(x => x.TransformBy(ucsMatrix));
            holes = cloneHoles.SelectMany(x => x.Buffer(avoidHoleDistance).Cast<Polyline>()).ToList();
        }

        /// <summary>
        /// 设置起点和终点信息
        /// </summary> 
        /// <param name="_startPt"></param>
        /// <param name="_endPt"></param>
        public void SetStartAndEndInfo(Point3d _startPt, Line _endLine)
        {
            startPt = _startPt.TransformBy(ucsMatrix);
            endLine = _endLine.Clone() as Line;
            endLine.TransformBy(ucsMatrix);
            CreateMap();
        }

        /// <summary>
        /// 判断该点是否在地图内
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool ContainsPt(Point3d point)
        {
            return polyline.Contains(point.TransformBy(ucsMatrix));
        }

        /// <summary>
        /// 判断线是否在地图内
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool ContainsLine(Line line)
        {
            var tempLine = line.Clone() as Line;
            tempLine.TransformBy(ucsMatrix);
            return polyline.Intersects(tempLine);
        }

        /// <summary>
        /// 构建path
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public Polyline CreatePath(List<Point3d> points)
        {
            points = points.Select(x => x.TransformBy(ucsMatrix.Inverse())).ToList();
            if (points == null || points.Count <= 0)
            {
                return null;
            }
            Polyline path = new Polyline();
            for (int i = 0; i < points.Count; i++)
            {
                path.AddVertexAt(0, points[i].ToPoint2D(), 0, 0, 0);
            }

            if (path.NumberOfVertices <= 1)
            {
                return null;
            }
            return path.DPSimplify(1);
        }

        /// <summary>
        /// 构建地图
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="xDir"></param>
        private void CreateMap()
        {
            var bbox = GetBoungdingBox(polyline);
            var pts = holes.SelectMany(x => x.Vertices().Cast<Point3d>()).ToList();
            pts.AddRange(bbox);
            pts.AddRange(polyline.Vertices().Cast<Point3d>());
            pts.Add(startPt);
            pts.Add(endLine.StartPoint);
            pts.Add(endLine.EndPoint);
            pts.Distinct();

            double minX = bbox[0].X;
            double minY = bbox[0].Y;
            double maxX = bbox[2].X;
            double maxY = bbox[2].Y;
            var mapXLines = new List<Line>();
            var mapYLines = new List<Line>();
            pts.Where(x => polyline.Contains(x)).ToList().ForEach(x =>
            {
                mapXLines.Add(new Line(new Point3d(minX, x.Y, 0), new Point3d(maxX, x.Y, 0)));
                mapYLines.Add(new Line(new Point3d(x.X, minY, 0), new Point3d(x.X, maxY, 0)));
            });
            var mapLines = FilterRepeatLines(mapXLines);
            mapLines.AddRange(FilterRepeatLines(mapYLines));
            var handleLines = ThMEPLineExtension.LineSimplifier(mapLines.ToCollection(), 500, 100.0, 1, Math.PI / 180.0);
            handleLines.AddRange(AddUsefulLines(handleLines, startPt, minX, minY, maxX, maxY));
            handleLines.AddRange(AddUsefulLines(handleLines, endLine.StartPoint, minX, minY, maxX, maxY));
            handleLines.AddRange(AddUsefulLines(handleLines, endLine.EndPoint, minX, minY, maxX, maxY));
            var nodedLines = GetNodedMapLines(handleLines);
            cellLines = FilterHoleLines(nodedLines);
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var item in cellLines)
                {
                    //var S = item;
                    //S.TransformBy(ucsMatrix.Inverse());
                    //db.ModelSpace.Add(S);
                }
            }
        }

        /// <summary>
        /// 补充点,避免起点或者终点线被过滤
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="pt"></param>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        private List<Line> AddUsefulLines(List<Line> lines, Point3d pt, double minX, double minY, double maxX, double maxY)
        {
            List<Line> resLines = new List<Line>();
            if (!lines.Any(x=>x.GetClosestPointTo(pt, false).DistanceTo(pt) < 0.01))
            {
                resLines.Add(new Line(new Point3d(minX, pt.Y, 0), new Point3d(maxX, pt.Y, 0)));
                resLines.Add(new Line(new Point3d(pt.X, minY, 0), new Point3d(pt.X, maxY, 0)));
            }

            return resLines;
        }

        /// <summary>
        /// 过滤掉重复线
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private List<Line> FilterRepeatLines(List<Line> lines)
        {
            List<Line> filLines = new List<Line>();
            while (lines.Count > 0)
            {
                var firLine = lines[0];
                filLines.Add(firLine);
                lines.Remove(firLine);
                var interLines = lines.Where(x => x.Distance(firLine) < 0.001).ToList();
                foreach (var line in interLines)
                {
                    lines.Remove(line);
                }
            }

            return filLines;
        }

        /// <summary>
        /// 获取boundingbox
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Point3d> GetBoungdingBox(Polyline polyline)
        {
            List<Point3d> allPts = new List<Point3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                allPts.Add(polyline.GetPoint3dAt(i));
            }

            allPts = allPts.OrderBy(x => x.X).ToList();
            double minX = allPts.First().X;
            double maxX = allPts.Last().X;
            allPts = allPts.OrderBy(x => x.Y).ToList();
            double minY = allPts.First().Y;
            double maxY = allPts.Last().Y;

            List<Point3d> boundingbox = new List<Point3d>();
            boundingbox.Add(new Point3d(minX, minY, 0));
            boundingbox.Add(new Point3d(maxX, minY, 0));
            boundingbox.Add(new Point3d(maxX, maxY, 0));
            boundingbox.Add(new Point3d(minX, maxY, 0));

            return boundingbox;
        }

        /// <summary>
        /// 将地图线打成一段段
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private List<Line> GetNodedMapLines(List<Line> lines)
        {
            var extendLines = lines.Select(y =>
            {
                var dir = (y.EndPoint - y.StartPoint).GetNormal();
                return new Line(y.StartPoint - dir * 1, y.EndPoint + dir * 1);
            }).ToList();
            var objs = new DBObjectCollection();
            extendLines.ForEach(x => objs.Add(x));
            var nodeGeo = objs.ToNTSNodedLineStrings();
            var handleLines = new List<Line>();
            if (nodeGeo != null)
            {
                handleLines = nodeGeo.ToDbObjects()
                .SelectMany(x =>
                {
                    DBObjectCollection entitySet = new DBObjectCollection();
                    (x as Polyline).Explode(entitySet);
                    return entitySet.Cast<Line>().ToList();
                })
                .ToList();
            }

            return handleLines;
        }

        /// <summary>
        /// 删掉与洞口相交的棋盘线
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private List<Line> FilterHoleLines(List<Line> lines)
        {
            double bufferDistance = 0;
            if (avoidHoleDistance > 0)
            {
                bufferDistance = -5;
            }

            var resLines = new List<Line>(lines);
            foreach (var hole in holes)
            {
                var bufferHole = hole.Buffer(bufferDistance)[0] as Polyline;
                using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
                {
                    var s = bufferHole.Clone() as Polyline;
                    s.TransformBy(ucsMatrix.Inverse());
                    //db.ModelSpace.Add(s);
                }
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(resLines.ToCollection());
                var intersectLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferHole).Cast<Curve>().ToList();
                foreach (Line line in intersectLines)
                {
                    resLines.Remove(line);
                }
            }

            var filLines = new List<Line>();
            foreach (var line in resLines)
            {
                if (originPoly.Intersects(line))
                {
                    filLines.Add(line);
                }
            }

            return filLines;
        }
    }
}
