using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.SParkingPartition.Sparam;

namespace ThParkingStall.Core.SParkingPartition
{
    public class SParkingService
    {
        public SParkingService(SPartition spartition)
        {
            SPartition = spartition;
        }
        private SPartition SPartition { get; set; }
        /// <summary>
        /// 是否允许通过边界收缩的方式重排布
        /// </summary>
        private bool AllowRelayoutByCompactingBound = Sparam.AllowCompactedLane;
        public void Process()
        {
            PreProcess();
            GenerateLanes();
            GenerateCars();
            PostProcess();
            if (AllowRelayoutByCompactingBound)
                ReLayoutByCompactingBound();
        }
        void PreProcess()
        {
            ComfirmPillarDimensions();
        }
        void GenerateLanes()
        {
            SLaneGenerationService service = new SLaneGenerationService(SPartition);
            service.Process();
        }
        void GenerateCars()
        {

        }
        void PostProcess()
        {

        }
        void ReLayoutByCompactingBound()
        {

        }
        /// <summary>
        /// 如果柱子完成面宽度对车道间距没有影响，则在一开始便将柱子缩小为净尺寸
        /// </summary>
        void ComfirmPillarDimensions()
        {
            if (!HasImpactOnDepthForPillarConstruct)
            {
                DisPillarLength = PillarNetLength;
                DisPillarDepth = PillarNetDepth;
            }
        }
    }
}
