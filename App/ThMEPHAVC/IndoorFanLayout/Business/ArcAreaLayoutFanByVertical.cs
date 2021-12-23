using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.IndoorFanLayout.Models;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class ArcAreaLayoutFanByVertical:RoomLayoutFanBase
    {
        public ArcAreaLayoutFanByVertical(Dictionary<string, List<string>> divisionAreaNearIds, Vector3d xAxis, Vector3d yAxis)
            : base(divisionAreaNearIds, xAxis, yAxis)
        {
        }
        public List<DivisionRoomArea> GetRectangle(AreaLayoutGroup layoutGroup, FanRectangle fanRectangle)
        {
            _roomIntersectAreas.Clear();
            if (!layoutGroup.IsArcGroup)
                return _roomIntersectAreas;
            _fanRectangle = fanRectangle;
            CalcRoomLoad(layoutGroup, true);
            if (_roomIntersectAreas.Count < 1)
                return _roomIntersectAreas;

            LayoutFanRectFirstStep();
            AdjustFanRect();
            //排布内部的小风口
            LayoutFanVent();
            //AlignmentFanVent();
            return _roomIntersectAreas;
        }

        public List<Polyline> getPolyline()
        {
            Dictionary<string, int> groupdic = new Dictionary<string, int>();
            List<Polyline> tmpList = new List<Polyline>();
            int index = 0;
            foreach (var roomIntersectArea in _roomIntersectAreas)
            {
                var poly = roomIntersectArea.divisionArea.AreaPolyline;
                if (groupdic.ContainsKey(roomIntersectArea.GroupId) == false)
                    groupdic.Add(roomIntersectArea.GroupId, index++);
                poly.ColorIndex = groupdic[roomIntersectArea.GroupId] % 8;
                tmpList.Add(poly);
            }
            foreach (var item in _roomIntersectAreas)
            {
                foreach (var item1 in item.FanLayoutAreaResult)
                {
                    foreach (var item2 in item1.FanLayoutResult)
                    {
                        tmpList.Add(item2.FanPolyLine);
                    }
                }
            }
            return tmpList;
        }
        private void LayoutFanRectFirstStep()
        {
            bool dirChange = true;
            //第一列往右布置
            for(int j = _firstGroupIndex; j >= 0; j--)
            {

                var curretnPoint = _allGroupCenterOrders[j];
                var currentGroupId = _allGroupPoints.Where(c => c.Value.DistanceTo(curretnPoint) < 1).First().Key;
                foreach (var item in _roomIntersectAreas)
                {
                    if (item.GroupId != currentGroupId)
                        continue;
                    OneDivisionAreaCalcFanRectangle(item, _fanRectangle,dirChange);
                }
                //dirChange = !dirChange;
            }
            dirChange = false;
            //第一行往左布置
            for (int j = _firstGroupIndex + 1; j < _allGroupCenterOrders.Count; j++)
            {
                var curretnPoint = _allGroupCenterOrders[j];
                var currentGroupId = _allGroupPoints.Where(c => c.Value.DistanceTo(curretnPoint) < 1).First().Key;
                foreach (var item in _roomIntersectAreas)
                {
                    if (item.GroupId != currentGroupId)
                        continue;
                    OneDivisionAreaCalcFanRectangle(item, _fanRectangle, dirChange);
                }
                //dirChange = !dirChange;
            }
        }
        private void AdjustFanRect()
        {
            foreach(var divisionRoomArea in _roomIntersectAreas)
            {
                foreach(var fanLayoutAreaResult in divisionRoomArea.FanLayoutAreaResult)
                {
                    if (fanLayoutAreaResult.FanLayoutResult.Count <= 1) continue;
                    var lengthList = fanLayoutAreaResult.FanLayoutResult.Select(c => c.Length).ToList();
                    lengthList.Sort();
                    double minLength = lengthList[0] / lengthList[1] < 0.75 ? lengthList[1] : lengthList[0];
                    for(int i=0;i< fanLayoutAreaResult.FanLayoutResult.Count;i++)
                    {
                        var fanLayoutResult = fanLayoutAreaResult.FanLayoutResult.ElementAt(i);
                        //需要调整的矩形
                        if(minLength/fanLayoutResult.Length>0.9&&minLength/fanLayoutResult.Length<1)
                        {
                            var center = fanLayoutResult.CenterPoint;
                            var lDir = fanLayoutResult.FanDirection;
                            var wDir = fanLayoutResult.FanDirection.RotateBy(Math.PI / 2, Vector3d.ZAxis);
                            var fanDir = fanLayoutResult.FanDirection;
                            Polyline poly = new Polyline();
                            poly.Closed = true;
                            poly.AddVertexAt(0, (center + lDir.MultiplyBy(minLength / 2) - wDir.MultiplyBy(fanLayoutResult.Width / 2)).ToPoint2D(), 0, 0, 0);
                            poly.AddVertexAt(1, (center + lDir.MultiplyBy(minLength / 2) + wDir.MultiplyBy(fanLayoutResult.Width / 2)).ToPoint2D(), 0, 0, 0);
                            poly.AddVertexAt(2, (center - lDir.MultiplyBy(minLength / 2) + wDir.MultiplyBy(fanLayoutResult.Width / 2)).ToPoint2D(), 0, 0, 0);
                            poly.AddVertexAt(3, (center - lDir.MultiplyBy(minLength / 2) - wDir.MultiplyBy(fanLayoutResult.Width / 2)).ToPoint2D(), 0, 0, 0);
                            fanLayoutAreaResult.FanLayoutResult[i] = new FanLayoutRect(poly, fanLayoutResult.Width, fanLayoutResult.LengthDirctor);
                            fanLayoutAreaResult.FanLayoutResult[i].FanDirection = fanDir;
                        }
                    }
                }
            }
        }

        //单个分割区域的排布
        private void OneDivisionAreaCalcFanRectangle(DivisionRoomArea divisionArea, FanRectangle fanRectangle,bool dirChange)
        {
            var fanCount = divisionArea.NeedFanCount;
            if (fanCount < 1) 
                return;
            var allPoints = new List<Point3d>();
            foreach (var item in divisionArea.RoomLayoutAreas)
            {
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            }
            if (allPoints.Count < 3)
                return;

            //初始化区域信息
            var arc = divisionArea.divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
            var center = arc.Center;
            var arcNormal = arc.Normal;
            var arcXVector = arc.Ecs.CoordinateSystem3d.Xaxis;
            //获取半径范围
            allPoints = allPoints.OrderBy(c => c.DistanceTo(center)).ToList();
            var innerRadius = allPoints.First().DistanceTo(center);
            var outRadius = allPoints.Last().DistanceTo(center);
            var dirLength = outRadius - innerRadius;
            //获取角度范围
            var arcOrderPoints = CircleArcUtil.PointOderByArcAngle(allPoints, arc);
            var orderPoints = arcOrderPoints.OrderBy(c => c.Value).Select(c => c.Key).ToList();
            var spAngle = arcXVector.GetAngleTo((orderPoints.First() - center).GetNormal(), arcNormal);
            var epAngle = arcXVector.GetAngleTo((orderPoints.Last() - center).GetNormal(), arcNormal);
            var otherDirLength = (epAngle - spAngle) * innerRadius;//内弧长
            if (dirLength < fanRectangle.Width || otherDirLength < fanRectangle.MinLength)
                return;
            int columnCount = divisionArea.ColumnCount;
            if (columnCount < 1)
                return;
            var radius = divisionArea.divisionArea.CenterPoint.DistanceTo(center);
            //var nearAreas = GetNearDivisionAreasByRadius(divisionArea.divisionArea, radius, center, dirChange);
            //var nearFans = new List<FanLayoutRect>();
            //foreach(var fan in nearAreas)
            //{
            //    if (fan.FanLayoutAreaResult == null || fan.FanLayoutAreaResult.Count < 1)
            //        continue;
            //    foreach (var item in fan.FanLayoutAreaResult)
            //    {
            //        if (item.FanLayoutResult == null || item.FanLayoutResult.Count < 1)
            //            continue;
            //        nearFans.AddRange(item.FanLayoutResult);
            //    }
            //}

            var count = fanCount / columnCount;
            var tempCount = fanCount % columnCount;
            //根据列等分区域
            for(int i = 0; i < columnCount; i++)
            {
                var layoutDivision = divisionArea.FanLayoutAreaResult.Where(c => c.ColumnId == i).First();
                if (layoutDivision == null || layoutDivision.LayoutAreas == null || layoutDivision.LayoutAreas.Count() < 1)
                {
                    tempCount += count;
                    continue;
                }
                var thisColumnCount = count;
                if (tempCount > 0)
                {
                    thisColumnCount += 1;
                    tempCount -= 1;
                }
                var calcResult = new List<Polyline>();
                ////根据相邻区域的进行对齐排布
                //calcResult = CalcFanRectangle(divisionArea.divisionArea, layoutDivision.LayoutAreas, nearFans, fanRectangle, thisColumnCount);
                //对齐后，如果排布的个数不够，进行部分对齐进行排布
                //还是不够时进行等分排布
                if (calcResult.Count != thisColumnCount)
                    calcResult = CalcFanRectangle(divisionArea.divisionArea, layoutDivision.LayoutAreas, fanRectangle, thisColumnCount, false);
                //最后进行最小间距排布
                if (calcResult.Count != thisColumnCount)
                    calcResult = CalcFanRectangle(divisionArea.divisionArea, layoutDivision.LayoutAreas, fanRectangle, thisColumnCount, true);
                var thisColumnFans = new List<FanLayoutRect>();
                tempCount += thisColumnCount - calcResult.Count;
                var xVector = layoutDivision.LayoutDir.RotateBy(dirChange ? Math.PI / 2 : -Math.PI / 2, Vector3d.ZAxis);
                foreach (var pline in calcResult)
                {
                    var allLines = IndoorFanCommon.GetPolylineCurves(pline);
                    var lengthLine = allLines.OrderByDescending(c => c.GetLength()).First();
                    var lengthDir = (lengthLine.EndPoint - lengthLine.StartPoint).GetNormal();

                    var fanDir = xVector.Length > 0.5 ? (lengthDir.DotProduct(xVector) > 0 ? lengthDir : lengthDir.Negate()) : xVector;
                    var fanPline = new FanLayoutRect(pline, _fanRectangle.Width, lengthDir);
                    fanPline.FanDirection = fanDir;
                    thisColumnFans.Add(fanPline);
                }
                layoutDivision.FanLayoutResult.AddRange(thisColumnFans);
            }
        }
        //排布风机外框线
        List<Polyline> CalcFanRectangle(DivisionArea divisionArea, List<Polyline> layoutPolylines, FanRectangle fanRectangle, int fanCount, bool isMinSapce)
        {
            var allPoints = new List<Point3d>();
            foreach (var item in layoutPolylines)
            {
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            }
            //初始化区域信息
            var arc = divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
            var center = arc.Center;
            var arcNormal = arc.Normal;
            var arcXVector = arc.Ecs.CoordinateSystem3d.Xaxis;
            //获取半径范围
            allPoints = allPoints.OrderBy(c => c.DistanceTo(center)).ToList();
            var innerRadius = allPoints.First().DistanceTo(center);
            var outRadius = allPoints.Last().DistanceTo(center);
            var dirLength = outRadius - innerRadius;
            //获取角度范围
            var arcOrderPoints = CircleArcUtil.PointOderByArcAngle(allPoints, arc);
            var orderPoints = arcOrderPoints.OrderBy(c => c.Value).Select(c => c.Key).ToList();
            var spAngle = arcXVector.GetAngleTo((orderPoints.First() - center).GetNormal(), arcNormal);
            var epAngle = arcXVector.GetAngleTo((orderPoints.Last() - center).GetNormal(), arcNormal);
            var otherDirLength = (epAngle - spAngle) * innerRadius;//内弧长
            var midAngle = (spAngle + epAngle) / 2;
            var vector = arcXVector.RotateBy(midAngle, arcNormal).GetNormal();//中心线
            var startDist = innerRadius + fanRectangle.Width / 2 + (isMinSapce ? 400 : (dirLength - fanRectangle.Width * fanCount) / (fanCount * 2));
            var stepDist = dirLength / fanCount;

            var tempPolylines = new List<Polyline>();
            while (true)
            {
                if (startDist > outRadius)
                    break;
                var centerPoint = center + vector.MultiplyBy(startDist);
                var startPoint = centerPoint - vector.MultiplyBy(fanRectangle.Width / 2);
                var endPoint = centerPoint + vector.MultiplyBy(fanRectangle.Width / 2);
                var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, vector.RotateBy(Math.PI / 2, arcNormal), fanRectangle.MinLength, otherDirLength);
                if (tempPlines == null || tempPlines.Count < 1)
                    startDist += 50;
                else
                {
                    startDist += stepDist;
                    tempPolylines.AddRange(tempPlines.Select(c => c.Value).ToList());
                }
            }
            return tempPolylines;
        }
    }
}
