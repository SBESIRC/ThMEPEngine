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
        FanRectangle _fanRectangle;
        Vector3d _firstDir;
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
        public List<AreaLayoutGroup> GetRoomInsterAreas(Vector3d firstDir, FanRectangle fanRectangle)
        {
            _firstDir = firstDir;
            _fanRectangle = fanRectangle;
            //根据外轮廓获取相交到的轮廓
            var resUCSGroups = new List<AreaLayoutGroup>();
            //CalaRoomInsertAreas();
            if (null == _roomIntersectAreas || _roomIntersectAreas.Count < 1)
                return resUCSGroups;
            resUCSGroups = CalcDivisionAreaGroup(_roomIntersectAreas, firstDir);
            resUCSGroups = CalcLayoutAreaDir(resUCSGroups);
            return resUCSGroups;
        }
        public List<AreaLayoutGroup> CalcLayoutGroupAreaDir(Dictionary<Point3d, Vector3d> hisFanDir)
        {
            var areaUCSGroups = new List<AreaLayoutGroup>();
            //CalaRoomInsertAreas();
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
        public List<DivisionRoomArea> CalaRoomInsertAreas(Vector3d firstDir, out List<DivisionRoomArea> addAreas) 
        {
            _firstDir = firstDir;
            var divisionAreas =new List<DivisionRoomArea>();
            addAreas = new List<DivisionRoomArea>();
            _roomIntersectAreas = new List<DivisionRoomArea>();
            var outGeo = _roomPLine.ToNTSPolygon();
            var targetAreas = GetDivisionAreas(_roomPLine);
            //房间有些区域可能没分割后的区域，这些区域需要根据区域去除后再计算
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
                divisionAreas.Add(layoutFan);
                _roomIntersectAreas.Add(layoutFan);
            }
            Geometry geometry = _roomPLine.ToNTSPolygon();
            foreach (var area in _roomIntersectAreas) 
            {
                var tempGeo = area.divisionArea.AreaPolyline.ToNTSPolygon();
                geometry = geometry.Difference(tempGeo);
            }
            foreach (var item in _roomInnerPLine) 
            {
                var tempGeo = item.ToNTSPolygon();
                geometry = geometry.Difference(tempGeo);
            }
            var needAddAreas = geometry.ToDbObjects(true);
            foreach (var area in needAddAreas) 
            {
                Polyline addPLine = null;
                if (area is Polyline polyline)
                {
                    if (polyline.Area < 100)
                        continue;
                    addPLine = polyline;
                }
                else if (area is MPolygon mPolygon) 
                {
                    if (mPolygon.Area < 100)
                        continue;
                    addPLine =mPolygon.Outline();
                }
                
                var needLoad = CalcAreaLoad(new List<Polyline> { addPLine });
                
                var tempBuffers = addPLine.Buffer(IndoorFanCommon.RoomBufferOffSet);
                if (tempBuffers == null || tempBuffers.Count < 1)
                    continue;
                var tempRoomPLine = tempBuffers[0] as Polyline;
                if (tempRoomPLine == null || tempRoomPLine.Area < 100)
                    continue;
                var addArea = new DivisionArea(false, addPLine);
                //计算区域的UCS,外扩100找相交的区域，判断用那个UCS
                addArea.XVector = GetAddAreaUCS(addPLine);
                var layoutFan = new DivisionRoomArea(addArea);
                layoutFan.RealIntersectAreas.Add(addPLine);
                layoutFan.RoomLayoutAreas.Add(tempRoomPLine);
                layoutFan.NeedLoad = needLoad;
                addAreas.Add(layoutFan);
                _roomIntersectAreas.Add(layoutFan);
            }
            return divisionAreas;
        }
        Vector3d GetAddAreaUCS(Polyline addPLine) 
        {
            var pl = (addPLine.Buffer(100)[0] as Polyline).ToNTSGeometry();
            if (_roomIntersectAreas.Count < 1)
                return _firstDir;
            var vectors = new List<Vector3d>();
            var vectorAreas = new Dictionary<Vector3d, double>();
            foreach (var item in _roomIntersectAreas) 
            {
                var tempGeo = item.divisionArea.AreaPolyline.ToNTSPolygon();
                var interGeo = OverlayNGRobust.Overlay(
                    pl,
                    tempGeo,
                    SpatialFunction.Intersection);
                var res = interGeo.ToDbCollection();
                if (res.Count < 1)
                    continue;
                var xVector = item.divisionArea.XVector;
                var interPolylines = new List<Polyline>();
                foreach (var area in res)
                {
                    if (area is Polyline polyline)
                    {
                        if (polyline.Area < 100)
                            continue;
                        interPolylines.Add(polyline);
                    }
                    else if (area is Polygon polygon)
                    {
                        if (polygon == null || polygon.Area < 100)
                            continue;
                        interPolylines.Add(polygon.Shell.ToDbPolyline());
                    }
                }
                if (interPolylines.Count < 1)
                    continue;
                var dArea = interPolylines.Sum(c => c.Area);
                vectors.Add(xVector);
                bool isAdd = true;
                foreach (var keyValue in vectorAreas) 
                {
                    if (Math.Abs(keyValue.Key.DotProduct(xVector)) > 0.999)
                    {
                        vectorAreas[keyValue.Key] += dArea;
                        isAdd = false;
                        break;
                    }
                }
                if (isAdd)
                    vectorAreas.Add(xVector, dArea);
            }
            if (vectors.Count < 1)
                return _roomIntersectAreas.First().divisionArea.XVector;
            var dir = vectorAreas.OrderByDescending(c => c.Value).First().Key; //vectors.GroupBy(c => c).ToDictionary(c => c.Key, x => x.Count()).OrderByDescending(c => c.Value).First().Key;
            return dir;

        }
        List<DivisionArea> GetDivisionAreas(Polyline roomOutPLine) 
        {
            var resList = new List<DivisionArea>();
            if (_areaSpatialIndex == null || _allDivisionAreas.Count < 1)
                return resList;
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
            roomOutInseterAreas.Clear();
            foreach (var item in thisOutAreas)
            {
                if (item is Polyline polyline)
                {
                    area += polyline.Area;
                    roomOutInseterAreas.Add(polyline);
                }
                else if (item is MPolygon mPolygon)
                {
                    area += mPolygon.Area;
                    roomOutInseterAreas.Add(mPolygon.Outline());
                }
            }
            return area * _roomUnitLoad;
        }
        List<AreaLayoutGroup> CalcLayoutAreaDir(List<AreaLayoutGroup> areaUCSGroups) 
        {
            if (null == areaUCSGroups || areaUCSGroups.Count < 1)
                return areaUCSGroups;
            areaUCSGroups = areaUCSGroups.OrderByDescending(c => c.UCSGroupLayoutArea).ToList();
            //根据每个UCS的第一排，计算每个UCS的实际第一排
            //多个UCS时，按照面积最大的UCS区域为准
            string baseUCSId = areaUCSGroups.First().UcsGroupId;
            var hisGroupIds = new List<string>();
            hisGroupIds.Add(baseUCSId);
            //确定第一个UCS中每排的方向
            CalcAreaRowDir(areaUCSGroups, new List<string> { baseUCSId });
            bool haveChange = true;
            while (haveChange) 
            {
                haveChange = false;
                List<string> hisAearIds = new List<string>(); 
                foreach (var group in areaUCSGroups)
                {
                    if (!hisGroupIds.Any(c => c == group.UcsGroupId))
                        continue;
                    foreach (var area in group.GroupDivisionAreas)
                        hisAearIds.Add(area.divisionArea.Uid);
                }
                string changeAreaId = "";
                foreach (var group in areaUCSGroups)
                {
                    if (hisGroupIds.Any(c => c == group.UcsGroupId))
                        continue;
                    string nearAreaId = "";
                    //计算是否有和已经有区域的地方进行
                    int firstIndex = -1;
                    for (int i = 0; i < group.OrderGroupIds.Count; i++)
                    {
                        var rowId = group.OrderGroupIds[i];
                        var rowAreas = group.GroupDivisionAreas.Where(c => c.GroupId == rowId).ToList();
                        var tempAreas = new List<DivisionArea>();
                        foreach (var area in rowAreas) 
                        {
                            if (area.RealIntersectAreas.Sum(c => c.Area) < 10000)
                                continue;
                            var bfArea = area.divisionArea.AreaPolyline.Buffer(10)[0] as Polyline;
                            var firstCenter = area.divisionArea.CenterPoint;
                            var interPLines = _areaSpatialIndex.SelectCrossingPolygon(bfArea);
                            foreach (var item in interPLines)
                            {
                                if (item is Polyline polyline) 
                                {
                                    if (polyline.Area < 150)
                                        continue;
                                    var pl = item as Polyline;
                                    var addArea = _areaPLine[pl];
                                    if (!hisAearIds.Any(c => c == addArea.Uid))
                                        continue;
                                    var addCenter = addArea.CenterPoint;
                                    var vector = addCenter - firstCenter;
                                    var testVector = vector.GetNormal();
                                    var checkDot = Math.Abs(vector.DotProduct(group.FirstDir));
                                    if (checkDot / vector.Length > 0.5)
                                        continue;
                                    tempAreas.Add(addArea);
                                }
                            }
                        }
                        if (tempAreas.Count < 1)
                            continue;
                        firstIndex = i;
                        nearAreaId = tempAreas.First().Uid;
                        break;
                    }
                    //计算第一排方向
                    foreach (var hisGroup in areaUCSGroups)
                    {
                        if (!hisGroupIds.Any(c => c == hisGroup.UcsGroupId))
                            continue;
                        bool isBreak = false;
                        foreach (var area in hisGroup.GroupDivisionAreas)
                        {
                            if (area.divisionArea.Uid == nearAreaId)
                            { 
                                group.FirstRowDir = area.GroupDir.DotProduct(group.FirstDir)>0 ? group.FirstDir: group.FirstDir.Negate();
                                isBreak = true;
                                break;
                            }
                        }
                        if (isBreak)
                            break;
                    }
                    group.GroupFirstId = group.OrderGroupIds[firstIndex];
                    changeAreaId = group.UcsGroupId;
                    haveChange = true;
                    hisGroupIds.Add(group.UcsGroupId);
                    break;
                }
                if (haveChange) 
                {
                    CalcAreaRowDir(areaUCSGroups, new List<string> { changeAreaId });
                    continue;
                }
                if (hisGroupIds.Count == areaUCSGroups.Count)
                    break;
                //再次以剩余的面积最大的进行排布
                string id = "";
                foreach (var item in areaUCSGroups) 
                {
                    if (hisGroupIds.Any(c => c == item.UcsGroupId))
                        continue;
                    id = item.UcsGroupId;
                    break;
                }
                if (string.IsNullOrEmpty(id))
                    break;
                CalcAreaRowDir(areaUCSGroups, new List<string> { id });
            }
            return areaUCSGroups;
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
                    groupByUcs.Add(new AreaLayoutGroup(thisGroupAreas, thisAreaFirstVector, roomWidth,_fanRectangle.MinLength));
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
                    groupByUcs.Add(new AreaLayoutGroup(thisGroupAreas, vector, roomWidth, _fanRectangle.MinLength, isByVertical));
                }
            }
            return groupByUcs;
        }

        void CalcAreaRowDir(List<AreaLayoutGroup> areaUCSGroups,List<string> calcIds)
        {
            foreach (var group in areaUCSGroups)
            {
                if (!calcIds.Any(c=>c == group.UcsGroupId))
                    continue;
                var thisGroupFirstId = group.GroupFirstId;
                if (string.IsNullOrEmpty(thisGroupFirstId))
                    continue;
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
        }
    }
}
