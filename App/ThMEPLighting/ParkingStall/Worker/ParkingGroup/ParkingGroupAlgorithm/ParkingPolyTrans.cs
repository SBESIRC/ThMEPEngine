using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.ParkingGroup
{
    public class ParkingPolyTrans
    {
        private List<NearParks> m_nearParksLst;

        public List<PolylineNode> PolylineNodes
        {
            get;
            set;
        } = new List<PolylineNode>();


        public List<NearParksPolylineNode> NearParksPolylineNodes
        {
            get;
            set;
        } = new List<NearParksPolylineNode>();

        public static List<NearParksPolylineNode> MakeParkingPolyTrans2LineSegment(List<NearParks> nearParksLst)
        {
            var parkingPolyTrans = new ParkingPolyTrans(nearParksLst);
            parkingPolyTrans.DoPolysTrans();
            return parkingPolyTrans.NearParksPolylineNodes;
        }

        public ParkingPolyTrans(List<NearParks> nearParksLst)
        {
            m_nearParksLst = nearParksLst;
        }

        public void DoPolysTrans()
        {
            foreach (var nearParks in m_nearParksLst)
            {
                NearParksPolylineNodes.Add(DealNearPark(nearParks));
            }
        }
        
        private NearParksPolylineNode DealNearPark(NearParks nearParks)
        {
            var polyNodes = new List<PolylineNode>();
            foreach (var singlePoly in nearParks.Polylines)
            {
                var lines = new List<Line>();
                for (int i = 0; i < singlePoly.NumberOfVertices; i++)
                {
                    var line2d = singlePoly.GetLineSegment2dAt(i);
                    lines.Add(Line2dToLine(line2d));
                }

                polyNodes.Add(SetPolyInfo(singlePoly, lines));
            }

            return new NearParksPolylineNode(polyNodes);
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

        private Line Line2dToLine(LineSegment2d lineSegment2d)
        {
            return new Line(ToPoint3D(lineSegment2d.StartPoint), ToPoint3D(lineSegment2d.EndPoint));
        }

        private Point3d ToPoint3D(Point2d point2D)
        {
            return new Point3d(point2D.X, point2D.Y, 0);
        }
    }
}
