using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPLighting.ParkingStall.Geometry;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Business.UserInteraction
{
    public class WallPolygonInfoCalculator
    {
        private List<Polyline> m_polylines;

        private List<PolygonInfo> WallProfileInfos;

        public WallPolygonInfoCalculator(List<Polyline> polylines)
        {
            m_polylines = polylines;
        }

        public static List<PolygonInfo> DoWallPolygonInfoCalculator(List<Polyline> polylines)
        {
            var wallPolygonInfoCalculator = new WallPolygonInfoCalculator(polylines);
            wallPolygonInfoCalculator.Do();
            return wallPolygonInfoCalculator.WallProfileInfos;
        }

        public void Do()
        {
            CalculateMaps(m_polylines);
        }

        private void CalculateMaps(List<Polyline> srcPolys)
        {
            var tempPolygonInfos = new List<PolygonInfo>();
            foreach (var poly in srcPolys)
            {
                tempPolygonInfos.Add(new PolygonInfo(poly));
            }

            // 被包含则不是有效的轮廓区域
            for (int i = 0; i < tempPolygonInfos.Count; i++)
            {
                if (tempPolygonInfos[i].IsUsed)
                    continue;

                var curPoly = tempPolygonInfos[i].ExternalProfile;
                for (int j = 0; j < tempPolygonInfos.Count; j++)
                {
                    if (i == j)
                        continue;

                    var otherPoly = tempPolygonInfos[j].ExternalProfile;
                    var startPt = otherPoly.StartPoint;
                    if (PolylineContainsPoly(curPoly, otherPoly))
                    {
                        tempPolygonInfos[j].IsUsed = true;
                        tempPolygonInfos[i].InnerProfiles.Add(otherPoly);
                    }
                }
            }

            WallProfileInfos = tempPolygonInfos.Where(p =>
                !p.IsUsed).ToList();
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
