using ThMEPStructure.Reinforcement.Draw;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
namespace ThMEPStructure.Reinforcement.Model
{
    public class ThRectangleColumnComponent : ThColumnComponent
    {
        public int Bw { get; set; }
        public int Hc { get; set; }

        /// <summary>
        /// 拉筋
        /// </summary>
        public string Link2 { get; set; } = "";
        /// <summary>
        /// 拉筋
        /// </summary>
        public string Link3 { get; set; } = "";

        DrawObjectColumn drawObjectColumn;
        public override DBObjectCollection Draw(double firstRowH, double firstRowW, Point3d point)
        {

            drawObjectColumn.CalAndDrawGangJin(firstRowH, firstRowW, this, point);
            return drawObjectColumn.objectCollection;
            throw new System.NotImplementedException();
        }

        public override void InitAndCalTableSize(string elevation, double tblRowHeight, double scale, out double firstRowH, out double firstRowW)
        {
            drawObjectColumn = new DrawObjectColumn();
            drawObjectColumn.init(this, elevation, tblRowHeight, scale, new Point3d(0, 0, 0));
            drawObjectColumn.GetTableFirstRowHW(out firstRowH, out firstRowW);
        }
    }
}
