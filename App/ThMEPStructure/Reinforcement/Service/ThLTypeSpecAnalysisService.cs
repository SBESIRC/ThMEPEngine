using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThLTypeSpecAnalysisService : ThAnalysisService
    {
        public int A { get; set; } // 端口长度
        public int B { get; set; } // 

        public int C { get; set; } // 
        public int D { get; set; } // 端口长度
        public int E { get; set; } // 
        public int F { get; set; } // 

        public override void Analysis(Polyline lType)
        {
            /*          A
             *        ------
             *        |    |
             *        |    | B
             *    (F) |    |___C___
             *        |            | D
             *        |____________|
             *            (E)
             */
            var lines = lType.ToLines();
            if(lines.Count!=6)
            {
                return;
            }
            var l1l2Edges = lines.FindLTypeEdge();
            if(l1l2Edges.Count!=2 || l1l2Edges[0].Item2.Count!=2 || l1l2Edges[1].Item2.Count != 2)
            {
                return;
            }
            var l1Groups = l1l2Edges[0];
            var l2Groups = l1l2Edges[1];
            var e = lines[l1Groups.Item1];
            var f = lines[l2Groups.Item1];

            var linkPt = FindLinkPt(e, f);
            if(!linkPt.HasValue)
            {
                return;
            }
            E = e.GetLineDistance().Round();
            F = f.GetLineDistance().Round();

            var newL1GroupsItem2 = l1Groups.Item2.SortEdgeIndexes(lines, linkPt.Value);
            var newL2GroupsItem2 = l2Groups.Item2.SortEdgeIndexes(lines, linkPt.Value);

            var aIndex = newL1GroupsItem2[0];
            var cIndex = newL1GroupsItem2[1];
            A = lines[aIndex].GetLineDistance().Round(); //端口
            C = lines[cIndex].GetLineDistance().Round();

            var dIndex = newL2GroupsItem2[0];
            var bIndex = newL2GroupsItem2[1];
            D = lines[dIndex].GetLineDistance().Round(); //端口
            B = lines[bIndex].GetLineDistance().Round(); 
        }
        private Point3d? FindLinkPt(Tuple<Point3d, Point3d> first, Tuple<Point3d, Point3d> second)
        {
            if (first.Item1.DistanceTo(second.Item1) <= 1.0 ||
                first.Item1.DistanceTo(second.Item2) <= 1.0)
            {
                return first.Item1;
            }
            else if (first.Item2.DistanceTo(second.Item1) <= 1.0 ||
                first.Item2.DistanceTo(second.Item2) <= 1.0)
            {
                return first.Item2;
            }
            else
            {
                return null;
            }
        }
    }
}
