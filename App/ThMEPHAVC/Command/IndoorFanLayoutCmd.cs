using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.GridOperation;
using ThMEPEngineCore.GridOperation.Model;
using ThMEPEngineCore.UCSDivisionService.DivisionMethod;
using ThMEPHVAC.Common;
using ThMEPHVAC.IndoorFanLayout;
using ThMEPHVAC.IndoorFanLayout.Business;
using ThMEPHVAC.IndoorFanLayout.DataEngine;
using ThMEPHVAC.IndoorFanLayout.Models;
using ThMEPHVAC.IndoorFanModels;
using ThMEPHVAC.ParameterService;

namespace ThMEPHVAC.Command
{
    class IndoorFanLayoutCmd : ThMEPBaseCommand, IDisposable
    {
        Dictionary<Polyline, List<Polyline>> _selectPLines;
        List<FanLoadBase> _allFanLoad;
        ThMEPOriginTransformer _originTransformer;
        ThCADCoreNTSSpatialIndex _curveSpatialIndex;
        ThCADCoreNTSSpatialIndex _areaSpatialIndex;
        Vector3d _xAxis;
        Vector3d _yAxis;
        CalcFanRectFormFanData fanRectFormFanData;
        ThIndoorFanData indoorFanData;
        bool onlyShowAxis = false;
        public List<Polyline> ErrorRoomPolylines;
        public IndoorFanLayoutCmd(Dictionary<Polyline, List<Polyline>> selectRoomLines,Vector3d xAxis,Vector3d yAxis,bool onlyShowArea) 
        {
            onlyShowAxis = onlyShowArea;
            CommandName = "THSNJBZ";
            ActionName = "室内机布置";
            ErrorRoomPolylines = new List<Polyline>();

            _selectPLines = new Dictionary<Polyline, List<Polyline>>();
            if (null == selectRoomLines || selectRoomLines.Count < 1)
                return;
            _xAxis = xAxis;
            _yAxis = yAxis;
            var pt = selectRoomLines.First().Key.StartPoint;
            _originTransformer = new ThMEPOriginTransformer(pt);
            foreach (var pline in selectRoomLines)
            {
                var copyOut = (Polyline)pline.Key.Clone();
                if (null != _originTransformer)
                    _originTransformer.Transform(copyOut);
                var innerPLines = new List<Polyline>();
                if (pline.Value != null)
                {
                    foreach (var item in pline.Value)
                    {
                        var copyInner = (Polyline)item.Clone();
                        if (null != _originTransformer)
                            _originTransformer.Transform(copyInner);
                        innerPLines.Add(copyInner);
                    }
                }
                _selectPLines.Add(copyOut, innerPLines);
            }
            var indoorFans = new List<IndoorFanBase>();
            if (null != IndoorFanParameter.Instance.LayoutModel)
                indoorFans = IndoorFanParameter.Instance.LayoutModel.TargetFanInfo;
            fanRectFormFanData = new CalcFanRectFormFanData(indoorFans);
            _allFanLoad = new List<FanLoadBase>();
            CalcFanLoad(indoorFans);
        }
        void CalcFanLoad(List<IndoorFanBase> indoorFans) 
        {
            _allFanLoad.Clear();
            foreach (var item in indoorFans) 
            {
                if (item is CoilUnitFan unitFan)
                {
                    _allFanLoad.Add(new CoilFanLoad(
                        unitFan,
                        IndoorFanParameter.Instance.LayoutModel.FanType,
                        IndoorFanParameter.Instance.LayoutModel.HotColdType,
                        IndoorFanParameter.Instance.LayoutModel.CorrectionFactor));
                }
                else if (item is VRFFan vrfFan) 
                {
                    _allFanLoad.Add(new VRFImpellerFanLoad(
                        vrfFan,
                        IndoorFanParameter.Instance.LayoutModel.FanType,
                        IndoorFanParameter.Instance.LayoutModel.HotColdType,
                        IndoorFanParameter.Instance.LayoutModel.CorrectionFactor));
                }
            }
        }
        public void Dispose()
        {
            
        }
        public override void SubExecute()
        {
            if (null == _selectPLines || _selectPLines.Count < 1)
                return;
            var showCurves = new List<Curve>();
            var fanTexts = new List<DBText>();

            indoorFanData = new ThIndoorFanData(_originTransformer);
            //获取轴网线，根据轴网计算分割区域,
            //用选中的所有房间的轮廓线构成的AABB，外扩15m获取相交到的线，进行计算区域
            Polyline roomPline = SelectRoomOutPolyline();
            var areaRegion = GetGridDivisonAreas(roomPline, out List<Curve> showRegionCurves);
            if (onlyShowAxis) 
            {
                ShowTestLineText(showRegionCurves, fanTexts);
                return;
            }

            //获取房间负荷信息
            var thRoomLoadTool = new ThRoomLoadTable(_originTransformer);
            var allRoomLoads = thRoomLoadTool.GetAllRoomLoadTable();
            thRoomLoadTool.CreateSpatialIndex(allRoomLoads);
            if (null == allRoomLoads || allRoomLoads.Count < 1)
                return;
            var allLeadLines = indoorFanData.GetAllLeadLine();
            var calcAreaNear = new CalcRegionAdjacent(areaRegion);
            //计算上下左右邻居时只计算选中的房间外轮廓相交到的区域，其它区域不进行计算邻居
            var nearRelation = calcAreaNear.GetDivisionAdjacent();
            var angle = Vector3d.XAxis.GetAngleTo(_xAxis);
            var fanLayoutRects = new List<FanLayoutRect>();
            using (var acdb = AcadDatabase.Active())
            {
                var allHisIndoorFans = indoorFanData.GetIndoorFanBlocks();
                IndoorFanBlockServices.LoadBlockLayerToDocument(acdb.Database);
                var dir = _yAxis.Negate();
                bool isHisDir = false;
                //沿用已布置区域，要先计算该区域内有布置的，一个区域内手动放置后，后续布置不在布置该区域内的风机
                switch (IndoorFanParameter.Instance.LayoutModel.FanDirction) 
                {
                    case EnumFanDirction.North:
                        dir = _yAxis;
                        break;
                    case EnumFanDirction.South:
                        dir = _yAxis.Negate();
                        break;
                    case EnumFanDirction.East:
                        dir = _xAxis;
                        break;
                    case EnumFanDirction.West:
                        dir = _xAxis.Negate();
                        break;
                    case EnumFanDirction.Routesare:
                        isHisDir = true;
                        break;
                }
                var calcLayoutArea = new CalcLayoutArea(areaRegion);
                foreach (var pline in _selectPLines)
                {
                    //showPLines.Add(pline.Key);
                    //continue;
                    //根据房间框线、负荷表、引线获取该房间的负荷表
                    var roomLoads = thRoomLoadTool.GetIndexTables(pline.Key);
                    roomLoads = thRoomLoadTool.GetRoomInnerTables(pline.Key, roomLoads);
                    if (roomLoads == null || roomLoads.Count < 1)
                        roomLoads = thRoomLoadTool.GetRoomLeadTables(pline.Key, allRoomLoads, allLeadLines);
                    if (roomLoads == null || roomLoads.Count < 1)
                        continue;
                    bool haveValue = RoomLoadTableReadLoad(roomLoads.First(),out double roomArea,out double roomLoad);
                    if (!haveValue)
                        continue;
                    calcLayoutArea.InitRoomData(pline.Key, pline.Value,roomArea*1000*1000, roomLoad);
                    string fanName = "";
                    var roomInserterAreas = calcLayoutArea.CalaRoomInsertAreas(dir,out List<DivisionRoomArea> addAreas);
                    if ((roomInserterAreas.Count<1 && addAreas.Count<1)|| roomInserterAreas.Count < 1 && pline.Value.Count>0)
                        continue;
                    if (!isHisDir)
                    {
                        var calcFanAreas = roomInserterAreas.Count > 0 ? roomInserterAreas : addAreas;
                        var canUseFans = RoomCalcFanNumber(calcFanAreas, calcLayoutArea.RoomUnitLoad);
                        if (canUseFans.Count < 1)
                            continue;
                        fanName = canUseFans.First();
                    }
                    var layoutAreas = new List<AreaLayoutGroup>();
                    
                    var correctionFactor = IndoorFanParameter.Instance.LayoutModel.CorrectionFactor;
                    FanRectangle rectangle = null;
                    if (isHisDir)
                    {
                        //沿用已布置时，获取房间框线内的已经布置的风机，计算方向，风机数据根据当前风机的参数进行计算
                        var needBlockName = IndoorFanBlockName(IndoorFanParameter.Instance.LayoutModel.FanType);
                        var hisFanDir = new Dictionary<Point3d, Vector3d>();
                        double realLoad = 0.0;
                        foreach (var block in allHisIndoorFans)
                        {
                            var point = block.Position;
                            point = new Point3d(point.X, point.Y, 0);
                            point = _originTransformer.Transform(point);
                            if (!pline.Key.Contains(point))
                                continue;
                            var blockName = ThMEPXRefService.OriginalFromXref(block.GetEffectiveName());
                            if (blockName != needBlockName)
                                continue;
                            var blockAngle = block.Rotation;
                            dir = Vector3d.YAxis.RotateBy(blockAngle, Vector3d.ZAxis);
                            hisFanDir.Add(point, dir);
                            fanName = block.Id.GetAttributeInBlockReference("设备编号");
                        }
                        if (string.IsNullOrEmpty(fanName))
                            continue;
                        layoutAreas = calcLayoutArea.CalcLayoutGroupAreaDir(hisFanDir);
                        rectangle = fanRectFormFanData.GetFanRectangle(fanName, correctionFactor);
                    }
                    else
                    {
                        rectangle = fanRectFormFanData.GetFanRectangle(fanName, correctionFactor);
                        layoutAreas = calcLayoutArea.GetRoomInsterAreas(dir, rectangle);
                    } 
                    if (layoutAreas.Count < 1 || rectangle == null)
                        continue;
                    //var rectangle = fanRectFormFanData.GetFanRectangle(fanName, correctionFactor);
                    var fanLayout = new AreaLayoutFan(nearRelation, _xAxis, _yAxis.Negate());
                    fanLayout.InitRoomData(layoutAreas,pline.Key, pline.Value, roomLoad);
                    var layoutRectRes = fanLayout.GetLayoutFanResult(rectangle);
                    if (null == layoutRectRes || layoutRectRes.Count < 1)
                        continue;
                    int thisAreaCount = 0;
                    //var layoutResultCheck = new LayoutResultCheck(layoutRectRes, roomLoad, rectangle.Load);
                    //var delFanIds = layoutResultCheck.GetDeleteFanByMinArea();
                    foreach (var item in layoutRectRes)
                    {
                        Point3d textPoint = item.divisionArea.CenterPoint;
                        var allPoints = new List<Point3d>();
                        foreach (var pl in item.RealIntersectAreas) 
                        {
                            allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(pl));
                        }
                        if (allPoints.Count > 0)
                            textPoint = ThPointVectorUtil.PointsAverageValue(allPoints);
                        //showCurves.Add(item.divisionArea.AreaPolyline);
                        string msg = string.Format("{0}kW/{1}kW ={2}台 行{3}", item.NeedLoad.ToString("N2"), rectangle.Load, item.NeedFanCount.ToString(), item.RowCount);
                        //string msg = string.Format("RowId{0}", item.GroupId);
                        var dbText = new DBText()
                        {
                            TextString = msg,
                            Height = 350,
                            WidthFactor = 0.7,
                            HorizontalMode = TextHorizontalMode.TextLeft,
                            Oblique = 0,
                            Position = textPoint,
                            Rotation = angle,
                        };
                        fanTexts.Add(dbText);
                        //continue;
                        if (item.FanLayoutAreaResult == null)
                            continue;
                        foreach (var fanLayoutArea in item.FanLayoutAreaResult)
                        {
                            foreach (var fan in fanLayoutArea.FanLayoutResult)
                            {
                                //if (delFanIds.Any(c => c == fan.FanId))
                                //{
                                //    continue;
                                //}
                                thisAreaCount += 1;
                                fan.FanLayoutName = fanName;
                                fanLayoutRects.Add(fan);
                            }
                            //showCurves.AddRange(fanLayoutArea.FanLayoutResult.Select(c => c.FanPolyLine).ToList());
                            //showCurves.AddRange(fanLayoutArea.FanLayoutResult.SelectMany(c => c.InnerVentRects.Select(x=>x.VentPolyline).ToList()).ToList());
                        }
                    }
                    if (!isHisDir && thisAreaCount < 1) 
                    {
                        //一个都没有排布出来，要根据负荷计算风机型号
                        var canUseFans = RoomFanNumberByLoad(roomLoad);
                        if (canUseFans.Count < 1)
                            continue;
                        fanName = canUseFans.First();
                        rectangle = fanRectFormFanData.GetFanRectangle(fanName, correctionFactor);
                        var addFans = fanLayout.GetRoomCenterFan(rectangle, roomLoad);
                        foreach (var fan in addFans)
                        {
                            fan.FanLayoutName = fanName;
                            fanLayoutRects.Add(fan);
                        }
                        thisAreaCount = addFans.Count;
                    }
                    int roomNeedFanCount = (int)Math.Ceiling(roomLoad / rectangle.Load);
                    var createPoint = IndoorFanCommon.PolylinCenterPoint(pline.Key);
                    string msg1 = string.Format("{0}kW/{1}kW ={2}台 排{3}台", roomLoad, rectangle.Load, roomNeedFanCount, thisAreaCount);
                    var color = Color.FromRgb(255,255,255);
                    if (roomNeedFanCount > thisAreaCount)
                    {
                        var roomPoints = IndoorFanCommon.GetPolylinePoints(pline.Key);
                        Point3dCollection points = new Point3dCollection();
                        roomPoints.ForEach(c => points.Add(c));
                        var addPLine = ThCADCoreNTSPoint3dCollectionExtensions.ConvexHull(points).ToDbCollection().OfType<Polyline>().FirstOrDefault();
                        if (null != addPLine)
                        {
                            if (null != _originTransformer)
                                _originTransformer.Reset(addPLine);
                            ErrorRoomPolylines.Add(addPLine);
                        }
                        color = Color.FromRgb(0, 255, 0);
                    }
                    else if (roomNeedFanCount < thisAreaCount) 
                    {
                        color = Color.FromRgb(255, 0, 0);
                    }
                    var dbText1 = new DBText()
                    {
                        TextString = msg1,
                        Height = 350,
                        Color = color,
                        WidthFactor = 0.7,
                        HorizontalMode = TextHorizontalMode.TextLeft,
                        Oblique = 0,
                        Position = createPoint,
                        Rotation = angle,
                    };
                    fanTexts.Add(dbText1);
                }
            }
            var fanRectangleToBlock = new FanRectangleToBlock(_allFanLoad,_originTransformer);
            fanRectangleToBlock.AddBlock(fanLayoutRects, IndoorFanParameter.Instance.LayoutModel.FanType);
            //将计算后的排布矩形转换为具体的块
            ShowTestLineText(showCurves, fanTexts);
        }

