using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using DotNetARX;
using ThCADExtension;

namespace ThMEPWSS.Pipe
{
    public class ThWBalconyFloordrainEngine : IDisposable
    {
        public void Dispose()
        {
        }
        public List<BlockReference> Floordrain_washing { get; set; }
  
        public List<BlockReference> Floordrain { get; set; }
       
        public Point3dCollection Downspout_to_Floordrain { get; set; }
        public Point3dCollection Rainpipe_to_Floordrain { get; set; }

        public Circle new_circle { get; set; }
        public ThWBalconyFloordrainEngine()
        {
            Floordrain_washing = new List<BlockReference>();      
            Floordrain = new List<BlockReference>();         
            Downspout_to_Floordrain = new Point3dCollection();
            Rainpipe_to_Floordrain = new Point3dCollection();
            new_circle = new Circle();
        }
        public void Run(List<BlockReference> bfloordrain, Polyline bboundary, Polyline rainpipe, Polyline downspout, BlockReference washingmachine, Polyline device, Polyline device_other,Polyline condensepipe)
        {
            List<BlockReference> floordrain =Isinside(bfloordrain, bboundary);
            int num = Washingfloordrain(floordrain, washingmachine);
            for (int i=0;i< floordrain.Count;i++)
            {
                if (i != num)
                {
                    Floordrain.Add(floordrain[i]);
                }
                else
                {
                    Floordrain_washing.Add(floordrain[i]);
                }
            }
            if (Isdownspout_in(bboundary, downspout))
            {      
                Downspout_to_Floordrain.Add((downspout.GetCenter() - 50 * ((Floordrain_washing[0].Position).GetVectorTo(downspout.GetCenter()).GetNormal())));
                Downspout_to_Floordrain.Add((Floordrain_washing[0].Position + 50 * ((Floordrain_washing[0].Position).GetVectorTo(downspout.GetCenter()).GetNormal())));
            }
            else
            { 
                if (downspout.GetCenter().DistanceTo((Floordrain_washing[0].Position))<=800)
                {
                    Downspout_to_Floordrain = Line_vertices(bboundary, downspout.GetCenter(), Floordrain_washing, washingmachine);
              
                }
                else
                {   var center= new_downspout(bboundary, condensepipe, Floordrain_washing,device);
                    new_circle = new Circle() {Radius=50,Center=center };
                    
                    Downspout_to_Floordrain = Line_vertices(bboundary, center, Floordrain_washing, washingmachine);
                }

            }
            if (Israinpipe_in(bboundary, rainpipe))
            {
                Rainpipe_to_Floordrain.Add((rainpipe.GetCenter() - 50 * ((Floordrain[0].Position).GetVectorTo(rainpipe.GetCenter()).GetNormal())));
                Rainpipe_to_Floordrain.Add((Floordrain[0].Position + 50 * ((Floordrain[0].Position).GetVectorTo(rainpipe.GetCenter()).GetNormal())));
            }
            else
            {
                if (rainpipe.GetCenter().DistanceTo((Floordrain[0].Position)) <= 800)
                {
                    Rainpipe_to_Floordrain = Line_vertices_rainpipe(bboundary, rainpipe.GetCenter(), Floordrain);

                }
                else
                {
                    throw new ArgumentNullException("Rainpipe was Null"); 
                }

            }

        }
        private static List<BlockReference> Isinside(List<BlockReference> bfloordrain, Polyline bboundary)
        {
            
            List<BlockReference> floordrain = new List<BlockReference>();
            for (int i = 0; i < bfloordrain.Count; i++)
            {
                var pts = new Point3dCollection();
                var basepoint = bfloordrain[i].Position;
                var center = bboundary.GetCenter();
                Line line = new Line(center, basepoint);
                bboundary.IntersectWith(line, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
                if (pts.Count>0)
                {
                    if ((pts[0].GetVectorTo(basepoint)).IsCodirectionalTo(basepoint.GetVectorTo(pts[1])))
                    {
                        floordrain.Add(bfloordrain[i]);

                    }
                }
            }
            return floordrain;
        }
        private static int Washingfloordrain(List<BlockReference> floordrain, BlockReference washingmachine)
        {   int num = 0;
            double dst=double.MaxValue;
            List<BlockReference> washingfloordrain = new List<BlockReference>();
            for (int i = 0; i < floordrain.Count; i++)
            {
                var basepoint = floordrain[i].Position;
                var basepoint1 = washingmachine.Position;
                
                if (dst>basepoint.DistanceTo(basepoint1))
                {
                    dst = basepoint.DistanceTo(basepoint1);
                    num = i;

                }
            }
            return num;
        }
        private bool Isdownspout_in(Polyline bboundary, Polyline downspout)
        {
            var center_b = bboundary.GetCenter();
            var center_d= downspout.GetCenter();
            Line line = new Line(center_b, center_d);
            var pts = new Point3dCollection();
            bboundary.IntersectWith(line, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            if ((pts[0].GetVectorTo(center_d)).IsCodirectionalTo(center_d.GetVectorTo(pts[1])))
            {
                return true;

            }
            else
            {
                return false;
            }
        }
        private bool Israinpipe_in(Polyline bboundary, Polyline rainpipe)
        {
            var center_b = bboundary.GetCenter();
            var center_d = rainpipe.GetCenter();
            Line line = new Line(center_b, center_d);
            var pts = new Point3dCollection();
            bboundary.IntersectWith(line, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            if ((pts[0].GetVectorTo(center_d)).IsCodirectionalTo(center_d.GetVectorTo(pts[1])))
            {
                return true;

            }
            else
            {
                return false;
            }
        }
        private static Point3dCollection Line_vertices(Polyline bboundary,  Point3d center_spout, List<BlockReference> Floordrain_washing, BlockReference washingmachine)
        {
            
            var vertices = new Point3dCollection();
            var pts = new Point3dCollection();
            double dst = double.MaxValue;
            int num = 0;
            for(int i=0;i< bboundary.Vertices().Count-1;i++)
            {   
                Line boundaryline = new Line(bboundary.Vertices()[i], bboundary.Vertices()[i+1]);
                var boundarypoint = boundaryline.ToCurve3d().GetClosestPointTo( washingmachine.Position).Point;
                if (dst > boundarypoint.DistanceTo(washingmachine.Position))
                {
                    dst = boundarypoint.DistanceTo(washingmachine.Position);
                    num = i;
                }

            }
            Line linespecific= new Line(bboundary.Vertices()[num], bboundary.Vertices()[num + 1]);
            var perpendicular_point = linespecific.ToCurve3d().GetClosestPointTo(center_spout).Point;
            var dmin = perpendicular_point.DistanceTo(center_spout);
            Line line = new Line(center_spout, perpendicular_point);         
            var perpendicular_point1= linespecific.ToCurve3d().GetClosestPointTo(Floordrain_washing[0].Position).Point;
            Line line1 = new Line(Floordrain_washing[0].Position, perpendicular_point1);
            line.Rotation(center_spout, Math.PI * 45 / 180);
            line1.IntersectWith(line, Intersect.ExtendBoth, pts, (IntPtr)0, (IntPtr)0);
            var perpendicular_point2 = linespecific.ToCurve3d().GetClosestPointTo(pts[0]).Point;
            if (perpendicular_point2.DistanceTo(pts[0])<dmin)
            { if(perpendicular_point2.DistanceTo(pts[0])>=200)
                {   if (pts[0].DistanceTo(center_spout) >= (perpendicular_point.DistanceTo(perpendicular_point1)) * Math.Sqrt(2))
                    {
                        vertices.Add(center_spout + 50 * center_spout.GetVectorTo(pts[0]).GetNormal());
                        vertices.Add(pts[0]);
                        vertices.Add(Floordrain_washing[0].Position + 50 * Floordrain_washing[0].Position.GetVectorTo(perpendicular_point1).GetNormal());
                    }
                else
                    {
                        var temp = center_spout + 50 * center_spout.GetVectorTo(pts[0]).GetNormal();
                        var temp1 = linespecific.ToCurve3d().GetClosestPointTo(temp).Point;
                        var tts = new Point3dCollection();
                        Line templine =new Line(temp,temp1);
                        Circle circletemp = new Circle() { Center = Floordrain_washing[0].Position, Radius = 50 };
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);

                        vertices.Add(temp);
                        if(tts[0].DistanceTo(temp)> tts[1].DistanceTo(temp))
                        {
                            vertices.Add(tts[1]);
                        }
                        else
                        {
                            vertices.Add(tts[0]);
                        }

                    }

                }
            else
                {   if (perpendicular_point2.DistanceTo(pts[0])<=150)
                    {
                        vertices.Add(center_spout + 50 * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(center_spout + (200 - perpendicular_point2.DistanceTo(pts[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(pts[0] + (200 - perpendicular_point2.DistanceTo(pts[0])) * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                        vertices.Add(Floordrain_washing[0].Position + 50 * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                    }
                    else
                    {
                        var temp = center_spout + (200 - perpendicular_point2.DistanceTo(pts[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal());
                        var temp1 = pts[0] + (200 - perpendicular_point2.DistanceTo(pts[0])) * perpendicular_point2.GetVectorTo(pts[0]).GetNormal();
                        var tts = new Point3dCollection();
                        Line templine = new Line(temp, temp1);
                        Circle circletemp = new Circle() { Center = center_spout, Radius = 50 };
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);
                        if (tts[0].DistanceTo(temp) > tts[1].DistanceTo(temp))
                        {
                            vertices.Add(tts[1]);
                        }
                        else
                        {
                            vertices.Add(tts[0]);
                        }
                        vertices.Add(temp1);
                        vertices.Add(Floordrain_washing[0].Position + 50 * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                    }
                }
            }
            else
            {
                var pts1 = new Point3dCollection();
                line.Rotation(center_spout, -Math.PI / 2);
                line1.IntersectWith(line, Intersect.ExtendBoth, pts1, (IntPtr)0, (IntPtr)0);
                var perpendicular_point3 = linespecific.ToCurve3d().GetClosestPointTo(pts1[0]).Point;
                if (perpendicular_point3.DistanceTo(pts1[0]) >= 200)
                {   if (pts1[0].DistanceTo(center_spout) >= (perpendicular_point.DistanceTo(perpendicular_point1)) * Math.Sqrt(2))
                    {
                        vertices.Add(center_spout + 50 * center_spout.GetVectorTo(pts1[0]).GetNormal());
                        vertices.Add(pts1[0]);
                        vertices.Add(Floordrain_washing[0].Position + 50 * Floordrain_washing[0].Position.GetVectorTo(perpendicular_point1).GetNormal());
                    }
                     else
                    {
                        var temp = center_spout + 50 * center_spout.GetVectorTo(pts1[0]).GetNormal();
                        var temp1 = linespecific.ToCurve3d().GetClosestPointTo(temp).Point;
                        var tts = new Point3dCollection();
                        Line templine = new Line(temp, temp1);
                        Circle circletemp = new Circle() { Center = Floordrain_washing[0].Position, Radius = 50 };
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);

                        vertices.Add(temp);
                        if (tts[0].DistanceTo(temp) > tts[1].DistanceTo(temp))
                        {
                            vertices.Add(tts[1]);
                        }
                        else
                        {
                            vertices.Add(tts[0]);
                        }
                    }

                }
                else
                {
                    if (perpendicular_point3.DistanceTo(pts1[0]) <= 150)
                    {
                        vertices.Add(center_spout + 50 * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(center_spout + (200 - perpendicular_point3.DistanceTo(pts1[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(pts1[0] + (200 - perpendicular_point3.DistanceTo(pts1[0])) * perpendicular_point3.GetVectorTo(pts1[0]).GetNormal());
                        vertices.Add(Floordrain_washing[0].Position + 50 * perpendicular_point3.GetVectorTo(pts1[0]).GetNormal());
                    }
                    else
                    {
                        var temp = center_spout + (200 - perpendicular_point3.DistanceTo(pts1[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal());
                        var temp1 = pts1[0] + (200 - perpendicular_point3.DistanceTo(pts1[0])) * perpendicular_point3.GetVectorTo(pts1[0]).GetNormal();
                        var tts = new Point3dCollection();
                        Line templine = new Line(temp, temp1);
                        Circle circletemp = new Circle() { Center = center_spout, Radius = 50 };
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);
                        if (tts[0].DistanceTo(temp) > tts[1].DistanceTo(temp))
                        {
                            vertices.Add(tts[1]);
                        }
                        else
                        {
                            vertices.Add(tts[0]);
                        }
                        vertices.Add(temp1);
                        vertices.Add(Floordrain_washing[0].Position + 50 * perpendicular_point3.GetVectorTo(pts1[0]).GetNormal());
                    }
                }
            }
            return vertices;
        }
        private static Point3d new_downspout(Polyline bboundary, Polyline rainpipe, List<BlockReference> Floordrain_washing, Polyline device)
        {   
            Point3d center = Point3d.Origin;
            var vertices = device.Vertices();
            double dmax = double.MaxValue;
            int a = 0;
           
            for (int i=0;i< vertices.Count;i++)
            {
               
                if(dmax>(Floordrain_washing[0].Position.DistanceTo(vertices[i])))
                {
                    dmax = (Floordrain_washing[0].Position.DistanceTo(vertices[i]));
                    a = i;
                }
            }

            var perpendicular_point = device.ToCurve3d().GetClosestPointTo(Floordrain_washing[0].Position).Point;
          

            if (a > 0)
            {
                double dst = 0.0;
                if (vertices[a].GetVectorTo(vertices[a - 1]).IsParallelTo((Floordrain_washing[0].Position).GetVectorTo(perpendicular_point)))
                {
                    Line line = new Line(vertices[a], vertices[a - 1]);
                    dst = line.ToCurve3d().GetDistanceTo(rainpipe.GetCenter());
                    center = vertices[a] + 100 * (Floordrain_washing[0].Position.GetVectorTo(perpendicular_point).GetNormal()) + dst * (vertices[a].GetVectorTo(vertices[a+1]).GetNormal());
                }
                else
                {
                    Line line = new Line(vertices[a], vertices[a+1]);
                    dst = line.ToCurve3d().GetDistanceTo(rainpipe.GetCenter());
                    center = vertices[a] + 100 * (Floordrain_washing[0].Position.GetVectorTo(perpendicular_point).GetNormal()) + dst * (vertices[a].GetVectorTo(vertices[a-1]).GetNormal());
                }
            }
            else
            {
                double dst = 0.0;
                if (vertices[0].GetVectorTo(vertices[1]).IsParallelTo((Floordrain_washing[0].Position).GetVectorTo(perpendicular_point)))
                {
                    Line line = new Line(vertices[0], vertices[1]);
                    var point = line.ToCurve3d().GetClosestPointTo(rainpipe.GetCenter()).Point;
                    dst = point.DistanceTo(rainpipe.GetCenter());
                    center = vertices[0] + 100 * (Floordrain_washing[0].Position.GetVectorTo(perpendicular_point).GetNormal()) + dst * (vertices[0].GetVectorTo(vertices[vertices.Count - 2]).GetNormal());
                }
                else
                {
                    Line line = new Line(vertices[0], vertices[vertices.Count-2]);
                    var point = line.ToCurve3d().GetClosestPointTo(rainpipe.GetCenter()).Point;
                    var s = rainpipe.GetCenter();
                    dst = point.DistanceTo(s);
                    center = vertices[0] + 100 * (Floordrain_washing[0].Position.GetVectorTo(perpendicular_point).GetNormal()) + dst * (vertices[0].GetVectorTo(vertices[1]).GetNormal());
                }

            }

           
            return center;
        }
        private static Point3dCollection Line_vertices_rainpipe(Polyline bboundary, Point3d center_rainpipe, List<BlockReference> Floordrain)
        {

            var vertices = new Point3dCollection();
            var pts = new Point3dCollection();
            double dst = double.MaxValue;
            int num = 0;
            for (int i = 0; i < bboundary.Vertices().Count - 1; i++)
            {
                Line boundaryline = new Line(bboundary.Vertices()[i], bboundary.Vertices()[i + 1]);
                Line line_temp = new Line(Floordrain[0].Position, center_rainpipe);
                var boundarypoints = new Point3dCollection();
                boundaryline.IntersectWith(line_temp, Intersect.ExtendArgument, boundarypoints, (IntPtr)0, (IntPtr)0);
                if (boundarypoints.Count>0)
                {  if (dst > boundarypoints[0].DistanceTo(center_rainpipe))
                    {
                        dst = boundarypoints[0].DistanceTo(center_rainpipe);
                        num = i;
                    }                  
                }
            }
            Line linespecific = new Line(bboundary.Vertices()[num], bboundary.Vertices()[num + 1]);
            var perpendicular_point = linespecific.ToCurve3d().GetClosestPointTo(center_rainpipe).Point;
            var dmin = perpendicular_point.DistanceTo(center_rainpipe);
            Line line = new Line(center_rainpipe, perpendicular_point);
            var perpendicular_point1 = linespecific.ToCurve3d().GetClosestPointTo(Floordrain[0].Position).Point;
            Line line1 = new Line(Floordrain[0].Position, perpendicular_point1);
            line.Rotation(center_rainpipe, Math.PI * 45 / 180);
            line1.IntersectWith(line, Intersect.ExtendBoth, pts, (IntPtr)0, (IntPtr)0);
            var perpendicular_point2 = linespecific.ToCurve3d().GetClosestPointTo(pts[0]).Point;
            if (perpendicular_point2.DistanceTo(pts[0]) < dmin)
            {
                if (perpendicular_point2.DistanceTo(pts[0]) >= 200)
                {
                    if (pts[0].DistanceTo(center_rainpipe) >= (perpendicular_point.DistanceTo(perpendicular_point1)) * Math.Sqrt(2))
                    {
                        vertices.Add(center_rainpipe + 50 * center_rainpipe.GetVectorTo(pts[0]).GetNormal());
                        vertices.Add(pts[0]);
                        vertices.Add(Floordrain[0].Position + 50 * Floordrain[0].Position.GetVectorTo(perpendicular_point1).GetNormal());
                    }
                    else
                    {
                        var temp = center_rainpipe + 50 * center_rainpipe.GetVectorTo(pts[0]).GetNormal();
                        var temp1 = linespecific.ToCurve3d().GetClosestPointTo(temp).Point;
                        var tts = new Point3dCollection();
                        Line templine = new Line(temp, temp1);
                        Circle circletemp = new Circle() { Center = Floordrain[0].Position, Radius = 50 };
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);

                        vertices.Add(temp);
                        if (tts[0].DistanceTo(temp) > tts[1].DistanceTo(temp))
                        {
                            vertices.Add(tts[1]);
                        }
                        else
                        {
                            vertices.Add(tts[0]);
                        }

                    }

                }
                else
                {   if (perpendicular_point2.DistanceTo(pts[0]) <= 150)
                    {
                        vertices.Add(center_rainpipe + 50 * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(center_rainpipe + (200 - perpendicular_point2.DistanceTo(pts[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(pts[0] + (200 - perpendicular_point2.DistanceTo(pts[0])) * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                        vertices.Add(Floordrain[0].Position + 50 * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                    }
                    else
                    {
                        var temp = center_rainpipe + (200 - perpendicular_point2.DistanceTo(pts[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal());
                        var temp1 = pts[0] + (200 - perpendicular_point2.DistanceTo(pts[0])) * perpendicular_point2.GetVectorTo(pts[0]).GetNormal();
                        var tts = new Point3dCollection();
                        Line templine = new Line(temp, temp1);
                        Circle circletemp = new Circle() { Center = center_rainpipe, Radius = 50 };
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);
                        if (tts[0].DistanceTo(temp) > tts[1].DistanceTo(temp))
                        {
                            vertices.Add(tts[1]);
                        }
                        else
                        {
                            vertices.Add(tts[0]);
                        }
                        vertices.Add(temp1);
                        vertices.Add(Floordrain[0].Position + 50 * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                    }
                }
            }
            else
            {
                var pts1 = new Point3dCollection();
                line.Rotation(center_rainpipe, -Math.PI / 2);
                line1.IntersectWith(line, Intersect.ExtendBoth, pts1, (IntPtr)0, (IntPtr)0);
                var perpendicular_point3 = linespecific.ToCurve3d().GetClosestPointTo(pts1[0]).Point;
                if (perpendicular_point3.DistanceTo(pts1[0]) >= 200)
                {
                    if (pts1[0].DistanceTo(center_rainpipe) >= (perpendicular_point.DistanceTo(perpendicular_point1)) * Math.Sqrt(2))
                    {
                        vertices.Add(center_rainpipe + 50 * center_rainpipe.GetVectorTo(pts1[0]).GetNormal());
                        vertices.Add(pts1[0]);
                        vertices.Add(Floordrain[0].Position + 50 * Floordrain[0].Position.GetVectorTo(perpendicular_point1).GetNormal());
                    }
                    else
                    {
                        var temp = center_rainpipe + 50 * center_rainpipe.GetVectorTo(pts1[0]).GetNormal();
                        var temp1 = linespecific.ToCurve3d().GetClosestPointTo(temp).Point;
                        var tts = new Point3dCollection();
                        Line templine = new Line(temp, temp1);
                        Circle circletemp = new Circle() { Center = Floordrain[0].Position, Radius = 50 };
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);

                        vertices.Add(temp);
                        if (tts[0].DistanceTo(temp) > tts[1].DistanceTo(temp))
                        {
                            vertices.Add(tts[1]);
                        }
                        else
                        {
                            vertices.Add(tts[0]);
                        }
                    }

                }
                else
                {
                    if (perpendicular_point3.DistanceTo(pts1[0]) <= 150)
                    {
                        vertices.Add(center_rainpipe + 50 * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(center_rainpipe + (200 - perpendicular_point3.DistanceTo(pts1[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(pts1[0] + (200 - perpendicular_point3.DistanceTo(pts1[0])) * perpendicular_point3.GetVectorTo(pts1[0]).GetNormal());
                        vertices.Add(Floordrain[0].Position + 50 * perpendicular_point3.GetVectorTo(pts1[0]).GetNormal());
                    }
                    else
                    {
                        var temp = center_rainpipe + (200 - perpendicular_point3.DistanceTo(pts1[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal());
                        var temp1 = pts1[0] + (200 - perpendicular_point3.DistanceTo(pts1[0])) * perpendicular_point3.GetVectorTo(pts1[0]).GetNormal();
                        var tts = new Point3dCollection();
                        Line templine = new Line(temp, temp1);
                        Circle circletemp = new Circle() { Center = center_rainpipe, Radius = 50 };
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);                 
                        if (tts[0].DistanceTo(temp) > tts[1].DistanceTo(temp))
                        {
                            vertices.Add(tts[1]);
                        }
                        else
                        {
                            vertices.Add(tts[0]);
                        }
                        vertices.Add(temp1);
                        vertices.Add(Floordrain[0].Position + 50 * perpendicular_point3.GetVectorTo(pts1[0]).GetNormal());
                    }
                }
            }
            return vertices;
        }
    }
}
