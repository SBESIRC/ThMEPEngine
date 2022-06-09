using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.InterProcess
{
    [Serializable]
    public static class VMStock
    {
        private static int _RunMode = 0;
        public static int RunMode //自动,横纵
        {
            get { return _RunMode; }
        }
        //平行车位尺寸,长度
        private static int _ParallelSpotLength = 6000; //mm

        public static int ParallelSpotLength
        {
            get
            {
                return _ParallelSpotLength;
            }
        }

        //平行车位尺寸,宽度
        private static int _ParallelSpotWidth = 2400; //mm

        public static int ParallelSpotWidth
        {
            get
            {
                return _ParallelSpotWidth;
            }
        }

        //垂直车位尺寸, 长度
        private static int _VerticalSpotLength = 5300; //mm

        public static int VerticalSpotLength
        {
            get
            {
                return _VerticalSpotLength;
            }
        }

        //垂直车位尺寸, 宽度
        private static int _VerticalSpotWidth = 2400; //mm

        public static int VerticalSpotWidth
        {
            get
            {
                return _VerticalSpotWidth;
            }
        }

        private static int _RoadWidth = 5500; //mm

        public static int RoadWidth
        {
            get
            {
                return _RoadWidth;
            }
        }

        //平行于车道方向柱子尺寸
        private static int _ColumnSizeOfParalleToRoad = 500; //mm

        public static int ColumnSizeOfParalleToRoad
        {
            get
            {
                return _ColumnSizeOfParalleToRoad;
            }
        }
        //垂直于车道方向柱子尺寸
        private static int _ColumnSizeOfPerpendicularToRoad = 500; //mm

        public static int ColumnSizeOfPerpendicularToRoad
        {
            get
            {
                return _ColumnSizeOfPerpendicularToRoad;
            }
        }
        //柱子完成面尺寸
        private static int _ColumnAdditionalSize = 50; //mm

        public static int ColumnAdditionalSize
        {
            get
            {
                return _ColumnAdditionalSize;
            }
        }

        //柱子完成面是否影响车道净宽
        private static bool _ColumnAdditionalInfluenceLaneWidth = true;

        public static bool ColumnAdditionalInfluenceLaneWidth
        {
            get { return _ColumnAdditionalInfluenceLaneWidth; }
        }

        //最大柱间距,需要改成柱间距
        private static int _ColumnWidth = 7800; //mm

        public static int ColumnWidth
        {
            get
            {
                return _ColumnWidth;
            }
        }

        //背靠背模块：柱子沿车道法向偏移距离
        private static int _ColumnShiftDistanceOfDoubleRowModular = 550; //mm

        public static int ColumnShiftDistanceOfDoubleRowModular
        {
            get
            {
                return _ColumnShiftDistanceOfDoubleRowModular;
            }
        }

        //背靠背模块是否使用中柱
        private static bool _MidColumnInDoubleRowModular = true;

        public static bool MidColumnInDoubleRowModular
        {
            get { return _MidColumnInDoubleRowModular; }
        }

        //单排模块：柱子沿车道法向偏移距离
        private static int _ColumnShiftDistanceOfSingleRowModular = 550; //mm

        public static int ColumnShiftDistanceOfSingleRowModular
        {
            get
            {
                return _ColumnShiftDistanceOfSingleRowModular;
            }
        }
        //车位碰撞参数D1（侧面）
        private static int _D1 = 300;
        public static int D1
        {
            get
            {
                return _D1;
            }
        }
        //车位碰撞参数D2（尾部）
        private static int _D2 = 200;
        public static int D2
        {
            get
            {
                return _D2;
            }
        }
        private static double _LayoutScareFactor_Intergral;
        //横向优先_纵向车道计算长度调整_背靠背模块
        public static double LayoutScareFactor_Intergral { get { return _LayoutScareFactor_Intergral; } }

        private static double _LayoutScareFactor_Adjacent;
        //横向优先_纵向车道计算长度调整_车道近段垂直生成相邻车道模块
        public static double LayoutScareFactor_Adjacent { get { return _LayoutScareFactor_Adjacent; } }

        private static double _LayoutScareFactor_betweenBuilds;
        //横向优先_纵向车道计算长度调整_建筑物之间的车道生成模块
        public static double LayoutScareFactor_betweenBuilds { get { return _LayoutScareFactor_betweenBuilds; } }

        private static double _LayoutScareFactor_SingleVert;
        //横向优先_纵向车道计算长度调整_孤立的单排垂直式模块
        public static double LayoutScareFactor_SingleVert { get { return _LayoutScareFactor_SingleVert; } }

        private static double _SingleVertModulePlacementFactor;
        //孤立的单排垂直式模块生成条件控制_非单排模块车位预计数与孤立单排车位的比值
        public static double SingleVertModulePlacementFactor { get { return _SingleVertModulePlacementFactor; } }
        public static void Init(DataWraper datawraper)
        {
            _RunMode = datawraper.RunMode;
            //平行车位尺寸,长度
            _ParallelSpotLength = datawraper.ParallelSpotLength; //mm
            //平行车位尺寸,宽度
            _ParallelSpotWidth = datawraper.ParallelSpotWidth; //mm
            //垂直车位尺寸, 长度
            _VerticalSpotLength = datawraper.VerticalSpotLength; //mm
            //垂直车位尺寸, 宽度
            _VerticalSpotWidth = datawraper.VerticalSpotWidth; //mm
            //道路宽
            _RoadWidth = datawraper.RoadWidth; //mm
            //平行于车道方向柱子尺寸
            _ColumnSizeOfParalleToRoad = datawraper.ColumnSizeOfParalleToRoad; //mm
            //垂直于车道方向柱子尺寸
            _ColumnSizeOfPerpendicularToRoad = datawraper.ColumnSizeOfPerpendicularToRoad; //mm
            //柱子完成面尺寸
            _ColumnAdditionalSize = datawraper.ColumnAdditionalSize; //mm
            //柱子完成面是否影响车道净宽
            _ColumnAdditionalInfluenceLaneWidth = datawraper.ColumnAdditionalInfluenceLaneWidth;
            //最大柱间距,需要改成柱间距
            _ColumnWidth = datawraper.ColumnWidth; //mm
            //背靠背模块：柱子沿车道法向偏移距离
            _ColumnShiftDistanceOfDoubleRowModular = datawraper.ColumnShiftDistanceOfDoubleRowModular; //mm
            //背靠背模块是否使用中柱
            _MidColumnInDoubleRowModular = datawraper.MidColumnInDoubleRowModular;
            //单排模块：柱子沿车道法向偏移距离
            _ColumnShiftDistanceOfSingleRowModular = datawraper.ColumnShiftDistanceOfSingleRowModular; //mm
            //车位碰撞参数D1（侧面）
            _D1 = datawraper.D1;
            //车位碰撞参数D2（尾部）
            _D2 = datawraper.D2;
            //横向优先_纵向车道计算长度调整_背靠背模块
            _LayoutScareFactor_Intergral = datawraper.LayoutScareFactor_Intergral;
            //横向优先_纵向车道计算长度调整_车道近段垂直生成相邻车道模块
            _LayoutScareFactor_Adjacent = datawraper.LayoutScareFactor_Adjacent;
            //横向优先_纵向车道计算长度调整_建筑物之间的车道生成模块
            _LayoutScareFactor_betweenBuilds = datawraper.LayoutScareFactor_betweenBuilds;
            //横向优先_纵向车道计算长度调整_孤立的单排垂直式模块
            _LayoutScareFactor_SingleVert = datawraper.LayoutScareFactor_SingleVert;
            //孤立的单排垂直式模块生成条件控制_非单排模块车位预计数与孤立单排车位的比值
            _SingleVertModulePlacementFactor = datawraper.SingleVertModulePlacementFactor;
        }
    }
}
