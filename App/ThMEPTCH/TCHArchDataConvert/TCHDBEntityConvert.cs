using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPTCH.Model;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.TCHArchDataConvert
{
    class TCHDBEntityConvert
    {
        private double openingThickinessAdd = 120;
        ThCADCoreNTSSpatialIndex spatialIndex;
        Dictionary<MPolygon, ThTCHWall> wallDic = new Dictionary<MPolygon, ThTCHWall>();
        Dictionary<MPolygon, WallEntity> wallEntityDic = new Dictionary<MPolygon, WallEntity>();
        Dictionary<Curve, WallEntity> wallCurveDic = new Dictionary<Curve, WallEntity>();
        public TCHDBEntityConvert() 
        {
        
        }
        public List<ThTCHWall> WallDoorWindowRelation(List<TArchWall> walls, List<TArchDoor> doors,List<TArchWindow> windows) 
        {
            wallDic = new Dictionary<MPolygon, ThTCHWall>();
            wallEntityDic = new Dictionary<MPolygon, WallEntity>();
            wallCurveDic = new Dictionary<Curve, WallEntity>();
            var addDBColl = new DBObjectCollection();
            foreach (var item in walls)
            {
                var entity = TArchWallToEntityWall(item,0,0,0,0);
                wallEntityDic.Add(entity.OutLine, entity);
                if (entity.WallCenterCurve != null &&(entity.WallCenterCurve is Line || entity.WallCenterCurve is Arc))
                    wallCurveDic.Add(entity.WallCenterCurve,entity);
                addDBColl.Add(entity.OutLine);
            }
            var wallCurves = wallCurveDic.Select(c => c.Key).ToList();
            foreach (var keyValue in wallEntityDic) 
            {
                var entity = keyValue.Value;
                double spLeftOffSet = 0.0;
                double epLeftOffSet = 0.0;
                double spRightOffSet = 0.0;
                double epRightOffSet = 0.0;
                
                if (null != entity.WallCenterCurve) 
                {
                    if (entity.WallCenterCurve is Line line)
                        spLeftOffSet = GetWallPointOffSet(line, wallCurves, out epLeftOffSet, out spRightOffSet,out epRightOffSet);
                    else if(entity.WallCenterCurve is Arc arc)
                        spLeftOffSet = GetWallPointOffSet(arc, wallCurves, out epLeftOffSet, out spRightOffSet, out epRightOffSet);
                }
                if (Math.Abs(spLeftOffSet)>0.001 || Math.Abs(epLeftOffSet)>0.001 || Math.Abs(spRightOffSet)>0.001 || Math.Abs(epRightOffSet)>0.001)
                {
                    var dbWall = walls.Find(c => c.Id == entity.DBId);
                    var tempEntity = TArchWallToEntityWall(dbWall, spLeftOffSet, epLeftOffSet, spRightOffSet, epRightOffSet);
                    wallDic.Add(entity.OutLine, WallEntityToTCHWall(tempEntity));
                }
                else 
                {
                    wallDic.Add(entity.OutLine, WallEntityToTCHWall(entity));
                }
            }

            spatialIndex = new ThCADCoreNTSSpatialIndex(addDBColl);

            var doorDic = new Dictionary<MPolygon, ThTCHDoor>();
            var doorEntityDic = new Dictionary<MPolygon, DoorEntity>();
            foreach (var item in doors)
            {
                var entity = TArchDoorToEntityDoor(item);
                doorDic.Add(entity.OutLine, DoorEntityToTCHDoor(entity));
                doorEntityDic.Add(entity.OutLine, entity);
            }

            var windowDic = new Dictionary<MPolygon, ThTCHWindow>();
            var windowEntityDic = new Dictionary<MPolygon, WindowEntity>();
            foreach (var item in windows)
            {
                var entity = TArchWindowToEntityWindow(item);
                windowDic.Add(entity.OutLine, WindowEntityToTCHWindow(entity));
                windowEntityDic.Add(entity.OutLine, entity);
            }

            foreach (var item in doorDic) 
            {
                var crossPLines = spatialIndex.SelectCrossingPolygon(item.Key).Cast<MPolygon>().ToList();
                var doorEntity = doorEntityDic[item.Key];
                foreach (var outLine in crossPLines) 
                {
                    var wall = wallDic[outLine];
                    wall.Doors.Add(item.Value);
                    wall.Openings.Add(WallDoorOpening(wallEntityDic[outLine], doorEntity));
                }
            }
            foreach (var item in windowDic)
            {
                var crossPLines = spatialIndex.SelectCrossingPolygon(item.Key).Cast<MPolygon>().ToList();
                var windowEntity = windowEntityDic[item.Key];
                foreach (var outLine in crossPLines)
                {
                    var wall = wallDic[outLine];
                    wall.Windows.Add(item.Value);
                    wall.Openings.Add(WallWindowOpening(wallEntityDic[outLine], windowEntity));
                }
            }
            return wallDic.Select(c=>c.Value).ToList();
        }

        double GetWallPointOffSet(Line wallLine, List<Curve> otherWallCurves, out double epLeftOffSet, out double spRightOffSet, out double epRightOffSet) 
        {
            double spLeftOffSet = 0.0;
            epLeftOffSet = 0.0;
            spRightOffSet = 0.0;
            epRightOffSet = 0.0;
            var sp = wallLine.StartPoint;
            var ep = wallLine.EndPoint;
            var thisDir = wallLine.LineDirection();
            double tolerance = 1;
            var otherCurves = otherWallCurves.Where(c => c != wallLine).ToList();
            var spCurves = PointGetCurves(sp, otherCurves, tolerance);
            spLeftOffSet = LeftRightPointOffSet(wallLine, true, thisDir, spCurves, tolerance, out spRightOffSet);
            var epCurves = PointGetCurves(ep, otherCurves, tolerance);
            epLeftOffSet = LeftRightPointOffSet(wallLine, false, thisDir.Negate(), epCurves, tolerance, out epRightOffSet);
            return spLeftOffSet;
        }
        double GetWallPointOffSet(Arc wallArc, List<Curve> otherWallCurves, out double epLeftOffSet,out double spRightOffSet,out double epRightOffSet)
        {
            double spLeftOffSet = 0.0;
            epLeftOffSet = 0.0;
            spRightOffSet = 0.0;
            epRightOffSet = 0.0;
            var sp = wallArc.StartPoint;
            var ep = wallArc.EndPoint;
            var center = wallArc.Center;
            var spDir = Vector3d.ZAxis.CrossProduct((sp - center).GetNormal());
            var epDir = Vector3d.ZAxis.CrossProduct((ep - center).GetNormal()).Negate();
            double tolerance = 1;
            var otherCurves = otherWallCurves.Where(c => c != wallArc).ToList();
            var spCurves = PointGetCurves(sp, otherCurves, tolerance);
            spLeftOffSet = LeftRightPointOffSet(wallArc,true, spDir, spCurves, tolerance,out spRightOffSet);
            var epCurves = PointGetCurves(ep, otherCurves, tolerance);
            epLeftOffSet = LeftRightPointOffSet(wallArc,false, epDir, epCurves, tolerance, out epRightOffSet);
            return spLeftOffSet;
        }
        double LeftRightPointOffSet(Curve thisCurve,bool isSp, Vector3d dir, Dictionary<Curve,Point3d> pointCurves, double tolerance,out double rightOffSet) 
        {
            double leftOffSet = 0.0;
            rightOffSet = 0.0;
            if (pointCurves == null || pointCurves.Count < 1)
                return leftOffSet;
            //判断交接类型，线和线交接，线和弧线交接
            //线和线交接，一字型，L型（90°和非90°），T型，Y型，十字型暂时只处理 L型
            //线和弧线交接，切线方向平行，非平行
            if (pointCurves.Count != 1)
                return leftOffSet;
            Curve curve = pointCurves.First().Key;
            var nearPoint = pointCurves.First().Value;
            var curveSp = curve.StartPoint;
            var curveEp = curve.EndPoint;
            if (nearPoint.DistanceTo(curveSp) > tolerance && nearPoint.DistanceTo(curveEp) > tolerance)
            {
                //T型，不处理
                return leftOffSet;
            }
            Vector3d otherDir;
            if (curve is Arc arc)
            {
                //计算切线方向
                if (nearPoint.DistanceTo(curveSp) < tolerance)
                {
                    var spDir = (curveSp - arc.Center).GetNormal();
                    otherDir = Vector3d.ZAxis.CrossProduct(spDir);
                }
                else 
                {
                    var epDir = (curveEp - arc.Center).GetNormal();
                    otherDir = Vector3d.ZAxis.CrossProduct(epDir).Negate();
                }
            }
            else
            {
                
                var line = curve as Line;
                otherDir = line.LineDirection();
                if (nearPoint.DistanceTo(curveEp) < tolerance)
                    otherDir = otherDir.Negate();
            }
            var angle = dir.GetAngleTo(otherDir);
            if (Math.Abs(angle - Math.PI) < 0.001 || Math.Abs(angle)<0.001)
            {
                //一字，不处理
                return leftOffSet;
            }
            var point = isSp ? thisCurve.StartPoint : thisCurve.EndPoint;
            var left = Vector3d.ZAxis.CrossProduct(dir);
            var thisEntity = wallCurveDic[thisCurve];
            var otherEntity = wallCurveDic[curve];
            var thisLeftCurve = GetLeftOrRightCurve(point, thisEntity, left);
            var thisRightCurve = GetLeftOrRightCurve(point, thisEntity, left.Negate());
            var otherLeftDir = otherDir.CrossProduct(Vector3d.ZAxis);
            var otherLeftCurve = GetLeftOrRightCurve(point, otherEntity, otherLeftDir);
            var otherRightCurve = GetLeftOrRightCurve(point, otherEntity, otherLeftDir.Negate());
            var leftInsPoint = thisLeftCurve.Intersect(otherLeftCurve, Intersect.ExtendBoth).OrderBy(c=>c.DistanceTo(point)).FirstOrDefault();
            var rightInsPoint = thisRightCurve.Intersect(otherRightCurve, Intersect.ExtendBoth).OrderBy(c => c.DistanceTo(point)).FirstOrDefault();
            var leftPt = isSp ? thisLeftCurve.StartPoint: thisLeftCurve.EndPoint;
            var rightPt = isSp ? thisRightCurve.StartPoint : thisRightCurve.EndPoint;
            if (thisCurve is Line)
            {
                if (thisLeftCurve == thisEntity.WallLeftCurve)
                {
                    leftOffSet = dir.Negate().DotProduct(leftInsPoint - leftPt);
                    rightOffSet = dir.Negate().DotProduct(rightInsPoint - rightPt);
                }
                else 
                {
                    rightOffSet = dir.Negate().DotProduct(leftInsPoint - leftPt);
                    leftOffSet = dir.Negate().DotProduct(rightInsPoint - rightPt);
                }
            }
            else 
            {
                var leftArc = thisEntity.WallLeftCurve as Arc;
                var rightArc = thisEntity.WallRightCurve as Arc;
                var leftAngle = (leftPt - leftArc.Center).GetNormal().GetAngleTo((leftInsPoint - leftArc.Center).GetNormal());
                var rightAngle = (rightPt - rightArc.Center).GetNormal().GetAngleTo((rightInsPoint - rightArc.Center).GetNormal());
                if (otherLeftCurve == otherEntity.WallLeftCurve)
                {
                    if (dir.DotProduct(leftInsPoint - leftPt) > 0)
                    {
                        leftOffSet = -leftArc.Radius * leftAngle;
                    }
                    else
                    {
                        leftOffSet = leftArc.Radius * leftAngle;
                    }
                    if (dir.DotProduct(rightInsPoint - rightPt) > 0)
                    {
                        rightOffSet = -rightArc.Radius * rightAngle;
                    }
                    else
                    {
                        rightOffSet = rightArc.Radius * rightAngle;
                    }
                }
                else 
                {
                    if (dir.DotProduct(leftInsPoint - leftPt) > 0)
                    {
                        rightOffSet = -leftArc.Radius * leftAngle;
                    }
                    else
                    {
                        rightOffSet = leftArc.Radius * leftAngle;
                    }
                    if (dir.DotProduct(rightInsPoint - rightPt) > 0)
                    {
                        leftOffSet = -rightArc.Radius * rightAngle;
                    }
                    else
                    {
                        leftOffSet = rightArc.Radius * rightAngle;
                    }
                }
                if (isSp)
                {
                    var temp = rightOffSet;
                    rightOffSet = leftOffSet;
                    leftOffSet = temp;
                }
            }
            return leftOffSet;
        }
        Curve GetLeftOrRightCurve(Point3d point, WallEntity entity, Vector3d dir) 
        {
            var leftSp = entity.WallLeftCurve.StartPoint;
            var leftEp = entity.WallLeftCurve.EndPoint;
            var nearPt = point.DistanceTo(leftSp) < point.DistanceTo(leftEp) ? leftSp : leftEp;
            if ((nearPt - point).DotProduct(dir) < -0.0001)
                return entity.WallRightCurve;
            return entity.WallLeftCurve;
        }
        Dictionary<Curve,Point3d> PointGetCurves(Point3d point,List<Curve> otherCurves,double tolerance =1) 
        {
            var resCurves = new Dictionary<Curve,Point3d>();
            foreach (var curve in otherCurves) 
            {
                var nearPoint = curve.GetClosestPointTo(point, false);
                if (nearPoint.DistanceTo(point) < tolerance)
                    resCurves.Add(curve,nearPoint);
            }
            return resCurves;
        }
        ThTCHWall WallEntityToTCHWall(WallEntity entity) 
        {
            var newWall = new ThTCHWall(entity.OutLine.Shell(),entity.WallHeight);
            return newWall;
        }
        ThTCHDoor DoorEntityToTCHDoor(DoorEntity entity)
        {
            var newDoor = new ThTCHDoor(entity.MidPoint, entity.Width,entity.Height,entity.Thickness,entity.Angle);
            return newDoor;
        }
        ThTCHWindow WindowEntityToTCHWindow(WindowEntity entity)
        {
            var newWindow = new ThTCHWindow(entity.MidPoint, entity.Width, entity.Height, entity.Thickness, entity.Angle);
            return newWindow;
        }
        WallEntity TArchWallToEntityWall(TArchWall arch,double leftSpOffSet,double leftEpOffSet,double rightSpOffSet,double rightEpOffSet) 
        {
            WallEntity wallEntity = new WallEntity(arch.Id);
            wallEntity.StartPoint = new Point3d(arch.StartPointX, arch.StartPointY, arch.StartPointZ);
            wallEntity.EndPoint = new Point3d(arch.EndPointX, arch.EndPointY, arch.EndPointZ);
            wallEntity.LeftWidth = arch.LeftWidth;
            wallEntity.RightWidth = arch.RightWidth;
            wallEntity.WallHeight = arch.Height;
            wallEntity.Elevtion = arch.Elevtion;
            if (arch.IsArc)
            {
                var sp = wallEntity.StartPoint;
                var ep = wallEntity.EndPoint;
                var leftWidth = arch.LeftWidth;
                var rightWidth = arch.RightWidth;
                var angle = arch.Bulge;
                var xAxis = (ep - sp).GetNormal();
                var yAxis = Vector3d.ZAxis.CrossProduct(xAxis);
                var length = sp.DistanceTo(ep);
                var centerPt = sp + xAxis.MultiplyBy(length / 2);
                var radius = length / (2 * Math.Sin(angle / 2));
                var moveDis = (length / 2) / Math.Tan(angle / 2);
                var arcCenter = centerPt + yAxis.MultiplyBy(moveDis);
                var sDir = (sp - arcCenter).GetNormal();
                var eDir = (ep - arcCenter).GetNormal();
                var innerArcSp = arcCenter + sDir.MultiplyBy(radius - leftWidth);
                var innerArcEp = arcCenter + eDir.MultiplyBy(radius - leftWidth);
                var outArcSp = arcCenter + sDir.MultiplyBy(radius + rightWidth);
                var outArcEp = arcCenter + eDir.MultiplyBy(radius + rightWidth);
                var sAngle = Vector3d.XAxis.GetAngleTo(sDir, Vector3d.ZAxis);
                var innerRadius = radius - leftWidth;
                var outRadius = radius + rightWidth;
                var innerSpOffSet = leftSpOffSet / innerRadius;
                var innerEpOffSet = leftEpOffSet / innerRadius;
                var outSpOffSet = rightSpOffSet / outRadius;
                var outEpOffSet = rightEpOffSet / outRadius;
                var eAngle = sAngle + angle;
                var innerArc = new Arc(arcCenter, Vector3d.ZAxis, innerRadius, sAngle - innerSpOffSet, eAngle + innerEpOffSet);
                var outArc = new Arc(arcCenter, Vector3d.ZAxis, outRadius, sAngle - outSpOffSet, eAngle + outEpOffSet);
                wallEntity.WallCenterCurve = new Arc(arcCenter, Vector3d.ZAxis, radius, sAngle, eAngle);
                wallEntity.WallLeftCurve = innerArc;
                wallEntity.WallRightCurve = outArc;
                innerArcSp = innerArc.StartPoint;
                innerArcEp = innerArc.EndPoint;
                outArcSp = outArc.StartPoint;
                outArcEp = outArc.EndPoint;
                var segments = new PolylineSegmentCollection();
                segments.Add(new PolylineSegment(outArcSp.ToPoint2D(), innerArcSp.ToPoint2D()));
                if (innerArcSp.DistanceTo(innerArc.StartPoint) < 1)
                {
                    segments.Add(new PolylineSegment(innerArc.StartPoint.ToPoint2D(), innerArc.EndPoint.ToPoint2D(), innerArc.BulgeFromCurve(innerArc.IsClockWise())));
                }
                else
                {
                    segments.Add(new PolylineSegment(innerArc.EndPoint.ToPoint2D(), innerArc.StartPoint.ToPoint2D(), -innerArc.BulgeFromCurve(innerArc.IsClockWise())));
                }
                segments.Add(new PolylineSegment(innerArcEp.ToPoint2D(), outArcEp.ToPoint2D()));
                if (outArcEp.DistanceTo(outArc.EndPoint) < 1)
                {
                    segments.Add(new PolylineSegment(outArc.EndPoint.ToPoint2D(), outArc.StartPoint.ToPoint2D(), -outArc.BulgeFromCurve(outArc.IsClockWise())));
                }
                else
                {
                    segments.Add(new PolylineSegment(outArc.StartPoint.ToPoint2D(), outArc.EndPoint.ToPoint2D(), outArc.BulgeFromCurve(outArc.IsClockWise())));
                }
                var temp = segments.Join(new Tolerance(2, 2));
                var newPLine = temp.First().ToPolyline();
                wallEntity.OutLine = ThMPolygonTool.CreateMPolygon(newPLine, new List<Curve> { });
            }
            else 
            {
                wallEntity.WallCenterCurve = new Line(wallEntity.StartPoint, wallEntity.EndPoint);
                var wallDir = (wallEntity.EndPoint - wallEntity.StartPoint).GetNormal();
                var normal = Vector3d.ZAxis;
                var sp = wallEntity.StartPoint ;
                var ep = wallEntity.EndPoint;
                var leftDir = normal.CrossProduct(wallDir);
                var spLeft = sp + leftDir.MultiplyBy(arch.LeftWidth) - wallDir.MultiplyBy(leftSpOffSet);
                var spRight = sp - leftDir.MultiplyBy(arch.RightWidth) - wallDir.MultiplyBy(rightSpOffSet);
                var epLeft = ep + leftDir.MultiplyBy(arch.LeftWidth) + wallDir.MultiplyBy(leftEpOffSet);
                var epRight = ep - leftDir.MultiplyBy(arch.RightWidth) + wallDir.MultiplyBy(rightEpOffSet);
                wallEntity.WallLeftCurve = new Line(spLeft, epLeft);
                wallEntity.WallRightCurve = new Line(spRight, epRight);
                Polyline outPLine = new Polyline();
                outPLine.Elevation = wallEntity.StartPoint.Z;
                outPLine.AddVertexAt(0, spLeft.ToPoint2D(), 0, 0, 0);
                outPLine.AddVertexAt(1, spRight.ToPoint2D(), 0, 0, 0);
                outPLine.AddVertexAt(2, epRight.ToPoint2D(), 0, 0, 0);
                outPLine.AddVertexAt(3, epLeft.ToPoint2D(), 0, 0, 0);
                outPLine.Closed = true;
                wallEntity.OutLine = ThMPolygonTool.CreateMPolygon(outPLine, new List<Curve> { });
            }
            return wallEntity;
        }
        DoorEntity TArchDoorToEntityDoor(TArchDoor arch)
        {
            DoorEntity entity = new DoorEntity();
            entity.MidPoint = new Point3d(arch.BasePointX, arch.BasePointY, arch.BasePointZ);
            entity.TextPoint = new Point3d(arch.TextPointX, arch.TextPointY, arch.TextPointZ);
            var normal = Vector3d.ZAxis;
            var leftDir = (entity.TextPoint-entity.MidPoint).GetNormal();
            var dir = normal.CrossProduct(leftDir);
            entity.Angle = Vector3d.XAxis.GetAngleTo(dir, normal);
            var sp = entity.MidPoint - dir.MultiplyBy(arch.Width / 2);
            var ep = entity.MidPoint + dir.MultiplyBy(arch.Width / 2);
            var spLeft = sp + leftDir.MultiplyBy(arch.Thickness/2);
            var spRight = sp - leftDir.MultiplyBy(arch.Thickness / 2);
            var epLeft = ep + leftDir.MultiplyBy(arch.Thickness/2);
            var epRight = ep - leftDir.MultiplyBy(arch.Thickness / 2);
            Polyline outPLine = new Polyline();
            outPLine.AddVertexAt(0, spLeft.ToPoint2D(), 0, 0, 0);
            outPLine.AddVertexAt(1, spRight.ToPoint2D(), 0, 0, 0);
            outPLine.AddVertexAt(2, epRight.ToPoint2D(), 0, 0, 0);
            outPLine.AddVertexAt(3, epLeft.ToPoint2D(), 0, 0, 0);
            outPLine.Closed = true;
            entity.OutLine = ThMPolygonTool.CreateMPolygon(outPLine, new List<Curve> { });
            entity.Width = arch.Width;
            entity.Height = arch.Height;
            entity.Thickness = arch.Thickness;
            return entity;
        }
        WindowEntity TArchWindowToEntityWindow(TArchWindow arch)
        {
            var entity = new WindowEntity();
            entity.MidPoint = new Point3d(arch.BasePointX, arch.BasePointY, arch.BasePointZ);
            entity.TextPoint = new Point3d(arch.TextPointX, arch.TextPointY, arch.TextPointZ);
            var normal = Vector3d.ZAxis;
            var leftDir = (entity.TextPoint - entity.MidPoint);
            leftDir = new Vector3d(leftDir.X, leftDir.Y, 0);
            leftDir = leftDir.GetNormal();
            var dir = normal.CrossProduct(leftDir);
            entity.Angle = Vector3d.XAxis.GetAngleTo(dir, normal);
            var sp = entity.MidPoint - dir.MultiplyBy(arch.Width / 2);
            var ep = entity.MidPoint + dir.MultiplyBy(arch.Width / 2);
            var spLeft = sp + leftDir.MultiplyBy(arch.Thickness / 2);
            var spRight = sp - leftDir.MultiplyBy(arch.Thickness / 2);
            var epLeft = ep + leftDir.MultiplyBy(arch.Thickness / 2);
            var epRight = ep - leftDir.MultiplyBy(arch.Thickness / 2);
            Polyline outPLine = new Polyline();
            outPLine.AddVertexAt(0, spLeft.ToPoint2D(), 0, 0, 0);
            outPLine.AddVertexAt(1, spRight.ToPoint2D(), 0, 0, 0);
            outPLine.AddVertexAt(2, epRight.ToPoint2D(), 0, 0, 0);
            outPLine.AddVertexAt(3, epLeft.ToPoint2D(), 0, 0, 0);
            outPLine.Closed = true;
            entity.OutLine = ThMPolygonTool.CreateMPolygon(outPLine, new List<Curve> { });
            entity.Width = arch.Width;
            entity.Height = arch.Height;
            entity.Thickness = arch.Thickness;
            return entity;
        }
        ThTCHOpening WallDoorOpening(WallEntity wallEntity, DoorEntity doorEntity) 
        {
            return  new ThTCHOpening(doorEntity.MidPoint, doorEntity.Width, doorEntity.Height, wallEntity.RightWidth + wallEntity.LeftWidth + openingThickinessAdd, doorEntity.Angle); 
        }
        ThTCHOpening WallWindowOpening(WallEntity wallEntity, WindowEntity entity)
        {
            return new ThTCHOpening(entity.MidPoint, entity.Width, entity.Height, wallEntity.RightWidth + wallEntity.LeftWidth + openingThickinessAdd, entity.Angle);
        }
    }
    class WallEntity 
    {
        public string Id { get; }
        public ulong DBId { get; }
        public Point3d StartPoint { get; set; }
        public Point3d EndPoint { get; set; }
        /// <summary>
        /// 墙中心线，非异形墙有数据（Line,Arc），后续用来计算交点处的延长问题
        /// 非线或弧线的异形墙不计算延长计算轮廓
        /// </summary>
        public Curve WallCenterCurve { get; set; }
        public Curve WallLeftCurve { get; set; }
        public Curve WallRightCurve { get; set; }
        public double Elevtion { get; set; }
        public MPolygon OutLine { get; set; }
        public double LeftWidth { get; set; }
        public double RightWidth { get; set; }
        public double WallHeight { get; set; }
        public double WallMinZ 
        {
            get 
            {
                if (null == WallCenterCurve && null == OutLine)
                    return 0.0;
                if (null != WallCenterCurve) 
                {
                    var pt = WallCenterCurve.StartPoint;
                    return pt.Z;
                }
                return 0.0;
            }
        }
        public double WallMaxZ 
        {
            get 
            {
                if (null == WallCenterCurve && null == OutLine)
                    return 0.0;
                if (null != WallCenterCurve)
                {
                    var pt = WallCenterCurve.StartPoint;
                    return pt.Z + WallHeight;
                }
                return 0.0;
            } 
        }
        public WallEntity(ulong dbWallId)
        {
            Id = Guid.NewGuid().ToString();
            DBId = dbWallId;
        }
    }

    class DoorEntity
    {
        public string Id { get; }
        public Point3d MidPoint { get; set; }
        public Point3d TextPoint { get; set; }
        public Vector3d XVector { get; set; }
        public double Angle { get; set; }
        public MPolygon OutLine { get; set; }
        public double Thickness { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double MinZ
        {
            get
            {
                return MidPoint.Z;
            }
        }
        public double MaxZ
        {
            get
            {
                return MidPoint.Z+ Height;
            }
        }
        public DoorEntity()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
    class WindowEntity
    {
        public string Id { get; }
        public Point3d MidPoint { get; set; }
        public Point3d TextPoint { get; set; }
        public Vector3d XVector { get; set; }
        public double Angle { get; set; }
        public MPolygon OutLine { get; set; }
        public double Thickness { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double MinZ
        {
            get
            {
                return MidPoint.Z;
            }
        }
        public double WallMaxZ
        {
            get
            {
                return MidPoint.Z + Height;
            }
        }
        public WindowEntity()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
