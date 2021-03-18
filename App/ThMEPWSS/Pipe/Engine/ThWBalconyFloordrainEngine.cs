using System;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Geom;
using ThMEPWSS.Pipe.Service;

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
            BlockReference washMachine = parameters.washingmachine;
            if(parameters.washingmachine==null)
            {
                washMachine = parameters.basinline;
            }
            Polyline rainpipe = GetRainPipe(parameters.boundary, parameters.washingmachine, parameters.rainpipes);
            List<BlockReference> floordrain = Isinside(parameters.floordrains, parameters.boundary);
            int num = 0;
            if (parameters.washingmachine != null)
            {
                num = Washingfloordrain(floordrain, parameters.washingmachine);//确认地漏序号
            }
            if (parameters.washingmachine != null)
            {
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
            }
            else
            {
                Floordrain = floordrain;
            }
            if (parameters.basinline != null)
            {
                if (Floordrain_washing.Count > 0)
                {
                    Bbasinline_Center = GetBbasinline_Center(
                        parameters.boundary,
                        Floordrain_washing,
                        washMachine,
                        parameters.basinline.Position);
                }
                else
                {
                    Bbasinline_Center = GetBbasinline_Center1(washMachine);
                }
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
                if (pipe.GetCenter().DistanceTo(parameters.condensepipe.GetCenter())<1)
                {
                    pipenum = 1;
                    break;
                }
            }
            if (pipenum == 0)
            {
                condensepipe = parameters.condensepipe;
            }
            else if(Floordrain_washing.Count > 0)
            {
                var dis = double.MaxValue;
                foreach (Polyline pipe in parameters.condensepipes)
                {
                    if (pipe.GetCenter().DistanceTo(Floordrain_washing[0].Position) < dis)
                    {
                        dis = pipe.GetCenter().DistanceTo(Floordrain_washing[0].Position);
                        if (GeomUtils.PtInLoop(device_other, pipe.GetCenter()))
                        {
                            condensepipe = pipe;
                        }
                        else
                        {
                            if (GeomUtils.PtInLoop(device, pipe.GetCenter()))
                            {
                                condensepipe = pipe;
                            }

                        }
                    }
                }
            }
           
            if (parameters.downspout == null)
            {
                if(device_other==null)
                {
                    var center = S_downspout(parameters.boundary, Floordrain_washing);
                    Downspout_to_Floordrain.Add(center);
                    Downspout_to_Floordrain.Add(Floordrain_washing[0].Position);
                }
                else if (Floordrain_washing.Count > 0)
                {
                    var center = Point3d.Origin;
                    if (condensepipe != null )
                    {
                        if (rainpipe == null || (rainpipe != null && !GeomUtils.PtInLoop(parameters.boundary, rainpipe.GetCenter())))
                        {
                            center = new_downspout(parameters.boundary, condensepipe, Floordrain_washing, device_other);
                        }
                    }
                    else if(rainpipe == null)
                    {
                        center = new_downspout1(parameters.boundary, Floordrain_washing);
                    }
                    if (rainpipe != null&& center== Point3d.Origin)
                    {
                        if ((rainpipe.GetCenter().DistanceTo(washMachine.Position) < ThWPipeCommon.MAX__RAINPIPE_TO_WASHMACHINE) && GeomUtils.PtInLoop(parameters.boundary, rainpipe.GetCenter()))
                        {
                            center = rainpipe.GetCenter();
                        }
                    }
                    new_circle = new Circle() { Radius = ThTagParametersService.BalconyFpipe, Center = center };
                    if (GeomUtils.PtInLoop(parameters.boundary, center)&& rainpipe!=null)//判断新生管井是否在阳台
                    {
                        if ((rainpipe.GetCenter().DistanceTo(washMachine.Position) < ThWPipeCommon.MAX__RAINPIPE_TO_WASHMACHINE))
                        {
                            foreach (var b_floordrain in Floordrain)
                            {
                                if (Floordrain_washing[0].Position.DistanceTo(b_floordrain.Position) < ThWPipeCommon.MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN)
                                {
                                    if (Floordrain_washing[0].Position.DistanceTo(b_floordrain.Position) > 10)
                                    {
                                        Downspout_to_Floordrain.Add(b_floordrain.Position);
                                        break;
                                    }
                                }
                            }
                            Downspout_to_Floordrain.Add(center);
                            Downspout_to_Floordrain.Add(Floordrain_washing[0].Position);
                        }
                        else
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
                        }
                        if (parameters.basinline != null &&
                            parameters.basinline.Position.DistanceTo(washMachine.Position) < ThWPipeCommon.MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
                        {
                            if (Bbasinline_Center[0].Y > center.Y)
                            {
                                Bbasinline_to_Floordrain.Add(Downspout_to_Floordrain[1]);
                            }
                            foreach (Point3d point in Getvertices(parameters.boundary, washMachine, Bbasinline_Center[0], Floordrain_washing, center))
                            {
                                Bbasinline_to_Floordrain.Add(point);
                            }
                        }
                    }
                    else
                    {
                        foreach (var b_floordrain in Floordrain)
                        {
                            if (Floordrain_washing[0].Position.DistanceTo(b_floordrain.Position) < ThWPipeCommon.MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN && !(GeomUtils.PtInLoop(parameters.boundary, center)))
                            {
                                Downspout_to_Floordrain = Line_Addvertices(parameters.boundary, center, Floordrain_washing, washMachine, device_other, b_floordrain.Position);
                                break;
                            }
                        }
                        if (Downspout_to_Floordrain.Count == 0)
                        {
                            if (center.DistanceTo(Floordrain_washing[0].Position) > ThWPipeCommon.MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN)
                            {
                                Downspout_to_Floordrain = Line_vertices1(parameters.boundary, center, Floordrain_washing, washMachine, device_other, Floordrain);
                            }
                            else
                            {
                                Downspout_to_Floordrain = Line_vertices(parameters.boundary, center, Floordrain_washing, washMachine, device_other);
                            }
                        }
                        if (parameters.basinline != null &&
                            parameters.basinline.Position.DistanceTo(washMachine.Position) < ThWPipeCommon.MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
                        {
                            if (GeomUtils.PtInLoop(parameters.boundary, center))
                            {
                                if (Bbasinline_Center[0].Y > center.Y)
                                {
                                    Bbasinline_to_Floordrain.Add(Downspout_to_Floordrain[1]);
                                }
                                foreach (Point3d point in Getvertices(parameters.boundary, washMachine, Bbasinline_Center[0], Floordrain_washing, center))
                                {
                                    Bbasinline_to_Floordrain.Add(point);
                                }
                            }
                            else
                            {
                                Bbasinline_to_Floordrain = Line_Addvertices1(parameters.boundary, center, Floordrain_washing, washMachine, device_other, Bbasinline_Center[0]);
                            }
                        }             
                    }
                }
            }
            else if(Floordrain_washing.Count > 0)
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
                        Downspout_to_Floordrain = Line_vertices(parameters.boundary, center, Floordrain_washing, washMachine, device_other);

                    }
                    else
                    {
                        if (condensepipe == null)
                        {
                            foreach (var pipe in parameters.condensepipes)
                            {
                                if (pipe.GetCenter().DistanceTo(washMachine.Position) < ThWPipeCommon.MAX_CONDENSEPIPE_TO_WASHMACHINE)
                                {
                                    condensepipe = pipe;
                                    break;
                                }
                            }
                            var new_center = new_downspout(parameters.boundary, condensepipe, Floordrain_washing, device_other);
                        }
                        new_circle = new Circle() { Radius = 50, Center = center };
                        Downspout_to_Floordrain = Line_vertices(parameters.boundary, center, Floordrain_washing, washMachine, device_other);
                    }
                }
            }
            if(Floordrain_washing.Count==0 && Floordrain.Count==1&& rainpipe== null&& parameters.downspout == null)
            {
                foreach(var pipe in parameters.condensepipes)
                {
                    if(pipe.GetCenter().DistanceTo(Floordrain[0].Position)<1500)
                    {
                        Rainpipe_to_Floordrain = GetRainPipe1(parameters.boundary,parameters.device,pipe, Floordrain[0]);
                        break;
                    }
                }
            }
            if (rainpipe != null&& rainpipe.GetCenter().DistanceTo(washMachine.Position)> ThWPipeCommon.MAX__RAINPIPE_TO_WASHMACHINE)
            {
                if (GeomUtils.PtInLoop(parameters.boundary, rainpipe.GetCenter()))
                {
                    Rainpipe_to_Floordrain.Add((rainpipe.GetCenter() - ThWPipeCommon.COMMONRADIUS * ((Floordrain[0].Position).GetVectorTo(rainpipe.GetCenter()).GetNormal())));
                    Rainpipe_to_Floordrain.Add((Floordrain[0].Position + ThWPipeCommon.COMMONRADIUS * ((Floordrain[0].Position).GetVectorTo(rainpipe.GetCenter()).GetNormal())));
                    if(Bbasinline_Center.Count>0)
                    {
                        Rainpipe_to_Floordrain.Add(Bbasinline_Center[0] - ThWPipeCommon.COMMONRADIUS * ((Floordrain[0].Position).GetVectorTo(Bbasinline_Center[0]).GetNormal()));
                    }
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
        private static Point3dCollection GetRainPipe1(Polyline boundary,Polyline device,Polyline pipe,BlockReference floordrain)
        {
            var result = new Point3dCollection();
            var point = boundary.ToCurve3d().GetClosestPointTo(pipe.GetCenter()).Point;
            var dis = Math.Abs(point.X - floordrain.Position.X);
            result.Add(pipe.GetCenter() + ThWPipeCommon.COMMONRADIUS * pipe.GetCenter().GetVectorTo(point).GetNormal());
            result.Add(point);
            if (floordrain.Position.Y< point.Y)
            {
                result.Add(new Point3d(floordrain.Position.X, point.Y-dis,0));
                result.Add(new Point3d(floordrain.Position.X, floordrain.Position.Y + ThWPipeCommon.COMMONRADIUS, 0));
            }
            else
            {
                result.Add(new Point3d(floordrain.Position.X, point.Y + dis, 0));
                result.Add(new Point3d(floordrain.Position.X, floordrain.Position.Y - ThWPipeCommon.COMMONRADIUS, 0));
            }
            return result;
        }

        private static Point3d S_downspout(Polyline boundary, List<BlockReference> Floordrain_washing)
        {
            double dst = double.MaxValue;
            var vertices = boundary.Vertices();
            int num = 0;
            for (int i=0;i< vertices.Count;i++)
            {
                if(Floordrain_washing[0].Position.DistanceTo(vertices[i])< dst)
                {
                    dst = Floordrain_washing[0].Position.DistanceTo(vertices[i]);
                    num = i;
                }
            }
           if(vertices[num].Y> Floordrain_washing[0].Position.Y)
            {
                return new Point3d(Floordrain_washing[0].Position.X, vertices[num].Y - 100, 0);
            }
            else
            {
                return new Point3d(Floordrain_washing[0].Position.X, vertices[num].Y +100, 0);
            }
        }
        private static Polyline GetRainPipe(Polyline bboundary, BlockReference washingmachine, List<Polyline> rainpipes)
        {   
            if (rainpipes.Count > 0)
            {
                foreach (var pipe in rainpipes)
                {
                    if (washingmachine != null)
                    {
                        if(GeomUtils.PtInLoop(bboundary, pipe.GetCenter()))
                        {
                            if (pipe.GetCenter().DistanceTo(washingmachine.Position) < ThWPipeCommon.MAX__RAINPIPE_TO_WASHMACHINE)
                            {
                                return pipe;
                            }
                           else if (pipe.GetCenter().Y < washingmachine.Position.Y)
                            {
                                return pipe;
                            }                         
                        }
                        
                    }
                    else
                    {
                        return pipe;
                    }
                }
                foreach (var pipe in rainpipes)
                {
                    if (washingmachine != null)
                    {
                        if (pipe.GetCenter().DistanceTo(washingmachine.Position) > ThWPipeCommon.MAX_RAINPIPE_TO_BALCONYFLOORDRAIN)//洗衣机地漏与洗衣机接近，此处借用参数
                        {
                            return pipe;
                        }
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
                    int num = 0;
                    for (int j=i;j< bfloordrain.Count;j++)
                    {
                        if(bfloordrain[i].Position.DistanceTo(bfloordrain[j].Position) <1&&i!=j)
                        {
                            num++;
                        }
                    }
                    if (num == 0)
                    {
                        floordrain.Add(bfloordrain[i]);
                    }
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
        private static Point3dCollection Line_vertices1(Polyline bboundary, Point3d center_spout, List<BlockReference> Floordrain_washing, BlockReference washingmachine, Polyline device, List<BlockReference> floordrains)
        {
            var points = new Point3dCollection();            
            Point3d point = new Point3d(center_spout.X, Floordrain_washing[0].Position.Y, 0);
            Point3d point1 = bboundary.ToCurve3d().GetClosestPointTo(point).Point;
            Point3d point2 = device.ToCurve3d().GetClosestPointTo(point).Point;
            points.Add(center_spout + ThWPipeCommon.COMMONRADIUS * center_spout.GetVectorTo(point).GetNormal());
            if (center_spout.DistanceTo(Floordrain_washing[0].Position) < 1000)
            {             
                points.Add(point - point.DistanceTo(point1) * center_spout.GetVectorTo(point).GetNormal());
                points.Add(point - point.DistanceTo(point1) * Floordrain_washing[0].Position.GetVectorTo(point).GetNormal());
                points.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * Floordrain_washing[0].Position.GetVectorTo(point).GetNormal());
            }
            else
            {
                
                var tpoint = point - point.DistanceTo(point2) * center_spout.GetVectorTo(point).GetNormal();
                if ((center_spout.Y- tpoint.Y)*(Floordrain_washing[0].Position.Y- tpoint.Y) >0&& floordrains.Count==1)
                {
                    points.Add(floordrains[0].Position);
                    points.Add(point + ThWPipeCommon.COMMONRADIUS * point.GetVectorTo(center_spout).GetNormal());
                }
                else
                {
                    points.Add(point - point.DistanceTo(point2) * center_spout.GetVectorTo(point).GetNormal());
                    points.Add(point - point.DistanceTo(point2) * Floordrain_washing[0].Position.GetVectorTo(point).GetNormal());   
                                    points.Add(Floordrain_washing[0].Position + ThWPipeCommon.COMMONRADIUS * Floordrain_washing[0].Position.GetVectorTo(point).GetNormal());
                }
                
            }
            return points;
        }
        private static Point3d new_downspout1(Polyline bboundary, List<BlockReference> Floordrain_washing)
        {
            var vertices_boundary = bboundary.Vertices();
            int b = CriticalNodeNumber(Floordrain_washing, bboundary);//balcony距离最近点
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
                    linespecific = new Line(vertices_boundary[b], vertices_boundary[0]);
                }
            }
            var perpendicular_point = linespecific.ToCurve3d().GetClosestPointTo(Floordrain_washing[0].Position).Point;
            return linespecific.EndPoint + 2*ThWPipeCommon.COMMONRADIUS * (perpendicular_point.GetVectorTo(Floordrain_washing[0].Position).GetNormal()+linespecific.EndPoint.GetVectorTo(linespecific.StartPoint).GetNormal());
        }
        private static Point3d new_downspout(Polyline bboundary, Polyline rainpipe, List<BlockReference> Floordrain_washing, Polyline device)//新的排水管井
        {
            Point3d center = Point3d.Origin;
            double dmax = double.MaxValue;
            int b = CriticalNodeNumber(Floordrain_washing, bboundary);//balcony距离最近点
            int c = 0;
            var vertices = device.Vertices();
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
                    linespecific = new Line(vertices_boundary[b], vertices_boundary[0]);
                }
            }

            var perpendicular_point = device.ToCurve3d().GetClosestPointTo(Floordrain_washing[0].Position).Point;

            if (rainpipe.GetCenter().DistanceTo(Floordrain_washing[0].Position) > ThWPipeCommon.MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE)
            {
                //balcony范围内生成新管井
                var perpendicular_basepoint = linespecific.ToCurve3d().GetClosestPointTo(Floordrain_washing[0].Position).Point;
                if (perpendicular_basepoint.DistanceTo(linespecific.EndPoint) > 1)
                {
                    center = Floordrain_washing[0].Position + (perpendicular_basepoint.GetVectorTo(linespecific.EndPoint).GetNormal()) * (perpendicular_basepoint.DistanceTo(linespecific.EndPoint) - 100);
                }
                else
                {
                    if (Floordrain_washing[0].Position.X < perpendicular_basepoint.X)
                    {
                        center = new Point3d(perpendicular_basepoint.X-2* ThWPipeCommon.COMMONRADIUS, Floordrain_washing[0].Position.Y,0);
                    }
                    else
                    {
                        center = new Point3d(perpendicular_basepoint.X+2* ThWPipeCommon.COMMONRADIUS, Floordrain_washing[0].Position.Y, 0);
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
        private static Point3dCollection GetBbasinline_Center1( BlockReference baseLine)
        {
            Point3dCollection baseCenters = new Point3dCollection();
            var s = new DBObjectCollection();
            baseLine.Explode(s);
            Point3dCollection points = new Point3dCollection();
            foreach (var s1 in s)
            {
                if (s1.GetType().Name.Contains("Circle"))
                {
                    Circle baseCircle = s1 as Circle;
                    points.Add(baseCircle.Center);
                }
            }
            baseCenters.Add(new Point3d((points[0].X+ points[1].X+ points[2].X)/3, (points[0].Y+ points[1].Y+ points[2].Y)/3, 0));
            return baseCenters;
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
