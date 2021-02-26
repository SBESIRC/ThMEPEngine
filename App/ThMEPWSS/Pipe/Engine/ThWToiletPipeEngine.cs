using System;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Geom;
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
                Identifier = Parameters.Identifier[index].Item1,
                Matrix = Matrix3d.Displacement(center.GetAsVector()),
                Representation = new DBObjectCollection()
                {
                    new Circle(Point3d.Origin, Vector3d.ZAxis, Parameters.Identifier[index].Item2 / 2.0),
                },
            };
        }

        public void Run(Polyline outline, Polyline well, Polyline closestool)
        {
            int d = index_d(outline, well);
            if (GeomUtils.PtInLoop(outline, well.GetCenter()))
            {
                Vector3d dir = Direction(outline, well);
                var pt = FindInsideVertex(index_a(outline, well), d,well);
                for (int i = 0; i < Parameters.Identifier.Count; i++)
                {
                    Pipes.Add(Create((pt + i * dir * ThWPipeCommon.TOILET_WELLS_INTERVAL),i));
                }
            }
            else
            {
               Pipes = FindOutsideVertex(outline, well);
               
            }
        }
        private int index_a(Polyline boundary, Polyline outline)
        {       //寻找管井关键边  
            var vertices = outline.Vertices();
            double dst = 0;
            int a = 0;
            for (int i = 0; i < vertices.Count; i++)
            {                        
                if ( dst< vertices[i].DistanceTo(boundary.GetCenter()))
                {
                    dst = vertices[i].DistanceTo(boundary.GetCenter());
                    a = i;
                }            
            }
            return a;
        }
        private int index_d(Polyline boundary, Polyline outline)
        {       //寻找管井关键边  
            var vertices = outline.Vertices();
            double dst = double.MaxValue;
            int a = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (dst > vertices[i].DistanceTo(boundary.GetCenter()))
                {
                    dst = vertices[i].DistanceTo(boundary.GetCenter());
                    a = i;
                }
            }
            return a;
        }
        private Point3d FindInsideVertex(int a,int d,Polyline outline)
        {                
            var vertices = outline.Vertices();  
            int b = Getnum(a,d, outline)[0];
            int c = Getnum(a,d, outline)[1];        
            return vertices[a]+100*(vertices[a].GetVectorTo(vertices[b]).GetNormal()+ vertices[a].GetVectorTo(vertices[c]).GetNormal());            
        }
        private Vector3d Direction_1(int a, int b,int c ,int d,Polyline boundary,Polyline outline,Polyline urinal,double sum)
        {           
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
        private Vector3d Direction(Polyline boundary, Polyline outline)
        {   
            var vertices = outline.Vertices();
            int a = index_a(boundary, outline);
            int d = index_d(boundary, outline);
            int b = Getnum(a,d, outline)[0];
            int c= Getnum(a, d,outline)[1];
            if(vertices[a].DistanceTo(vertices[b])> vertices[a].DistanceTo(vertices[c]))
            {
                return vertices[a].GetVectorTo(vertices[b]).GetNormal();
            }
            else
            {
                return vertices[a].GetVectorTo(vertices[c]).GetNormal();
            }
        }
        private List<int> Getnum(int a, int d,Polyline outline)
        {          
            var item = new List<int>();
            var vertices = outline.Vertices();
            for(int i=0;i< vertices.Count;i++)
            {
                if((vertices[i].DistanceTo(vertices[a])>1)&&(vertices[i].DistanceTo(vertices[d]) > 1))
                {
                    item.Add(i);
                }
            }
            return item;
        }
        private List<ThWToiletPipe> FindOutsideVertex(Polyline outline, Polyline well)
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
                    pt = vertices[a + 1] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (vertices[a + 1].GetVectorTo(vertices[a]).GetNormal() + vertices[a].GetVectorTo(vertices[a - 1]).GetNormal());
                    dir = vertices[a + 1].GetVectorTo(vertices[a]).GetNormal();
                }
                else
                {
                    pt = vertices[a-1] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (vertices[a-1].GetVectorTo(vertices[a]).GetNormal() + vertices[a].GetVectorTo(vertices[a+1]).GetNormal());
                    dir = vertices[a + 1].GetVectorTo(vertices[a]).GetNormal();
                }

            }
            else
            {
                if (vertices[0].DistanceTo(vertices[1]) > vertices[1].DistanceTo(vertices[2]))
                {
                    pt = vertices[1] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (vertices[1].GetVectorTo(vertices[0]).GetNormal() + vertices[1].GetVectorTo(vertices[2]).GetNormal());
                    dir = vertices[1].GetVectorTo(vertices[0]).GetNormal();
                }
                else
                {
                    pt = vertices[vertices.Count-1] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (vertices[0].GetVectorTo(vertices[1]).GetNormal() + vertices[2].GetVectorTo(vertices[1]).GetNormal());
                    dir = vertices[2].GetVectorTo(vertices[1]).GetNormal();
                }
            }
            for (int i = 0; i < Parameters.Identifier.Count; i++)
            {
                pipes.Add(Create((pt + i * dir * ThWPipeCommon.TOILET_WELLS_INTERVAL),i));
            }
            return pipes;
        }
    }
}
