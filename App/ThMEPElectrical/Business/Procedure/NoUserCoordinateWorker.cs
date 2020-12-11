using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Business.Procedure
{
    public class NoUserCoordinateWorker
    {
        private List<Curve> m_srcWallCurves;

        private List<PolygonInfo> WallProfileInfos;

        public NoUserCoordinateWorker(List<Curve> srcWallCurves)
        {
            m_srcWallCurves = srcWallCurves;
        }

        public static List<PolygonInfo> MakeNoUserCoordinateWorker(List<Curve> srcWallCurves)
        {
            var noUserWorker = new NoUserCoordinateWorker(srcWallCurves);
            noUserWorker.Do();
            return noUserWorker.WallProfileInfos;
        }

        public void Do()
        {
            var wallPolys = SplitWallWorker.MakeSplitWallProfiles(m_srcWallCurves);
            CalculateMaps(wallPolys);
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
