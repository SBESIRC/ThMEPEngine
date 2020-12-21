using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPLighting.ParkingStall.Geometry;

namespace ThMEPLighting.ParkingStall.Worker.ParkingGroup
{
    public class GenerateValidParkPolys
    {
        private List<Polyline> m_parkPolylines;

        private List<Polyline> m_holePolylines;

        public List<Polyline> ValidPolylines
        {
            private get;
            set;
        } = new List<Polyline>();

        public GenerateValidParkPolys(List<Polyline> parkPolylines, List<Polyline> holePolylines)
        {
            m_parkPolylines = parkPolylines;
            m_holePolylines = holePolylines;
        }

        public static List<Polyline> MakeValidParkPolylines(List<Polyline> parkPolylines, List<Polyline> holePolylines)
        {
            var parkPolysGenerator = new GenerateValidParkPolys(parkPolylines, holePolylines);
            parkPolysGenerator.Do();
            return parkPolysGenerator.ValidPolylines;
        }

        public void Do()
        {
            foreach (var parkPoly in m_parkPolylines)
            {
                if (IsInvalidPolyline(parkPoly, m_holePolylines))
                    continue;

                ValidPolylines.Add(parkPoly);
            }
        }

        private bool IsInvalidPolyline(Polyline polyline, List<Polyline> holePolylines)
        {
            foreach (var holePoly in holePolylines)
            {
                if (PolylineContainsPoly(holePoly, polyline))
                    return true;
            }

            return false;
        }

        private bool PolylineContainsPoly(Polyline polyFir, Polyline polySec)
        {
            var secPts = polySec.Vertices();
            foreach (Point3d pt in secPts)
            {
                if (!GeomUtils.PtInLoop(polyFir, pt))
                    return false;
            }

            return true;
        }
    }
}
