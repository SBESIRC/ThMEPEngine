using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 无梁楼盖探测区域布置
    /// </summary>
    public class NoBeamStoreyDetectionCalculator : DetectionCalculator
    {
        private List<Polyline> m_gridPolys;
        private List<Polyline> m_swallColumns;
        private ThMEPOriginTransformer m_originTransformer;

        /// <summary>
        /// 无梁楼盖处理
        /// </summary>
        /// <param name="gridPolys"></param>
        /// <param name="columns"></param>
        /// <param name="wallProfile"></param>
        /// <returns></returns>
        public static List<PlaceInputProfileData> MakeNoBeamStoreyDetectionCalculator(List<Polyline> gridPolys, List<Polyline> swallColumns, Polyline wallProfile, ThMEPOriginTransformer originTransformer)
        {
            var noBeamStoreyCalculator = new NoBeamStoreyDetectionCalculator(gridPolys, swallColumns, wallProfile,originTransformer);
            noBeamStoreyCalculator.Do();
            return noBeamStoreyCalculator.RegionBeamSpanProfileData;
        }

        public NoBeamStoreyDetectionCalculator(List<Polyline> gridPolys, List<Polyline> swallColumns, Polyline wallProfile, ThMEPOriginTransformer originTransformer)
            : base(wallProfile)
        {
            m_gridPolys = gridPolys;
            m_swallColumns = swallColumns;
        }

        public void Do()
        {
            // 计算第一次的区域
            var gridProfiles = CalculateRegions(m_gridPolys);

            var innerRelatedInfos = new List<SecondBeamProfileInfo>();
            m_swallColumns.ForEach(e => innerRelatedInfos.Add(new SecondBeamProfileInfo(e)));

            // 计算关系组
            var detectRegions = CalculateDetectionRelations(gridProfiles, innerRelatedInfos);
            CalculateDetectionRegionWithHoles(detectRegions, m_swallColumns);
            // 数据转换
            RegionBeamSpanProfileData = DetectRegion2ProfileData(detectRegions);
            DrawUtils.DrawGroup(RegionBeamSpanProfileData,m_originTransformer);
        }
    }
}