        List<DivisionArea> GetGridDivisonAreas(Polyline roomsAABB,out List<Curve> showCurves)
        {
            showCurves = new List<Curve>();
            var allAreaRegion = new List<DivisionArea>();
            var allAxis = indoorFanData.GetAllAxisCurves();
            if (null == allAxis || allAxis.Count < 1)
                return allAreaRegion;
            var curveObjs = new DBObjectCollection();
            foreach (var item in allAxis)
            {
                curveObjs.Add(item);
            }
            _curveSpatialIndex = new ThCADCoreNTSSpatialIndex(curveObjs);
            var calcCurves = GetCalaAxisCurves(allAxis, roomsAABB);

            var columns = new List<Polyline>();
            var gridLineClean = new GridLineCleanService();
            gridLineClean.CleanGrid(calcCurves, columns, out List<LineGridModel> lineGirds, out List<ArcGridModel> arcGrids);
            var curves = new List<List<Curve>>(lineGirds.Select(x => { var lines = new List<Curve>(x.xLines); lines.AddRange(x.yLines); return lines; }));
            curves.Add(arcGrids.SelectMany(x => { var lines = new List<Curve>(x.arcLines); lines.AddRange(x.lines); return lines; }).ToList());
            curves = curves.Where(x => x.Count > 0).ToList();
            var gridDivision = new GridDivision();
            var ucsPolygons = gridDivision.DivisionGridRegions(curves);
            var areaObjs = new DBObjectCollection();
            var areaDic = new Dictionary<Polyline, DivisionArea>();
            int colorIndex = 0;
           
            foreach (var item in ucsPolygons)
            {
                var isArc = item.gridType == GridType.ArcGrid;
                var centerPoint = item.centerPt;
                foreach (var polyline in item.regions)
                {
                    if (polyline.Area < 10000)
                        continue;
                    if (onlyShowAxis)
                    {
                        polyline.ColorIndex = colorIndex;
                        showCurves.Add(polyline);
                    }
                    var area = new DivisionArea(isArc, polyline);
                    if (isArc)
                        area.ArcCenterPoint = centerPoint;
                    else
                        area.XVector = item.vector;
                    areaObjs.Add(polyline);
                    areaDic.Add(polyline, area);
                    allAreaRegion.Add(area);
                }
                colorIndex += 1;
            }
            if (onlyShowAxis)
                return allAreaRegion;
            _areaSpatialIndex = new ThCADCoreNTSSpatialIndex(areaObjs);
            var crossPL = _areaSpatialIndex.SelectCrossingPolygon(roomsAABB);
            var areaRegion = new List<DivisionArea>();
            foreach (var item in crossPL)
            {
                if (item is Polyline polyline)
                    areaRegion.Add(areaDic[polyline]);
            }
            return areaRegion;
        }
        List<string> RoomCalcFanNumber(List<DivisionRoomArea> roomInsterAreas,double roomUnitLoad) 
        {
            double maxInsterArea = 0.0;
            double maxArea = 0.0;
            double maxRatio = 0.0;
            //判断房间是否有标准的分割区域
            foreach (var group in roomInsterAreas) 
            {
                var thisInsterArea = group.RealIntersectAreas.Sum(c => c.Area);
                var thisArea = group.divisionArea.AreaPolyline.Area;
                var thisRatio = thisInsterArea / thisArea;
                if (thisRatio < maxRatio)
                    continue;
                if (maxArea < thisArea)
                {
                    maxInsterArea = thisInsterArea;
                    maxArea = thisArea;
                    maxRatio = thisRatio;
                }
            }
            //根据房间的内部的闭合区域计算可以使用哪一种风机
            var oneAreaLoad = maxArea* roomUnitLoad;
            var fanCount = new Dictionary<string, int>();
            bool haveMaxFan = IndoorFanParameter.Instance.LayoutModel.MaxFanTypeIsAuto != EnumMaxFanNumber.Auto;
            var maxFanStr = IndoorFanParameter.Instance.LayoutModel.MaxFanType;
            bool isBreak = false;
            foreach (var item in _allFanLoad) 
            {
                if (isBreak)
                    break;
                if (haveMaxFan)
                    isBreak = maxFanStr == item.FanNumber;
                var fanLoad = item.FanLoad;
                var count = (int)Math.Ceiling(oneAreaLoad / fanLoad);
                fanCount.Add(item.FanNumber, count);
            }
            return fanCount.OrderBy(c => c.Value).ThenBy(c => c.Key).Select(c => c.Key).ToList();
        }
        List<string> RoomFanNumberByLoad(double roomLoad)
        {
            //根据房间负荷计算风机
            var fanCount = new Dictionary<string, int>();
            bool haveMaxFan = IndoorFanParameter.Instance.LayoutModel.MaxFanTypeIsAuto != EnumMaxFanNumber.Auto;
            var maxFanStr = IndoorFanParameter.Instance.LayoutModel.MaxFanType;
            bool isBreak = false;
            foreach (var item in _allFanLoad)
            {
                if (isBreak)
                    break;
                if (haveMaxFan)
                    isBreak = maxFanStr == item.FanNumber;
                var fanLoad = item.FanLoad;
                var count = (int)Math.Ceiling(roomLoad / fanLoad);
                fanCount.Add(item.FanNumber, count);
            }
            return fanCount.OrderBy(c => c.Value).ThenBy(c => c.Key).Select(c => c.Key).ToList();
        }
        bool RoomLoadTableReadLoad(Table roomTable,out double roomArea,out double roomLoad) 
        {
            var roomLoadTable = new LoadTableRead();
            roomLoad = 0.0;
            roomArea = 0.0;
            bool haveValue = roomLoadTable.ReadRoomLoad(roomTable, out string roomAreaStr, out string roomLoadStr);
            if (!haveValue)
                return false;
            double.TryParse(roomAreaStr, out roomArea);
            var spliteLoads = roomLoadStr.Split('/').ToList();
            if (spliteLoads.Count < 2)
                return false;
            var roomCoolLoadStr = spliteLoads[0];
            var roomHotLoadStr = spliteLoads[1];
            if (IndoorFanParameter.Instance.LayoutModel.HotColdType == EnumHotColdType.Cold)
            {
                if (string.IsNullOrEmpty(roomCoolLoadStr) || roomCoolLoadStr.Contains("-"))
                    return false;
                double.TryParse(roomCoolLoadStr, out roomLoad);
            }
            else 
            {
                if (string.IsNullOrEmpty(roomHotLoadStr) || roomHotLoadStr.Contains("-"))
                    return false;
                double.TryParse(roomHotLoadStr, out roomLoad);
            }
            return true;
        }
        void ShowTestLineText(List<Curve> showCurves,List<DBText> showTexts) 
        {
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return;
            using (var acdb = AcadDatabase.Active())
            {
                foreach (var region in showCurves)
                {
                    //continue;
                    if (region == null)
                        continue;
                    var copy = region.Clone() as Curve;
                    //copy.ColorIndex = 2;
                    if (null != _originTransformer)
                        _originTransformer.Reset(copy);
                    acdb.ModelSpace.Add(copy);
                }
                foreach (var text in showTexts)
                {
                    var dbText = text.Clone() as DBText;
                    if (null != _originTransformer)
                        _originTransformer.Reset(dbText);
                    acdb.ModelSpace.Add(dbText);
                }
            }
        }
        string IndoorFanBlockName (EnumFanType fanType)
        {
            var blockName = "";
            switch (fanType) 
            {
                case EnumFanType.FanCoilUnitTwoControls:
                    blockName = IndoorFanBlockServices.CoilFanTwoBlackName;
                    break;
                case EnumFanType.FanCoilUnitFourControls:
                    blockName = IndoorFanBlockServices.CoilFanFourBlackName;
                    break;
                case EnumFanType.IntegratedAirConditionin:
                    blockName = IndoorFanBlockServices.AirConditionFanBlackName;
                    break;
                case EnumFanType.VRFConditioninConduit:
                    blockName = IndoorFanBlockServices.VRFFanBlackName;
                    break;
                case EnumFanType.VRFConditioninFourSides:
                    blockName = IndoorFanBlockServices.VRFFanFourSideBlackName;
                    break;
            }
            return blockName;
        }

