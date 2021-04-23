using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using NetTopologySuite.Index.KdTree;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPWSS.Model;

namespace ThMEPWSS.Service
{
    public class ThWSprinklerKdTree
    {
        private KdTree<ThWSprinkler> Tree { get; set; }

        private Dictionary<KdNode<ThWSprinkler>, List<ThWSprinkler>> Nodes { get; set; }

        public ThWSprinklerKdTree(double tolerance)
        {
            Tree = new KdTree<ThWSprinkler>(tolerance);
            Nodes = new Dictionary<KdNode<ThWSprinkler>, List<ThWSprinkler>>();
        }

        public void InsertSprinkler(ThWSprinkler sprinkler)
        {
            var centroid = GetCentroidPoint(sprinkler);
            var node = Tree.Insert(centroid.ToNTSCoordinate(), sprinkler);
            if (!Nodes.ContainsKey(node))
            {
                Nodes[node] = new List<ThWSprinkler>();
            }
            if (!Nodes[node].Contains(sprinkler))
            {
                Nodes[node].Add(sprinkler);
            }
        }
        
        private Point3d GetCentroidPoint(ThWSprinkler sprinkler)
        {
            var outline = sprinkler.Outline as Polyline;
            return outline.GetCentroidPoint();
        }
    }
}
