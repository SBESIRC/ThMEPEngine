using System;
using System.Collections.Generic;

namespace ThMEPLighting.Garage.Model
{
    public class ThLightArrangeParameter
    {
        /// <summary>
        /// 是否输出天正桥架
        /// </summary>
        public bool IsTCHCableTray { get; set; }
        /// <summary>
        /// 灯间距
        /// </summary>
        public double Interval { get; set; } = 2700;
        /// <summary>
        /// 灯距离线边的最小距离
        /// </summary>
        public double Margin { get; set; } = 800.0;
        /// <summary>
        /// 线槽间距
        /// (双排布置，线槽往两端偏移的间距)
        /// </summary>
        public double DoubleRowOffsetDis { get; set; } = 2700.0;
        /// <summary>
        /// 线槽高度
        /// </summary>
        public double Height { get; set; } = 75.0;
        /// <summary>
        /// 线槽宽度
        /// </summary>
        public double Width { get; set; } = 150.0;
        /// <summary>
        /// 单排布置
        /// </summary>
        public bool IsSingleRow { get; set; } = false;
        /// <summary>
        /// 用户指定回路数量
        /// </summary>
        public int LoopNumber { get; set; } = 4;
        /// <summary>
        /// 自动计算回路数量
        /// </summary>
        public bool AutoCalculate { get; set; } = false;
        /// <summary>
        /// 一个回路多少盏灯
        /// </summary>
        public int LightNumberOfLoop { get; set; } = 25;
        /// <summary>
        /// 图纸比例
        /// </summary>
        public double PaperRatio { get; set; } = 1.0;
        /// <summary>
        /// 灯编号文字高度
        /// </summary>
        public double LightNumberTextHeight { get; set; } = 350;
        /// <summary>
        /// 灯编号文字宽度因子
        /// </summary>
        public double LightNumberTextWidthFactor { get; set; } = 0.65;
        /// <summary>
        /// 灯编号文字样式
        /// </summary>
        public string LightNumberTextStyle { get; set; } = "TH-STYLE3";
        public InstallWay InstallWay { get; set; } = InstallWay.CableTray;
        public ConnectMode ConnectMode { get; set; } = ConnectMode.CircularArc;
        /// <summary>
        /// 布置模式
        /// </summary>
        public LayoutMode LayoutMode { get; set; } = LayoutMode.ColumnSpan;
        /// <summary>
        /// 柱子靠近车道线的距离
        /// </summary>
        public double NearByDistance { get; set; } = 5400;
        /// <summary>
        /// 默认灯的起始编号
        /// </summary>
        public int DefaultStartNumber { get; set; } = 1;
        /// <summary>
        /// 灯具长度
        /// </summary>
        public double LampLength { get; set; } = 1200.0;
        /// <summary>
        /// 灯两边与连线之间的间隔长度
        /// </summary>
        public double LampSideIntervalLength { get; set; } = 150.0;
        /// <summary>
        /// 灯文字底部与灯基点偏移线的间隙
        /// </summary>
        public double LightNumberTextGap { get; set; } = 100.0;
        /// <summary>
        /// 跳接线偏移高度
        /// </summary>
        public double JumpWireOffsetDistance
        {
            get
            {
                return Width / 2.0;
            }
        }

        /// <summary>
        /// 弧形跳线顶部距离灯线的高度
        /// </summary>
        public double CircularArcTopDistanceToDxLine { get; set; } = 600.0;

        /// <summary>
        /// 跳线连接，相交线的打断长度
        /// </summary>
        public double LightWireBreakLength { get; set; } = 150.0;
        /// <summary>
        /// 建筑车道线图层
        /// </summary>
        public List<string> LaneLineLayers { get; set; } = new List<string>();

        /// <summary>
        /// 过滤点的距离
        /// </summary>
        public double FilterPointDistance
        {
            get
            {
                return 0.4 * Interval;
            }
        }
        /// <summary>
        /// 为了避免T字、十字路口灯比较拥挤，需要将分支布灯的线内缩一点，形成不可布区域
        /// </summary>
        public double ShortenDistance { get; set; } = 500.0;

        public int GetLoopNumber(int lightNumber)
        {
            if (AutoCalculate)
            {
                return IsSingleRow ?
                    CalculateSingleRowLoopNumber(lightNumber, LightNumberOfLoop) :
                    CalculateDoubleRowLoopNumber(lightNumber, LightNumberOfLoop);
            }
            else
            {
                return IsSingleRow ?
                    GetSingleRowUILoopNumber() :
                    GetDoubleRowUILoopNumber();
            }
        }
        private int CalculateSingleRowLoopNumber(int lightNumbers, int lightNumberOfLoop)
        {
            double number = Math.Ceiling(lightNumbers * 1.0 / lightNumberOfLoop);
            if (number < 2)
            {
                number = 2;
            }
            return (int)number;
        }
        /// <summary>
        /// 根据灯的数量和每一个回路包含灯的数量，计算灯回路
        /// eg. 灯的数量为100,每个回路25盏灯，计算得出4个回路
        /// </summary>
        /// <param name="lightNumbers">灯的数量</param>
        /// <param name="lightNumberOfLoop">每一个回路包含多少盏灯</param>
        /// <returns></returns>
        private int CalculateDoubleRowLoopNumber(int lightNumbers, int lightNumberOfLoop)
        {
            var value = lightNumbers * 1.0 / lightNumberOfLoop;
            double number = Math.Ceiling(value);
            int intNumber = (int)number;
            if (intNumber < 4)
            {
                intNumber = 4;
            }
            if (intNumber % 2 == 1)
            {
                intNumber += 1;
            }
            return intNumber / 2; // 计算单回路数量
        }
        private int GetSingleRowUILoopNumber()
        {
            return LoopNumber < 2 ? 2 : LoopNumber;
        }
        private int GetDoubleRowUILoopNumber()
        {
            int result = 0;
            if (LoopNumber < 4)
            {
                result = 4;
            }
            else
            {
                result = LoopNumber;
            }
            if (result % 2 == 1)
            {
                result += 1;
            }
            return result / 2; // 计算单回路数量
        }
    }
    public enum InstallWay
    {
        /// <summary>
        /// 线槽安装
        /// </summary>
        CableTray,
        /// <summary>
        /// 吊链安装
        /// </summary>
        Chain,
    }
    public enum LayoutMode
    {
        /// <summary>
        /// 按柱跨布置
        /// </summary>
        ColumnSpan,
        /// <summary>
        /// 避梁布置
        /// </summary>
        AvoidBeam,
        /// <summary>
        /// 等间距布置
        /// </summary>
        EqualDistance,
        /// <summary>
        /// 可跨梁布置
        /// </summary>
        SpanBeam
    }
    public enum ConnectMode
    {
        /// <summary>
        /// 直线连接
        /// </summary>
        Linear,
        /// <summary>
        /// 弧线连接
        /// </summary>
        CircularArc,
    }
}
