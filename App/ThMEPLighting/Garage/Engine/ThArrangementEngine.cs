using System;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Engine
{
    public abstract class ThArrangementEngine : IDisposable
    {
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        /// <summary>
        /// 根据一个分区的布灯点的数量，计算的回路数
        /// </summary>
        public int LoopNumber { get; protected set; }
        public int DefaultStartNumber { get; protected set; }
        /// <summary>
        /// 通过布灯线生成的图
        /// </summary>
        public List<ThLightGraphService> Graphs { get; protected set; }

        public ThArrangementEngine(ThLightArrangeParameter arrangeParameter)
        {
            ArrangeParameter = arrangeParameter;
            Graphs = new List<ThLightGraphService>();
            DefaultStartNumber = ArrangeParameter.DefaultStartNumber;
        }
        public void Dispose()
        {
        }
        public abstract void Arrange(ThRegionBorder regionBorder);
        protected abstract void Preprocess(ThRegionBorder regionBorder);
        
        protected virtual void Filter(ThRegionBorder regionBorder)
        {
            double tTypeBranchFilterLength = Math.Max(ArrangeParameter.MinimumEdgeLength,
                ArrangeParameter.Margin*2.0+ ArrangeParameter.Interval / 2.0);
            regionBorder.DxCenterLines = ThFilterTTypeCenterLineService.Filter(
                regionBorder.DxCenterLines, tTypeBranchFilterLength);      
        }        
        
        public void SetDefaultStartNumber(int defaultStartNumber)
        {
            this.DefaultStartNumber = defaultStartNumber;
        }
    }
}
