namespace ThMEPStructure.HuaRunPeiJin.Model
{
    public class ThWallColumnReinforceSet
    {
        /// <summary>
        /// 砼强度等级
        /// </summary>
        public string ConcreteStrengthGrade { get; set; }
        /// <summary>
        /// 抗震等级
        /// </summary>
        public string AntiSeismicGrade { get; set; }
        /// <summary>
        /// 保护层厚度
        /// </summary>
        public double C { get; set; }
        /// <summary>
        /// 自适应主表
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
    }
}
