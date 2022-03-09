using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThTTypeSpecAnalysisService : ThAnalysisService
    {
        public Tuple<Point3d, Point3d> EdgeA { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        /// <summary>
        /// 主分支端口
        /// </summary>
        public Tuple<Point3d, Point3d> EdgeB { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        public Tuple<Point3d, Point3d> EdgeC { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        public Tuple<Point3d, Point3d> EdgeD { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        /// <summary>
        /// 分支端口
        /// </summary>
        public Tuple<Point3d, Point3d> EdgeE { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        public Tuple<Point3d, Point3d> EdgeF { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        public Tuple<Point3d, Point3d> EdgeG { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        /// <summary>
        /// 主分支端口
        /// </summary>
        public Tuple<Point3d, Point3d> EdgeH { get; private set; } = Tuple.Create(Point3d.Origin, Point3d.Origin);
        public int A => EdgeA.GetLineDistance().Round();
        public int B => EdgeB.GetLineDistance().Round();
        public int C => EdgeC.GetLineDistance().Round();
        public int D => EdgeD.GetLineDistance().Round();
        public int E => EdgeE.GetLineDistance().Round();
        public int F => EdgeF.GetLineDistance().Round();
        public int G => EdgeG.GetLineDistance().Round();
        public int H => EdgeH.GetLineDistance().Round();

        public override void Analysis(Polyline lType)
        {
            /*              E
             *            ------
             *            |    |
             *          F |    | D
             *    ___G____|    |___C____
             *  H |                     | B
             *    |_____________________|
             *               A       
             */
            var lines = lType.ToLines();
            if(lines.Count!=8)
            {
                return;
            }
            var l1l2Edges = lines.FindTTypeMainEdge();
            if(l1l2Edges.Count!=1 || l1l2Edges[0].Item2.Count!=3)
            {
                return;
            }
            var aIndex = l1l2Edges[0].Item1; // A边索引
            var indexes = new List<int>();            
            for(int i=1;i< lines.Count;i++)
            {
                var nextIndex = (aIndex + i)/ lines.Count;
                indexes.Add(nextIndex);
            }
            EdgeA = lines[indexes[0]];
            EdgeB = lines[indexes[1]];
            EdgeC = lines[indexes[2]];
            EdgeD = lines[indexes[3]];
            EdgeE = lines[indexes[4]];
            EdgeF = lines[indexes[5]];
            EdgeG = lines[indexes[6]];
            EdgeH = lines[indexes[7]];
        }
    }
}
