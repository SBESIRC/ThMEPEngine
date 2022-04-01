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
        /// 构造筋部位(底部加强筋，其它部位)
        /// </summary>
        public string GbzPlace { get; set; }
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
            GbzPlace = "底部加强区";
        }
        public void Reset()
        {
            Init();
        }
    }
}
