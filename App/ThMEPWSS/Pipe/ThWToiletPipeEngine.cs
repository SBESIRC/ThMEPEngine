using System;
using System.Collections.Generic;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;

namespace ThMEPWSS.Pipe
{
    public class ThWToiletPipeEngine : IDisposable
    {
        public ThWPipeZone Zone { get; set; }
        public Point3dCollection Pipes { get; set; }
        public ThWToiletPipeParameters Parameters { get; set; }
        
        public ThWToiletPipeEngine()
        {
            Pipes = new Point3dCollection();
        }
        public void Dispose()
        {
        }
        public void Run(Polyline boundary, Polyline outline, Polyline urinal)
        {
            int a = index_a( boundary, outline);
            int b = index_b(boundary, urinal);
            int c = index_c(boundary, outline);
            Vector3d dir = Direction(a,b,c, boundary,outline, urinal, boundary.Length / 2.0);
            var pt = FindInsideVertex(a, outline,dir);
            for (int i = 0;i < Parameters.Number; i++)
            {
                Pipes.Add(pt + i*dir * 200);
            }            
        }
        private int index_a(Polyline boundary, Polyline outline)
        {
          
            var vertices = outline.Vertices();
            Point3d midpoint = Point3d.Origin;
            double dst = double.MaxValue;
            int a = 0;
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                midpoint = vertices[i] + vertices[i].GetVectorTo(vertices[i+1]) * 0.5;
                Point3d reference_p = boundary.ToCurve3d().GetClosestPointTo(midpoint).Point;              
                if (dst > midpoint.DistanceTo(reference_p))
                {
                    dst = midpoint.DistanceTo(reference_p);
                    a = i;
                } 
                else if (dst==midpoint.DistanceTo(reference_p))
                {
                    dst = midpoint.DistanceTo(reference_p);
                    if (vertices[i].DistanceTo(vertices[i+1])> vertices[a].DistanceTo(vertices[a+1]))
                    {
                        a = i;
                    }

                }
            }

            return a;
        }
   
