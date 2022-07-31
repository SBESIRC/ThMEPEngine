using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.SprinklerConnect.Model
{
    public class ThSprinklerEdgeNode
    {
        public int EdgeIndex { get; set; }
        public ThSprinklerEdgeNode Next { get; set; }
        public ThSprinklerEdgeNode(int edgeIndex)
        {
            EdgeIndex = edgeIndex;
        }
    }

    public class ThSprinklerVertexNode
    {
        //
        public int NodeIndex { get; private set; }
        public ThSprinklerEdgeNode FirstEdge { get; set; }
        public ThSprinklerVertexNode(int ptIndex)
        {
            NodeIndex = ptIndex;
        }
    }

    public class ThSprinklerGraph
    {
        public List<ThSprinklerVertexNode> SprinklerVertexNodeList { get; private set; }

        public ThSprinklerGraph()
        {
            this.SprinklerVertexNodeList = new List<ThSprinklerVertexNode>();
        }

        public void AddVertex(int ptIndex)
        {
            if (SearchNodeIndex(ptIndex) == -1)
            {
                var vertex = new ThSprinklerVertexNode(ptIndex);
                SprinklerVertexNodeList.Add(vertex);
            }
        }

        public void AddEdge(int fromVertex, int toVertex)
        {
            var fromVertexIdx = SearchNodeIndex(fromVertex);
            var toVertexIdx = SearchNodeIndex(toVertex);
            if (fromVertexIdx != -1)
            {
                ThSprinklerVertexNode tmp = SprinklerVertexNodeList[fromVertexIdx];
                ThSprinklerEdgeNode toAdd = new ThSprinklerEdgeNode(toVertexIdx);

                if (tmp.FirstEdge == null)
                {
                    tmp.FirstEdge = toAdd;
                }
                else
                {
                    var p = tmp.FirstEdge;
                    while (p.Next != null && p.EdgeIndex != toVertexIdx)
                    {
                        p = p.Next;
                    }
                    if (p.Next == null && p.EdgeIndex != toVertexIdx)
                    {
                        p.Next = toAdd;
                    }

                }
            }
        }

        public void DeleteEdge(int fromVertex, int toVertex)
        {
            var fromVertexIdx = SearchNodeIndex(fromVertex);
            var toVertexIdx = SearchNodeIndex(toVertex);

            if (fromVertexIdx != -1)
            {
                ThSprinklerEdgeNode tmp = SprinklerVertexNodeList[fromVertexIdx].FirstEdge;

                if(tmp != null)
                {
                    if(tmp.EdgeIndex == toVertexIdx)
                    {
                        SprinklerVertexNodeList[fromVertexIdx].FirstEdge = tmp.Next;
                    }
                    else
                    {
                        while (tmp.Next != null && tmp.Next.EdgeIndex != toVertexIdx)
                        {
                            tmp = tmp.Next;
                        }

                        if (tmp.Next != null && tmp.Next.EdgeIndex == toVertexIdx)
                        {
                            tmp.Next = tmp.Next.Next;
                        }
                    }

                    
                }

            }
        }

        public List<int> GetEdgeList(int ptIndex)
        {
            int idx = SearchNodeIndex(ptIndex);

            var edges = new List<int>();
            var curEdge = SprinklerVertexNodeList[idx].FirstEdge;
            while (curEdge != null)
            {
                edges.Add(curEdge.EdgeIndex);
                curEdge = curEdge.Next;
            }

            return edges;
        }

        public bool IsEdgeDuplicated()
        {
            foreach (ThSprinklerVertexNode vertex in SprinklerVertexNodeList)
            {
                var edges = new List<int>();
                var curEdge = vertex.FirstEdge;
                while (curEdge != null)
                {
                    if (edges.Contains(curEdge.EdgeIndex))
                        return true;
                    else
                        edges.Add(curEdge.EdgeIndex);

                    curEdge = curEdge.Next;
                }

            }
            return false;
        }


        /// <summary>
        /// 根据pt index找 图node index
        /// </summary>
        /// <param name="ptIndex"></param>
        /// <returns></returns>
        public int SearchNodeIndex(int ptIndex)
        {
            var idx = -1;
            var vertexNode = SprinklerVertexNodeList.Where(x => x.NodeIndex == ptIndex);
            if (vertexNode.Count() > 0)
            {
                idx = SprinklerVertexNodeList.IndexOf(vertexNode.First());
            }

            return idx;
        }

        public List<Line> Print(List<Point3d> pts)
        {
            var lines = new List<Line>();

            for (int i = 0; i < SprinklerVertexNodeList.Count; i++)
            {
                var node = SprinklerVertexNodeList[i].FirstEdge;
                while (node != null)
                {
                    var l = new Line(pts[SprinklerVertexNodeList[i].NodeIndex], pts[SprinklerVertexNodeList[node.EdgeIndex].NodeIndex]);



                    if (ContainLine(l, lines) == false)
                    {
                        lines.Add(l);
                    }
                    node = node.Next;
                }
            }
            return lines;
        }

        private bool ContainLine(Line l, List<Line> lList)
        {
            var tol = new Tolerance(10, 10);
            var bReturn = false;
            var contains = lList.Where(x => (x.StartPoint.IsEqualTo(l.StartPoint, tol) && x.EndPoint.IsEqualTo(l.EndPoint, tol)) ||
                                           (x.EndPoint.IsEqualTo(l.StartPoint, tol) && x.StartPoint.IsEqualTo(l.EndPoint, tol)));
            if (contains.Count() > 0)
            {
                bReturn = true;
            }

            return bReturn;
        }
    }
}
