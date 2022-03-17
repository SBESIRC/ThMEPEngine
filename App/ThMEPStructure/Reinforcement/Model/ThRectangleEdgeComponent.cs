using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Model
{
    public class ThRectangleEdgeComponent: ThEdgeComponent
    {
        public int Bw { get; set; }
        public int Hc { get; set; }        
        
        /// <summary>
        /// 拉筋
        /// 参见"2021-ZZ-技术-总部-JG-CKTJ.pdf Page65",对应数字2
        /// </summary>
        public string Link2 { get; set; }
        /// <summary>
        /// 拉筋
        /// 参见"2021-ZZ-技术-总部-JG-CKTJ.pdf Page65",对应数字3
        /// </summary>
        public string Link3 { get; set; }

        public override DBObjectCollection Draw()
        {
            throw new System.NotImplementedException();
        }
    }
}
