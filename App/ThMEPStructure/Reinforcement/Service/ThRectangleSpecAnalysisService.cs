using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThRectangleSpecAnalysisService : ThAnalysisService
    {
        public Tuple<Point3d, Point3d> EdgeA { get; private set; }
        public Tuple<Point3d, Point3d> EdgeB { get; private set; }
        public Tuple<Point3d, Point3d> EdgeC { get; private set; }
        public Tuple<Point3d, Point3d> EdgeD { get; private set; }
        public int A => EdgeA.GetLineDistance().Round();
        public int B => EdgeB.GetLineDistance().Round();
        public int C => EdgeC.GetLineDistance().Round();
        public int D => EdgeD.GetLineDistance().Round();

        public override void Analysis(Polyline rectangle)
        {
            /*
             *          C
             *   --------------
             * D |            | B
             *   |            |
             *   --------------
             *          A
             */
            var lines = rectangle.ToLines();
            if(lines.Count!=4)
            {
                return;
            }
            var firstDis = lines[0].GetLineDistance();
            var secondDis= lines[1].GetLineDistance();
            if (firstDis.IsEqual(secondDis,1.0))
            {
                // 正方形
                EdgeA = lines[0];
                EdgeB = lines[1];
                EdgeC = lines[2];
                EdgeD = lines[3];
            }
            else
            { 
                // 长方形
                if(firstDis > secondDis)
                {
                    EdgeA = lines[0];
                    EdgeB = lines[1];
                    EdgeC = lines[2];
                    EdgeD = lines[3];
                }
                else
                {
                    EdgeA = lines[1];
                    EdgeB = lines[2];
                    EdgeC = lines[3];
                    EdgeD = lines[0];
                }
            }
        }
    }
}
