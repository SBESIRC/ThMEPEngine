using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;

namespace ThParkingStall.Core.SParkingPartition
{
    public static class Sparam
    {
        #region 运行逻辑参数
        public static bool QuickCalculate = false;//快速计算
        public static bool AllowCompactedLane = VMStock.BoundaryShrink;//边界收缩
        public static bool AllowProcessEndLanes = false;//尽端停车
        public static bool LoopThroughEnd = false;//尽端环通
        #endregion

        #region VMStock参数
        public static int LayoutMode = ((int)LayoutDirection.FOLLOWPREVIOUS);
        public static double DisParallelCarLength = Math.Max(VMStock.ParallelSpotLength, VMStock.ParallelSpotWidth);
        public static double DisParallelCarWidth = Math.Min(VMStock.ParallelSpotLength, VMStock.ParallelSpotWidth);
        public static double DisVertCarLength = Math.Max(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth);
        public static double DisVertCarWidth = Math.Min(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth);
        public static double DisLaneWidth = VMStock.RoadWidth;
        public static double PillarSpacing = VMStock.ColumnWidth;
        public static double PillarNetLength = VMStock.ColumnSizeOfParalleToRoad;
        public static double PillarNetDepth = VMStock.ColumnSizeOfPerpendicularToRoad;
        public static double ThicknessOfPillarConstruct = VMStock.ColumnAdditionalSize;

        public static bool ScareEnabledForBackBackModule = true;
        public static bool GeneratePillars = PillarSpacing < DisVertCarWidth ? false : true;
        public static bool GenerateMiddlePillars = VMStock.MidColumnInDoubleRowModular;
        public static bool HasImpactOnDepthForPillarConstruct = VMStock.ColumnAdditionalInfluenceLaneWidth;
        #endregion

        #region 关系参数
        public static double DisPillarMoveDeeplyBackBack = VMStock.ColumnShiftDistanceOfDoubleRowModular > DisPillarDepth / 2 ? VMStock.ColumnShiftDistanceOfDoubleRowModular : DisPillarDepth / 2;
        public static double DisPillarMoveDeeplySingle = VMStock.ColumnShiftDistanceOfSingleRowModular > DisPillarDepth / 2 ? VMStock.ColumnShiftDistanceOfSingleRowModular : DisPillarDepth / 2;

        public static int CountPillarDist = (int)Math.Floor((PillarSpacing - PillarNetLength - ThicknessOfPillarConstruct * 2) / DisVertCarWidth);
        public static double DisPillarLength = PillarNetLength + ThicknessOfPillarConstruct * 2;
        public static double DisPillarDepth = PillarNetDepth + ThicknessOfPillarConstruct * 2;
        public static double DisCarAndHalfLane = DisLaneWidth / 2 + DisVertCarLength;
        public static double DisModulus = DisCarAndHalfLane* 2;
        public static double LengthCanGIntegralModulesConnectSingle = 4 * DisVertCarWidth + DisLaneWidth / 2 + DisPillarLength* 2;
        public static double LengthCanGIntegralModulesConnectDouble = 6 * DisVertCarWidth + DisLaneWidth + DisPillarLength* 2;
        public static double LengthCanGAdjLaneConnectSingle = DisLaneWidth / 2 + DisVertCarWidth* 4 + DisPillarLength* 2;
        public static double LengthCanGAdjLaneConnectDouble = DisLaneWidth + DisVertCarWidth* 7 + DisPillarLength* 2;
        public static double DisVertCarLengthBackBack = DisVertCarLength-DifferenceFromBackBack;
        public static double DisCarAndHalfLaneBackBack = DisLaneWidth / 2 + DisVertCarLengthBackBack;
        public static double DisBackBackModulus = DisVertCarLengthBackBack * 2 + DisLaneWidth;
        #endregion

        #region 常量参数
        public static double DifferenceFromBackBack = 200;
        public static double DisAllowMaxLaneLength = 50000;//允许生成车道最大长度-尽端环通车道条件判断长度
        public static double CollisionD = /*300;*/0;
        public static double CollisionTOP = /*100;*/0;
        public static double CollisionCT = 1400;
        public static double CollisionCM = 1500;
        public static double MaxLength = double.PositiveInfinity;
        #endregion

        #region 权值参数
        public static double ScareFactorForCollisionCheck = 0.999999;
        public static double LayoutScareFactor_Intergral = 0.7;
        public static double LayoutScareFactor_Adjacent = 0.7;
        public static double LayoutScareFactor_betweenBuilds = 0.7;
        public static double LayoutScareFactor_SingleVert = 0.7;
        public static double LayoutScareFactor_ParentDir = 3;
        #endregion

        public static void Init()
        {
            //背靠背缩进相关尺寸参数确认
            if (!ScareEnabledForBackBackModule)
            {
                DisBackBackModulus = DisModulus;
                DisVertCarLengthBackBack = DisVertCarLength;
                DisCarAndHalfLaneBackBack = DisCarAndHalfLane;
            }
            //如果是快速计算模式，所以附加功能模块均失效
            if (QuickCalculate)
            {
                AllowCompactedLane = false;
                AllowProcessEndLanes = false;
                LoopThroughEnd = false;
            }
        }

    }
    public enum LayoutDirection : int
    {
        LENGTH = 0,
        HORIZONTAL = 1,
        VERTICAL = 2,
        /// <summary>
        /// 与生成的车道线方向一致
        /// </summary>
        FOLLOWPREVIOUS = 3
    }
}
