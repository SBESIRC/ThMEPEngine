using System;
using QuickGraph;
using QuickGraph.Algorithms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Duct
{
    public static class ThGraphExtension
    {
        public static double GetTwoVertexDistance(this ThDuctVertex vertexa, ThDuctVertex vertexb)
        {
            return vertexa.DistanceTo(vertexb);
        }
        public static double GetEdgeLength(this ThDuctEdge<ThDuctVertex> edge)
        {
            return edge.Source.GetTwoVertexDistance(edge.Target);
        }

        public static List<Point3d> GetEdgeDividePoint(this ThDuctEdge<ThDuctVertex> edge, int dividecount)
        {
            double dx = Math.Abs(edge.Target.XPosition - edge.Source.XPosition);
            double dy = Math.Abs(edge.Target.YPosition - edge.Source.YPosition);
            Point3d maxinx = edge.Source.XPosition > edge.Target.XPosition ? edge.Source.VertexToPoint3D() : edge.Target.VertexToPoint3D();
            Point3d maxiny = edge.Source.YPosition > edge.Target.YPosition ? edge.Source.VertexToPoint3D() : edge.Target.VertexToPoint3D();

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
