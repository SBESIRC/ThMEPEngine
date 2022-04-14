using System.Collections.Generic;
using ThMEPStructure.Reinforcement.Service;

namespace ThMEPStructure.Reinforcement.Model
{
    public class ThColumnReinforceConfig
    {
        private static readonly ThColumnReinforceConfig instance = new ThColumnReinforceConfig() { };
        public static ThColumnReinforceConfig Instance { get { return instance; } }
        internal ThColumnReinforceConfig()
        {
            Init();
        }
        static ThColumnReinforceConfig()
        {
        }
        /// <summary>
        /// 砼强度等级
        /// </summary>
        public string ConcreteStrengthGrade { get; set; }
        /// <summary>
        /// λ是否大于2
        /// </summary>
        public bool IsBiggerThanTwo { get; set; }
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
                return new List<string>() { "C35", "C40", "C45", "C50"};
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
                return new List<string>() { "A0", "A1", "A2"};
            }
        }
        public List<string> DrawScales
        {
            get
            {
                return new List<string>() { "1:1", "1:10", "1:20", "1:25", "1:30", "1:50" };
            }
        }
        private void Init()
        {
            C = 20;
            Frame = "A1";
            DrawScale = "1:25";
            TableRowHeight = 800;
            IsBiggerThanTwo = true;
            Elevation = "0.000~3.000";
            ConcreteStrengthGrade = "C40";
            AntiSeismicGrade = "二级";
            StirrupLineWeight = 30.0;
            PointReinforceLineWeight = 50.0;
        }
        public void Reset()
        {
            Init();
        }
    }
    public class ThColumnReinforceDrawConfig
    {
        private static readonly ThColumnReinforceDrawConfig instance = new ThColumnReinforceDrawConfig() { };
        public static ThColumnReinforceDrawConfig Instance { get { return instance; } }
        internal ThColumnReinforceDrawConfig()
        {
            Init();
        }
        static ThColumnReinforceDrawConfig()
        {
        }
        public string LayerLinkCharater { get; private set; } = "、";
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
        /// 柱图层
        /// </summary>
        public string ColumnLayer { get; set; }
        public List<string> ColumnLayers
        {
            get
            {
                return ThReinforcementUtils.Split(ColumnLayer,LayerLinkCharater);
            }
        }
        /// <summary>
        /// 文字图层
        /// </summary>
        public string TextLayer { get; set; }
        public List<string> TextLayers
        {
            get
            {
                return ThReinforcementUtils.Split(TextLayer, LayerLinkCharater);
            }
        }
        private void Init()
        {
            Size = 1;
            DwgSource = "YJK";
            LeaderType = "折线引出";
            MarkPosition = "右上";
            TextLayer = "dsptext_col";
            SortWay = "从左到右，从下到上";
            ColumnLayer = "砼柱、SPRE_SPECCL_JIAO";
            DwgSources = new List<string>() { "YJK" };
            SortWays = new List<string>() { "从左到右，从下到上" };
            LeaderTypes = new List<string>() { "折线引出" };
            MarkPositions = new List<string>() { "右上", "右下", "左上", "左下" };
        }
        public void Reset()
        {
            Init();
        }
    }
}
