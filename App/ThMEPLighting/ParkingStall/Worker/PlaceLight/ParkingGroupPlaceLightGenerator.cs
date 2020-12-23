using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Assistant;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.PlaceLight
{
    /// <summary>
    /// place light
    /// </summary>
    class ParkingGroupPlaceLightGenerator
    {
        private List<ParkingRelatedGroup> m_parkingRelatedGroups;

        public List<LightPlaceInfo> LightPlaceInfos
        {
            get;
            set;
        } = new List<LightPlaceInfo>();

        public ParkingGroupPlaceLightGenerator(List<ParkingRelatedGroup> parkingRelatedGroups)
        {
            m_parkingRelatedGroups = parkingRelatedGroups;
        }

        public static List<LightPlaceInfo> MakeParkingPlaceLightGenerator(List<ParkingRelatedGroup> parkingRelatedGroups)
        {
            var parkingLightGenerator = new ParkingGroupPlaceLightGenerator(parkingRelatedGroups);
            parkingLightGenerator.Do();
            return parkingLightGenerator.LightPlaceInfos;
        }

        public void Do()
        {
            var parkGroupInfos = CalculateNearParksProfile(m_parkingRelatedGroups);
            CalculateLightPlaceInfos(parkGroupInfos);
        }

        private void CalculateLightPlaceInfos(List<ParkGroupInfo> parkGroupInfos)
        {
            var polylines = new List<Polyline>();
            var smallPolys = new List<Polyline>();
            foreach(var groupInfo in parkGroupInfos)
            {
                polylines.Add(groupInfo.BigPolyline);
                smallPolys.Add(groupInfo.SmallPolyline);
            }

            var smallPolyPolylineNodes = new List<PolylineNode>();
            foreach (var singlePoly in smallPolys)
            {
                var lines = new List<Line>();
                for (int i = 0; i < singlePoly.NumberOfVertices; i++)
                {
                    var line2d = singlePoly.GetLineSegment2dAt(i);
                    lines.Add(Line2dToLine(line2d));
                }

                smallPolyPolylineNodes.Add(SetPolyInfo(singlePoly, lines));
            }

            foreach (var polyNode in smallPolyPolylineNodes)
            {
                var lineSegments = polyNode.LineSegments;
                var firstSegment = lineSegments[0];
                var secondSegment = lineSegments[1];
                var position = PolylineCenter(firstSegment.SegmentLine, secondSegment.SegmentLine);

                if (firstSegment.SegmentLineLengthType == LineLengthType.LONG_TYPE)
                {
                    LightPlaceInfos.Add(new LightPlaceInfo(position, firstSegment.SegmentLine, secondSegment.SegmentLine));
                }
                else if (firstSegment.SegmentLineLengthType == LineLengthType.SHORT_TYPE)
                {
                    LightPlaceInfos.Add(new LightPlaceInfo(position, secondSegment.SegmentLine, firstSegment.SegmentLine));
                }
            }


            var polyNodes = new List<PolylineNode>();
            foreach (var singlePoly in polylines)
            {
                var lines = new List<Line>();
                for (int i = 0; i < singlePoly.NumberOfVertices; i++)
                {
                    var line2d = singlePoly.GetLineSegment2dAt(i);
                    lines.Add(Line2dToLine(line2d));
                }

                polyNodes.Add(SetPolyInfo(singlePoly, lines));
            }

            for (int i = 0; i < polyNodes.Count; i++)
            {
                var polyNode = polyNodes[i];
                var lineSegments = polyNode.LineSegments;
                var firstSegment = lineSegments[0];
                var secondSegment = lineSegments[1];
                var position = PolylineCenter(firstSegment.SegmentLine, secondSegment.SegmentLine);

                LightPlaceInfos[i].Position = position;
            }
        }

        private Point3d PolylineCenter(Line firstLine, Line secLine)
        {
            var xSum = firstLine.StartPoint.X + secLine.EndPoint.X;
            var ySum = firstLine.StartPoint.Y + secLine.EndPoint.Y;

            return new Point3d(0.5 * xSum, 0.5 * ySum, 0);
        }

        private Line Line2dToLine(LineSegment2d lineSegment2d)
        {
            return new Line(ToPoint3D(lineSegment2d.StartPoint), ToPoint3D(lineSegment2d.EndPoint));
        }

        private Point3d ToPoint3D(Point2d point2D)
        {
            return new Point3d(point2D.X, point2D.Y, 0);
        }

        private PolylineNode SetPolyInfo(Polyline polyline, List<Line> lines)
        {
            var linesegments = new List<LineSegment>();
            if (lines[0].Length > lines[1].Length)
            {
                // 0, 2 long
                linesegments.Add(new LineSegment(lines[0], LineLengthType.LONG_TYPE));
                linesegments.Add(new LineSegment(lines[1], LineLengthType.SHORT_TYPE));
                linesegments.Add(new LineSegment(lines[2], LineLengthType.LONG_TYPE));
                linesegments.Add(new LineSegment(lines[3], LineLengthType.SHORT_TYPE));
            }
            else
            {
                // 1, 3 long
                linesegments.Add(new LineSegment(lines[0], LineLengthType.SHORT_TYPE));
                linesegments.Add(new LineSegment(lines[1], LineLengthType.LONG_TYPE));
                linesegments.Add(new LineSegment(lines[2], LineLengthType.SHORT_TYPE));
                linesegments.Add(new LineSegment(lines[3], LineLengthType.LONG_TYPE));
            }

            return (new PolylineNode(polyline, linesegments));
        }

        private List<ParkGroupInfo> CalculateNearParksProfile(List<ParkingRelatedGroup> parkingRelatedGroups)
        {
            var groupInfos = new List<ParkGroupInfo>();
            foreach (var parkGroup in parkingRelatedGroups)
            {
                groupInfos.Add(GenerateParkGroupProfile.MakeParkGroupProfile(parkGroup.RelatedParks));
            }

            return groupInfos;
        }
    }
}
