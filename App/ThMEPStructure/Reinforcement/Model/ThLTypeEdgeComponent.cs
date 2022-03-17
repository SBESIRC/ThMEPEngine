using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Model
{
    public class ThLTypeEdgeComponent:ThEdgeComponent
    {
        // 请参见"2021-ZZ-技术-总部-JG-CKTJ.pdf Page70"
        public int Bw { get; set; }
        public int Hc1 { get; set; }
        public int Bf { get; set; }
        public int Hc2 { get; set; }
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

        public override DBObjectCollection Draw()
        {
            throw new System.NotImplementedException();
        }
    }
}
