using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThPlatform3D.StructPlane.Model
{
    internal class ThBeamMarkBlkInfo
    {
        public BlockReference GeneratedBlk { get; set; }
        public DBObjectCollection Marks { get; set; }
        public Point3dCollection OrginArea { get; set; }
        public Vector3d TextMoveDir { get; set; }
        public ThBeamMarkBlkInfo()
        {
            TextMoveDir = new Vector3d();
            Marks = new DBObjectCollection();
            OrginArea = new Point3dCollection();
        }

        public ThBeamMarkBlkInfo(DBObjectCollection marks, Point3dCollection originArea, Vector3d textMoveDir)
        {
            Marks = marks;
            OrginArea = originArea;
            TextMoveDir = textMoveDir;
        }
        public ThBeamMarkBlkInfo(BlockReference br,DBObjectCollection marks, Point3dCollection originArea, Vector3d textMoveDir)
            :this(marks, originArea, textMoveDir)
        {
            GeneratedBlk = br;
        }
    }
}
