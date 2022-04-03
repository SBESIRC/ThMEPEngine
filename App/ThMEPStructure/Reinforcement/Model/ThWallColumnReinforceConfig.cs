using System.Collections.Generic;

namespace ThMEPStructure.Reinforcement.Model
{
    public class ThWallColumnReinforceConfig
    {
        private static readonly ThWallColumnReinforceConfig instance = new ThWallColumnReinforceConfig() { };
        public static ThWallColumnReinforceConfig Instance { get { return instance; } }
        internal ThWallColumnReinforceConfig()
        {
            Init();
        }
        static ThWallColumnReinforceConfig()
        {
        }
        /// <summary>
        /// 砼强度等级
        /// </summary>
        public string ConcreteStrengthGrade { get; set; }
        /// <summary>
        /// 墙所在部位(底部加强筋，其它部位)
        /// </summary>
        public string WallLocation { get; set; }
        /// <summary>
        /// 抗震等级
        /// </summary>
        public string AntiSeismicGrade { get; set; }
        /// <summary>
        /// 保护层厚度
        /// </summary>
        public double C { get; set; }
        /// <summary>
        /// 自适应柱表
        /// 取值为：A0、A1、A2...
        /// </summary>
        public string Frame { get; set; }
        /// <summary>
        /// 字符行高
        /// </summary>
        public double TableRowHeight { get; set; }
        /// <summary>
        /// 墙柱标高
        /// </summary>
        public string Elevation { get; set; }
        /// <summary>
        /// 绘图比例
        /// </summary>
        public string DrawScale { get; set; }
        /// <summary>
        /// 点筋线宽
        /// </summary>
        public double PointReinforceLineWeight { get; set; }
        /// <summary>
        /// 箍线线宽
        /// </summary>
        public double StirrupLineWeight { get; set; }

        public List<string> ConcreteStrengthGrades
        {
            get
            {
                return new List<string>() { "C35", "C40", "C45", "C50", "C55", "C60" };
            }
        }
        public List<string> AntiSeismicGrades
        {
            get
            {
                return new List<string>() { "一级", "二级", "三级", "四级" };
            }
        }
        public List<string> Frames
        {
            get
            {
                return new List<string>() { "A0", "A1", "A2", "A3" };
            }
        }
        public List<string> DrawScales
        {
            get
            {
                return new List<string>() { "1:1", "1:10", "1:20", "1:25", "1:30", "1:50" };
            }
        }
        public List<string> WallLocations
        {
            get
            {
                return new List<string>() { "底部加强区", "其它部位" };
            }
        }
        private void Init()
        {
            C = 20;
            Frame = "A1";
            DrawScale = "1:25";
            TableRowHeight = 800;
            Elevation = "0.000~3.000";
            ConcreteStrengthGrade = "C40";
            AntiSeismicGrade = "二级";
            StirrupLineWeight = 30.0;
            PointReinforceLineWeight = 50.0;
            WallLocation = "底部加强区";
        }
        public void Reset()
        {
            Init();
        }
    }
    public class ThEdgeComponentDrawConfig
    {
        private static readonly ThEdgeComponentDrawConfig instance = new ThEdgeComponentDrawConfig() { };
        public static ThEdgeComponentDrawConfig Instance { get { return instance; } }
        internal ThEdgeComponentDrawConfig()
        {
            Init();
        }
        static ThEdgeComponentDrawConfig()
        {
        }
        public List<string> DwgSources { get; private set; }
        public List<string> SortWays { get; private set; }
        public List<string> LeaderTypes { get; private set; }
        public List<string> MarkPositions { get; private set; }

        /// <summary>
        /// 引线形式
        /// </summary>
        public string LeaderType { get; set; }
        /// <summary>
        /// 标注位置
        /// </summary>
        public string MarkPosition { get; set; }
        public string SortWay { get; set; }
        public string DwgSource { get; set; }
        public int Size { get; set; }
        /// <summary>
        /// 配箍率
        /// </summary>
        public double StirrupRatio { get; set; }
        /// <summary>
        /// 墙层
        /// </summary>
        public string WallLayer { get; set; }
        /// <summary>
        /// 归并系数->考虑墙体
        /// </summary>
        public bool IsConsiderWall { get; set; }
        /// <summary>
        /// 配筋率
        /// </summary>
        public double ReinforceRatio { get; set; }
        /// <summary>
        /// 墙柱图层
        /// </summary>

        public string WallColumnLayer { get; set; }
        /// <summary>
        /// 文字图层
        /// </summary>
        public string TextLayer { get; set; }        
        private void Init()
        {
            Size = 1;
            StirrupRatio = 1.0;
            DwgSource = "YJK";
            WallLayer = "砼墙";
            ReinforceRatio = 0.06;
            IsConsiderWall = true;
            LeaderType = "折线引出";
            MarkPosition = "右上";
            SortWay = "从左到右，从下到上";
            WallColumnLayer = "边构";
            DwgSources = new List<string>() { "YJK" };
            SortWays = new List<string>() { "从左到右，从下到上" };
            LeaderTypes = new List<string>() { "折线引出" };
            MarkPositions = new List<string>() { "右上", "右下", "左上", "左下" };
            TextLayer = "dsptext_walledge、dsptext_walledgeCal、dsptext_walledgeCX";
        }
        public void Reset()
        {
            Init();
        }
    }
}
