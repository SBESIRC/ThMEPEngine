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
        public ThWPipeZone Zone { get; set; }
        public Point3dCollection Pipes { get; set; }
        public ThWKitchenPipeParameters Parameters { get; set; }
        public ThWKitchenPipeEngine()
        {
            Pipes = new Point3dCollection();
        }
        public void Dispose()
        {
        }
        public void Run(Polyline boundary, Polyline outline, BlockReference basinline, Polyline pype)
        {
            if (OutlineInBoundary(boundary, outline))
            {
                var pt = FindInsideVertex(boundary, outline);
                var dir = GetDirection(boundary, outline, pt);
                Pipes.Add(pt + dir * 100);

                //如果管井和台盆不共边，则需要添加一个管子
                if (Commonline(boundary, outline, basinline))
                {
                    Pipes.Add(Addpipe(boundary, basinline, pype, outline));
                }
            }
            else
            {
                if (Parallelline(boundary, outline, basinline))
                {
                    var pt = FindOutsideVertex(basinline, outline);              
                    Pipes.Add(pt);

                }
                else
                {
                    var pt = FindOutsideVertex(basinline, outline);
                    Pipes.Add(pt);
                    Pipes.Add(Addpipe(boundary, basinline, pype, outline));
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

            Point3d center = outline.GetCenter();
            Point3d Ray_bou = Point3d.Origin;
            var pts = new Point3dCollection();
            List<int> num = new List<int>();
            List<double> dst = new List<double>();
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                Point3d midpoint = GetMidPoint(vertices[i], vertices[i + 1]);

                Ray_bou = boundary.ToCurve3d().GetClosestPointTo(midpoint).Point;

                dst.Add(midpoint.DistanceTo(Ray_bou));


            }

            for (int i = 0; i < 2; i++)
            {
                if (dst[i] < dst[i + 2])
                {
                    num.Add(i);
                }
                else
                {
                    num.Add(i + 2);
                }
            }
            Line line2 = new Line(vertices[num[0]], vertices[num[0] + 1]);
            Line line3 = new Line(vertices[num[1]], vertices[num[1] + 1]);
            line2.IntersectWith(line3, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            return pts[0];
        }
        private Point3d GetMidPoint(Point3d pt1, Point3d pt2)
        {
            return pt1 + pt1.GetVectorTo(pt2) * 0.5;
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
        private static bool Parallelline(Polyline boundary, Polyline outline, BlockReference basinline)
        {
            var vertices = boundary.Vertices();
            Line boundarybaseline = new Line();
            double dst = double.MaxValue;
            int a = 0;
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                boundarybaseline = new Line(vertices[i], vertices[i + 1]);
                if (dst > boundarybaseline.GetDistToPoint(basinline.Position))
                {
                    dst = boundarybaseline.GetDistToPoint(basinline.Position);
                    a = i;
                }
            }
            //boundarybaseline = new Line(vertices[a], vertices[a + 1]);
            var vertices_1 = outline.Vertices();
            Line outlinebaseline = new Line();
            if (vertices_1[0].DistanceTo(vertices_1[1]) < vertices_1[2].DistanceTo(vertices_1[1]))
            {
                if (vertices[a].GetVectorTo(vertices[a + 1]).IsParallelTo(vertices_1[2].GetVectorTo(vertices_1[1])))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (vertices[a].GetVectorTo(vertices[a + 1]).IsParallelTo(vertices_1[0].GetVectorTo(vertices_1[1])))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
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
                    return vertices[a + 1] + 100.0 * (-dir + dir1);
                }
                else
                {
                    return vertices[a] + 100.0 * (dir + dir1);
                }
            }
            else
            {
                if (outline.GetCenter().DistanceTo(vertices[a + 1]) < outline.GetCenter().DistanceTo(vertices[a]))
                {
                    return vertices[a + 1] + 100.0 * (-dir + dir1);
                }
                else
                {
                    return vertices[a] + 100.0 * (dir + dir1);
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
            if (a > 0)
            {
                return vertices[a]+100*( vertices[a].GetVectorTo(vertices[a+1]).GetNormal()+ vertices[a].GetVectorTo(vertices[a-1]).GetNormal());
            }
            else
            {
                return vertices[0] + 100 * (vertices[0].GetVectorTo(vertices[1]).GetNormal() + vertices[1].GetVectorTo(vertices[2]).GetNormal());
            }
        }      
    }
}
