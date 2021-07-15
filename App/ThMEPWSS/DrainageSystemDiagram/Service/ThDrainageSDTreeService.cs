using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDTreeService
    {
        public static void buildPipeTree(ThDrainageSDDataExchange dataset)
        {
            if (dataset.Pipes != null && dataset.Pipes.Count > 0)
            {
                var nodes = buildPipeTree(dataset.Pipes, dataset.SupplyStart.Pt);
                dataset.PipeTreeRoot = nodes;
            }
        }

        private static ThDrainageSDTreeNode buildPipeTree(List<Line> allLines, Point3d supplyStart)
        {
            var lines = ThDrainageSDCleanLineService.simplifyLine(allLines);

            var root = buildTree(lines, supplyStart);

            printTree(root, "l063tree");

            return root;
        }

        public static void printTree(ThDrainageSDTreeNode root, string layer)
        {

            int cs = root.getLeafCount();
            int dp = root.getDepth();
            DrawUtils.ShowGeometry(new Point3d(root.Node.X + 20, root.Node.Y, 0), string.Format("{0}_{1}", dp, cs), layer, (short)(dp % 7), 25, 100);

            root.Child.ForEach(x => printTree(x, layer));
        }

        private static ThDrainageSDTreeNode buildTree(List<Line> lines, Point3d startPt)
        {

            int[] travaled = new int[lines.Count];
            travaled.ForEach(x => x = 0);

            var rootNode = new ThDrainageSDTreeNode(startPt);
            var nodeList = new List<ThDrainageSDTreeNode>();
            nodeList.Add(rootNode);
            findNextLeaf(rootNode, travaled, lines, nodeList);

            return rootNode;
        }

        private static void findNextLeaf(ThDrainageSDTreeNode thisNode, int[] travaled, List<Line> lines, List<ThDrainageSDTreeNode> nodeList)
        {

            var tol = new Tolerance(10, 10);

            var startPt = thisNode.Node;
            var lLink = lines.Where(x => x.StartPoint.IsEqualTo(startPt, tol) || x.EndPoint.IsEqualTo(startPt, tol)).ToList();

            if (lLink != null && lLink.Count > 0)
            {
                var toLine = lLink.Where(x => travaled[lines.IndexOf(x)] == 0).ToList();

                foreach (var l in toLine)
                {

                    var lStart = startPt;

                    if (l.EndPoint.IsEqualTo(lStart, new Tolerance(10, 10)))
                    {
                        var ept = l.StartPoint;
                        l.StartPoint = startPt;
                        l.EndPoint = ept;
                    }
                    travaled[lines.IndexOf(l)] = 1;

                    var child = new ThDrainageSDTreeNode(l.EndPoint);
                    thisNode.Child.Add(child);
                    child.Parent = thisNode;
                    nodeList.Add(child);
                    findNextLeaf(child, travaled, lines, nodeList);

                }
            }
        }






        public static ThDrainageSDTreeNode buildPipeTreeTest(List<Line> allLines, Point3d supplyStart)
        {
            var lines = ThDrainageSDCleanLineService.simplifyLine(allLines);

            //  var lines2 = ThDrainageSDCleanLineService.simplifyLineTest(allLines);


            var root = buildTree(lines, supplyStart);
            // var root2 = buildTree(lines2, supplyStart);

            printTree(root, "l063tree");
            //  printTree(root2, "l031tree");

            return root;
        }
    }
}
