using System;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Geom;
using ThMEPWSS.Assistant;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWBalconyFloordrainEngineParameters
    {
        /// <summary>
        /// 轮廓线
        /// </summary>
        public Polyline boundary;
        /// <summary>
        /// 排水管
        /// </summary>
        public Polyline downspout;
        /// <summary>
        /// 屋顶雨水管
        /// </summary>     
        public List<Polyline> rainpipes;
        /// <summary>
        /// 第一设备平台轮廓线
        /// </summary>
        public Polyline device;
        /// <summary>
        /// 第二设备平台轮廓线
        /// </summary>
        public Polyline device_other;
        /// <summary>
        /// 冷凝管
        /// </summary>
        public Polyline condensepipe;
        /// <summary>
        /// 冷凝管组
        /// </summary>
        public List<Polyline> condensepipes;
        /// <summary>
        /// 洗衣机
        /// </summary>
        public BlockReference washingmachine;
        /// <summary>
        /// 台盆
        /// </summary>
        public BlockReference basinline;
        /// <summary>
        /// 地漏
        /// </summary>
        public List<BlockReference> floordrains;
    }

    public class ThWBalconyFloordrainEngine : IDisposable
    {
        public void Dispose()
        {
        }
        public List<BlockReference> Floordrain_washing { get; set; }

        public List<BlockReference> Floordrain { get; set; }

        public Point3dCollection Downspout_to_Floordrain { get; set; }
        public Point3dCollection Rainpipe_to_Floordrain { get; set; }
        public Point3dCollection Bbasinline_to_Floordrain { get; set; }
        public Point3dCollection Bbasinline_Center { get; set; }
        public Circle new_circle { get; set; }
        public ThWBalconyFloordrainEngine()
        {
            Floordrain_washing = new List<BlockReference>();
            Floordrain = new List<BlockReference>();
            Downspout_to_Floordrain = new Point3dCollection();
            Rainpipe_to_Floordrain = new Point3dCollection();
            Bbasinline_to_Floordrain = new Point3dCollection();
            new_circle = new Circle();
            Bbasinline_Center = new Point3dCollection();
        }

        public void Run(ThWBalconyFloordrainEngineParameters parameters)
        {
            Polyline rainpipe = GetRainPipe(parameters.boundary, parameters.washingmachine, parameters.rainpipes);
            List<BlockReference> floordrain = Isinside(parameters.floordrains, parameters.boundary);
            int num = 0;
            if (parameters.washingmachine != null)
            {
                num = Washingfloordrain(floordrain, parameters.washingmachine);//确认地漏序号
            }
            for (int i = 0; i < floordrain.Count; i++)
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
            if (parameters.basinline != null)
            {
                Bbasinline_Center = GetBbasinline_Center(
                    parameters.boundary, 
                    Floordrain_washing, 
                    parameters.washingmachine, 
                    parameters.basinline.Position);
            }
            var device = parameters.device;
            var device_other = parameters.device_other;
            if (device_other == null)
            {
                device_other = device;
            }
            Polyline condensepipe = null;
            int pipenum = 0;
            foreach (Polyline pipe in parameters.condensepipes)
            {
                if (pipe.GetCenter().Equals(parameters.condensepipe.GetCenter()))
                {
                    pipenum = 1;
                    break;
                }

            }
            if (pipenum == 0)
            {
                condensepipe = parameters.condensepipe;
            }
            else
            {
                foreach (Polyline pipe in parameters.condensepipes)
                {
                    if (GeomUtils.PtInLoop(device_other, pipe.GetCenter()))
                    {
                        condensepipe = pipe;
                        break;
                    }
                    else
                    {
                        if (GeomUtils.PtInLoop(device, pipe.GetCenter()))
                        {
                            condensepipe = pipe;
                            break;
                        }

                    }
                }
            }
           
            if (parameters.downspout == null)
            {               
                var center = new_downspout(parameters.boundary, condensepipe, Floordrain_washing, device_other);
                new_circle = new Circle() { Radius = 50, Center = center };
                if (GeomUtils.PtInLoop(parameters.boundary, center))//判断新生管井是否在阳台
                {
                    Downspout_to_Floordrain.Add(center);
                    foreach (var b_floordrain in Floordrain)
                    {
                        if (Floordrain_washing[0].Position.DistanceTo(b_floordrain.Position) < ThWPipeCommon.MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN)
                        {
                            Downspout_to_Floordrain.Add(b_floordrain.Position);
                            break;
                        }
                    }
                    Downspout_to_Floordrain.Add(Floordrain_washing[0].Position);
                    if (parameters.basinline != null && 
                        parameters.basinline.Position.DistanceTo(parameters.washingmachine.Position) < ThWPipeCommon.MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
                    {
                        if (Bbasinline_Center[0].Y > center.Y)
                        {
                            Bbasinline_to_Floordrain.Add(Downspout_to_Floordrain[1]);
                        }
                        foreach (Point3d point in Getvertices(parameters.boundary, parameters.washingmachine, Bbasinline_Center[0], Floordrain_washing, center))
                        {
                            Bbasinline_to_Floordrain.Add(point);
                        }
                    }
                }
                else
                {
                    foreach (var b_floordrain in Floordrain)
                    {
                        if (Floordrain_washing[0].Position.DistanceTo(b_floordrain.Position) < ThWPipeCommon.MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN)
                        {
                            Downspout_to_Floordrain = Line_Addvertices(parameters.boundary, center, Floordrain_washing, parameters.washingmachine, device_other, b_floordrain.Position);
                            break;
                        }
                    }
                    if (Downspout_to_Floordrain.Count == 0)
                    {
                        if (center.DistanceTo(Floordrain_washing[0].Position) > 450)
                        {
                            Downspout_to_Floordrain = Line_vertices1(parameters.boundary, center, Floordrain_washing, parameters.washingmachine, device_other);
                        }
                        else
                        {
                            Downspout_to_Floordrain = Line_vertices(parameters.boundary, center, Floordrain_washing, parameters.washingmachine, device_other);
                        }
                    }
                    if (parameters.basinline != null &&
                        parameters.basinline.Position.DistanceTo(parameters.washingmachine.Position) < ThWPipeCommon.MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
                    {
                        Bbasinline_to_Floordrain = Line_Addvertices1(parameters.boundary, center, Floordrain_washing, parameters.washingmachine, device_other, Bbasinline_Center[0]);
                    }
                }
            }
            else
            {
                var center = parameters.downspout.GetCenter();
                if (GeomUtils.PtInLoop(parameters.boundary, center))
                {
                    Downspout_to_Floordrain.Add((center - ThWPipeCommon.COMMONRADIUS * ((Floordrain_washing[0].Position).GetVectorTo(center).GetNormal())));
                    Downspout_to_Floordrain.Add((Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * ((Floordrain_washing[0].Position).GetVectorTo(center).GetNormal())));
                }
                else
                {
                    if (center.DistanceTo((Floordrain_washing[0].Position)) <= ThWPipeCommon.MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN)
                    {
                        Downspout_to_Floordrain = Line_vertices(parameters.boundary, center, Floordrain_washing, parameters.washingmachine, device_other);

                    }
                    else
                    {
                        var new_center = new_downspout(parameters.boundary, condensepipe, Floordrain_washing, device_other);
                        new_circle = new Circle() { Radius = 50, Center = center };
                        Downspout_to_Floordrain = Line_vertices(parameters.boundary, center, Floordrain_washing, parameters.washingmachine, device_other);
                    }
                }
            }
            if (rainpipe != null)
            {
                if (GeomUtils.PtInLoop(parameters.boundary, rainpipe.GetCenter()))
                {
                    Rainpipe_to_Floordrain.Add((rainpipe.GetCenter() - ThWPipeCommon.COMMONRADIUS * ((Floordrain[0].Position).GetVectorTo(rainpipe.GetCenter()).GetNormal())));
                    Rainpipe_to_Floordrain.Add((Floordrain[0].Position + ThWPipeCommon.COMMONRADIUS * ((Floordrain[0].Position).GetVectorTo(rainpipe.GetCenter()).GetNormal())));
                }
                else
                {
                    foreach (var criticalfloordrain in Floordrain)
                    {
                        if (rainpipe.GetCenter().DistanceTo((criticalfloordrain.Position)) <= ThWPipeCommon.MAX_RAINPIPE_TO_BALCONYFLOORDRAIN)
                        {
                            Rainpipe_to_Floordrain = Line_vertices_rainpipe(parameters.boundary, rainpipe.GetCenter(), criticalfloordrain, device);
                            break;

                        }
                    }
                    if (Rainpipe_to_Floordrain == null)
                    {
                        throw new ArgumentNullException("Rainpipe was Null");
                    }
                }
            }
        }
        private static Polyline GetRainPipe(Polyline bboundary, BlockReference washingmachine, List<Polyline> rainpipes)
        {   
            if (rainpipes.Count > 0)
            {
                foreach (var pipe in rainpipes)
                {
                    if (GeomUtils.PtInLoop(bboundary, pipe.GetCenter())&&
                        pipe.GetCenter().Y< washingmachine.Position.Y)
                    {
                        return pipe;
                    }
                }
                foreach (var pipe in rainpipes)
                {
                    if (pipe.GetCenter().DistanceTo(washingmachine.Position) >ThWPipeCommon.MAX_RAINPIPE_TO_BALCONYFLOORDRAIN)//洗衣机地漏与洗衣机接近，此处借用参数
                    {
                        return pipe;
                    }
                }
            }
            return null;
        }
        private static List<BlockReference> Isinside(List<BlockReference> bfloordrain, Polyline bboundary)
        {
            List<BlockReference> floordrain = new List<BlockReference>();
            for (int i = 0; i < bfloordrain.Count; i++)
            {             
                if (GeomUtils.PtInLoop(bboundary, bfloordrain[i].Position))
                {
                    floordrain.Add(bfloordrain[i]);
                }
            }
            return floordrain;
        }
        private static int Washingfloordrain(List<BlockReference> floordrain, BlockReference washingmachine)
        { 
            int num = 0;
            double dst = double.MaxValue;
            List<BlockReference> washingfloordrain = new List<BlockReference>();
            for (int i = 0; i < floordrain.Count; i++)
            {
                var basepoint = floordrain[i].Position;
                var basepoint1 = washingmachine.Position;
                if (dst > basepoint.DistanceTo(basepoint1))
                {
                    dst = basepoint.DistanceTo(basepoint1);
                    num = i;

                }
            }
            return num;
        }     
        private static Point3dCollection Line_vertices(Polyline bboundary, Point3d center_spout, List<BlockReference> Floordrain_washing, BlockReference washingmachine, Polyline device)
        {
            var vertices = new Point3dCollection();
            var pts = new Point3dCollection();           
            int num = CriticalLineNumber(bboundary, washingmachine);     
            Line linespecific = new Line(bboundary.Vertices()[num], bboundary.Vertices()[num + 1]);
            var perpendicular_point = linespecific.ToCurve3d().GetClosestPointTo(center_spout).Point;//排水井在洗衣机所在边垂点
            var dmin = perpendicular_point.DistanceTo(center_spout);
            Line line = new Line(center_spout, perpendicular_point); //排水管构造横边  
            var perpendicular_point1 = linespecific.ToCurve3d().GetClosestPointTo(Floordrain_washing[0].Position).Point;//洗衣机所在边洗衣机地漏垂点
            Line line1 = new Line(Floordrain_washing[0].Position, perpendicular_point1);//构造横边  
            line.Rotation(center_spout, Math.PI/4);
            line1.IntersectWith(line, Intersect.ExtendBoth, pts, (IntPtr)0, (IntPtr)0);
            var perpendicular_point2 = linespecific.ToCurve3d().GetClosestPointTo(pts[0]).Point;//交点垂足
            var perpendicular_point3 = device.ToCurve3d().GetClosestPointTo(Floordrain_washing[0].Position).Point;//排水井在洗衣机所在边垂点
            if (perpendicular_point2.DistanceTo(pts[0]) < dmin)
            {
                if (perpendicular_point2.DistanceTo(pts[0]) >= ThWPipeCommon.MAX_ROOM_INTERVAL)
                {
                    if (perpendicular_point1.DistanceTo(perpendicular_point) >= (ThWPipeCommon.MAX_ROOM_INTERVAL - ThWPipeCommon.COMMONRADIUS))
                    {
                        vertices.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * Floordrain_washing[0].Position.GetVectorTo(perpendicular_point3).GetNormal());
                        vertices.Add(perpendicular_point3);
                        vertices.Add(perpendicular_point3 + (center_spout.DistanceTo(perpendicular_point) - perpendicular_point1.DistanceTo(perpendicular_point3)) * (perpendicular_point.GetVectorTo(center_spout).GetNormal() + (perpendicular_point1.GetVectorTo(perpendicular_point).GetNormal())));
                        vertices.Add(center_spout + ThWPipeCommon.COMMONRADIUS * center_spout.GetVectorTo(perpendicular_point1).GetNormal());
                    }
                    else
                    {
                        vertices.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * Floordrain_washing[0].Position.GetVectorTo(perpendicular_point3).GetNormal());
                        vertices.Add(perpendicular_point3);
                        var temp1 = perpendicular_point3 + (perpendicular_point.GetVectorTo(center_spout).GetNormal() + (perpendicular_point1.GetVectorTo(perpendicular_point).GetNormal()));
                        var tts = new Point3dCollection();
                        Line templine = new Line(perpendicular_point3, temp1);
                        Circle circletemp = new Circle() { Center = center_spout, Radius = 50 };//取与排水井为中心圆的交点
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);
                        if (tts[0].DistanceTo(center_spout) > tts[1].DistanceTo(center_spout))
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
                    if (perpendicular_point2.DistanceTo(pts[0]) <= (ThWPipeCommon.MAX_ROOM_INTERVAL - ThWPipeCommon.COMMONRADIUS))
                    {
                        //第一点已在排水井圆心外
                        vertices.Add(center_spout + ThWPipeCommon.COMMONRADIUS * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(center_spout + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point2.DistanceTo(pts[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(pts[0] + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point2.DistanceTo(pts[0])) * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                        vertices.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                    }
                    else
                    {
                        //第一点在排水井圆心内
                        var temp = center_spout + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point2.DistanceTo(pts[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal());
                        var temp1 = pts[0] + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point2.DistanceTo(pts[0])) * perpendicular_point2.GetVectorTo(pts[0]).GetNormal();
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
                        vertices.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                    }
                }
            }
            else
            {
                var pts1 = new Point3dCollection();
                line.Rotation(center_spout, -Math.PI / 2);
                line1.IntersectWith(line, Intersect.ExtendBoth, pts1, (IntPtr)0, (IntPtr)0);
                var perpendicular_point4 = linespecific.ToCurve3d().GetClosestPointTo(pts1[0]).Point;
                if (perpendicular_point4.DistanceTo(pts1[0]) >= ThWPipeCommon.MAX_ROOM_INTERVAL)
                {
                    if (pts1[0].DistanceTo(center_spout) >= ThWPipeCommon.COMMONRADIUS)
                    {
                        vertices.Add(center_spout + ThWPipeCommon.COMMONRADIUS * center_spout.GetVectorTo(pts1[0]).GetNormal());
                        vertices.Add(pts1[0]);
                        vertices.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * Floordrain_washing[0].Position.GetVectorTo(perpendicular_point1).GetNormal());
                    }
                    else
                    {
                        var temp1 = linespecific.ToCurve3d().GetClosestPointTo(pts1[0]).Point;
                        var tts = new Point3dCollection();
                        Line templine = new Line(pts1[0], temp1);
                        Circle circletemp = new Circle() { Center = center_spout, Radius = 50 };
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);
                        if (tts[0].DistanceTo(pts1[0]) > tts[1].DistanceTo(pts1[0]))
                        {
                            vertices.Add(tts[1]);
                        }
                        else
                        {
                            vertices.Add(tts[0]);
                        }
                        vertices.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * Floordrain_washing[0].Position.GetVectorTo(perpendicular_point1).GetNormal());
                    }

                }
                else
                {
                    if (perpendicular_point4.DistanceTo(pts1[0]) <= (ThWPipeCommon.MAX_ROOM_INTERVAL - ThWPipeCommon.COMMONRADIUS))
                    {
                        vertices.Add(center_spout + ThWPipeCommon.COMMONRADIUS * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(center_spout + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point4.DistanceTo(pts1[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(pts1[0] + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point4.DistanceTo(pts1[0])) * perpendicular_point4.GetVectorTo(pts1[0]).GetNormal());
                        vertices.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * perpendicular_point4.GetVectorTo(pts1[0]).GetNormal());
                    }
                    else
                    {
                        var temp = center_spout + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point4.DistanceTo(pts1[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal());
                        var temp1 = pts1[0] + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point4.DistanceTo(pts1[0])) * perpendicular_point4.GetVectorTo(pts1[0]).GetNormal();
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
                        vertices.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * perpendicular_point4.GetVectorTo(pts1[0]).GetNormal());
                    }
                }
            }
            return vertices;
        }
        private static Point3dCollection Line_vertices1(Polyline bboundary, Point3d center_spout, List<BlockReference> Floordrain_washing, BlockReference washingmachine, Polyline device)
        {
            var points = new Point3dCollection();            
            Point3d point = new Point3d(center_spout.X, Floordrain_washing[0].Position.Y, 0);
            Point3d point1 = bboundary.ToCurve3d().GetClosestPointTo(point).Point;
            points.Add(center_spout + ThWPipeCommon.COMMONRADIUS * center_spout.GetVectorTo(point).GetNormal());
            points.Add(point -point.DistanceTo(point1) * center_spout.GetVectorTo(point).GetNormal());
            points.Add(point - point.DistanceTo(point1) * Floordrain_washing[0].Position.GetVectorTo(point).GetNormal());
            points.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * Floordrain_washing[0].Position.GetVectorTo(point).GetNormal());
            return points;
        }
        private static Point3d new_downspout(Polyline bboundary, Polyline rainpipe, List<BlockReference> Floordrain_washing, Polyline device)//新的排水管井
        {
            Point3d center = Point3d.Origin;
            var vertices = device.Vertices();
            double dmax = double.MaxValue;
            int b = CriticalNodeNumber(Floordrain_washing, bboundary);//balcony距离最近点
            int c = 0;
            var vertices_boundary = bboundary.Vertices();
            //rainpipe距离洗衣机最近点
            for (int i = 0; i < vertices_boundary.Count; i++)
            {
                if (dmax > (rainpipe.GetCenter().DistanceTo(vertices_boundary[i])))
                {
                    dmax = (rainpipe.GetCenter().DistanceTo(vertices_boundary[i]));
                    c = i;
                }
            }
            //判断关键边
            Line linespecific;
            if (b > 0 && b < vertices_boundary.Count - 1)
            {
                if (vertices_boundary[b - 1].DistanceTo(vertices_boundary[b]) < vertices_boundary[b + 1].DistanceTo(vertices_boundary[b]))
                {
                    linespecific = new Line(vertices_boundary[b], vertices_boundary[b - 1]);
                }
                else
                {
                    linespecific = new Line(vertices_boundary[b], vertices_boundary[b + 1]);
                }
            }
            else if (b == 0)
            {
                if (vertices_boundary[0].DistanceTo(vertices_boundary[1]) < vertices_boundary[vertices_boundary.Count - 1].DistanceTo(vertices_boundary[0]))
                {
                    linespecific = new Line(vertices_boundary[0], vertices_boundary[1]);
                }
                else
                {
                    linespecific = new Line(vertices_boundary[0], vertices_boundary[vertices_boundary.Count - 1]);
                }
            }
            else
            {
                if (vertices_boundary[b - 1].DistanceTo(vertices_boundary[b]) < vertices_boundary[b].DistanceTo(vertices_boundary[0]))
                {
                    linespecific = new Line(vertices_boundary[b], vertices_boundary[b - 1]);
                }
                else
                {
                    linespecific = new Line(vertices_boundary[0], vertices_boundary[b]);
                }
            }

            var perpendicular_point = device.ToCurve3d().GetClosestPointTo(Floordrain_washing[0].Position).Point;

            if (rainpipe.GetCenter().DistanceTo(Floordrain_washing[0].Position) > ThWPipeCommon.MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE)
            {
                //balcony范围内生成新管井
                var perpendicular_basepoint = linespecific.ToCurve3d().GetClosestPointTo(Floordrain_washing[0].Position).Point;
                if (b == c)
                {
                    center = Floordrain_washing[0].Position + (perpendicular_basepoint.GetVectorTo(vertices_boundary[b]).GetNormal()) * (perpendicular_basepoint.DistanceTo(vertices_boundary[b]) - 100);
                }
                else
                {
                    for (int i = 0; i < vertices_boundary.Count; i++)
                    {
                        if ((i != b) && ((vertices_boundary[b].GetVectorTo(perpendicular_basepoint)).IsCodirectionalTo(perpendicular_basepoint.GetVectorTo(vertices_boundary[i]))))
                        {
                            center = Floordrain_washing[0].Position + (vertices_boundary[i].GetVectorTo(perpendicular_basepoint).GetNormal()) * (-vertices_boundary[i].DistanceTo(Floordrain_washing[0].Position) + 100);//生成内部立管
                            break;
                        }
                    }
                }
            }
            else
            {
                int a = CriticalNodeNumber(Floordrain_washing, device);//device距离洗衣机最近点           
                //设备平台范围内生成新管井
                if (vertices[a].DistanceTo(rainpipe.GetCenter()) < 700)
                {
                    if (a > 0)
                    {
                        double dst = 0.0;
                        var s = Math.Abs(vertices[a].GetVectorTo(vertices[a - 1]).GetAngleTo((Floordrain_washing[0].Position).GetVectorTo(perpendicular_point)));
                        if (s< ThWPipeCommon.MAX_ANGEL_TOLERANCE)
                        {        
                            Line line = new Line(vertices[a], vertices[a - 1]);
                            dst = line.ToCurve3d().GetDistanceTo(rainpipe.GetCenter());
                            center = vertices[a] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (Floordrain_washing[0].Position.GetVectorTo(perpendicular_point).GetNormal()) + dst * (vertices[a].GetVectorTo(vertices[a + 1]).GetNormal());
                        }
                        else
                        {
                            Line line = new Line(vertices[a], vertices[a + 1]);
                            dst = line.ToCurve3d().GetDistanceTo(rainpipe.GetCenter());
                            center = vertices[a] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (Floordrain_washing[0].Position.GetVectorTo(perpendicular_point).GetNormal()) + dst * (vertices[a].GetVectorTo(vertices[a - 1]).GetNormal());
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
                            center = vertices[0] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (Floordrain_washing[0].Position.GetVectorTo(perpendicular_point).GetNormal()) + dst * (vertices[0].GetVectorTo(vertices[vertices.Count - 2]).GetNormal());
                        }
                        else
                        {
                            Line line = new Line(vertices[0], vertices[vertices.Count - 2]);
                            var point = line.ToCurve3d().GetClosestPointTo(rainpipe.GetCenter()).Point;
                            var s = rainpipe.GetCenter();
                            dst = point.DistanceTo(s);
                            center = vertices[0] + ThWPipeCommon.WELL_TO_WALL_OFFSET * (Floordrain_washing[0].Position.GetVectorTo(perpendicular_point).GetNormal()) + dst * (vertices[0].GetVectorTo(vertices[1]).GetNormal());
                        }
                    }
                }
                else
                {
                    var point = bboundary.ToCurve3d().GetClosestPointTo(rainpipe.GetCenter()).Point;
                    center = rainpipe.GetCenter() + ThWPipeCommon.TOILET_WELLS_INTERVAL * rainpipe.GetCenter().GetVectorTo(point).GetNormal();//此处借用管井间隔参数
                }
            }

            return center;
        }
        private static Point3dCollection Line_vertices_rainpipe(Polyline bboundary, Point3d center_rainpipe, BlockReference Floordrain, Polyline device)
        {
            var vertices = new Point3dCollection();
            var pts = new Point3dCollection();
            double dst = double.MaxValue;
            int num = 0;
            for (int i = 0; i < bboundary.Vertices().Count - 1; i++)
            {
                Line boundaryline = new Line(bboundary.Vertices()[i], bboundary.Vertices()[i + 1]);
                Line line_temp = new Line(Floordrain.Position, center_rainpipe);
                var boundarypoints = new Point3dCollection();
                boundaryline.IntersectWith(line_temp, Intersect.ExtendArgument, boundarypoints, (IntPtr)0, (IntPtr)0);
                if (boundarypoints.Count > 0)
                { if (dst > boundarypoints[0].DistanceTo(center_rainpipe))
                    {
                        dst = boundarypoints[0].DistanceTo(center_rainpipe);
                        num = i;
                    }
                }
            }
            Line linespecific = new Line(bboundary.Vertices()[num], bboundary.Vertices()[num + 1]);
            var perpendicular_point = linespecific.ToCurve3d().GetClosestPointTo(center_rainpipe).Point;//雨水管基点
            var dmin = perpendicular_point.DistanceTo(center_rainpipe);
            Line line = new Line(center_rainpipe, perpendicular_point);
            var perpendicular_point1 = linespecific.ToCurve3d().GetClosestPointTo(Floordrain.Position).Point;//地漏基点
            Line line1 = new Line(Floordrain.Position, perpendicular_point1);
            line.Rotation(center_rainpipe, Math.PI / 4);
            line1.IntersectWith(line, Intersect.ExtendBoth, pts, (IntPtr)0, (IntPtr)0);
            var perpendicular_point2 = linespecific.ToCurve3d().GetClosestPointTo(pts[0]).Point;//交点基点

            if (perpendicular_point2.DistanceTo(pts[0]) < dmin)
            {
                if (perpendicular_point2.DistanceTo(pts[0]) >= ThWPipeCommon.MAX_ROOM_INTERVAL)
                {
                    if (pts[0].DistanceTo(center_rainpipe) >= ThWPipeCommon.COMMONRADIUS)
                    {

                        vertices.Add(center_rainpipe + ThWPipeCommon.COMMONRADIUS * (center_rainpipe.GetVectorTo(perpendicular_point).GetNormal()));
                        vertices.Add(perpendicular_point);
                        vertices.Add(Floordrain.Position + (perpendicular_point1.GetVectorTo(perpendicular_point).GetNormal()) * (perpendicular_point.DistanceTo(perpendicular_point1) - perpendicular_point1.DistanceTo(Floordrain.Position)));
                        vertices.Add(Floordrain.Position + ThWPipeCommon.COMMONRADIUS * perpendicular_point1.GetVectorTo(perpendicular_point).GetNormal());
                    }
                    else
                    {
                        var temp1 = linespecific.ToCurve3d().GetClosestPointTo(pts[0]).Point;
                        var tts = new Point3dCollection();
                        Line templine = new Line(pts[0], temp1);
                        Circle circletemp = new Circle() { Center = pts[0], Radius = 50 };
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);
                        if (tts[0].DistanceTo(pts[0]) > tts[1].DistanceTo(pts[0]))
                        {
                            vertices.Add(tts[1]);
                        }
                        else
                        {
                            vertices.Add(tts[0]);
                        }
                        vertices.Add(Floordrain.Position + ThWPipeCommon.COMMONRADIUS * Floordrain.Position.GetVectorTo(perpendicular_point1).GetNormal());
                    }
                }
                else
                {
                    if (perpendicular_point2.DistanceTo(pts[0]) <= (ThWPipeCommon.MAX_ROOM_INTERVAL - ThWPipeCommon.COMMONRADIUS))
                    {
                        vertices.Add(center_rainpipe + ThWPipeCommon.COMMONRADIUS * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(center_rainpipe + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point2.DistanceTo(pts[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal()));
                        vertices.Add(pts[0] + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point2.DistanceTo(pts[0])) * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                        vertices.Add(Floordrain.Position + ThWPipeCommon.COMMONRADIUS * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                    }
                    else
                    {
                        var temp = center_rainpipe + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point2.DistanceTo(pts[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal());
                        var temp1 = pts[0] + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point2.DistanceTo(pts[0])) * perpendicular_point2.GetVectorTo(pts[0]).GetNormal();
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
                        vertices.Add(Floordrain.Position + ThWPipeCommon.COMMONRADIUS * perpendicular_point2.GetVectorTo(pts[0]).GetNormal());
                    }
                }
            }
            else
            {
                var pts1 = new Point3dCollection();
                line.Rotation(center_rainpipe, -Math.PI / 2);
                line1.IntersectWith(line, Intersect.ExtendBoth, pts1, (IntPtr)0, (IntPtr)0);
                var perpendicular_point3 = linespecific.ToCurve3d().GetClosestPointTo(pts1[0]).Point;
                if (perpendicular_point3.DistanceTo(pts1[0]) >= ThWPipeCommon.MAX_ROOM_INTERVAL)
                {
                    if (pts1[0].DistanceTo(center_rainpipe) >= ThWPipeCommon.COMMONRADIUS)
                    {
                        vertices.Add(center_rainpipe + ThWPipeCommon.COMMONRADIUS * (center_rainpipe.GetVectorTo(perpendicular_point).GetNormal()));
                        vertices.Add(perpendicular_point);
                        vertices.Add(Floordrain.Position + (perpendicular_point1.GetVectorTo(perpendicular_point).GetNormal()) * (perpendicular_point.DistanceTo(perpendicular_point1) - perpendicular_point1.DistanceTo(Floordrain.Position)));
                        vertices.Add(Floordrain.Position + ThWPipeCommon.COMMONRADIUS * perpendicular_point1.GetVectorTo(perpendicular_point).GetNormal()); ;
                    }
                    else
                    {
                        var temp1 = linespecific.ToCurve3d().GetClosestPointTo(pts1[0]).Point;
                        var tts = new Point3dCollection();
                        Line templine = new Line(pts1[0], temp1);
                        Circle circletemp = new Circle() { Center = center_rainpipe, Radius = 50 };
                        circletemp.IntersectWith(templine, Intersect.ExtendArgument, tts, (IntPtr)0, (IntPtr)0);
                        if (tts[0].DistanceTo(pts1[0]) > tts[1].DistanceTo(pts1[0]))
                        {
                            vertices.Add(tts[1]);
                        }
                        else
                        {
                            vertices.Add(tts[0]);
                        }
                        vertices.Add(Floordrain.Position + ThWPipeCommon.COMMONRADIUS * Floordrain.Position.GetVectorTo(perpendicular_point1).GetNormal());
                    }
                }
                else
                {
                    if (perpendicular_point3.DistanceTo(pts1[0]) <= (ThWPipeCommon.MAX_ROOM_INTERVAL - ThWPipeCommon.COMMONRADIUS))
                    {
                        vertices.Add(center_rainpipe + ThWPipeCommon.COMMONRADIUS * (center_rainpipe.GetVectorTo(perpendicular_point).GetNormal()));
                        vertices.Add(perpendicular_point);
                        vertices.Add(Floordrain.Position + (perpendicular_point1.GetVectorTo(perpendicular_point1).GetNormal()) * (perpendicular_point.DistanceTo(perpendicular_point1) - perpendicular_point1.DistanceTo(Floordrain.Position)));
                        vertices.Add(Floordrain.Position + ThWPipeCommon.COMMONRADIUS * perpendicular_point1.GetVectorTo(perpendicular_point).GetNormal());
                    }
                    else
                    {
                        var temp = center_rainpipe + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point3.DistanceTo(pts1[0])) * (perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal());
                        var temp1 = pts1[0] + (ThWPipeCommon.MAX_ROOM_INTERVAL - perpendicular_point3.DistanceTo(pts1[0])) * perpendicular_point3.GetVectorTo(pts1[0]).GetNormal();
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
                        vertices.Add(Floordrain.Position + ThWPipeCommon.COMMONRADIUS * perpendicular_point3.GetVectorTo(pts1[0]).GetNormal());
                    }
                }
            }
            return vertices;
        }
        private static Point3dCollection Getvertices(Polyline bboundary, BlockReference washingmachine, Point3d bbasinline, List<BlockReference> Floordrain_washing,Point3d center)
        {
            var vertices = new Point3dCollection();           
            int num = CriticalLineNumber(bboundary, washingmachine);          
            var linespecific = new Line(bboundary.Vertices()[num], bboundary.Vertices()[num + 1]);
            var perpendicular_point = linespecific.ToCurve3d().GetClosestPointTo(Floordrain_washing[0].Position).Point;//洗衣机地漏在洗衣机所在边垂点
            var perpendicular_point1 = linespecific.ToCurve3d().GetClosestPointTo(bbasinline).Point;
            if (bbasinline.Y < center.Y)//向上偏
            {
                vertices.Add(Floordrain_washing[0].Position + (perpendicular_point1.DistanceTo(perpendicular_point) + bbasinline.DistanceTo(perpendicular_point1) - Floordrain_washing[0].Position.DistanceTo(perpendicular_point)) * perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal());
                vertices.Add(bbasinline + (bbasinline).GetVectorTo(vertices[0]).GetNormal() * ThWPipeCommon.COMMONRADIUS);              
            }
            else//向下偏
            {
                vertices.Add(Floordrain_washing[0].Position + (perpendicular_point1.DistanceTo(perpendicular_point) - bbasinline.DistanceTo(perpendicular_point1) + Floordrain_washing[0].Position.DistanceTo(perpendicular_point)) * perpendicular_point.GetVectorTo(perpendicular_point1).GetNormal());
                vertices.Add(bbasinline + (bbasinline).GetVectorTo(vertices[0]).GetNormal() * ThWPipeCommon.COMMONRADIUS);
            }
            return vertices;
        }    
        private static Point3dCollection Line_Addvertices(Polyline bboundary, Point3d center_spout, List<BlockReference> Floordrain_washing, BlockReference washingmachine, Polyline device, Point3d b_floordrain)
        {
            var vertices = new Point3dCollection();                     
            int num = CriticalLineNumber(bboundary, washingmachine);      
            var linespecific = new Line(bboundary.Vertices()[num], bboundary.Vertices()[num + 1]);
            var perpendicular_point = linespecific.ToCurve3d().GetClosestPointTo(center_spout).Point;//排水井在洗衣机所在边垂点                
            var perpendicular_point1 = linespecific.ToCurve3d().GetClosestPointTo(b_floordrain).Point;//洗衣机所在边洗衣机地漏垂点            
            var perpendicular_point3 = device.ToCurve3d().GetClosestPointTo(b_floordrain).Point;//排水井在洗衣机所在边垂点                                  
            vertices.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * Floordrain_washing[0].Position.GetVectorTo(b_floordrain).GetNormal());
            vertices.Add(b_floordrain);
            vertices.Add(perpendicular_point3);
            vertices.Add(perpendicular_point3 + (center_spout.DistanceTo(perpendicular_point) - perpendicular_point1.DistanceTo(perpendicular_point3)) * (perpendicular_point.GetVectorTo(center_spout).GetNormal() + (perpendicular_point1.GetVectorTo(perpendicular_point).GetNormal())));
            vertices.Add(center_spout + ThWPipeCommon.COMMONRADIUS * center_spout.GetVectorTo(vertices[3]).GetNormal());
            return vertices;
        }
        private static Point3dCollection Line_Addvertices1(Polyline bboundary, Point3d center_spout, List<BlockReference> Floordrain_washing, BlockReference washingmachine, Polyline device, Point3d b_floordrain)
        {
            var vertices = new Point3dCollection();        
            int num = CriticalLineNumber(bboundary, washingmachine);          
            var linespecific = new Line(bboundary.Vertices()[num], bboundary.Vertices()[num + 1]);
            var perpendicular_point = linespecific.ToCurve3d().GetClosestPointTo(center_spout).Point;//排水井在洗衣机所在边垂点                
            var perpendicular_point1 = linespecific.ToCurve3d().GetClosestPointTo(b_floordrain).Point;//洗衣机所在边洗衣机地漏垂点            
            var perpendicular_point3 = device.ToCurve3d().GetClosestPointTo(b_floordrain).Point;//排水井在洗衣机所在边垂点                                          
            vertices.Add(b_floordrain+ ThWPipeCommon.COMMONRADIUS * b_floordrain.GetVectorTo(perpendicular_point3).GetNormal());
            vertices.Add(perpendicular_point3);
            vertices.Add(perpendicular_point3 + (center_spout.DistanceTo(perpendicular_point) - perpendicular_point1.DistanceTo(perpendicular_point3)) * (perpendicular_point.GetVectorTo(center_spout).GetNormal() + (perpendicular_point1.GetVectorTo(perpendicular_point).GetNormal())));
            vertices.Add(center_spout + ThWPipeCommon.COMMONRADIUS * center_spout.GetVectorTo(vertices[2]).GetNormal());
            return vertices;
        }

        private static Point3dCollection GetBbasinline_Center(Polyline bboundary,  List<BlockReference> Floordrain_washing, BlockReference washingmachine,Point3d b_floordrain)
        {
            var vertices = new Point3dCollection();           
            int num = CriticalLineNumber(bboundary, washingmachine);         
            var linespecific = new Line(bboundary.Vertices()[num], bboundary.Vertices()[num + 1]);
            var perpendicular_point1 = linespecific.ToCurve3d().GetClosestPointTo(b_floordrain).Point;
            var perpendicular_point2 = linespecific.ToCurve3d().GetClosestPointTo(Floordrain_washing[0].Position).Point;//洗衣机所在边洗衣机地漏垂点
            double dis = 0.0;
            if(b_floordrain.DistanceTo(perpendicular_point1)>100)//'100'为可调型的判断参数
            {
                dis = ThWPipeCommon.MIN_BALCONYBASIN_TO_BALCONY;
            }
            else
            { 
                dis = ThWPipeCommon.MAX_BALCONYBASIN_TO_BALCONY; 
            }
            if (bboundary.Vertices()[num].DistanceTo(washingmachine.Position)> bboundary.Vertices()[num+1].DistanceTo(washingmachine.Position))
            {
                if (b_floordrain.DistanceTo(bboundary.Vertices()[num]) > ThWPipeCommon.MAX_BALCONYBASIN_TO_BALCONY)
                {
                    var center = b_floordrain + dis * perpendicular_point2.GetVectorTo(Floordrain_washing[0].Position).GetNormal() + ThWPipeCommon.MAX_BALCONYBASIN_TO_BALCONY * perpendicular_point2.GetVectorTo(perpendicular_point1).GetNormal();
                    if (GeomUtils.PtInLoop(bboundary, center))
                    {
                        vertices.Add(center);
                    }
                    else
                    {
                        vertices.Add(b_floordrain + dis * perpendicular_point2.GetVectorTo(Floordrain_washing[0].Position).GetNormal() - ThWPipeCommon.MAX_BALCONYBASIN_TO_BALCONY * perpendicular_point2.GetVectorTo(perpendicular_point1).GetNormal());
                    }
                }
                else
                {
                    var center = b_floordrain + dis * perpendicular_point2.GetVectorTo(Floordrain_washing[0].Position).GetNormal() - ThWPipeCommon.MAX_BALCONYBASIN_TO_BALCONY * perpendicular_point2.GetVectorTo(perpendicular_point1).GetNormal();
                    if (GeomUtils.PtInLoop(bboundary, center))
                    {
                        vertices.Add(center);
                    }
                    else
                    { 
                        vertices.Add(b_floordrain + dis * perpendicular_point2.GetVectorTo(Floordrain_washing[0].Position).GetNormal() + ThWPipeCommon.MAX_BALCONYBASIN_TO_BALCONY * perpendicular_point2.GetVectorTo(perpendicular_point1).GetNormal()); 
                    }                       
                }
            }
            else
            {
                if (b_floordrain.DistanceTo(bboundary.Vertices()[num+1]) > ThWPipeCommon.MAX_BALCONYBASIN_TO_BALCONY)
                {
                    var center = b_floordrain + dis * perpendicular_point2.GetVectorTo(Floordrain_washing[0].Position).GetNormal() + ThWPipeCommon.MAX_BALCONYBASIN_TO_BALCONY * perpendicular_point2.GetVectorTo(perpendicular_point1).GetNormal();
                    if (GeomUtils.PtInLoop(bboundary, center))
                    { 
                        vertices.Add(center); 
                    }
                    else
                    { 
                        vertices.Add(b_floordrain + dis * perpendicular_point2.GetVectorTo(Floordrain_washing[0].Position).GetNormal() - ThWPipeCommon.MAX_BALCONYBASIN_TO_BALCONY * perpendicular_point2.GetVectorTo(perpendicular_point1).GetNormal()); 
                    }                      
                }
                else
                {
                    var center = b_floordrain + dis * perpendicular_point2.GetVectorTo(Floordrain_washing[0].Position).GetNormal() - ThWPipeCommon.MAX_BALCONYBASIN_TO_BALCONY * perpendicular_point2.GetVectorTo(perpendicular_point1).GetNormal();
                    if (GeomUtils.PtInLoop(bboundary, center))
                    {
                        vertices.Add(center); 
                    }
                    else
                    { 
                        vertices.Add(b_floordrain + dis * perpendicular_point2.GetVectorTo(Floordrain_washing[0].Position).GetNormal() + ThWPipeCommon.MAX_BALCONYBASIN_TO_BALCONY * perpendicular_point2.GetVectorTo(perpendicular_point1).GetNormal()); 
                    }                       
                }
            }
            return vertices;
        }
        private static int CriticalLineNumber(Polyline bboundary, BlockReference washingmachine)
        {
            double dst = double.MaxValue;
            int num = 0;
            for (int i = 0; i < bboundary.Vertices().Count - 1; i++)
            {
                var boundaryline = new Line(bboundary.Vertices()[i], bboundary.Vertices()[i + 1]);
                var boundarypoint = boundaryline.ToCurve3d().GetClosestPointTo(washingmachine.Position).Point;
                if (dst > boundarypoint.DistanceTo(washingmachine.Position))
                {
                    //洗衣机所在边
                    dst = boundarypoint.DistanceTo(washingmachine.Position);
                    num = i;
                }
            }
            return num;
        }
        private static int CriticalNodeNumber(List<BlockReference> Floordrain_washing, Polyline device)
        {
            var vertices = device.Vertices();
            double dmax = double.MaxValue;
            int a = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (dmax > (Floordrain_washing[0].Position.DistanceTo(vertices[i])))
                {
                    dmax = (Floordrain_washing[0].Position.DistanceTo(vertices[i]));
                    a = i;
                }
            }
            return a;
        }
    }
}
