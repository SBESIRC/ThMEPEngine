using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPHVAC.IndoorFanLayout.Models;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class AreaLayoutGroup
    {
        public string UcsGroupId { get; }
        public bool IsArcGroup { get; }
        public string GroupFirstId { get; set; }
        /// <summary>
        /// 弧形区域是否是垂直扇形区域布置
        /// 在弧形区域有效,矩形区域该值无效
        /// </summary>
        public bool ArcVertical { get; set; }
        public List<string> OrderGroupIds { get; }
        public Dictionary<string, Point3d> GroupCenterPoints { get; }
        public List<DivisionRoomArea> GroupDivisionAreas { get; }
        public Vector3d FirstDir { get; }
        public Vector3d FirstRowDir { get; set; }
        public Point3d ArcCenter { get; }
        public bool IsInnerFirst { get; }
        public double UCSGroupLayoutArea { get; set; }
        double _precisionAngle = 15.0 * Math.PI / 180.0;
        public AreaLayoutGroup(List<DivisionRoomArea> thisGroupAreas, Vector3d firstVector,double roomYWidth, double fanMinLength,bool isByVertical = false)
        {
            this.UcsGroupId = Guid.NewGuid().ToString();
            this.GroupCenterPoints = new Dictionary<string, Point3d>();
            this.GroupDivisionAreas = new List<DivisionRoomArea>();
            this.OrderGroupIds = new List<string>();
            this.FirstDir = firstVector;
            this.FirstRowDir = firstVector;
            var firstArea = thisGroupAreas.First();
            this.IsArcGroup = firstArea.divisionArea.IsArc;
            if (this.IsArcGroup)
            {
                this.ArcCenter = firstArea.divisionArea.ArcCenterPoint;
                var arc = firstArea.divisionArea.AreaCurves.OfType<Arc>().First();
                var vector = (arc.EndPoint - this.ArcCenter).GetNormal();
                this.IsInnerFirst = vector.DotProduct(firstVector) > 0;
            }
            foreach (var item in thisGroupAreas)
            {
                item.UscGroupId = this.UcsGroupId;
                this.GroupDivisionAreas.Add(item);
            }
            this.UCSGroupLayoutArea = thisGroupAreas.Sum(c => c.RealIntersectAreas.Sum(x => x.Area));
            ArcVertical = isByVertical;
            if (!isByVertical)
            {
                CalcGroupAreaRow(roomYWidth, fanMinLength);
                CalcGroupPointOrder();
            }
            else
            {
                //这里只有弧形区域
                CalcGroupAreaColumn();
                CalcGroupPointOrderByVertical();
            }
        }
        void CalcGroupAreaRow(double roomWidth, double fanMinLength)
        {
            if (this.GroupDivisionAreas.Count < 1)
                return;
            var areaCenterPoints = this.GroupDivisionAreas.Select(c => c.divisionArea.CenterPoint).ToList();
            var allAreaCenterPoints = new List<Point3d>();
            var otherDir = Vector3d.ZAxis.CrossProduct(this.FirstDir);
            var centerPoint = this.ArcCenter;
            string firstRowGroupId = "";
            if (this.IsArcGroup)
            {
                var firstArea = this.GroupDivisionAreas.First();
                var arc = firstArea.divisionArea.AreaCurves.OfType<Arc>().First();
                var outDir = (arc.EndPoint - centerPoint).GetNormal();
                if (outDir.DotProduct(this.FirstDir) < 0)
                    areaCenterPoints = areaCenterPoints.OrderBy(c => c.DistanceTo(centerPoint)).ToList();
                else
                    areaCenterPoints = areaCenterPoints.OrderByDescending(c => c.DistanceTo(centerPoint)).ToList();
            }
            else
                areaCenterPoints = ThPointVectorUtil.PointsOrderByDirection(areaCenterPoints, this.FirstDir, true);
            allAreaCenterPoints.AddRange(areaCenterPoints);
            while (areaCenterPoints.Count > 0)
            {
                string groupId = Guid.NewGuid().ToString();
                var first = areaCenterPoints.First();
                var firstArea = GroupDivisionAreas.Where(c => c.divisionArea.CenterPoint.DistanceTo(first) < 5).First();
                areaCenterPoints.Remove(first);
                var areaPoints = IndoorFanCommon.GetPolylinePoints(firstArea.divisionArea.AreaPolyline);
                var linePoints = new List<Point3d>();
                
                
                if (this.IsArcGroup)
                {
                    var thisRadius = first.DistanceTo(centerPoint);
                    foreach (var point in areaCenterPoints)
                    {
                        var pointRadius = point.DistanceTo(centerPoint);
                        if (Math.Abs(pointRadius - thisRadius) < 1000)
                            linePoints.Add(point);
                    }
                }
                else
                {
                    foreach (var point in areaCenterPoints)
                    {
                        var orderPoints = ThPointVectorUtil.PointsOrderByDirection(areaPoints, this.FirstDir, false);
                        var sp = orderPoints.First();
                        var ep = orderPoints.Last();
                        var prjEp = ThPointVectorUtil.PointToLine(ep, sp, this.FirstDir);

                        var area = GroupDivisionAreas.Where(c => c.divisionArea.CenterPoint.DistanceTo(point) < 5).First();
                        var thisAreaPoints = IndoorFanCommon.GetPolylinePoints(area.divisionArea.AreaPolyline);
                        var thisOrderPoints = ThPointVectorUtil.PointsOrderByDirection(thisAreaPoints, this.FirstDir, false);
                        var thisPrjSp = ThPointVectorUtil.PointToLine(thisOrderPoints.First(), sp, this.FirstDir);
                        var thisPrjEp = ThPointVectorUtil.PointToLine(thisOrderPoints.Last(), sp, this.FirstDir);
                        IndoorFanCommon.FindIntersection(new Line(sp, prjEp), new Line(thisPrjSp, thisPrjEp), out List<Point3d> interPoints);
                        if (interPoints.Count < 2)
                            continue;
                        if (interPoints.First().DistanceTo(interPoints.Last()) < 1000)
                            continue;
                        areaPoints.AddRange(thisOrderPoints);
                        linePoints.Add(point);
                        //var checkDir = (point - first).GetNormal();
                        //var angle = otherDir.GetAngleTo(checkDir);
                        //angle %= Math.PI;
                        //if (angle > _precisionAngle && angle < Math.PI - _precisionAngle)
                        //    continue;
                        //var dot = (point - first).DotProduct(this.FirstDir);
                        //if (Math.Abs(dot) > 2000)
                        //    continue;
                        //linePoints.Add(point);
                    }
                }
                foreach (var point in linePoints)
                    areaCenterPoints.Remove(point);
                linePoints.Add(first);

                double length = 0.0;
                var thisAreas = new List<DivisionRoomArea>();

                bool canLayoutFan = false;
                foreach (var item in this.GroupDivisionAreas)
                {
                    if (linePoints.Any(c => c.DistanceTo(item.divisionArea.CenterPoint) < 5))
                    {
                        thisAreas.Add(item);
                        item.GroupId = groupId;
                        foreach (var pl in item.RealIntersectAreas) 
                        {
                            var thisGroupPoints = new List<Point3d>();
                            thisGroupPoints.AddRange(IndoorFanCommon.GetPolylinePoints(pl));
                            thisGroupPoints = ThPointVectorUtil.PointsOrderByDirection(thisGroupPoints, otherDir, false);
                            var thisLength = Math.Abs((thisGroupPoints.First() - thisGroupPoints.Last()).DotProduct(otherDir));
                            if (!canLayoutFan && thisLength > 1800 + Math.Abs(IndoorFanCommon.RoomBufferOffSet * 2)) 
                            {
                                thisGroupPoints = ThPointVectorUtil.PointsOrderByDirection(thisGroupPoints, FirstDir, false);
                                var dirLength = Math.Abs((thisGroupPoints.First() - thisGroupPoints.Last()).DotProduct(FirstDir));
                                canLayoutFan = dirLength > fanMinLength + Math.Abs(IndoorFanCommon.RoomBufferOffSet * 2);
                            }
                            length += Math.Abs(thisLength);
                        }
                    }
                }
                var center = GroupRowCenterPoints(thisAreas);
                this.GroupCenterPoints.Add(groupId, center);

                if (!string.IsNullOrEmpty(firstRowGroupId))
                    continue;
                if (canLayoutFan)
                    firstRowGroupId = groupId;
                //if ((roomWidth / length) < 2.0) 
                //{
                //    firstRowGroupId = groupId;
                //}
            }
            if (string.IsNullOrEmpty(firstRowGroupId)) 
            {
                var firstCenter = allAreaCenterPoints.First();
                firstRowGroupId = GroupCenterPoints.Where(c => c.Value.DistanceTo(firstCenter) < 10).FirstOrDefault().Key;
                firstRowGroupId = GroupCenterPoints.First().Key;
            }
               
            this.GroupFirstId = firstRowGroupId;
        }
        void CalcGroupAreaColumn()
        {
            //获取区域按钮垂直极轴方向进行分组
            if (this.GroupDivisionAreas.Count < 1)
                return;
            var areaCenterPoints = this.GroupDivisionAreas.Select(c => c.divisionArea.CenterPoint).ToList();
            var firstArea = this.GroupDivisionAreas.First();
            var centerPoint = this.ArcCenter;
            string firstColumnId = "";
            var arc = firstArea.divisionArea.AreaCurves.OfType<Arc>().First();
            var arcXVector = arc.Ecs.CoordinateSystem3d.Xaxis;
            var arcNormal = arc.Normal;
            var orderDic = CircleArcUtil.PointOderByArcAngle(areaCenterPoints,centerPoint,arcNormal,arcXVector); //areaCenterPoints.OrderBy(c => arcXVector.GetAngleTo((c - centerPoint).GetNormal(), arcNormal)).ToList();
            double columnAllAreas = 0.0;
            double columnAreaRadio = 0.0;
            bool haveFirst = false;
            while (areaCenterPoints.Count > 0)
            {
                string groupId = Guid.NewGuid().ToString();
                var first = areaCenterPoints.First();
                areaCenterPoints.Remove(first);
                var linePoints = new List<Point3d>();
                var dir = (first - centerPoint).GetNormal();
                var otherDir = Vector3d.ZAxis.CrossProduct(dir);
                if (this.IsArcGroup)
                {
                    var thisDir = (first - centerPoint).GetNormal();
                    foreach (var point in areaCenterPoints)
                    {
                        var checkDir = point - first;
                        var dot = checkDir.DotProduct(otherDir);
                        if (Math.Abs(dot) > 1000)
                            continue;
                        linePoints.Add(point);
                        //方向一致
                        //var pointDir = (point - first).GetNormal();
                        //var deltaAngle = pointDir.GetAngleTo(thisDir) / Math.PI * 180;
                        //if (Math.Abs(deltaAngle) < 1 || Math.Abs(180 - deltaAngle) < 1 || Math.Abs(-180 - deltaAngle) < 1) 
                        //    linePoints.Add(point);
                    }
                }
                foreach (var point in linePoints)
                    areaCenterPoints.Remove(point);
                linePoints.Add(first);

                //检查相交面积和比值
                var thisAreas = new List<DivisionRoomArea>();
                double thisAllAreas = 0.0;
                double thisInsertAreas = 0.0;
                foreach (var item in this.GroupDivisionAreas)
                {
                    if (linePoints.Any(c => c.DistanceTo(item.divisionArea.CenterPoint) < 5))
                    {
                        thisAreas.Add(item);
                        item.GroupId = groupId;
                        thisAllAreas += item.divisionArea.AreaPolyline.Area;
                        thisInsertAreas += item.RealIntersectAreas.Sum(c => c.Area);
                    }
                }
                var center = GroupColumnCenterPoints(thisAreas);
                this.GroupCenterPoints.Add(groupId, center);
                if (haveFirst)
                    continue;
                var ratio = thisInsertAreas / thisAllAreas;
                if (ratio > 0.4)
                {
                    haveFirst = true;
                    columnAllAreas = thisAllAreas;
                    columnAreaRadio = ratio;
                    firstColumnId = groupId;
                }
                else if (ratio > columnAreaRadio)
                {
                    columnAllAreas = thisAllAreas;
                    columnAreaRadio = ratio;
                    firstColumnId = groupId;
                }
            }
            this.GroupFirstId = firstColumnId;
        }
        void CalcGroupPointOrder()
        {
            var hisDic = new Dictionary<string, Point3d>();
            foreach (var item in this.GroupCenterPoints)
            {
                hisDic.Add(item.Key, item.Value);
            }
            this.GroupCenterPoints.Clear();
            this.OrderGroupIds.Clear();
            var gCenterPoints = hisDic.Select(c => c.Value).ToList();
            if (this.IsArcGroup)
            {
                if (IsInnerFirst)
                    gCenterPoints = gCenterPoints.OrderBy(c => c.DistanceTo(this.ArcCenter)).ToList();
                else
                    gCenterPoints = gCenterPoints.OrderByDescending(c => c.DistanceTo(this.ArcCenter)).ToList();
            }
            else
            {
                gCenterPoints = ThPointVectorUtil.PointsOrderByDirection(gCenterPoints, FirstDir, false);
            }
            foreach (var point in gCenterPoints)
            {
                var groupId = hisDic.Where(c => c.Value.DistanceTo(point) < 1).FirstOrDefault().Key;
                this.GroupCenterPoints.Add(groupId, point);
                this.OrderGroupIds.Add(groupId);
            }
        }
        void CalcGroupPointOrderByVertical()
        {
            var hisDic = new Dictionary<string, Point3d>();
            foreach (var item in this.GroupCenterPoints)
            {
                hisDic.Add(item.Key, item.Value);
            }
            this.GroupCenterPoints.Clear();
            this.OrderGroupIds.Clear();
            var gCenterPoints = hisDic.Select(c => c.Value).ToList();
            gCenterPoints = gCenterPoints.OrderBy(c => Vector3d.XAxis.GetAngleTo((c - this.ArcCenter).GetNormal(), Vector3d.ZAxis)).ToList();
            foreach (var point in gCenterPoints)
            {
                var groupId = hisDic.Where(c => c.Value.DistanceTo(point) < 1).FirstOrDefault().Key;
                this.GroupCenterPoints.Add(groupId, point);
                this.OrderGroupIds.Add(groupId);
            }
        }
        Point3d GroupRowCenterPoints(List<DivisionRoomArea> thisRowAreas)
        {
            var area = thisRowAreas.First();
            var thisRowCenterPoints = thisRowAreas.Select(c => c.divisionArea.CenterPoint).ToList();
            var groupCenter = Point3d.Origin;
            if (area.divisionArea.IsArc)
            {
                var arcCenter = area.divisionArea.ArcCenterPoint;
                var orderAngles = CircleArcUtil.PointOderByArcAngle(
                    thisRowCenterPoints,
                    arcCenter,
                    Vector3d.ZAxis, Vector3d.XAxis);
                var allAngles = orderAngles.Select(c => c.Value).ToList().OrderBy(c => c).ToList();
                var minAngle = allAngles.First();
                var maxAngle = allAngles.Last();
                var midAngle = (minAngle + maxAngle) / 2;
                var midVector = Vector3d.XAxis.RotateBy(midAngle, Vector3d.ZAxis);
                groupCenter = arcCenter + midVector.MultiplyBy(thisRowCenterPoints.First().DistanceTo(arcCenter));
            }
            else
            {
                groupCenter = ThPointVectorUtil.PointsAverageValue(thisRowCenterPoints);
            }
            return groupCenter;
        }
        Point3d GroupColumnCenterPoints(List<DivisionRoomArea> thisColumnAreas)
        {
            var area = thisColumnAreas.First();
            var thisColumnCenterPoints = thisColumnAreas.Select(c => c.divisionArea.CenterPoint).ToList();
            var groupCenter = Point3d.Origin;
            if (area.divisionArea.IsArc)
            {
                var arcCenter = area.divisionArea.ArcCenterPoint;
                var orderRadius = thisColumnCenterPoints.OrderBy(o => o.DistanceTo(arcCenter)).ToList();
                var minRadius = orderRadius.First().DistanceTo(arcCenter);
                var maxRadius = orderRadius.Last().DistanceTo(arcCenter);
                var midRadius = (minRadius + maxRadius) / 2;
                var rotateAngle = Vector3d.XAxis.GetAngleTo((orderRadius.First() - arcCenter).GetNormal(), Vector3d.ZAxis);
                var midVector = Vector3d.XAxis.RotateBy(rotateAngle, Vector3d.ZAxis);
                groupCenter = arcCenter + midVector.MultiplyBy(midRadius);
            }
            else
            {
                groupCenter = ThPointVectorUtil.PointsAverageValue(thisColumnCenterPoints);
            }
            return groupCenter;
        }
    }
}
