using System;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;
using System.Collections.Generic;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWToiletPipeEngine : IDisposable
    {
        public ThWPipeZone Zone { get; set; }
        public List<ThWToiletPipe> Pipes { get; set; }
        public ThWToiletPipeParameters Parameters { get; set; }
        
        public ThWToiletPipeEngine()
        {
            Pipes = new List<ThWToiletPipe>();
        }
        public void Dispose()
        {
        }
        public ThWToiletPipe Create(Point3d center, int index)
        {
            return new ThWToiletPipe()
            {
                Center = center,
                Identifier = Parameters.Identifier[index],
                Matrix = Matrix3d.Displacement(center.GetAsVector()),
                Representation = new DBObjectCollection()
                {
                    new Circle(Point3d.Origin, Vector3d.ZAxis, Parameters.Diameter[index] / 2.0),
                },
            };
        }

        public void Run(Polyline outline, Polyline well, Polyline closestool)
        {
          
                int a = index_a(outline, well);
                int d = index_d(outline, closestool, well);
                int b = index_b(outline, closestool,d);
                int c = index_c(outline, well);
            if (Wellisinboundary(outline, well))
            {
                Vector3d dir = Direction(a, b, c,d, outline, well, closestool, outline.Length / 2.0);
                var pt = FindInsideVertex(a, well, dir);
                for (int i = 0; i < Parameters.Number; i++)
                {
                    Pipes.Add(Create((pt + i * dir * 200),i));
                }
            }
            else
            {
               Pipes = FindOutsideVertex(outline, well, closestool, d);
               
            }
        }
        private int index_a(Polyline boundary, Polyline outline)
        {       //寻找管井关键边  
            var vertices = outline.Vertices();
            Point3d midpoint = Point3d.Origin;
            double dst = double.MaxValue;
            int a = 0;
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                midpoint = vertices[i] + vertices[i].GetVectorTo(vertices[i+1]) * 0.5;
                Point3d reference_p = boundary.ToCurve3d().GetClosestPointTo(midpoint).Point;              
                
                if ( dst- midpoint.DistanceTo(reference_p)>1)
                {
                    dst = midpoint.DistanceTo(reference_p);
                    a = i;
                } 
                else
                {             
                    if ((vertices[i].DistanceTo(vertices[i+1])- vertices[a].DistanceTo(vertices[a+1]))>1)
                    {
                        a = i;
                    }

                }
            }

            return a;
        }
   
        private int index_b(Polyline boundary, Polyline urinal,int d)
        {//寻找相对于马桶的toilet关键边
            var vertices = urinal.Vertices();
            Point3d base_point = boundary.ToCurve3d().GetClosestPointTo(vertices[d]+ (vertices[d].GetVectorTo(vertices[d+1]))*0.5).Point;
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
        {//寻找相对于管井的toilet关键边
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
        private int index_d(Polyline boundary, Polyline urinal, Polyline well)
        {//寻找马桶的关键边
            var center = well.GetCenter();
            var vertices1 = urinal.Vertices();
            int b = 0;
            for (int i = 0; i < vertices1.Count-1; i++)
            {
                if (well.GetDistToPoint((vertices1[i]+0.5* vertices1[i].GetVectorTo(vertices1[i+1])))<60)
                {                 
                    b = i;
                }
                else
                {
                    if(boundary.GetDistToPoint((vertices1[i] + 0.5 * vertices1[i].GetVectorTo(vertices1[i + 1]))) < 60)
                    {
                        b = i;
                    }
                }
            }        
            return b;
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
        private Vector3d Direction(int a, int b,int c ,int d,Polyline boundary,Polyline outline,Polyline urinal,double sum)
        {
            var vertices2 = urinal.Vertices();
            var vertices1 = boundary.Vertices();
            var vertices = outline.Vertices();
            Point3d base_point = boundary.ToCurve3d().GetClosestPointTo(vertices1[d]+ 0.5*vertices1[d].GetVectorTo(vertices1[d+1])).Point;//马桶关键点在toilet
            Point3d base_point1 = boundary.ToCurve3d().GetClosestPointTo(vertices[a] + 0.5 * vertices[a].GetVectorTo(vertices[a + 1])).Point;//管井关键点在toilet            
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
                if (vertices[a].GetVectorTo(vertices[a+1]).IsCodirectionalTo(base_point1.GetVectorTo(base_point)))
                {
                    return vertices[a].GetVectorTo(vertices[a+1]).GetNormal();
                }
                else
                {
                    return vertices[a+1].GetVectorTo(vertices[a]).GetNormal();
                }
            }
        }
        private bool Wellisinboundary(Polyline boundary, Polyline outline)
        {
            var center_o = outline.GetCenter();
            var center_b = boundary.GetCenter();
            Line line = new Line(center_o, center_b);
            var pts = new Point3dCollection();
            boundary.IntersectWith(line, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            if (center_o.GetVectorTo(pts[0]).IsCodirectionalTo(center_o.GetVectorTo(pts[1])))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        private List<ThWToiletPipe> FindOutsideVertex(Polyline outline, Polyline well, Polyline closestool,int d)
        {
            var vertices = well.Vertices();
            double dst = double.MaxValue;
            int a = 0;
            Point3d pt = Point3d.Origin;
            Vector3d dir = Vector3d.XAxis;
            var pipes = new List<ThWToiletPipe>();          
            for (int i = 0; i < vertices.Count; i++)
            {
                if (dst> outline.GetCenter().DistanceTo(vertices[i]))
                {
                    dst = outline.GetCenter().DistanceTo(vertices[i]);
                    a = i;
                }                 
            }
            if(a>0)
            {
                if(vertices[a].DistanceTo(vertices[a+1])> vertices[a].DistanceTo(vertices[a-1]))
                {
                    pt = vertices[a + 1] + 100 * (vertices[a + 1].GetVectorTo(vertices[a]).GetNormal() + vertices[a].GetVectorTo(vertices[a - 1]).GetNormal());
                    dir = vertices[a + 1].GetVectorTo(vertices[a]).GetNormal();
                }
                else
                {
                    pt = vertices[a-1] + 100 * (vertices[a-1].GetVectorTo(vertices[a]).GetNormal() + vertices[a].GetVectorTo(vertices[a+1]).GetNormal());
                    dir = vertices[a + 1].GetVectorTo(vertices[a]).GetNormal();
                }

            }
            else
            {
                if (vertices[0].DistanceTo(vertices[1]) > vertices[1].DistanceTo(vertices[2]))
                {
                    pt = vertices[1] + 100 * (vertices[1].GetVectorTo(vertices[0]).GetNormal() + vertices[1].GetVectorTo(vertices[2]).GetNormal());
                    dir = vertices[1].GetVectorTo(vertices[0]).GetNormal();
                }
                else
                {
                    pt = vertices[vertices.Count-1] + 100 * (vertices[0].GetVectorTo(vertices[1]).GetNormal() + vertices[2].GetVectorTo(vertices[1]).GetNormal());
                    dir = vertices[2].GetVectorTo(vertices[1]).GetNormal();
                }
            }
            for (int i = 0; i < Parameters.Number; i++)
            {
                pipes.Add(Create((pt + i * dir * 200),i));
            }
            return pipes;
        }
    }
}
