namespace ThMEPStructure.HuaRunPeiJin.Model
{
    public abstract class ThEdgeComponent
    {
        /// <summary>
        /// 编号
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 保护层厚度
        /// </summary>
        public double C { get; set; }
        /// <summary>
        /// 箍筋规格
        /// </summary>
        public string Stirrup { get; set; }
        /// <summary>
        /// 纵筋规格
        /// </summary>
        public string Reinforce { get; set; }
        /// <summary>
        /// 抗震等级
        /// </summary>
        public string AntiSeismicGrade { get; set; }
        /// <summary>
        /// 类型
        /// 取值："A" or "B"
        /// </summary>
        public string Type { get; set; }
    }
}
