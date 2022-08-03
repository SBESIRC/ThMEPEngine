namespace ThMEPStructure.Reinforcement.TSSD
{
    public class TSSDWallColumnConfig
    {
        /// <summary>
        /// 砼强度等级
        /// </summary>
        public string ConcreteStrengthGrade { get; set; } = "";
        /// <summary>
        /// 抗震等级
        /// </summary>
        public string AntiSeismicGrade { get; set; } = "";
        /// <summary>
        /// 墙所在部位(底部加强筋，其它部位)
        /// </summary>
        public string WallLocation { get; set; } = "";
        /// <summary>
        /// 绘图比例
        /// </summary>
        public string DrawScale { get; set; }
        /// <summary>
        /// 墙柱标高
        /// </summary>
        public string Elevation { get; set; }
        /// <summary>
        /// 点筋线宽
        /// </summary>
        public string PointReinforceLineWeight { get; set; }
        /// <summary>
        /// 箍线线宽
        /// </summary>
        public string StirrupLineWeight { get; set; }
        /// <summary>
        /// 保护层厚度
        /// </summary>
        public string ProtectThick { get; set; }
    }
}
