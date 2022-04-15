namespace ThMEPHVAC.EQPMFanModelEnums
{
    public class SceneResistaCalcModel
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int No { get; set; }
        /// <summary>
        /// 场景
        /// </summary>
        public EnumScenario Scene { get; set; }
        /// <summary>
        /// 比摩阻：小数点后最多1位
        /// </summary>
        public double Friction { get; set; }
        /// <summary>
        /// 局部阻力倍数：小数点后最多1位
        /// </summary>
        public double LocRes { get; set; }

        /// <summary>
        /// 消音器阻力：正整数
        /// </summary>
        public int Damper { get; set; }
        /// <summary>
        /// 动压
        /// </summary>
        public int DynPress { get; set; }
    }
}