        List<Curve> GetCalaAxisCurves(List<Curve> allAxisCurves,Polyline roomPolyline) 
        {
            var axisCurves = new List<Curve>();
            var polylines = _selectPLines.Select(c => c.Key).ToList();
            var geo = polylines.First().GeometricExtents;
            
            for (int i = 1; i < polylines.Count; i++) 
            {
                geo.AddExtents(polylines[i].GeometricExtents);
            }
            //根据选中的房间框线，外扩15m找相交到的轴网线
            var polyline = roomPolyline.Buffer(15000)[0] as Polyline;
            var curves= _curveSpatialIndex.SelectCrossingPolygon(polyline);
            foreach (var item in curves) 
            {
                if (item is Curve curve)
                    axisCurves.Add(curve);
            }
            var otherCurves = new List<Curve>();
            foreach(var item in allAxisCurves)
            {
                if (axisCurves.Any(c => c.Equals(item)))
                    continue;
                otherCurves.Add(item);
            }
            //根据相交到的轴线计算相交到的线段，防止区域中有线偏差，获取该区域的中的线
            //如果有多个楼层的轴网 ，把其它楼层的线去除。
            while (true) 
            {
                bool haveAdd = false;
                Curve addCurve = null;
                foreach (var line in otherCurves) 
                {
                    foreach (var hisLine in axisCurves)
                    {
                        if (haveAdd)
                            break;
                        var intersectPoints = line.Intersect(hisLine, Intersect.OnBothOperands);
                        haveAdd = intersectPoints.Count > 0;
                    }
                    if (haveAdd)
                    {
                        addCurve = line;
                        break;
                    }
                }
                if (!haveAdd)
                    break;
                axisCurves.Add(addCurve);
                otherCurves.Remove(addCurve);
            }
            return axisCurves;
        }

        Polyline SelectRoomOutPolyline() 
        {
            var polylines = _selectPLines.Select(c => c.Key).ToList();
            var geo = polylines.First().GeometricExtents;

            for (int i = 1; i < polylines.Count; i++)
            {
                geo.AddExtents(polylines[i].GeometricExtents);
            }
            Polyline polyline = new Polyline();
            var minPt = geo.MinPoint;
            var maxPt = geo.MaxPoint;
            var pt1 = minPt;
            var pt2 = new Point3d(pt1.X, maxPt.Y, 0);
            var pt3 = maxPt;
            var pt4 = new Point3d(pt3.X, pt1.Y, 0);
            polyline.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, pt3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, pt4.ToPoint2D(), 0, 0, 0);
            polyline.Closed = true;
            return polyline;
        }
    }
}
