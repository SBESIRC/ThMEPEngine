using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPElectrical.Business.ClearScene;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.Business.Procedure
{
    public class UserCoordinateWorker
    {
        public List<PolygonInfo> WallProfileInfos;
        private string m_ucsLayerName;
        private List<Curve> m_srcWallCurves;
        private ThMEPOriginTransformer m_originTransformer;

        private List<Polyline> m_srcWallPolys; // poly 墙线


        public static List<PolygonInfo> MakeUserCoordinateWorker(List<Curve> srcWallCurves, string ucsLayerName, ThMEPOriginTransformer originTransformer)
        {
            var ucsCoorWorker = new UserCoordinateWorker(srcWallCurves, ucsLayerName, originTransformer);
            ucsCoorWorker.Do();
            return ucsCoorWorker.WallProfileInfos;
        }

        public static List<PolygonInfo> MakeUserCoordinateWorkerFromSelectPolys(List<Polyline> wallPolys, string ucsLayerName, ThMEPOriginTransformer originTransformer)
        {
            var ucsCoorWorker = new UserCoordinateWorker(wallPolys, ucsLayerName, originTransformer);
            ucsCoorWorker.DoWallPolys();
            return ucsCoorWorker.WallProfileInfos;
        }

        public UserCoordinateWorker(List<Polyline> srcWallPolys, string ucsLayerName, ThMEPOriginTransformer originTransformer)
        {
            m_srcWallPolys = srcWallPolys;
            m_ucsLayerName = ucsLayerName;
            m_originTransformer = originTransformer;
        }

        public UserCoordinateWorker(List<Curve> srcWallCurves, string ucsLayerName, ThMEPOriginTransformer originTransformer)
        {
            m_srcWallCurves = srcWallCurves;
            m_ucsLayerName = ucsLayerName;
            m_originTransformer = originTransformer;
        }

        public void DoWallPolys()
        {
            var ucsInfos = UcsInfoCalculator.MakeUcsInfos(m_ucsLayerName, m_originTransformer);
            CalculatePolygonInfo(ucsInfos, m_srcWallPolys);
        }

        public void Do()
        {
            var ucsInfos = UcsInfoCalculator.MakeUcsInfos(m_ucsLayerName, m_originTransformer);
            var wallPolys = SplitWallWorker.MakeSplitWallProfiles(m_srcWallCurves);
            //DrawUtils.DrawProfile(wallPolys.Polylines2Curves(), "wallPolys");
            //WallProfileInfos = new List<PolygonInfo>();
            //return;

            ClearSmoke.MakeClearSmoke(wallPolys,m_originTransformer);
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
    }
}
