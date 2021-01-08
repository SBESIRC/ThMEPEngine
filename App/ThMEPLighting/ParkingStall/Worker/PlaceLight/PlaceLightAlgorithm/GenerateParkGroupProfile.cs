using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPLighting.ParkingStall.Assistant;
using ThMEPLighting.ParkingStall.Geometry;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.PlaceLight
{
    public class GenerateParkGroupProfile
    {
        private List<Polyline> m_parkPolylines;

        private Polyline m_profile;

        public ParkGroupInfo GroupInfo
        {
            get;
            set;
        }

        public GenerateParkGroupProfile(List<Polyline> polylines)
        {
            m_parkPolylines = polylines;
        }


        public static ParkGroupInfo MakeParkGroupProfile(List<Polyline> polylines)
        {
            var parkGenerator = new GenerateParkGroupProfile(polylines);
            parkGenerator.Do();
            return parkGenerator.GroupInfo;
        }

        public void Do()
        {
            CalculateMinProfile(m_parkPolylines);
            GroupInfo = new ParkGroupInfo(m_profile, m_parkPolylines.First());
        }

        private void CalculateMinProfile(List<Polyline> polylines)
        {
            var objs = new DBObjectCollection();

            polylines.ForEach(p => objs.Add(p));
            m_profile = objs.GetMinimumRectangle();
        }

        private void CalculateProfile(List<Point3d> ptLst)
        {
            var xLst = ptLst.Select(e => e.X).ToList();
            var yLst = ptLst.Select(e => e.Y).ToList();

            var xMin = xLst.Min();
            var yMin = yLst.Min();

            var xMax = xLst.Max();
            var yMax = yLst.Max();
            var leftBottomPt = new Point3d(xMin, yMin, 0);
            var rightTopPt = new Point3d(xMax, yMax, 0);
            var rightBottomPt = new Point3d(xMax, yMin, 0);
            var leftTopPt = new Point3d(xMin, yMax, 0);

            var pts = new List<Point3d>();
            pts.Add(leftBottomPt);
            pts.Add(rightBottomPt);
            pts.Add(rightTopPt);
            pts.Add(leftTopPt);

            m_profile = GeometryTransfer.Points2Poly(pts);
        }

        private List<Point3d> CalculatePoints(List<Polyline> polylines)
        {
            var pts = new List<Point3d>();
            foreach (var polyline in m_parkPolylines)
            {
                foreach (Point3d pt in polyline.Vertices())
                {
                    pts.Add(pt);
                }
            }

            return pts;
        }
    }
}
