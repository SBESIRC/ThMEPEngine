using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPHVAC.IndoorFanLayout.Models;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class CalcLayoutArea
    {
        Polyline _roomPLine;
        List<Polyline> _roomInnerPLine;
        List<DivisionArea> _allDivisionAreas;
        List<DivisionRoomArea> _roomIntersectAreas;
        double _roomUnitLoad = 0.0;
        ThCADCoreNTSSpatialIndex _areaSpatialIndex;
        Dictionary<Polyline, DivisionArea> _areaPLine;

        public CalcLayoutArea(List<DivisionArea> divisionAreas)
        {
            _allDivisionAreas = new List<DivisionArea>();
            _roomIntersectAreas = new List<DivisionRoomArea>();
            _areaPLine = new Dictionary<Polyline, DivisionArea>();
            if (null == divisionAreas || divisionAreas.Count < 1)
                return;
            foreach (var item in divisionAreas)
            {
                _allDivisionAreas.Add(item);
                _areaPLine.Add(item.AreaPolyline, item);
            }
            var areaObjs = new DBObjectCollection();
            foreach (var item in divisionAreas)
            {
                areaObjs.Add(item.AreaPolyline);
            }
            _areaSpatialIndex = new ThCADCoreNTSSpatialIndex(areaObjs);
        }
        public void InitRoomData(Polyline roomOutPLine, List<Polyline> innerPLines,double roomArea, double roomLoad)
        {
            _roomPLine = roomOutPLine;
            _roomInnerPLine = new List<Polyline>();
            roomArea = roomArea < 0 ? (roomOutPLine.Area - innerPLines.Sum(c => c.Area)) : roomArea;
            _roomUnitLoad = roomLoad / roomArea;
            if (null == innerPLines || innerPLines.Count < 1)
                return;
            foreach (var item in innerPLines)
                _roomInnerPLine.Add(item);
        }
        public double RoomUnitLoad 
        {
            get { return _roomUnitLoad; }
        }
        public List<AreaLayoutGroup> GetRoomInsterAreas(Vector3d firstDir)
        {
            //根据外轮廓获取相交到的轮廓
            var resUCSGroups = new List<AreaLayoutGroup>();
            CalaRoomInsertAreas();
            if (null == _roomIntersectAreas || _roomIntersectAreas.Count < 1)
                return resUCSGroups;
            resUCSGroups = CalcDivisionAreaGroup(_roomIntersectAreas, firstDir);
            resUCSGroups = CalcLayoutAreaDir(resUCSGroups);
            return resUCSGroups;
        }
        public List<AreaLayoutGroup> CalcLayoutGroupAreaDir(Dictionary<Point3d, Vector3d> hisFanDir)
        {
            var areaUCSGroups = new List<AreaLayoutGroup>();
            CalaRoomInsertAreas();
            if (null == _roomIntersectAreas || _roomIntersectAreas.Count < 1)
                return areaUCSGroups;
            var areaDir = GetLayoutAreaHisFandir(hisFanDir);
            var firstDir = hisFanDir.First().Value;
            areaUCSGroups = CalcDivisionAreaGroup(_roomIntersectAreas, firstDir);
            //设置第一排和第一排朝向
            foreach (var item in areaUCSGroups)
            {
                var oldFirstRowId = item.GroupFirstId;
                foreach (var id in item.OrderGroupIds)
                {
                    bool rowInHis = false;
                    var newVector = new Vector3d();
                    foreach (var area in item.GroupDivisionAreas)
                    {
                        if (area.GroupId != id)
                            continue;
                        if (rowInHis)
                            break;
                        foreach (var hisId in areaDir)
                        {
                            if (hisId.Key == area.divisionArea.Uid)
                            {
                                rowInHis = true;
                                newVector = hisId.Value;
                                break;
                            }
                        }
                    }
                    var rmAreas = item.GroupDivisionAreas.Where(c => areaDir.Any(x => x.Key == c.divisionArea.Uid)).ToList();
                    foreach (var rm in rmAreas) 
                    {
                        item.GroupDivisionAreas.Remove(rm);
                    }
                    if (!rowInHis)
                        continue;
                    item.GroupFirstId = id;
                    item.FirstRowDir = newVector;
                    break;
                }
            }
            areaUCSGroups = CalcLayoutAreaDir(areaUCSGroups);
            return areaUCSGroups;
        }
        private Dictionary<string, Vector3d> GetLayoutAreaHisFandir(Dictionary<Point3d, Vector3d> hisFanDir) 
        {
            var hisDir = new Dictionary<string, Vector3d>();

            foreach (var item in _roomIntersectAreas) 
            {
                foreach (var fanKeyValue in hisFanDir) 
                {
                    if (item.divisionArea.AreaPolyline.Contains(fanKeyValue.Key))
                    {
                        hisDir.Add(item.divisionArea.Uid,fanKeyValue.Value);
                        break;
                    }
                }
            }

            return hisDir;
        }
        private void CalaRoomInsertAreas() 
        {
            _roomIntersectAreas = new List<DivisionRoomArea>();
            var outGeo = _roomPLine.ToNTSPolygon();
            var targetAreas = GetDivisionAreas(_roomPLine);
            foreach (var divisionArea in targetAreas)
            {
                var tempGeo = divisionArea.AreaPolyline.ToNTSPolygon();
                var isAdd = outGeo.Contains(tempGeo) || tempGeo.Intersects(outGeo);
                if (!isAdd)
                    continue;
                var layoutFan = new DivisionRoomArea(divisionArea);
                var interGeo = OverlayNGRobust.Overlay(
                    outGeo,
                    tempGeo,
                    SpatialFunction.Intersection);
                var res = interGeo.ToDbCollection();
                var interPolylines = new List<Polyline>();
                foreach (var item in res)
                {
                    if (item is Polyline polyline)
                    {
                        if (polyline.Area < 100)
                            continue;
                        interPolylines.Add(polyline);
                    }
                    else if (item is Polygon polygon)
                    {
                        if (polygon == null || polygon.Area < 100)
                            continue;
                        interPolylines.Add(polygon.Shell.ToDbPolyline());
                    }
                }
                if (interPolylines.Count < 1)
                    continue;
                var needLoad = CalcAreaLoad(interPolylines);
                foreach (var polyline in interPolylines)
                {
                    layoutFan.RealIntersectAreas.Add(polyline);
                    var tempBuffers = polyline.Buffer(IndoorFanCommon.RoomBufferOffSet);
                    if (tempBuffers == null || tempBuffers.Count < 1)
                        continue;
                    var tempRoomPLine = tempBuffers[0] as Polyline;
                    if (tempRoomPLine == null || tempRoomPLine.Area < 100)
                        continue;
                    layoutFan.RoomLayoutAreas.Add(tempRoomPLine);
                }
                layoutFan.NeedLoad = needLoad;
                _roomIntersectAreas.Add(layoutFan);
            }
        }
        
        List<DivisionArea> GetDivisionAreas(Polyline roomOutPLine) 
        {
            var resList = new List<DivisionArea>();
            //通过空间索引初步过滤
            var interPLines = _areaSpatialIndex.SelectCrossingPolygon(roomOutPLine);
            foreach (var item in interPLines) 
            {
                var pl = item as Polyline;
                var addArea = _areaPLine[pl];
                resList.Add(addArea);
            }
            return resList;
        }
        double CalcAreaLoad(List<Polyline> roomOutInseterAreas) 
        {
            double areaLoad = 0.0;
            if (roomOutInseterAreas == null || roomOutInseterAreas.Count<1)
                return areaLoad;
            double area = 0.0;
            if (_roomInnerPLine == null || _roomInnerPLine.Count < 1) 
            {
                area = roomOutInseterAreas.Sum(c => c.Area);
                return area * _roomUnitLoad;
            }
            var thisOutAreas = new DBObjectCollection();
            foreach (var item in roomOutInseterAreas)
                thisOutAreas.Add(item);
            foreach (var innerPL in _roomInnerPLine)
            {
                if (thisOutAreas.Count < 1)
                    break;
                var dBObjects = new DBObjectCollection();
                dBObjects.Add(innerPL);
                var newPlines = new DBObjectCollection();
                foreach (var tempArea in thisOutAreas)
                {
                    var diffObj = new DBObjectCollection();
                    if (tempArea is Polyline polyline)
                    {
                        diffObj = polyline.DifferenceMP(dBObjects);
                    }
                    else if (tempArea is MPolygon mPolygon)
                    {
                        diffObj = mPolygon.DifferenceMP(dBObjects);
                    }
                    foreach (var item in diffObj)
                    {
                        if (item is Polyline polyline1)
                            newPlines.Add(polyline1);
                        else if (item is MPolygon mPolygon)
                            newPlines.Add(mPolygon);
                    }
                }
                thisOutAreas.Clear();
                foreach (var item in newPlines)
                {
                    if (item is Polyline polyline)
                        thisOutAreas.Add(polyline);
                    else if (item is MPolygon mPolygon)
                        thisOutAreas.Add(mPolygon);
                }

            }
            foreach (var item in thisOutAreas)
            {
                if (item is Polyline polyline)
                    area += polyline.Area;
                else if (item is MPolygon mPolygon)
                    area += mPolygon.Area;
            }
            return area * _roomUnitLoad;
        }
        List<AreaLayoutGroup> CalcLayoutAreaDir(List<AreaLayoutGroup> areaUCSGroups) 
        {
            if (null == areaUCSGroups || areaUCSGroups.Count < 1)
                return areaUCSGroups;
            //根据每个UCS的第一排，计算每个UCS的实际第一排
            //Step1，计算第一排方向上没有其它UCS的分组
            var ucsIds = new List<string>();
            var dirUcsGroupIds = new Dictionary<string, string>();
            var dirGroupRowIds = new Dictionary<string, string>();
            for (int i = 0; i < areaUCSGroups.Count; i++)
            {
                var firstGroup = areaUCSGroups[i];
                bool dirHaveGroup = false;
                //计算第一排方向上是否和其它的区域相交，比较时是和其它UCS的区域数据进行比较
                var firstGroupFirstRows = firstGroup.GroupDivisionAreas.Where(c => c.GroupId == firstGroup.GroupFirstId).ToList();
                var firstRowDir = firstGroupFirstRows.First().GroupDir;
                //将第一排区域外扩5mm，找相交到的区域
                var firstPLines = new List<Polyline>();
                foreach (var item in firstGroupFirstRows) 
                {
                    var buff = item.divisionArea.AreaPolyline.Buffer(5)[0] as Polyline;
                    firstPLines.Add(buff);
                }
                for (int j = 0; j < areaUCSGroups.Count; j++) 
                {
                    if (dirHaveGroup)
                        break;
                    if (i == j)
                        continue;
                    var secondGroup = areaUCSGroups[j];
                    var secondGroupFirstRows = secondGroup.GroupDivisionAreas.Where(c => c.GroupId == secondGroup.GroupFirstId).ToList();
                    var secondRowDir = secondGroupFirstRows.First().GroupDir;
                    //先初步获取
                    var inserts = GetGroupInsert(firstPLines, secondGroup);
                    if (inserts.Count < 1)
                        continue;
                    var firstDirInsert = GetFirstRowDirInsert(firstGroupFirstRows, firstRowDir, inserts);
                    if (firstDirInsert.Count < 1)
                        continue;
                    dirUcsGroupIds.Add(firstGroup.UcsGroupId, secondGroup.UcsGroupId);
                    dirGroupRowIds.Add(firstGroup.UcsGroupId, firstDirInsert.First().GroupId);
                    dirHaveGroup = true;
                    break;
                }
                if (dirHaveGroup)
                    continue;
                ucsIds.Add(firstGroup.UcsGroupId);
            }
            
            //Step2,每个UCS确定第一排方向后，计算其余每一排的方向
            foreach (var group in areaUCSGroups) 
            {
                if (!ucsIds.Any(c => c == group.UcsGroupId))
                    continue;
                var thisGroupFirstId = group.GroupFirstId;
                var firstGroupIndex = group.OrderGroupIds.IndexOf(thisGroupFirstId);
                var orientation = group.FirstRowDir;
                bool isCurrentDir = true;
                for (int j = firstGroupIndex; j >= 0; j--)
                {
                    var currentDir = isCurrentDir ? orientation : orientation.Negate();
                    var currentGroupId = group.OrderGroupIds[j];
                    CalcGroupRowDir(currentGroupId, currentDir, group.ArcVertical);
                    isCurrentDir = !isCurrentDir;
                }
                isCurrentDir = false;
                for (int j = firstGroupIndex + 1; j < group.OrderGroupIds.Count; j++)
                {
                    var currentDir = isCurrentDir ? orientation : orientation.Negate();
                    var currentGroupId = group.OrderGroupIds[j];
                    CalcGroupRowDir(currentGroupId, currentDir, group.ArcVertical);
                    isCurrentDir = !isCurrentDir;
                }
            }
            if (ucsIds.Count == areaUCSGroups.Count)
                return areaUCSGroups;
            //再计算受影响的每个UCS的方向,要考虑先后影响的问题，哪个区域的第一排朝向优先计算
            while (ucsIds.Count < areaUCSGroups.Count) 
            {
                foreach (var item in areaUCSGroups)
                {
                    if (ucsIds.Any(c => c == item.UcsGroupId))
                        continue;
                    //获取根据哪一个group计算第一排方向
                    var nearUcsGroupId = dirUcsGroupIds.Where(c => c.Key == item.UcsGroupId).First().Value;
                    if (ucsIds.Any(c => c == nearUcsGroupId))
                        continue;
                    var nearGroup = areaUCSGroups.Where(c => c.UcsGroupId == nearUcsGroupId).First();
                    var nearRowId = dirGroupRowIds.Where(c => c.Key == item.UcsGroupId).First().Value;
                    var nearRow = nearGroup.GroupDivisionAreas.Where(c => c.GroupId == nearRowId).First();
                    var nearRowDir = nearRow.GroupDir;
                    if (null == nearRowDir)
                        continue;
                    var dot = item.FirstRowDir.DotProduct(nearRowDir);
                    if (dot > 0)
                        item.FirstRowDir = item.FirstRowDir.Negate();
                    ucsIds.Add(item.UcsGroupId);
                }
            }
            
            //计算其它
            foreach (var group in areaUCSGroups)
            {
                if (ucsIds.Any(c => c == group.UcsGroupId))
                    continue;
                var thisGroupFirstId = group.GroupFirstId;
                var firstGroupIndex = group.OrderGroupIds.IndexOf(thisGroupFirstId);
                var orientation = group.FirstRowDir;
                bool isCurrentDir = true;
                for (int j = firstGroupIndex; j >= 0; j--)
                {
                    var currentDir = isCurrentDir ? orientation : orientation.Negate();
                    var currentGroupId = group.OrderGroupIds[j];
                    CalcGroupRowDir(currentGroupId, currentDir, group.ArcVertical);
                    isCurrentDir = !isCurrentDir;
                }
                isCurrentDir = false;
                for (int j = firstGroupIndex + 1; j < group.OrderGroupIds.Count; j++)
                {
                    var currentDir = isCurrentDir ? orientation : orientation.Negate();
                    var currentGroupId = group.OrderGroupIds[j];
                    CalcGroupRowDir(currentGroupId, currentDir, group.ArcVertical);
                    isCurrentDir = !isCurrentDir;
                }
            }
            return areaUCSGroups;
        }
        List<DivisionRoomArea> GetGroupInsert(List<Polyline> polylines, AreaLayoutGroup secondGroup) 
        {
            var insertAreas = new List<DivisionRoomArea>();
            foreach (var item in secondGroup.GroupDivisionAreas) 
            {
                bool ins = false;
                var checkPLine = item.divisionArea.AreaPolyline;
                foreach (var pl in polylines) 
                {
                    if (ins)
                        break;
                    ins = pl.Intersects(checkPLine);
                }
                if (!ins)
                    continue;
                insertAreas.Add(item);
            }
            return insertAreas;
        }
        List<DivisionRoomArea> GetFirstRowDirInsert(List<DivisionRoomArea> firstGroupRows,Vector3d firstDir, List<DivisionRoomArea> insertGroupRows) 
        {
            //获取相交到的区域是否有第一排方向上的，然后判断是影响方向
            var firstCenterPoint = firstGroupRows.First().divisionArea.CenterPoint;
            var dirInsert = new List<DivisionRoomArea>();
            var resInsert = new List<DivisionRoomArea>();
            foreach (var item in insertGroupRows)
            {
                var vector = item.divisionArea.CenterPoint - firstCenterPoint;
                var dot = vector.DotProduct(firstDir);
                if (dot > 0)
                    dirInsert.Add(item);
            }
            foreach (var first in firstGroupRows) 
            {
                var centerPoint = first.divisionArea.CenterPoint;
                foreach (var item in dirInsert)
                {
                    if (resInsert.Any(c => c.divisionArea.Uid == item.divisionArea.Uid))
                        continue;
                    //获取共线边，进一步判断是否
                    var insertCurves = new List<Curve>();
                    foreach (var firstCurve in first.divisionArea.AreaCurves) 
                    {
                        foreach (var secondCurve in item.divisionArea.AreaCurves) 
                        {
                            //这里线只有线段和圆弧
                            if (firstCurve is Line firstLine)
                            {
                                if (secondCurve is Line secondLine) 
                                {
                                    IndoorFanCommon.FindIntersection(firstLine, secondLine, out List<Point3d> interPoints);
                                    if (interPoints.Count > 1 && interPoints[0].DistanceTo(interPoints[1]) > 100) 
                                    {
                                        insertCurves.Add(firstCurve);
                                    }
                                }
                            }
                            else if(firstCurve is Arc firstArc)
                            {
                                if (secondCurve is Arc secondArc)
                                {
                                    var interArc = CircleArcUtil.ArcIntersectArc(firstArc, secondArc);
                                    if (interArc == null || interArc.Length < 10)
                                        continue;
                                    insertCurves.Add(firstArc);
                                }
                            }
                        }
                    }
                    if (insertCurves.Count < 1)
                        continue;
                    foreach (var curve in insertCurves) 
                    {
                        if (curve is Line line)
                        {
                            var prjPoint = centerPoint.PointToLine(line);
                            var dir = (prjPoint - centerPoint).GetNormal();
                            if (dir.DotProduct(firstDir) > 0.5)
                                resInsert.Add(item);
                        }
                        else if (curve is Arc arc) 
                        {
                            var prjPoint = CircleArcUtil.PointToArc(centerPoint, arc);
                            var dir = (prjPoint - centerPoint).GetNormal();
                            if (dir.DotProduct(firstDir) > 0.5)
                                resInsert.Add(item);
                        }
                    }
                }
            }
            return resInsert;
        }
        void CalcGroupRowDir(string currentGroupId,Vector3d currentDir,bool isArcVertical) 
        {
            foreach (var item in _roomIntersectAreas)
            {
                if (item.GroupId != currentGroupId)
                    continue;
                if (!item.divisionArea.IsArc)
                    item.GroupDir = currentDir;
                else
                {
                    var arc = item.divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
                    var center = arc.Center;
                    var outVector = (item.divisionArea.CenterPoint - center).GetNormal();
                    if (isArcVertical)
                    {
                        var dir = Vector3d.ZAxis.CrossProduct(outVector);
                        if (dir.DotProduct(currentDir) > 0)
                            item.GroupDir = dir;
                        else
                            item.GroupDir = dir.Negate();
                    }
                    else
                    {
                        if (outVector.DotProduct(currentDir) > 0)
                            item.GroupDir = outVector;
                        else
                            item.GroupDir = outVector.Negate();
                    }
                }
            }
        }
        List<AreaLayoutGroup> CalcDivisionAreaGroup(List<DivisionRoomArea> insertAreas, Vector3d vector)
        {
            var otherDir = Vector3d.ZAxis.CrossProduct(vector);
            var points = IndoorFanCommon.GetPolylinePoints(_roomPLine);
            points = ThPointVectorUtil.PointsOrderByDirection(points,otherDir,false);
            var roomWidth = (points.First() - points.Last()).DotProduct(otherDir);
            roomWidth = Math.Abs(roomWidth);
            //按照UCS对房间内的分割区域分组
            var groupByUcs = new List<AreaLayoutGroup>();
            var tempDivisions = new List<DivisionRoomArea>();
            tempDivisions.AddRange(insertAreas);

            while (tempDivisions.Count > 0) 
            {
                var first = tempDivisions.First();
                bool isArc = first.divisionArea.IsArc;
                tempDivisions.Remove(first);
                var thisGroupAreas = new List<DivisionRoomArea>();
                string ucsId = Guid.NewGuid().ToString();
                foreach (var item in tempDivisions) 
                {
                    if (item.divisionArea.IsArc != isArc)
                        continue;
                    bool isUcs = false;
                    if (isArc)
                    {
                        isUcs = item.divisionArea.ArcCenterPoint.DistanceTo(first.divisionArea.ArcCenterPoint) < 1;
                    }
                    else
                    {
                        var angle = first.divisionArea.XVector.GetAngleTo(item.divisionArea.XVector);
                        angle = angle % Math.PI;
                        if (angle < Math.PI / 180.0)
                            isUcs = true;
                        else if (angle > (Math.PI * 179.0 / 180.0))
                            isUcs = true;
                        else if (Math.Abs(angle - Math.PI / 2) < Math.PI / 180.0)
                            isUcs = true;
                    }
                    if (!isUcs)
                        continue;
                    item.UscGroupId = ucsId;
                    thisGroupAreas.Add(item);
                }
                foreach (var rm in thisGroupAreas)
                    tempDivisions.Remove(rm);
                thisGroupAreas.Add(first);
                //计算每个UCS的内部分组
                if (!isArc)//矩形区域横向分组
                {
                    var xVector = first.divisionArea.XVector;
                    var yVector = xVector.CrossProduct(Vector3d.ZAxis);
                    var dotX = xVector.DotProduct(vector);
                    var dotY = yVector.DotProduct(vector);
                    var thisAreaFirstVector = vector;
                    if (Math.Abs(dotX) > Math.Abs(dotY))
                        thisAreaFirstVector = dotX > 0 ? xVector : xVector.Negate();
                    else
                        thisAreaFirstVector = dotY > 0 ? yVector : yVector.Negate();
                    groupByUcs.Add(new AreaLayoutGroup(thisGroupAreas, thisAreaFirstVector, roomWidth));
                }
                else
                {
                    //弧形区域根据输入决定横向还是竖向
                    var thisGroupCenters = thisGroupAreas.Select(c => c.divisionArea.CenterPoint).ToList();
                    var centerPoint = ThPointVectorUtil.PointsAverageValue(thisGroupCenters);
                    var centerDir = (centerPoint -thisGroupAreas.First().divisionArea.ArcCenterPoint).GetNormal();
                    var angle = centerDir.GetAngleTo(vector);
                    angle %= Math.PI;
                    if (angle > Math.PI / 2)
                        angle = Math.PI - angle;
                    var isByVertical = angle > (Math.PI * 45.0 / 180.0);
                    groupByUcs.Add(new AreaLayoutGroup(thisGroupAreas, vector, roomWidth, isByVertical));
                }
            }
            return groupByUcs;
        }
    }
}
