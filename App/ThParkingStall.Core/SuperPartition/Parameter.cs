using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;

namespace ThParkingStall.Core.SuperPartition
{
    public static class Parameter
    {
        #region 运行模式参数
        //快速计算
        public static bool QuickCalculate = VMStock.SpeedUpMode;
        //收缩边界
        public static bool AllowCompactedLane = QuickCalculate ? VMStock.BoundaryShrink : false;

        #endregion
        #region VMStock参数
        //平行式车位长度
        public static double DisParallelCarLength = Math.Max(VMStock.ParallelSpotLength, VMStock.ParallelSpotWidth);
        //平行式车位宽度
        public static double DisParallelCarWidth = Math.Min(VMStock.ParallelSpotLength, VMStock.ParallelSpotWidth);
        //垂直式车位长度
        public static double DisVertCarLength = Math.Max(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth);
        //垂直式车位宽度
        public static double DisVertCarWidth = Math.Min(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth);
        //车道宽度
        public static double DisLaneWidth = VMStock.RoadWidth;
        //柱间距
        public static double PillarSpacing = VMStock.ColumnWidth;
        //背靠背车位柱子偏移进深
        public static double DisPillarMoveDeeplyBackBack = VMStock.ColumnShiftDistanceOfDoubleRowModular;
        //单排车位柱子偏移进深
        public static double DisPillarMoveDeeplySingle = VMStock.ColumnShiftDistanceOfSingleRowModular;
        //柱子宽度
        public static double PillarNetLength = VMStock.ColumnSizeOfParalleToRoad;
        //柱子深度
        public static double PillarNetDepth = VMStock.ColumnSizeOfPerpendicularToRoad;
        //柱子抹灰厚度
        public static double ThicknessOfPillarConstruct = VMStock.ColumnAdditionalSize;
        //柱子抹灰是否影响柱子尺寸
        public static bool HasImpactOnDepthForPillarConstruct = VMStock.ColumnAdditionalInfluenceLaneWidth;
        //生成中柱
        public static bool GenerateMiddlePillars = VMStock.MidColumnInDoubleRowModular;
        #endregion
        #region 关系参数
        //柱子计算宽度
        public static double DisPillarLength = PillarNetLength + ThicknessOfPillarConstruct * 2;
        //柱子计算深度
        public static double DisPillarDepth = PillarNetDepth + ThicknessOfPillarConstruct * 2;
        #endregion


    }
}
