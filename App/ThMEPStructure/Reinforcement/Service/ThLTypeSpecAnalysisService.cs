using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThLTypeSpecAnalysisService : ThAnalysisService
    {
        /// <summary>
        /// 端口
        /// </summary>
        public Tuple<Point3d, Point3d> EdgeA { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        public Tuple<Point3d, Point3d> EdgeB { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        public Tuple<Point3d, Point3d> EdgeC { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        /// <summary>
        /// 端口
        /// </summary>
        public Tuple<Point3d, Point3d> EdgeD { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        public Tuple<Point3d, Point3d> EdgeE { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        public Tuple<Point3d, Point3d> EdgeF { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        public int A => EdgeA.GetLineDistance().Round(); // 端口长度
        public int B => EdgeB.GetLineDistance().Round();
        public int C => EdgeC.GetLineDistance().Round();
        public int D => EdgeD.GetLineDistance().Round(); // 端口长度
        public int E => EdgeE.GetLineDistance().Round();
        public int F => EdgeF.GetLineDistance().Round();

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
            EdgeE = lines[l1Groups.Item1];
            EdgeF = lines[l2Groups.Item1];

            var linkPt = FindLinkPt(EdgeE, EdgeF);
            if(!linkPt.HasValue)
            {
                return;
            }
           
            var newL1GroupsItem2 = l1Groups.Item2.SortEdgeIndexes(lines, linkPt.Value);
            var newL2GroupsItem2 = l2Groups.Item2.SortEdgeIndexes(lines, linkPt.Value);

            var aIndex = newL1GroupsItem2[0];
            var cIndex = newL1GroupsItem2[1];
            EdgeA = lines[aIndex]; //端口
            EdgeC = lines[cIndex];

            var dIndex = newL2GroupsItem2[0];
            var bIndex = newL2GroupsItem2[1];
            EdgeD = lines[dIndex]; //端口
            EdgeB = lines[bIndex]; 
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
