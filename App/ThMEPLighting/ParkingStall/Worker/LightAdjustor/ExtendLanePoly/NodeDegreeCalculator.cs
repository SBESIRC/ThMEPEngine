using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ParkingStall.Geometry;


namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class NodeDegreeCalculator
    {
        public List<PointEdgeInfo> PointEdgeInfos
        {
            get;
            set;
        } = new List<PointEdgeInfo>();

        private List<Polyline> m_lanePolylines;

        private Dictionary<Point3d, List<LanePolyline>> m_pointMap = new Dictionary<Point3d, List<LanePolyline>>();


        public static List<PointEdgeInfo> MakeLanePolylineNodeDegree(List<Polyline> polylines)
        {
            var nodeDegreeCalculator = new NodeDegreeCalculator(polylines);
            nodeDegreeCalculator.Do();
            return nodeDegreeCalculator.PointEdgeInfos;
        }

        public NodeDegreeCalculator(List<Polyline> polylines)
        {
            m_lanePolylines = polylines;
        }

        public void Do()
        {
            var lanePolyNodes = CalculateLanePolylineNodes();
            InsertNodes(lanePolyNodes);
            CalculateDegree();
        }


        private void CalculateDegree()
        {
            // 初步计算
            foreach (var pairInfo in m_pointMap)
            {
                var point = pairInfo.Key;
                var lanePolys = pairInfo.Value;
                PointEdgeInfos.Add(new PointEdgeInfo(point, lanePolys, lanePolys.Count()));
            }

            // 端点在线的中间部分

            for (int i = 0; i < PointEdgeInfos.Count; i++)
            {
                var curEdgeInfo = PointEdgeInfos[i];
                var startPoint = curEdgeInfo.Point;

                var splitPolys = CalculatePointOnPolys(startPoint, m_lanePolylines);

                curEdgeInfo.Degree += splitPolys.Count() * 2;
            }
        }


        private List<Polyline> CalculatePointOnPolys(Point3d point, List<Polyline> polylines)
        {
            var resPolys = new List<Polyline>();

            foreach (var poly in polylines)
            {
                if (IsDegreePoint(point, poly))
                    resPolys.Add(poly);
            }
            
            return resPolys;
        }

        private bool IsDegreePoint(Point3d point, Polyline polyline)
        {
            var curves = GeomUtils.Polyline2Curves(polyline);
            var lines = new List<Line>();
            curves.ForEach(curve =>
            {
                if (curve is Line line)
                {
                    lines.Add(line);
                }
                else if (curve is Arc arc)
                {
                    lines.Add(new Line(arc.StartPoint, arc.EndPoint));
                }
            });

            var plStart = polyline.StartPoint;
            var plEnd = polyline.EndPoint;

            // 剔除端点情形
            var ptLst = new List<Point3d>() { plStart, plEnd };
            foreach (var pt in ptLst)
            {
                if (GeomUtils.Point3dIsEqualPoint3d(point, pt))
                    return false;
            }

            // 中间部分
            foreach (var line in lines)
            {
                if (IsValidLine(point, line))
                    return true;
            }

            return false;
        }

        private bool IsValidLine(Point3d point, Line line)
        {
            if (GeomUtils.IsPointOnLine(point, line))
            {
                return true;
            }

            return false;
        }

        private void InsertNodes(List<LanePolyline> lanePolylines)
        {
            foreach (var laneNode in lanePolylines)
            {
                var ptStart = laneNode.StartPoint;
                List<LanePolyline> edges = null;
                var result = m_pointMap.TryGetValue(ptStart, out edges);
                if (result)
                {
                    edges.Add(laneNode);
                }
                else
                {
                    var lanes = new List<LanePolyline>();
                    lanes.Add(laneNode);
                    m_pointMap.Add(ptStart, lanes);
                }
            }
        }

        private List<LanePolyline> CalculateLanePolylineNodes()
        {
            var lanePolylines = new List<LanePolyline>();
            foreach (var poly in m_lanePolylines)
            {
                var clonePoly = poly.Clone() as Polyline;
                clonePoly.ReverseCurve();
                var srcLanePoly = new LanePolyline(poly, poly.StartPoint, poly.EndPoint);
                var symLanePoly = new LanePolyline(clonePoly, clonePoly.StartPoint, clonePoly.EndPoint);
                srcLanePoly.Sym = symLanePoly;
                symLanePoly.Sym = srcLanePoly;

                lanePolylines.Add(srcLanePoly);
                lanePolylines.Add(symLanePoly);
            }

            return lanePolylines;
        }
    }
}
