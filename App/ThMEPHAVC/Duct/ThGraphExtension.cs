using System;
using QuickGraph;
using QuickGraph.Algorithms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHAVC.Duct
{
    public static class ThGraphExtension
    {
        public static double GetTwoVertexDistance(this ThDuctVertex vertexa, ThDuctVertex vertexb)
        {
            return vertexa.Position.DistanceTo(vertexb.Position);
        }
        public static double GetEdgeLength(this ThDuctEdge<ThDuctVertex> edge)
        {
            return edge.Source.GetTwoVertexDistance(edge.Target);
        }

        public static List<Point3d> GetEdgeDividePoint(this ThDuctEdge<ThDuctVertex> edge, int dividecount)
        {
            double dx = Math.Abs(edge.Target.Position.X - edge.Source.Position.X);
            double dy = Math.Abs(edge.Target.Position.Y - edge.Source.Position.Y);
            Point3d maxinx = edge.Source.Position.X > edge.Target.Position.X ? edge.Source.Position : edge.Target.Position;
            Point3d maxiny = edge.Source.Position.Y > edge.Target.Position.Y ? edge.Source.Position : edge.Target.Position;

            List <Point3d> dividepoints = new List<Point3d>();
            for (int i = 1; i <= dividecount; i++)
            {
                if (maxinx.IsEqualTo(maxiny))
                {
                    Point3d dividepoint = new Point3d(maxinx.X - (i * dx / (dividecount + 1)), maxinx.Y - (i * dy / (dividecount + 1)), 0);
                    dividepoints.Add(dividepoint);
                }
                else
                {
                    Point3d dividepoint = new Point3d(maxinx.X - (i * dx / (dividecount + 1)), maxinx.Y + (i * dy / (dividecount + 1)), 0);
                    dividepoints.Add(dividepoint);
                }
            }

            return dividepoints;
        }
    }
}
