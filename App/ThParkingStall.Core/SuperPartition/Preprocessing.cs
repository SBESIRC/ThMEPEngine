using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.SuperPartition.Parameter;

namespace ThParkingStall.Core.SuperPartition
{
    public class Preprocessing
    {
        public Preprocessing()
        {

        }
        public void Execute()
        {
            AdjustPillarDimension();
        }
        void AdjustPillarDimension()
        {
            //如果柱子完成面宽度对车道间距没有影响，则在一开始便将柱子缩小为净尺寸
            if (!HasImpactOnDepthForPillarConstruct)
            {
                DisPillarLength = PillarNetLength;
                DisPillarDepth = PillarNetDepth;
            }
        }
    }
}
