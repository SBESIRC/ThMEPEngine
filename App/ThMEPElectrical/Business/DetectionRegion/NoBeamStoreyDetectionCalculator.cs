using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 无梁楼盖探测区域布置
    /// </summary>
    public class NoBeamStoreyDetectionCalculator : DetectionCalculator
    {
        private List<Polyline> m_gridPolys;
        private List<Polyline> m_columns;

        /// <summary>
        /// 无梁楼盖处理
        /// </summary>
        /// <param name="gridPolys"></param>
        /// <param name="columns"></param>
        /// <param name="wallProfile"></param>
        /// <returns></returns>
        public static List<PlaceInputProfileData> MakeNoBeamStoreyDetectionCalculator(List<Polyline> gridPolys, List<Polyline> columns, Polyline wallProfile)
        {
            var noBeamStoreyCalculator = new NoBeamStoreyDetectionCalculator(gridPolys, columns, wallProfile);
            noBeamStoreyCalculator.Do();
            return noBeamStoreyCalculator.RegionBeamSpanProfileData;
        }

        public NoBeamStoreyDetectionCalculator(List<Polyline> gridPolys, List<Polyline> columns, Polyline wallProfile)
            : base(wallProfile)
        {
            m_gridPolys = gridPolys;
            m_columns = columns;
        }

        public void Do()
        {
            // 计算第一次的区域
            var gridProfiles = CalculateRegions(m_gridPolys);

            var innerRelatedInfos = new List<SecondBeamProfileInfo>();
            m_columns.ForEach(e => innerRelatedInfos.Add(new SecondBeamProfileInfo(e)));

            // 计算关系组
            var detectRegions = CalculateDetectionRelations(gridProfiles, innerRelatedInfos);

            // 数据转换
            RegionBeamSpanProfileData = DetectRegion2ProfileData(detectRegions);
            //DrawUtils.DrawGroup(RegionBeamSpanProfileData);
        }
    }
}
