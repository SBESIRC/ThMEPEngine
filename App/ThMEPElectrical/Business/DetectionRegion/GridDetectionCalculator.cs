using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 轴网生成大的轮廓
    /// </summary>
    class GridDetectionCalculator : DetectionCalculator
    {
        private List<Polyline> m_gridPolys; // 原始的轴网线
        private List<Polyline> m_holes; // 内部洞相关数据
        private List<Polyline> m_swallColumnPlys; // 剪力墙柱子数据
        ThMEPOriginTransformer m_originTransformer;

        public GridDetectionCalculator(Polyline wallPoly, List<Polyline> gridPolys, List<Polyline> holes, List<Polyline> swallColumnPlys, ThMEPOriginTransformer originTransformer)
            : base(wallPoly)
        {
            m_gridPolys = gridPolys;
            m_holes = holes;
            m_swallColumnPlys = swallColumnPlys;
            m_originTransformer = originTransformer;
        }

        public static List<PlaceInputProfileData> MakeGridDetectionCalculator(Polyline wallPoly, List<Polyline> gridPolys, List<Polyline> holes, List<Polyline> swallColumnPlys, ThMEPOriginTransformer originTransformer)
        {
            var gridDetectionCalculator = new GridDetectionCalculator(wallPoly, gridPolys, holes, swallColumnPlys, originTransformer);
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

            CalculateDetectionRegionWithHoles(detectRegions, m_swallColumnPlys);
            // 数据转换
            RegionBeamSpanProfileData = DetectRegion2ProfileData(detectRegions);
            DrawUtils.DrawGroup(RegionBeamSpanProfileData, m_originTransformer);
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
    }
}
