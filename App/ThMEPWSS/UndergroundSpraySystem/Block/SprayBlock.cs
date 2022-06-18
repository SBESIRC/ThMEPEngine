using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;

namespace ThMEPWSS.UndergroundSpraySystem.Block
{
    public class SprayBlock
    {
        public string BlockName { get; set; }
        public Point3d Pt { get; set; }
        public double Angle { get; set; }
        public Scale3d Scale { get; set; }
        public string Layer { get; set; }
        
        public SprayBlock(string blockName, Point3d pt, double angle = 0, double scale = 1.0)
        {
            BlockName = blockName;
            Pt = pt;
            Angle = angle;
            Scale = new Scale3d(scale, scale, scale);
            Layer = "W-FRPT-SPRL-EQPM";
        }
        public BlockReference Insert(AcadDatabase acadDatabase)
        {
            var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(Layer, BlockName, Pt, Scale, Angle);
            var blk = acadDatabase.Element<BlockReference>(objID);
            return blk;
        }
    }
}
