using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.EmgLightConnect.Model
{
    public class ThLaneSideEdgeNode
    {
        private int m_node;

        public ThLaneSideEdgeNode next { get; set; }


        public int nodeIndex
        {
            get
            {
                return m_node;
            }
        }

        public ThLaneSideEdgeNode(int nodeIndex)
        {
            this.m_node = nodeIndex;
        }


    }

    public class ThLaneSideVertexNode
    {
        private int m_laneSideIndex;

        public ThLaneSideVertexNode(int laneSideIndex)
        {
            m_laneSideIndex = laneSideIndex;
        }

        public ThLaneSideEdgeNode firstEdge { get; set; }


    }

    public class ThLaneSideGraph
    {
        public List<ThLaneSideVertexNode> sideVertexNodeList { get; set; }

        public ThLaneSideGraph()
        {
            this.sideVertexNodeList = new List<ThLaneSideVertexNode>();

        }
        public void AddVertex(int laneSideIndex)
        {
            var vertex = new ThLaneSideVertexNode(laneSideIndex);
            sideVertexNodeList.Add(vertex);
        }

        public void AddEdge(int fromVertex, int toVertex)
        {
            ThLaneSideVertexNode tmp = sideVertexNodeList[fromVertex];
            ThLaneSideEdgeNode toAdd = new ThLaneSideEdgeNode(toVertex);

            if (tmp.firstEdge == null)
            {
                tmp.firstEdge = toAdd;
            }
            else
            {
                ThLaneSideEdgeNode p = tmp.firstEdge;
                while (p.next != null)
                {
                    p = p.next;
                }
                p.next = toAdd;
            }
        }

        public void traverse(int start, ref int[] visited, ref List<int> visitPath, ref List<List<int>> visitPathList)
        {
            visitPath.Add(start);
            visited[start] = 1;

            ThLaneSideVertexNode tmpV = sideVertexNodeList[start];

            var tmpE = tmpV.firstEdge;

            tmpE = tmpE.next;
            while (tmpE != null)
            {
                if (visited[tmpE.nodeIndex] == 0)
                {
                    var nextPath = new List<int>();
                    nextPath.AddRange(visitPath);
                    visitPathList.Add(nextPath);
                    int[] visitedNext = (int[])visited.Clone();
                    traverse(tmpE.nodeIndex, ref visitedNext, ref nextPath, ref visitPathList);
                }
                tmpE = tmpE.next;
            }

            tmpE = tmpV.firstEdge;
            if (tmpE != null)
            {
                if (visited[tmpE.nodeIndex] == 0)
                {
                    traverse(tmpE.nodeIndex, ref visited, ref visitPath, ref visitPathList);
                }
            }



        }
    }
}
