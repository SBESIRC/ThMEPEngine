using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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
            var showPLines = new List<Polyline>();
            var centerCircles = new List<Curve>();
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
                showPLines.Add(item.AreaPolyline);
            }

            Dictionary<string,List<string>> nearRelation = null;
            //横向排列还是竖向排列
            bool isLayoutByVertical = false;
            if (!isLayoutByVertical)
                nearRelation = calcAreaNear.GetDivisionAdjacent();
            else nearRelation = calcAreaNear.GetDivisionAdjacentByVertical(_xAxis);
            var angle = _xAxis.GetAngleTo(Vector3d.XAxis);
            var fanLayoutRects = new List<FanLayoutRect>();
            using (var acdb = AcadDatabase.Active())
            {
                LoadFanBlockServices.LoadBlockLayerToDocument(acdb.Database);
                var dir = _yAxis.Negate();
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
                }
                var calcLayoutArea = new CalcLayoutArea(areaRegion, dir);
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
                    var layoutAreas = calcLayoutArea.GetRoomInsterAreas();
                    if (layoutAreas.Count < 1)
                        continue;
                    var canUseFans = RoomCalcFanNumber(layoutAreas, calcLayoutArea.RoomUnitLoad);
                    if (canUseFans.Count < 1)
                        continue;
                    string fanName = canUseFans.First();
                    var rectangle = fanRectFormFanData.GetFanRectangle(fanName);
                    var fanLayout = new AreaLayoutFan(nearRelation, _xAxis, _yAxis.Negate());
                    fanLayout.InitRoomData(layoutAreas,pline.Key, pline.Value, roomLoad);
                    var layoutRectRes = fanLayout.GetLayoutFanResult(rectangle);
                    if (null == layoutRectRes || layoutRectRes.Count < 1)
                        continue;
                    foreach (var item in layoutRectRes)
                    {
                        //showPLines.Add(item.divisionArea.AreaPolyline);
                        string msg = string.Format("{0}kW/{1}kW ={2}台 行{3}", item.NeedLoad.ToString("N2"), rectangle.Load, item.NeedFanCount.ToString(), item.RowCount);
                        //string msg = string.Format("Id:{0}", item.GroupId);
                        var dbText = new DBText()
                        {
                            TextString = msg,
                            Height = 350,
                            WidthFactor = 0.7,
                            HorizontalMode = TextHorizontalMode.TextLeft,
                            Oblique = 0,
                            Position = item.divisionArea.CenterPoint,
                            Rotation = -angle,
                        };
                        fanTexts.Add(dbText);
                        //continue;
                        if (item.FanLayoutAreaResult == null)
                            continue;
                        foreach (var fanLayoutArea in item.FanLayoutAreaResult)
                        {
                            foreach (var fan in fanLayoutArea.FanLayoutResult)
                            {
                                fan.FanLayoutName = fanName;
                                fanLayoutRects.Add(fan);
                                var lengthLine = fan.LengthLines.First();
                                var test = pline.Key.Trim(lengthLine).OfType<Curve>().ToList();
                                Circle circle = new Circle(fan.CenterPoint, Vector3d.ZAxis, 100);
                                centerCircles.Add(circle);
                                if (fan.FanDirection != null)
                                {
                                    centerCircles.Add(new Line(fan.CenterPoint, fan.CenterPoint + fan.FanDirection.MultiplyBy(1000)));
                                }
                            }
                            //showPLines.AddRange(fanLayoutArea.FanLayoutResult.Select(c => c.FanPolyLine).ToList());
                            //showPLines.AddRange(fanLayoutArea.FanLayoutResult.SelectMany(c => c.InnerVentRects.Select(x=>x.VentPolyline).ToList()).ToList());
                        }
                    }
                }
            }
            var fanRectangleToBlock = new FanRectangleToBlock(_allFanLoad,_originTransformer);
            fanRectangleToBlock.AddBlock(fanLayoutRects, IndoorFanParameter.Instance.LayoutModel.FanType);
            //return;
            //将计算后的排布矩形转换为具体的块
            using (var acdb = AcadDatabase.Active())
            {
                foreach (var region in showPLines)
                {
                    //continue;
                    if (region == null)
                        continue;
                    var copy = region.Clone() as Polyline;
                    //copy.ColorIndex = 2;
                    if (null != _originTransformer)
                        _originTransformer.Reset(copy);
                    acdb.ModelSpace.Add(copy);
                }
                foreach (var region in centerCircles)
                {
                    continue;
                    if (region == null)
                        continue;
                    var copy = region.Clone() as Curve;
                    if (null != _originTransformer)
                        _originTransformer.Reset(copy);
                    acdb.ModelSpace.Add(copy);
                }
                foreach (var text in fanTexts) 
                {
                    var dbText = text.Clone() as DBText;
                    if (null != _originTransformer)
                        _originTransformer.Reset(dbText);
                    acdb.ModelSpace.Add(dbText);
                }
            }
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
            bool haveMaxFan = !IndoorFanParameter.Instance.LayoutModel.MaxFanTypeIsAuto;
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
    }
}
