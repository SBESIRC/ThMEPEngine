namespace ThMEPStructure.Reinforcement.Model
{
    public abstract class ThColumnComponent
    {
        /// <summary>
        /// 编号
        /// </summary>
        public string Number { get; set; } = "";
        /// <summary>
        /// 保护层厚度
        /// </summary>
        public float C { get; set; }
        /// <summary>
        /// 箍筋规格
        /// </summary>
        public string Stirrup { get; set; } = "";
        /// <summary>
        /// 纵筋规格
        /// </summary>
        public string Reinforce { get; set; } = "";
        /// <summary>
        /// 抗震等级
        /// </summary>
        public string AntiSeismicGrade { get; set; } = "";
        /// <summary>
        /// 砼强度等级
        /// </summary>
        public string ConcreteStrengthGrade { get; set; } = "";
        /// <summary>
        /// 点筋宽度
        /// </summary>
        public double PointReinforceLineWeight { get; set; }
        /// <summary>
        /// 箍筋线宽
        /// </summary>
        public double StirrupLineWeight { get; set; }
    }
}
