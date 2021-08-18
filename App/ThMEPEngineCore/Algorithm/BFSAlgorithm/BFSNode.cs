using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Algorithm.BFSAlgorithm
{
    public class BFSNode
    {
        public BFSNode(int _x, int _y, Point3d pt)
        {
            X = _x;
            Y = _y;
            point = pt;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public Point3d point { get; set; }

        public BFSNode parent { get; set; }
    }
}
