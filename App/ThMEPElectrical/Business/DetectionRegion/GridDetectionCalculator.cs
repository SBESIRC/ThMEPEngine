using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThCADCore.NTS;
using ThMEPElectrical.Assistant;
using NFox.Cad;
using ThMEPElectrical.Geometry;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 轴网生成大的轮廓
    /// </summary>
    class GridDetectionCalculator : DetectionCalculator
    {
        private List<Polyline> m_gridPolys; // 原始的轴网线
        private List<Polyline> m_holes; // 内部洞相关数据

        public GridDetectionCalculator(Polyline wallPoly, List<Polyline> gridPolys, List<Polyline> holes)
            : base(wallPoly)
        {
            m_gridPolys = gridPolys;
            m_holes = holes;
        }

        public static List<PlaceInputProfileData> MakeGridDetectionCalculator(Polyline wallPoly, List<Polyline> gridPolys, List<Polyline> holes)
        {
            var gridDetectionCalculator = new GridDetectionCalculator(wallPoly, gridPolys, holes);
            gridDetectionCalculator.Do();

            return gridDetectionCalculator.RegionBeamSpanProfileData;
        }

        public void Do()
        {
            // 计算第一次的区域
            var gridProfiles = CalculateRegions(m_gridPolys);
            //DrawUtils.DrawProfile(gridProfiles.Polylines2Curves(), "gridRegions");
            //RegionBeamSpanProfileData = new List<PlaceInputProfileData>();
            //return;
            var innerRelatedInfos = new List<SecondBeamProfileInfo>();
            m_holes.ForEach(e => innerRelatedInfos.Add(new SecondBeamProfileInfo(e)));
            //DrawUtils.DrawProfile(m_holes.Polylines2Curves(), "relatedCurves");
            // 计算关系组
            var detectRegions = CalculateDetectionRelations(gridProfiles, innerRelatedInfos);

            // 数据转换
            RegionBeamSpanProfileData = DetectRegion2ProfileData(detectRegions);
            DrawUtils.DrawGroup(RegionBeamSpanProfileData);
        }


        protected override List<DetectionRegion> CalculateDetectionRelations(List<Polyline> profiles, List<SecondBeamProfileInfo> secondBeamInfos)
        {
            var detectRegions = new List<DetectionRegion>();

            // 探测区域
            foreach (var profile in profiles)
            {
                var detectRegion = new DetectionRegion()
                {
                    DetectionProfile = profile
                };

                detectRegions.Add(detectRegion);

                // 内部扣减区域
                foreach (var secondBeam in secondBeamInfos)
                {
                    var secondBeamProfile = secondBeam.Profile;

                    if (IsIntersectOrContains(profile, secondBeamProfile))
                    {
                        secondBeam.IsUsed = true;
                        detectRegion.secondBeams.Add(secondBeam);
                    }
                }
            }

            return detectRegions;
        }

        private bool IsIntersectOrContains(Polyline firPoly, Polyline secPoly)
        {
            var ptLst = secPoly.Polyline2Point2d();
            foreach (var pt in ptLst)
            {
                if (GeomUtils.PtInLoop(firPoly, pt))
                    return true;
            }

            if (firPoly.Intersects(secPoly))
                return true;

            return false;
        }
    }
}
