using AcHelper.Commands;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
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
        Vector3d _xAxis;
        Vector3d _yAxis;
        CalcFanRectFormFanData fanRectFormFanData;
        public IndoorFanLayoutCmd(Dictionary<Polyline, List<Polyline>> selectRoomLines,Vector3d xAxis,Vector3d yAxis) 
        {
            CommandName = "THSNJBZ";
            ActionName = "室内机布置";

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
            var indoorFanData = new ThIndoorFanData(_originTransformer);
            //获取轴网线，根据轴网计算分割区域
            var allAxis = indoorFanData.GetAllAxisCurves();
            var showCurves = new List<Curve>();
            var fanTexts = new List<DBText>();
            var area = new AxisSimpleRegion(allAxis, new List<Polyline>());
            //首先根据线段分坐标系
            var areaRegion = area.AxisSimpleResults(3500, 100, new List<AreaRegionType>());

            //获取房间负荷信息
            var thRoomLoadTool = new ThRoomLoadTable(_originTransformer);
            var allRoomLoads = thRoomLoadTool.GetAllRoomLoadTable();
            thRoomLoadTool.CreateSpatialIndex(allRoomLoads);
            if (null == allRoomLoads || allRoomLoads.Count < 1)
                return;

            

            var allLeadLines = indoorFanData.GetAllLeadLine();
            var calcAreaNear = new CalcRegionAdjacent(areaRegion);
            foreach (var item in areaRegion) 
            {
                continue;
                showCurves.Add(item.AreaPolyline);
            }

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
                    var layoutAreas = new List<AreaLayoutGroup>();
                    string fanName = "";
                    var correctionFactor = IndoorFanParameter.Instance.LayoutModel.CorrectionFactor;
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
                            hisFanDir.Add(point,dir);
                            fanName = block.Id.GetAttributeInBlockReference("设备编号");
                        }
                        if (string.IsNullOrEmpty(fanName))
                            continue;
                        layoutAreas = calcLayoutArea.CalcLayoutGroupAreaDir(hisFanDir);
                    }
                    else
                        layoutAreas = calcLayoutArea.GetRoomInsterAreas(dir);
                    if (layoutAreas.Count < 1)
                        continue;
                    if (!isHisDir) 
                    {
                        var canUseFans = RoomCalcFanNumber(layoutAreas, calcLayoutArea.RoomUnitLoad);
                        if (canUseFans.Count < 1)
                            continue;
                        fanName = canUseFans.First();
                    }
                    if (string.IsNullOrEmpty(fanName))
                        continue;
                    var rectangle = fanRectFormFanData.GetFanRectangle(fanName, correctionFactor);
                    var fanLayout = new AreaLayoutFan(nearRelation, _xAxis, _yAxis.Negate());
                    fanLayout.InitRoomData(layoutAreas,pline.Key, pline.Value, roomLoad);
                    var layoutRectRes = fanLayout.GetLayoutFanResult(rectangle);
                    if (null == layoutRectRes || layoutRectRes.Count < 1)
                        continue;
                    int thisAreaCount = 0;
                    var layoutResultCheck = new LayoutResultCheck(layoutRectRes, roomLoad, rectangle.Load);
                    var delFanIds = layoutResultCheck.GetDeleteFan();
                    foreach (var item in layoutRectRes)
                    {
                        //showCurves.Add(item.divisionArea.AreaPolyline);
                        string msg = string.Format("{0}kW/{1}kW ={2}台 行{3}", item.NeedLoad.ToString("N2"), rectangle.Load, item.NeedFanCount.ToString(), item.RowCount);
                        var dbText = new DBText()
                        {
                            TextString = msg,
                            Height = 350,
                            WidthFactor = 0.7,
                            HorizontalMode = TextHorizontalMode.TextLeft,
                            Oblique = 0,
                            Position = item.divisionArea.CenterPoint,
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
                                if (delFanIds.Any(c => c == fan.FanId))
                                {
                                    continue;
                                }
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
        List<string> RoomCalcFanNumber(List<AreaLayoutGroup> roomInsterAreas,double roomUnitLoad) 
        {
            double maxInsterArea = 0.0;
            double maxArea = 0.0;
            double maxRatio = 0.0;
            //判断房间是否有标准的分割区域
            foreach (var group in roomInsterAreas) 
            {
                foreach (var area in group.GroupDivisionAreas) 
                {
                    var thisInsterArea = area.RealIntersectAreas.Sum(c => c.Area);
                    var thisArea = area.divisionArea.AreaPolyline.Area;
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
            using (var acdb = AcadDatabase.Active())
            {
                foreach (var region in showCurves)
                {
                    continue;
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
    
        
    }
}
