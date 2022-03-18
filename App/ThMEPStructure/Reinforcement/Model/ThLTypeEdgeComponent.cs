using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Reinforcement.Draw;
using Autodesk.AutoCAD.Geometry;
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

        public override DBObjectCollection Draw(string elevation, double tblRowHeight, double scale)
        {
            DrawObjectLType drawObjectLType = new DrawObjectLType();
            drawObjectLType.CalAndDrawGangJin(this, elevation, tblRowHeight, scale, new Point3d(0, 0, 0));
            return drawObjectLType.objectCollection;
            throw new System.NotImplementedException();
        }
    }
}
