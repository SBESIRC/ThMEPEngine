using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Reinforcement.Draw;
using Autodesk.AutoCAD.Geometry;
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

        
        DrawObjectRectangle drawObjectRectangle;
        public override DBObjectCollection Draw(double firstRowH, double firstRowW, Point3d point)
        {

            drawObjectRectangle.CalAndDrawGangJin(firstRowH, firstRowW, this, point);
            return drawObjectRectangle.objectCollection;
            throw new System.NotImplementedException();
        }

        public override void InitAndCalTableSize(string elevation, double tblRowHeight, double scale, out double firstRowH, out double firstRowW)
        {
            drawObjectRectangle = new DrawObjectRectangle();
            drawObjectRectangle.init(this, elevation, tblRowHeight, scale, new Point3d(0, 0, 0));
            drawObjectRectangle.GetTableFirstRowHW(out firstRowH, out firstRowW);
        }

    }
}
