using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPLighting.ParkingStall.Geometry;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.ParkingGroup
{
    /// <summary>
    /// 两个相交矩形中分别长边的最短边---分母
    /// 相交局域的最短边 --分子
    /// </summary>
    public class IntersectParkRelatedNodeCalculator
    {
        private PolylineNode m_curPolylineNode;
        private PolylineNode m_nextPolylineNode;

        private Polyline m_intersectPoly;

        public IntersectParkRelatedNodeCalculator(PolylineNode curPolylineNode, PolylineNode nextPolylineNode, Polyline polyline)
        {
            m_curPolylineNode = curPolylineNode;
            m_nextPolylineNode = nextPolylineNode;
            m_intersectPoly = polyline;
        }

        public static void MakeIntersectParkRelatedNodeCalculator(PolylineNode curPolylineNode, PolylineNode nextPolylineNode, Polyline polyline)
        {
            var relatedNodeCalculator = new IntersectParkRelatedNodeCalculator(curPolylineNode, nextPolylineNode, polyline);
            relatedNodeCalculator.Do();
        }


        public void Do()
        {
            var validLines = CalculateValidLines(m_intersectPoly);
            if (validLines.Count == 0)
                return;

            var numeratorEdge = validLines.First();
            var denominatorEdgeLength = CalculateDenominateLength();
            var ratio = numeratorEdge.Length / denominatorEdgeLength;
            if (ratio < 0.7)
            {
                return;
            }

            if (validLines.Count == 1)
            {
                var validLine = validLines.First();
                var findCurLineSegment = FindLineSegment(m_curPolylineNode, validLine);
                if (findCurLineSegment == null)
                    throw new NotSupportedException();

                findCurLineSegment.IntersectPolyNodes.Add(m_nextPolylineNode);

                var findNextLineSegment = FindLineSegment(m_nextPolylineNode, validLine);
                if (findNextLineSegment == null)
                    throw new NotSupportedException();

                findNextLineSegment.IntersectPolyNodes.Add(m_curPolylineNode);
            }
            else if (validLines.Count == 2)
            {
                // 第一条长边
                var firstLine = validLines.First();
                var firstCurLineSegment = FindLineSegment(m_curPolylineNode, firstLine);

                if (firstCurLineSegment != null)
                    firstCurLineSegment.IntersectPolyNodes.Add(m_nextPolylineNode);

                var firstNextLineSegment = FindLineSegment(m_nextPolylineNode, firstLine);
                if (firstNextLineSegment != null)
                    firstNextLineSegment.IntersectPolyNodes.Add(m_curPolylineNode);

                // 第二条长边
                var secLine = validLines.Last();
                var secCurLineSegment = FindLineSegment(m_curPolylineNode, secLine);
                if (secCurLineSegment != null)
                    secCurLineSegment.IntersectPolyNodes.Add(m_nextPolylineNode);

                var secNextLineSegment = FindLineSegment(m_nextPolylineNode, secLine);
                if (secNextLineSegment != null)
                    secNextLineSegment.IntersectPolyNodes.Add(m_curPolylineNode);

            }
        }


        private LineSegment FindLineSegment(PolylineNode polylineNode, Line line)
        {
            foreach (var lineSegment in polylineNode.LineSegments)
            {
                if (IsInLineSegment(line, lineSegment, 1e-3))
                    return lineSegment;
            }

            return null;
        }

        private bool IsInLineSegment(Line line, LineSegment lineSegment, double tolerance)
        {
            var srcPtS = line.StartPoint;
            var srcPtE = line.EndPoint;

            var segmentLine = lineSegment.SegmentLine;
            if (srcPtS.PointInLineSegment(segmentLine,tolerance,tolerance) && srcPtE.PointInLineSegment(segmentLine, tolerance, tolerance))
                return true;

            return false;
        }

        private double CalculateDenominateLength()
        {
            double firstPolylineNodeMaxEdge = MaxPolylineNodeEdge(m_curPolylineNode);
            double secondPolylineNodeMaxEdge = MaxPolylineNodeEdge(m_nextPolylineNode);

            return Math.Min(firstPolylineNodeMaxEdge, secondPolylineNodeMaxEdge);
        }


        private double MaxPolylineNodeEdge(PolylineNode polylineNode)
        {
            double maxLength = 0;

            foreach (var lineInfo in polylineNode.LineSegments)
            {
                if (lineInfo.SegmentLineLengthType == LineLengthType.LONG_TYPE)
                {
                    if (lineInfo.SegmentLine.Length > maxLength)
                        maxLength = lineInfo.SegmentLine.Length;
                }
            }

            return maxLength;
        }

        private List<Line> CalculateValidLines(Polyline intersectPoly)
        {
            var validLines = new List<Line>();
            //封闭
            if (intersectPoly.Closed)
            {
                var curves = GeomUtils.Polyline2Curves(intersectPoly, false);
                var lines = new List<Line>();
                curves.ForEach(p =>
                {
                    if (p is Line line)
                        lines.Add(line);
                });

                var orderLengths = lines.OrderBy(p => p.Length);

                if (orderLengths.Count() >= 2)
                {
                    validLines.Add(orderLengths.Last());
                    validLines.Add(orderLengths.ElementAt(orderLengths.Count() - 2));
                }
            }
            else
            {
                // 非封闭
                var curves = GeomUtils.Polyline2Curves(intersectPoly, false);
                var lines = new List<Line>();
                curves.ForEach(p =>
                {
                    if (p is Line line)
                        lines.Add(line);
                });

                var orderLengths = lines.OrderBy(p => p.Length);

                if (orderLengths.Count() != 0)
                    validLines.Add(orderLengths.Last());
            }

            return validLines;
        }
    }
}
