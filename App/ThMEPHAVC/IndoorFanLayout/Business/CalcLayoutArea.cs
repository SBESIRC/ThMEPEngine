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
        Vector3d _firstDir;
        double _roomUnitLoad = 0.0;
        ThCADCoreNTSSpatialIndex _areaSpatialIndex;
        Dictionary<Polyline, DivisionArea> _areaPLine;

        public CalcLayoutArea(List<DivisionArea> divisionAreas,Vector3d firstDir)
        {
            _allDivisionAreas = new List<DivisionArea>();
            _roomIntersectAreas = new List<DivisionRoomArea>();
            _areaPLine = new Dictionary<Polyline, DivisionArea>();
            _firstDir = firstDir;
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
        public List<AreaLayoutGroup> GetRoomInsterAreas()
        {
            //根据外轮廓获取相交到的轮廓
            var restGroups = new List<AreaLayoutGroup>();
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
            restGroups = CalcLayoutAreaDir();
            //else 
            //    restGroups = CalcLayoutAreaDirByVertical();
            return restGroups;
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
        List<AreaLayoutGroup> CalcLayoutAreaDir() 
        {
            var areaGroups = new List<AreaLayoutGroup>();
            if (null == _roomIntersectAreas || _roomIntersectAreas.Count < 1)
                return areaGroups;
            areaGroups = CalcDivisionAreaGroup(_roomIntersectAreas, _firstDir);
            //根据每个UCS的第一排，计算每个UCS的实际第一排

            //Step1，计算第一排方向上没有其它UCS的分组
            var ucsIds = new List<string>();
            for (int i = 0; i < areaGroups.Count; i++)
            {
                var firstGroup = areaGroups[i];
                bool dirHaveGroup = false;
                for (int j = 0; j < areaGroups.Count; j++) 
                {
                    if (i == j)
                        continue;
                    var secondGroup = areaGroups[j];
                }
                if (dirHaveGroup)
                    continue;
                ucsIds.Add(firstGroup.UcsGroupId);
            }


            //Step2,每个UCS确定第一排方向后，计算其余每一排的方向
            foreach (var group in areaGroups) 
            {
                var thisGroupFirstId = group.GroupFirstId;
                var firstGroupIndex = group.OrderGroupIds.IndexOf(thisGroupFirstId);
                var orientation = group.FirstDir;
                bool isCurrentDir = true;
                for (int j = firstGroupIndex; j >= 0; j--)
                {
                    var currentDir = isCurrentDir ? orientation : orientation.Negate();
                    var currentGroupId = group.OrderGroupIds[j];
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
                            var outVector = (arc.EndPoint - center).GetNormal();
                            if (outVector.DotProduct(currentDir) > 0)
                                item.GroupDir = outVector;
                            else
                                item.GroupDir = outVector.Negate();
                        }
                    }
                    isCurrentDir = !isCurrentDir;
                }
                isCurrentDir = false;
                for (int j = firstGroupIndex + 1; j < group.OrderGroupIds.Count; j++)
                {
                    var currentDir = isCurrentDir ? orientation : orientation.Negate();
                    var currentGroupId = group.OrderGroupIds[j];
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
                            var outVector = (arc.EndPoint - center).GetNormal();
                            if (outVector.DotProduct(currentDir) > 0)
                                item.GroupDir = outVector;
                            else
                                item.GroupDir = outVector.Negate();
                        }
                    }
                }
            }

            return areaGroups;
        }
        //List<AreaLayoutGroup> CalcLayoutAreaDirByVertical()
        //{
        //    var areaGroups = new List<AreaLayoutGroup>();
        //    if (null == _roomIntersectAreas || _roomIntersectAreas.Count < 1)
        //        return areaGroups;
        //    //按照UCS初步分组
        //    areaGroups = CalcDivisionAreaGroup(_roomIntersectAreas, _firstDir, true);
        //    //每个UCS确定第一列后，计算其余每列的方向
        //    foreach(var group in areaGroups)
        //    {
        //        var thisGroupFirstId = group.GroupFirstId;
        //        var firstGroupIndex = group.OrderGroupIds.IndexOf(thisGroupFirstId);
        //        var orientation = group.FirstDir; //_yAxis.Negate();
        //        bool isCurrentDir = true;
        //        for (int j = firstGroupIndex; j >= 0; j--)
        //        {
        //            var currentDir = isCurrentDir ? orientation : orientation.Negate();
        //            var currentGroupId = group.OrderGroupIds[j];
        //            foreach (var item in _roomIntersectAreas)
        //            {
        //                if (item.GroupId != currentGroupId)
        //                    continue;
        //                if (!item.divisionArea.IsArc)
        //                    item.GroupDir = currentDir;
        //                else
        //                {
        //                    var arc = item.divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
        //                    var center = arc.Center;
        //                    var outVector = (arc.EndPoint - center).GetNormal();
        //                    if (outVector.DotProduct(currentDir) > 0)
        //                        item.GroupDir = outVector;
        //                    else
        //                        item.GroupDir = outVector.Negate();
        //                }
        //            }
        //            isCurrentDir = !isCurrentDir;
        //        }
        //        isCurrentDir = false;
        //        for (int j = firstGroupIndex + 1; j < group.OrderGroupIds.Count; j++)
        //        {
        //            var currentDir = isCurrentDir ? orientation : orientation.Negate();
        //            var currentGroupId = group.OrderGroupIds[j];
        //            foreach (var item in _roomIntersectAreas)
        //            {
        //                if (item.GroupId != currentGroupId)
        //                    continue;
        //                if (!item.divisionArea.IsArc)
        //                    item.GroupDir = currentDir;
        //                else
        //                {
        //                    var arc = item.divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
        //                    var center = arc.Center;
        //                    var outVector = (arc.EndPoint - center).GetNormal();
        //                    if (outVector.DotProduct(currentDir) > 0)
        //                        item.GroupDir = outVector;
        //                    else
        //                        item.GroupDir = outVector.Negate();
        //                }
        //            }
        //        }
        //    }
        //    return areaGroups;
        //}
        void CheckGroupInsert(AreaLayoutGroup firstGroup, AreaLayoutGroup secondGroup) 
        {
            var firstRowAreas = firstGroup.GroupDivisionAreas.Where(c => c.GroupId == firstGroup.GroupFirstId).ToList();
            var firstDir = firstGroup.FirstDir;
            bool innerSecond = false;
            var secondAreas = secondGroup.GroupDivisionAreas.Select(c => c.divisionArea).ToList();
            foreach (var area in firstRowAreas) 
            {
                if (innerSecond)
                    break;
                var nearAreas = new List<DivisionArea>();
                foreach (var item in secondAreas) 
                {
                    if (item.CenterPoint.DistanceTo(item.CenterPoint) > 10000)
                        continue;
                    nearAreas.Add(item);
                }
                if (nearAreas.Count < 1)
                    continue;
                if (firstGroup.IsArcGroup)
                {
                    if (secondGroup.IsArcGroup)
                    { }
                    else 
                    { }
                }
                else 
                {
                    if (secondGroup.IsArcGroup)
                    { }
                    else
                    { }
                }
            }
        }
        List<AreaLayoutGroup> CalcDivisionAreaGroup(List<DivisionRoomArea> insertAreas, Vector3d vector)
        {
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
                    groupByUcs.Add(new AreaLayoutGroup(thisGroupAreas, thisAreaFirstVector));
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
                    groupByUcs.Add(new AreaLayoutGroup(thisGroupAreas, vector, isByVertical));
                }
            }
            return groupByUcs;
        }
    }
}
