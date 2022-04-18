using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Reinforcement.Draw;
using Autodesk.AutoCAD.Geometry;
namespace ThMEPStructure.Reinforcement.Model
{
    public class ThTTypeEdgeComponent:ThEdgeComponent
    {
        // 请参见"2021-ZZ-技术-总部-JG-CKTJ.pdf Page76"
        public int Bw { get; set; }
        public int Hc1 { get; set; }
        public int Bf { get; set; }
        public int Hc2s { get; set; }
        public int Hc2l { get; set; }
        /// <summary>
        /// 拉筋
        /// 对应标注图中的数字2
        /// </summary>
        public string Link2 { get; set; } = "";
        /// <summary>
        /// 拉筋
        /// 对应标注图中的数字3
        /// </summary>
        public string Link3 { get; set; } = "";
        /// <summary>
        /// 拉筋
        /// 对应标注图中的数字4
        /// </summary>
        public string Link4 { get; set; } = "";

        DrawObjectTType drawObjectTType;
        

        public override DBObjectCollection Draw(double firstRowH, double firstRowW, Point3d point)
        {

            drawObjectTType.CalAndDrawGangJin(firstRowH, firstRowW, this, point);
            return drawObjectTType.objectCollection;
            throw new System.NotImplementedException();
        }

        public override void InitAndCalTableSize(string elevation, double tblRowHeight, double scale, out double firstRowH, out double firstRowW)
        {
            drawObjectTType = new DrawObjectTType();
            drawObjectTType.init(this, elevation, tblRowHeight, scale, new Point3d(0, 0, 0));
            drawObjectTType.GetTableFirstRowHW(out firstRowH, out firstRowW);
        }
    }
}
