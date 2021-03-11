using System;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWKitchenPipeEngine : IDisposable
    {
        public List<ThWKitchenPipe> Pipes { get; set; }
        public ThWKitchenPipeParameters Parameters { get; set; }
        public ThWKitchenPipeEngine()
        {
            Pipes = new List<ThWKitchenPipe>();
        }
        public void Dispose()
        {
        }

        public ThWKitchenPipe Create(Point3d center)
        {
            return new ThWKitchenPipe()
            {
                Center = center,
                Identifier = Parameters.Identifier,
                Matrix = Matrix3d.Displacement(center.GetAsVector()),
                Representation = new DBObjectCollection()
                {
                    new Circle(Point3d.Origin, Vector3d.ZAxis, Parameters.Diameter / 2.0),
                }
            };
        }

        public void Run(Polyline boundary, Polyline outline, BlockReference basinline, Polyline pype)
        {
            if (OutlineInBoundary(boundary, outline))
            {
                var pt = FindInsideVertex(boundary, outline);
                var dir = GetDirection(boundary, outline, pt);
                Pipes.Add(Create(pt + dir * ThWPipeCommon.WELL_TO_WALL_OFFSET));

                //如果管井和台盆不共边，则需要添加一个管子
                if (Commonline(boundary, outline, basinline))
                {
                    Pipes.Add(Create(Addpipe(boundary, basinline, pype, outline)));
                }
            }
            else
            {
                var pt = FindOutsideVertex(basinline, outline);
                if (GetOutsidePipe(pt, basinline))
                {                              
                    Pipes.Add(Create(pt));
                }
                else
                {                 
                    Pipes.Add(Create(pt));
                    Pipes.Add(Create(Addpipe(boundary, basinline, pype, outline)));
                }
            }

        }
        private static bool OutlineInBoundary(Polyline boundary, Polyline outline)
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
        private Point3d FindInsideVertex(Polyline boundary, Polyline outline)
        {
            var vertices = outline.Vertices();
            var vertices1 = boundary.Vertices();          
            double dst = double.MaxValue;
            int num = 0;
            for (int i = 0; i < vertices.Count; i++)//判断管井中点距外廓距离
            {
                double dst1 = double.MaxValue;
                for (int j=0;j< vertices1.Count;j++)
                {
                   if(dst1> vertices[i].DistanceTo(vertices1[j]))
                    {
                        dst1 = vertices[i].DistanceTo(vertices1[j]);
                    }                   
                }
                if(dst>dst1)
                {
                    dst = dst1;
                    num = i;
                }
            }
            return vertices[num];
        }    
        private Vector3d GetDirection(Polyline boundary, Polyline outline, Point3d pt)
        {
            var vertices = outline.Vertices();
            var dir = pt.GetVectorTo(vertices[2]).GetNormal() + (vertices[2]).GetVectorTo(vertices[1]).GetNormal();
            for (int i = 0; i < vertices.Count - 2; i++)
            {
                if (vertices[i] == pt)
                {
                    dir = pt.GetVectorTo(vertices[i + 1]).GetNormal() + (vertices[i + 1]).GetVectorTo(vertices[i + 2]).GetNormal();
                }

            }
            return dir;
        }
        private static bool Commonline(Polyline boundary, Polyline outline, BlockReference basinline)
        {
            var pt = boundary.ToCurve3d().GetClosestPointTo(basinline.Position).Point;
            int a = 0;
            var vertices = boundary.Vertices();
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                if (pt.GetVectorTo(vertices[i]).GetNormal() == -pt.GetVectorTo(vertices[i + 1]).GetNormal())
                {
                    a = i;
                }
            }
            //用以下两点构造线来判断是否与pype相交，‘10’与‘70’为自定义偏置参数。
            Point3d evaluate = Point3d.Origin;
            Point3d evaluate1 = Point3d.Origin;
            if (a > 0)
            {
                var dir1 = (vertices[a]).GetVectorTo((vertices[a - 1])).GetNormal();
                evaluate = vertices[a] + 70.0 * dir1;
                evaluate1 = vertices[a + 1] + 70.0 * dir1;
            }
            else
            {
                var dir1 = (vertices[1]).GetVectorTo((vertices[2])).GetNormal();
                evaluate = vertices[a] + 70.0 * dir1;
                evaluate1 = vertices[a + 1] + 70.0 * dir1;
            }

            var pts = new Point3dCollection();
            Line line2 = new Line(evaluate, evaluate1);
            outline.IntersectWith(line2, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            return pts.Count == 0;
        }    
        private static bool GetOutsidePipe(Point3d pt, BlockReference basinline)
        {
            if((pt.X<= basinline.Position.X+500)&& (pt.X >= basinline.Position.X-500))
            {
                return true;
            }
            else if ((pt.Y <= basinline.Position.Y + 500) && (pt.Y >= basinline.Position.Y - 500))
            {
                return true;
            }
            return false;
        }
        private Point3d Addpipe(Polyline boundary, BlockReference basinline, Polyline pype, Polyline outline)
        {
            var pt = boundary.ToCurve3d().GetClosestPointTo(basinline.Position).Point;
            int a = 0;
            var vertices = boundary.Vertices();
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                if (pt.GetVectorTo(vertices[i]).GetNormal() == -pt.GetVectorTo(vertices[i + 1]).GetNormal())
                {
                    a = i;
                }
            }
            var dir = vertices[a].GetVectorTo(vertices[a + 1]).GetNormal();
            var dir1 = Vector3d.XAxis;
            Point3d evaluate = Point3d.Origin;
            Point3d evaluate_1 = Point3d.Origin;
            Point3d evaluate_2 = Point3d.Origin;
            Point3d evaluate_3 = Point3d.Origin;
            Point3d evaluate_4 = Point3d.Origin;
            Point3d evaluate_5 = Point3d.Origin;
            if (a > 0)
            {
                dir1 = (vertices[a]).GetVectorTo((vertices[a - 1])).GetNormal();
                evaluate = vertices[a] + 70.0 * dir;
                evaluate_1 = vertices[a - 1] + 70.0 * dir;
                evaluate_2 = vertices[a] - 10.0 * dir;
                evaluate_3 = vertices[a - 1] - 10.0 * dir;
                evaluate_4 = vertices[a] + 10.0 * dir1;
                evaluate_5 = vertices[a + 1] + 10.0 * dir1;
            }
            else
            {
                dir1 = (vertices[1]).GetVectorTo((vertices[2])).GetNormal();
                evaluate = vertices[0] + 70.0 * dir;
                evaluate_1 = vertices[1] + 70.0 * dir;
                evaluate_2 = vertices[0] - 10.0 * dir;
                evaluate_3 = vertices[1] - 10.0 * dir;
                evaluate_4 = vertices[0] + 10.0 * dir1;
                evaluate_5 = vertices[1] + 10.0 * dir1;
            }
            var pts = new Point3dCollection();
            var pts1 = new Point3dCollection();
            var pts2 = new Point3dCollection();
            Line line1 = new Line(evaluate, evaluate_1);
            Line line2 = new Line(evaluate_2, evaluate_3);
            Line line3 = new Line(evaluate_4, evaluate_5);
            pype.IntersectWith(line1, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            pype.IntersectWith(line2, Intersect.ExtendArgument, pts1, (IntPtr)0, (IntPtr)0);
            pype.IntersectWith(line3, Intersect.ExtendArgument, pts2, (IntPtr)0, (IntPtr)0);
            if (pts.Count > 0)
            {
                if ((pts.Count > 0) || (pts1.Count > 0))
                {
                    return vertices[a + 1] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (-dir + dir1);
                }
                else
                {
                    return vertices[a] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (dir + dir1);
                }
            }
            else
            {
                if (outline.GetCenter().DistanceTo(vertices[a + 1]) < outline.GetCenter().DistanceTo(vertices[a]))
                {
                    return vertices[a + 1] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (-dir + dir1);
                }
                else
                {
                    return vertices[a] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (dir + dir1);
                }
            }
        }
        private Point3d FindOutsideVertex(BlockReference basinline, Polyline outline)
        {
            var vertices = outline.Vertices();
            Double dst = double.MaxValue;
            int a = 0;
            for (int i = 0; i < vertices.Count; i++)
            { if(dst>vertices[i].DistanceTo(basinline.Position))
                {
                    dst = vertices[i].DistanceTo(basinline.Position);
                    a = i;
                }
            }
            if (a > 0&&a< vertices.Count-1)
            {
                return vertices[a]+ ThWPipeCommon.WELL_TO_WALL_OFFSET * ( vertices[a].GetVectorTo(vertices[a+1]).GetNormal()+ vertices[a].GetVectorTo(vertices[a-1]).GetNormal());
            }
            else if(a==0)
            {
                return vertices[0] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (vertices[0].GetVectorTo(vertices[1]).GetNormal() + vertices[1].GetVectorTo(vertices[2]).GetNormal());
            }
            else
            {
                return vertices[a] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (vertices[a].GetVectorTo(vertices[a-1]).GetNormal() + vertices[a-1].GetVectorTo(vertices[a - 2]).GetNormal());
            }
        }      
    }
}
