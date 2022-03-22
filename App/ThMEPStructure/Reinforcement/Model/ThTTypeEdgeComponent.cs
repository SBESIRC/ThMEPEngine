using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Reinforcement.Draw;
using Autodesk.AutoCAD.Geometry;
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

        public override DBObjectCollection Draw(string elevation, double tblRowHeight, double scale)
        {
            DrawObjectTType drawObjectTType = new DrawObjectTType();
            drawObjectTType.CalAndDrawGangJin(this, elevation, tblRowHeight, scale, new Point3d(0, 0, 0));
            return drawObjectTType.objectCollection;
            throw new System.NotImplementedException();
        }
    }
}
