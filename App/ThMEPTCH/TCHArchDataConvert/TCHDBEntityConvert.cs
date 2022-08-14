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
using ThMEPTCH.TCHArchDataConvert.THArchEntity;

namespace ThMEPTCH.TCHArchDataConvert
{
    class TCHDBEntityConvert
    {
        private double openingThickinessAdd = 120;
        ThCADCoreNTSSpatialIndex spatialIndex;
        Dictionary<MPolygon, ThTCHWall> wallDic = new Dictionary<MPolygon, ThTCHWall>();
        Dictionary<MPolygon, WallEntity> wallEntityDic = new Dictionary<MPolygon, WallEntity>();
        Dictionary<Curve, WallEntity> wallCurveDic = new Dictionary<Curve, WallEntity>();
        private string projectId="";
        public TCHDBEntityConvert(string prjId) 
        {
            projectId = prjId;
        }
        public List<ThTCHWall> WallDoorWindowRelation(List<TArchWall> walls, List<TArchDoor> doors,List<TArchWindow> windows,Vector3d moveOffSet) 
        {
            wallDic = new Dictionary<MPolygon, ThTCHWall>();
            wallEntityDic = new Dictionary<MPolygon, WallEntity>();
            wallCurveDic = new Dictionary<Curve, WallEntity>();
            var addDBColl = new DBObjectCollection();
            foreach (var item in walls)
            {
                var entity = DBToTHEntityCommon.TArchWallToEntityWall(item,0,0,0,0, moveOffSet);
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
                    var tempEntity = DBToTHEntityCommon.TArchWallToEntityWall(dbWall, spLeftOffSet, epLeftOffSet, spRightOffSet, epRightOffSet, moveOffSet);
                    if (tempEntity.OutLine != null && tempEntity.OutLine.Area>100)
                        wallDic.Add(entity.OutLine, WallEntityToTCHWall(tempEntity));
                }
                else 
                {
                    if (entity.OutLine != null && entity.OutLine.Area > 100)
                        wallDic.Add(entity.OutLine, WallEntityToTCHWall(entity));

                }
            }

            spatialIndex = new ThCADCoreNTSSpatialIndex(addDBColl);

            var doorDic = new Dictionary<MPolygon, ThTCHDoor>();
            var doorEntityDic = new Dictionary<MPolygon, DoorEntity>();
            foreach (var item in doors)
            {
                var entity = DBToTHEntityCommon.TArchDoorToEntityDoor(item, moveOffSet);
                doorDic.Add(entity.OutLine, DoorEntityToTCHDoor(entity));
                doorEntityDic.Add(entity.OutLine, entity);
            }

            var windowDic = new Dictionary<MPolygon, ThTCHWindow>();
            var windowEntityDic = new Dictionary<MPolygon, WindowEntity>();
            foreach (var item in windows)
            {
                var entity = DBToTHEntityCommon.TArchWindowToEntityWindow(item, moveOffSet);
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
                    var wallEntity = wallEntityDic[outLine];
                    var copyDoor = item.Value.Clone() as ThTCHDoor;
                    copyDoor.Uuid += wallEntity.DBId.ToString();
                    wall.Doors.Add(copyDoor);
                    wall.Openings.Add(WallDoorOpening(wallEntity, doorEntity));
                }
            }
            foreach (var item in windowDic)
            {
                var crossPLines = spatialIndex.SelectCrossingPolygon(item.Key).Cast<MPolygon>().ToList();
                var windowEntity = windowEntityDic[item.Key];
                foreach (var outLine in crossPLines)
                {
                    var wall = wallDic[outLine];
                    var wallEntity = wallEntityDic[outLine];
                    var copyWindow = item.Value.Clone() as ThTCHWindow;
                    copyWindow.Uuid += wallEntity.DBId.ToString();
                    wall.Windows.Add(copyWindow);
                    wall.Openings.Add(WallWindowOpening(wallEntity, windowEntity));
                }
            }
            var resList = new List<ThTCHWall>();
            foreach (var keyValue in wallDic) 
            {
                if (keyValue.Value.Outline is Polyline polyline) 
                {
                    if (polyline.Area < 100)
                        continue;
                    resList.Add(keyValue.Value);
                }
            }
            return resList;
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
                var curveSp = curve.StartPoint;
                if (Math.Abs(curveSp.Z - point.Z) > 1)
                    continue;
                var nearPoint = curve.GetClosestPointTo(point, false);
                if (nearPoint.DistanceTo(point) > tolerance)
                    continue;
                    resCurves.Add(curve,nearPoint);
            }
            return resCurves;
        }
        
        ThTCHWall WallEntityToTCHWall(WallEntity entity) 
        {
            var pl = entity.OutLine.Shell();
            pl.Closed = false;
            pl.Elevation = entity.OutLine.Elevation;
            var newWall = new ThTCHWall(pl, entity.WallHeight);
            newWall.Uuid = projectId + entity.DBId;
            newWall.Width = entity.LeftWidth + entity.RightWidth;
            //var newWall = new ThTCHWall(entity.StartPoint,entity.EndPoint,entity.RightWidth+entity.LeftWidth, entity.WallHeight);
            return newWall;
        }
        ThTCHDoor DoorEntityToTCHDoor(DoorEntity entity)
        {
            var newDoor = new ThTCHDoor(entity.MidPoint, entity.Width,entity.Height,entity.Thickness,entity.Rotation);
            newDoor.Uuid = projectId + entity.DBId.ToString();
            return newDoor;
        }
        ThTCHWindow WindowEntityToTCHWindow(WindowEntity entity)
        {
            var newWindow = new ThTCHWindow(entity.MidPoint, entity.Width, entity.Height, entity.Thickness, entity.Rotation);
            newWindow.Uuid = projectId + entity.DBId;
            return newWindow;
        }
        
        ThTCHOpening WallDoorOpening(WallEntity wallEntity, DoorEntity doorEntity) 
        {
            var opening = new ThTCHOpening(doorEntity.MidPoint, doorEntity.Width, doorEntity.Height, wallEntity.RightWidth + wallEntity.LeftWidth + openingThickinessAdd, doorEntity.Rotation);
            opening.Uuid = projectId + wallEntity.DBId.ToString() + doorEntity.DBId.ToString();
            return opening;
        }
        ThTCHOpening WallWindowOpening(WallEntity wallEntity, WindowEntity entity)
        {
            var opening = new ThTCHOpening(entity.MidPoint, entity.Width, entity.Height, wallEntity.RightWidth + wallEntity.LeftWidth + openingThickinessAdd, entity.Rotation);
            opening.Uuid = projectId + wallEntity.DBId.ToString() + entity.DBId.ToString();
            return opening;
        }
    }
}
