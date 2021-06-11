using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.Point_PathFinding.Model
{
    public class PointMap
    {
        Polyline frame;
        List<Polyline> holes = new List<Polyline>();
        List<Point3d> allPts = new List<Point3d>();
        public Point3d endPoint;
        public PointMap(Polyline _frame, Point3d _endPt)
        {
            endPoint = _endPt;
            frame = _frame;
            for (int i = 0; i < _frame.NumberOfVertices; i++)
            {
                allPts.Add(_frame.GetPoint3dAt(i));
            }
            allPts.Add(endPoint);
        }

        /// <summary>
        /// 设置地图
        /// </summary>
        /// <param name="_holes"></param>
        public void SetObstacle(List<Polyline> _holes)
        {
            holes.AddRange(_holes);
            foreach (var hole in _holes)
            {
                for (int i = 0; i < hole.NumberOfVertices; i++)
                {
                    allPts.Add(hole.GetPoint3dAt(i));
                }
            }
        }

        /// <summary>
        /// 找到附近节点
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public List<Point3d> GetClostNode(Point3d point)
        {
            Polyline bufferPoly = frame.Buffer(1)[0] as Polyline;
            var bufferHoles = holes.Select(x => x.Buffer(-1)[0] as Polyline);
            List<Point3d> closetPt = new List<Point3d>();
            foreach (var pt in allPts)
            {
                if (pt.IsEqualTo(point))
                {
                    continue;
                }
                Line line = new Line(point, pt);
                if (bufferPoly.Contains(line) && bufferHoles.Where(x=>x.Intersects(line)).Count() <= 0)
                {
                    closetPt.Add(pt);
                }
            }

            return closetPt;
        }

        /// <summary>
        /// 构建path
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public Polyline CreatePath(List<Point3d> points)
        {
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
    }
}