        private int index_b(Polyline boundary, Polyline urinal)
        {
            var center = urinal.GetCenter();
            Point3d base_point = boundary.ToCurve3d().GetClosestPointTo(center).Point;
            var vertices1 = boundary.Vertices();
            int b = 0;
            for (int i = 0; i < vertices1.Count - 1; i++)
            {
                if (vertices1[i].GetVectorTo(base_point).IsCodirectionalTo(base_point.GetVectorTo(vertices1[i + 1])))
                {
                    b = i;
                }               
            }
            return b;
        }
        private int index_c(Polyline boundary, Polyline outline)
        {
            var center = outline.GetCenter();
            Point3d base_point = boundary.ToCurve3d().GetClosestPointTo(center).Point;
            var vertices1 = boundary.Vertices();
            int c = 0;
            for (int i = 0; i < vertices1.Count - 1; i++)
            {
                if (vertices1[i].GetVectorTo(base_point).IsCodirectionalTo(base_point.GetVectorTo(vertices1[i + 1])) )
                {
                    c = i;
                }
            }
            return c;
        }
        private Point3d FindInsideVertex(int a,Polyline outline,Vector3d dir )
        { 
            Point3d pt =Point3d.Origin;             
            var vertices = outline.Vertices();       

            if (a > 0)
            {
                if (dir.IsCodirectionalTo(vertices[a].GetVectorTo(vertices[a + 1])))
                {
                    pt = vertices[a] + (vertices[a].GetVectorTo(vertices[a + 1]).GetNormal() + vertices[a].GetVectorTo(vertices[a - 1]).GetNormal()) * 100;
                }
                else
                {
                    pt = vertices[a + 1] + (vertices[a + 1].GetVectorTo(vertices[a]).GetNormal() + vertices[a].GetVectorTo(vertices[a - 1]).GetNormal()) * 100;
                }
            }
            else
            {
                if (dir.IsCodirectionalTo(vertices[0].GetVectorTo(vertices[1])))
                {
                    pt = vertices[0] + (vertices[0].GetVectorTo(vertices[1]).GetNormal() + vertices[1].GetVectorTo(vertices[2]).GetNormal()) * 100;
                }
                else
                {
                    pt = vertices[1] + (vertices[1].GetVectorTo(vertices[0]).GetNormal() + vertices[1].GetVectorTo(vertices[2]).GetNormal()) * 100;
                }
            }
            return pt;
            
        }
        private Vector3d Direction(int a, int b,int c ,Polyline boundary,Polyline outline,Polyline urinal,double sum)
        {
            var center = urinal.GetCenter();
            var center1 = outline.GetCenter();          
            Point3d base_point = boundary.ToCurve3d().GetClosestPointTo(center).Point;
            Point3d base_point1 = boundary.ToCurve3d().GetClosestPointTo(center1).Point;
            var vertices1 = boundary.Vertices();
            var vertices = outline.Vertices();
            double sum1 = 0;
            double sum2 = 0;
            double sum3 = 0;        
            if (c<b)
            {
                for(int i=c;i<=b;i++)
                {
                    sum1+= vertices1[i].DistanceTo(vertices1[i + 1]);
                }
                sum2 = vertices1[b+1].DistanceTo(base_point);
                sum3 = vertices1[c].DistanceTo(base_point1);                
                if (sum1-sum2 - sum3<sum)
                {
                    if (vertices1[c + 1].GetVectorTo(vertices1[c]).IsCodirectionalTo(vertices[a + 1].GetVectorTo(vertices[a])))
                    {
                        return vertices[a].GetVectorTo(vertices[a+1]).GetNormal();
                    }
                    else
                    {
                        return vertices[a+1].GetVectorTo(vertices[a]).GetNormal();
                    }
                }
                else
                {
                    if (vertices1[c + 1].GetVectorTo(vertices1[c]).IsCodirectionalTo(vertices[a + 1].GetVectorTo(vertices[a])))
                    {
                        return vertices[a + 1].GetVectorTo(vertices[a]).GetNormal();
                    }
                    else
                    {
                        return vertices[a].GetVectorTo(vertices[a + 1]).GetNormal();

                    }
                }
                
            }
            else if(c>b)
            {
                for (int i = b; i <= c; i++)
                {
                    sum1 += vertices1[i].DistanceTo(vertices1[i + 1]);
                }
                sum2 = vertices1[b].DistanceTo(base_point);
                sum3 = vertices1[c+1].DistanceTo(base_point1);
                if (sum1 - sum2 - sum3 < sum)
                {
                    if (vertices1[c + 1].GetVectorTo(vertices1[c]).IsCodirectionalTo(vertices[a + 1].GetVectorTo(vertices[a])))
                    {
                        return vertices[a+1].GetVectorTo(vertices[a]).GetNormal();
                    }
                    else
                    {
                        return vertices[a].GetVectorTo(vertices[a+1]).GetNormal();
                    }
                }
                else
                {
                    if (vertices1[c + 1].GetVectorTo(vertices1[c]).IsCodirectionalTo(vertices[a + 1].GetVectorTo(vertices[a])))
                    {
                        return vertices[a].GetVectorTo(vertices[a+1]).GetNormal();
                    }
                    else
                    {
                        return vertices[a+1].GetVectorTo(vertices[a]).GetNormal();
                    }
                }
            }
            else
            {
                if (vertices[a + 1].GetVectorTo(vertices[a]).IsCodirectionalTo(base_point.GetVectorTo(base_point1)))
                {
                    return vertices[a].GetVectorTo(vertices[a + 1]).GetNormal();
                }
                else
                {
                    return vertices[a + 1].GetVectorTo(vertices[a]).GetNormal();
                }
            }
        }
    }
}
