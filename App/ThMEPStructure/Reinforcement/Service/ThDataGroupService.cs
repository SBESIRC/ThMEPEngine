using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThDataGroupService
    {
        private bool ConsiderWall { get; set; }
        /// <summary>
        /// 配筋率
        /// </summary>
        private double ReinforceRatio { get; set; }
        /// <summary>
        /// 配箍率
        /// </summary>
        private double StirrupRatio { get; set; }
        public ThDataGroupService(
            bool considerWall,
            double stirrupRatio,
            double reinforceRatio)
        {
            ConsiderWall = considerWall;
            StirrupRatio = stirrupRatio;
            ReinforceRatio = reinforceRatio;
        }
        public void Group(List<EdgeComponentExtractInfo> infos)
        {
            throw new NotImplementedException();
        }
        private void GroupStandards(List<EdgeComponentExtractInfo> infos)
        {
            //TODO
            //分组规则
            //1、外形相同 2、尺寸相同，3、YBZ、GBZ 4、考虑墙体位置
            throw new NotImplementedException();
        }
        private void GroupStandardCals(List<EdgeComponentExtractInfo> infos)
        {
            /*
             * 分组规则：
             * 1、外形相同 2、尺寸相同，3、YBZ、GBZ 4、考虑墙体位置
             * 上述分组完后，再对组内的元素先按配筋率再按配箍率的阶梯进行组划分
             * 组划分原则为：以同类的最小值作为基数，逐级向上划分
             * 例如：｛200,220,240,250,260,255,300｝一组数，按归并阶差50，
             * 归并结果为｛250,250,250,250,300,300,300｝
            */
            throw new NotImplementedException();
        }
    }
}
