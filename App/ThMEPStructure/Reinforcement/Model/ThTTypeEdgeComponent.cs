namespace ThMEPStructure.Reinforcement.Model
{
    public class ThTTypeEdgeComponent:ThEdgeComponent
    {
        // 请参见"2021-ZZ-技术-总部-JG-CKTJ.pdf Page76"
        public double Bw { get; set; }
        public double Hc1 { get; set; }
        public double Bf { get; set; }
        public double Hc2s { get; set; }
        public double Hc2l { get; set; }
        /// <summary>
        /// 拉筋
        /// 对应标注图中的数字2
        /// </summary>
        public string Link2 { get; set; }
        /// <summary>
        /// 拉筋
        /// 对应标注图中的数字3
        /// </summary>
        public string Link3 { get; set; }
        /// <summary>
        /// 拉筋
        /// 对应标注图中的数字4
        /// </summary>
        public string Link4 { get; set; }

        /// <summary>
        /// 迭代步数
        /// </summary>
        public int X { get; set; }
        /// <summary>
        /// 迭代增大后的纵筋规格
        /// </summary>
        public string EnhancedReinforce { get; set; }
    }
}
