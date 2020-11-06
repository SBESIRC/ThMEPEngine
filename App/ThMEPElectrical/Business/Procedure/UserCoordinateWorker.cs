using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;
using ThMEPElectrical.Assistant;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.Business.Procedure
{
    public class UserCoordinateWorker
    {
        public List<PolygonInfo> WallProfileInfos;
        private string m_ucsLayerName;
        private List<Curve> m_srcWallCurves;

        private List<Polyline> m_srcWallPolys; // poly 墙线


        public static List<PolygonInfo> MakeUserCoordinateWorker(List<Curve> srcWallCurves, string ucsLayerName)
        {
            var ucsCoorWorker = new UserCoordinateWorker(srcWallCurves, ucsLayerName);
            ucsCoorWorker.Do();
            return ucsCoorWorker.WallProfileInfos;
        }

        public static List<PolygonInfo> MakeUserCoordinateWorkerFromSelectPolys(List<Polyline> wallPolys, string ucsLayerName)
        {
            var ucsCoorWorker = new UserCoordinateWorker(wallPolys, ucsLayerName);
            ucsCoorWorker.DoWallPolys();
            return ucsCoorWorker.WallProfileInfos;
        }

        public UserCoordinateWorker(List<Polyline> srcWallPolys, string ucsLayerName)
        {
            m_srcWallPolys = srcWallPolys;
            m_ucsLayerName = ucsLayerName;
        }

        public UserCoordinateWorker(List<Curve> srcWallCurves, string ucsLayerName)
        {
            m_srcWallCurves = srcWallCurves;
            m_ucsLayerName = ucsLayerName;
        }

        public void DoWallPolys()
        {
            var ucsInfos = UcsInfoCalculator.MakeUcsInfos(m_ucsLayerName);
            CalculatePolygonInfo(ucsInfos, m_srcWallPolys);
        }

        public void Do()
        {
            var ucsInfos = UcsInfoCalculator.MakeUcsInfos(m_ucsLayerName);
            var wallPolys = WallDataPicker.MakeWallPickProfiles(m_srcWallCurves);
            CalculatePolygonInfo(ucsInfos, wallPolys);
        }

        private void CalculatePolygonInfo(List<UcsInfo> ucsInfos, List<Polyline> srcWallPolys)
        {
            CalculateMaps(srcWallPolys);
            CalculateUcsInfoMap(ucsInfos);
        }

        private void CalculateUcsInfoMap(List<UcsInfo> ucsInfos)
        {
            foreach (var singleRegionInfo in WallProfileInfos)
            {
                CalculateUcsInfoMap(singleRegionInfo, ucsInfos);
            }
        }

        private void CalculateUcsInfoMap(PolygonInfo polygonInfo, List<UcsInfo> ucsInfos)
        {
            bool IsChanged = false;
            foreach (var ucsInfo in ucsInfos)
            {
                if (GeomUtils.PtInLoop(polygonInfo.ExternalProfile, ucsInfo.ucsInsertPoint))
                {
                    polygonInfo.UserSys = ucsInfo.ucsMatrix;
                    polygonInfo.BlockXAxis = ucsInfo.BlockXAxis;
                    //var coordinateSystem = new CoordinateSystem3d(Point3d.Origin, polygonInfo.UserSys.CoordinateSystem3d.Xaxis, polygonInfo.UserSys.CoordinateSystem3d.Yaxis);
                    //polygonInfo.UserSys = ;
                    polygonInfo.rotateAngle = ucsInfo.rotateAngle;
                    polygonInfo.OriginMatrix = ucsInfo.OriginMatrix;
                    IsChanged = true;
                }
            }

            if (!IsChanged)
            {
                var userSystemX = polygonInfo.UserSys.CoordinateSystem3d.Xaxis;
                var worldSystemX = Vector3d.XAxis;
                polygonInfo.rotateAngle = GeomUtils.CalAngle(userSystemX, worldSystemX);
            }
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

        private void CalculateMaps(List<Polyline> srcPolys)
        {
            var tempPolygonInfos = new List<PolygonInfo>();
            foreach (var poly in srcPolys)
            {
                tempPolygonInfos.Add(new PolygonInfo(poly));
            }

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
    }
}
