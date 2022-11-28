using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Common;
using ThMEPHVAC.IndoorFanLayout;
using ThMEPHVAC.IndoorFanLayout.Business;
using ThMEPHVAC.IndoorFanLayout.DataEngine;
using ThMEPHVAC.IndoorFanLayout.Models;
using ThMEPHVAC.IndoorFanModels;
using ThMEPHVAC.Model;
using ThMEPHVAC.ParameterService;

namespace ThMEPHVAC.Command
{
    class IndoorFanChange : ThMEPBaseCommand, IDisposable
    {
        Dictionary<Polyline, List<Polyline>> _selectPLines;
        List<FanLoadBase> _allFanLoad;
        ThMEPOriginTransformer _originTransformer;
        public List<Polyline> ErrorRoomPolylines;
        CalcFanRectFormFanData fanRectFormFanData;
        ThIndoorFanData _indoorFanData;
        Dictionary<ObjectId, Line> hisFanPipes = new Dictionary<ObjectId, Line>();
        Dictionary<ObjectId, Line> hisFanReducings = new Dictionary<ObjectId, Line>();
        Dictionary<ObjectId, Polyline> hisFanBoxPolylines = new Dictionary<ObjectId, Polyline>();
        List<ObjectId> delOldBlockIds = new List<ObjectId>();
        List<ObjectId> delGroupIds = new List<ObjectId>();
        List<ObjectId> delEntityIds = new List<ObjectId>();
        List<IndoorFanVentBlock> hisIndoorFanVents;
        List<IndoorFanBlock> hisIndoorFans;
        List<string> airSupplyOutlet = new List<string>
        {
            "送风口",
            "散流器",
            "圆形风口"
        };
        public IndoorFanChange(Dictionary<Polyline, List<Polyline>> selectRoomLines) 
        {
            CommandName = "THSNJJHXG";
            ActionName = "室内机校核修改";

            ErrorRoomPolylines = new List<Polyline>();
            _selectPLines = new Dictionary<Polyline, List<Polyline>>();
            if (null == selectRoomLines || selectRoomLines.Count < 1)
                return;
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
            if (null != IndoorFanParameter.Instance.ChangeLayoutModel)
                indoorFans = IndoorFanParameter.Instance.ChangeLayoutModel.TargetFanInfo;
            fanRectFormFanData = new CalcFanRectFormFanData(indoorFans);
            _allFanLoad = new List<FanLoadBase>();
            CalcFanLoad(indoorFans);
        }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            //获取选中框线中的风机，风口，风箱，计算风机的位置大小
            if (null == _selectPLines || _selectPLines.Count < 1)
                return;
            _indoorFanData = new ThIndoorFanData(_originTransformer);
            using (var acdb = AcadDatabase.Active())
            {
                IndoorFanBlockServices.LoadBlockLayerToDocument(acdb.Database);
            }
            var showCurves = new List<Curve>();
            var fanTexts = new List<DBText>();
            InitData();
            //获取房间负荷信息
            var thRoomLoadTool = new ThRoomLoadTable(_originTransformer);
            var allRoomLoads = thRoomLoadTool.GetAllRoomLoadTable();
            thRoomLoadTool.CreateSpatialIndex(allRoomLoads);
            if (null == allRoomLoads || allRoomLoads.Count < 1)
                return;
            var allLeadLines = _indoorFanData.GetAllLeadLine();
            bool isCool = IndoorFanParameter.Instance.ChangeLayoutModel.HotColdType == EnumHotColdType.Cold;
            var fanType = IndoorFanParameter.Instance.ChangeLayoutModel.FanType;
            var returnType = IndoorFanParameter.Instance.ChangeLayoutModel.AirReturnType;
            var correctionFactor = IndoorFanParameter.Instance.ChangeLayoutModel.CorrectionFactor;
            using (var acdb = AcadDatabase.Active())
            {
                var fanChanges = new List<FanLayoutDetailed>();
                foreach (var pline in _selectPLines)
                {
                    //根据房间框线、负荷表、引线获取该房间的负荷表
                    var roomLoads = thRoomLoadTool.GetIndexTables(pline.Key);
                    roomLoads = thRoomLoadTool.GetRoomInnerTables(pline.Key, roomLoads);
                    if (roomLoads == null || roomLoads.Count < 1)
                        roomLoads = thRoomLoadTool.GetRoomLeadTables(pline.Key, allRoomLoads, allLeadLines);
                    if (roomLoads == null || roomLoads.Count < 1)
                        continue;
                    bool haveValue = IndoorFanCommon.RoomLoadTableReadLoad(roomLoads.First(),isCool, out double roomArea, out double roomLoad);
                    if (!haveValue)
                        continue;
                    var roomFans = GetRoomIndoorFans(pline.Key);
                    if (roomFans.Count < 1)
                        continue;
                    var realLoad = isCool ? roomFans.Sum(c => c.CoolLoad) : roomFans.Sum(c => c.HotLoad);
                    int realCount = roomFans.Count;
                    var oneFanLoad = roomLoad / realCount;
                    var canUseFans = _allFanLoad.Where(c => (isCool && oneFanLoad <= c.FanRealCoolLoad) || (!isCool && oneFanLoad <= c.FanRealHotLoad)).ToList();
                    if (canUseFans.Count < 1)
                    {
                        //房间标记，
                        var roomPoints = IndoorFanCommon.GetPolylinePoints(pline.Key);
                        var points = new Point3dCollection();
                        roomPoints.ForEach(c => points.Add(c));
                        var addPLine = ThCADCoreNTSPoint3dCollectionExtensions.ConvexHull(points).ToDbCollection().OfType<Polyline>().FirstOrDefault();
                        if (null != addPLine)
                        {
                            if (null != _originTransformer)
                                _originTransformer.Reset(addPLine);
                            addPLine.Color = IndoorFanCommon.RoomLoadNotEnoughLineColor;
                            ErrorRoomPolylines.Add(addPLine);
                        }
                        continue;
                    }
                    string fanName = canUseFans.OrderBy(c=>c.FanRealCoolLoad).First().FanNumber;
                    var rectangle = fanRectFormFanData.GetFanRectangle(fanName, fanType, isCool, correctionFactor,returnType);
                    double width = rectangle.Width;
                    var changes = new List<FanLayoutDetailed>();
                    switch (fanType) 
                    {
                        case EnumFanType.FanCoilUnitFourControls:
                        case EnumFanType.FanCoilUnitTwoControls:
                            changes = CoilFanChange(roomFans, rectangle);
                            break;
                        case EnumFanType.VRFConditioninConduit:
                            changes = VRFFanChange(roomFans, rectangle);
                            break;
                        case EnumFanType.VRFConditioninFourSides:
                            changes = VRFFourSideFanChange(roomFans, rectangle);
                            break;
                    }
                    if (changes.Count < 1)
                        continue;
                    fanChanges.AddRange(changes);
                }
                var fanRectangleToBlock = new FanRectangleToBlock(_allFanLoad, _originTransformer, IndoorFanParameter.Instance.ChangeLayoutModel);
                fanRectangleToBlock.AddBlock(fanChanges, IndoorFanParameter.Instance.ChangeLayoutModel.FanType);
                var ids = new ObjectIdList();
                foreach (var groupId in delGroupIds) 
                {
                    ids.Add(groupId);
                }
                ThDuctPortsDrawService.ClearGraphs(ids);
                foreach (var blockId in delOldBlockIds)
                {
                    BlockReference block = acdb.Element<BlockReference>(blockId);
                    block.UpgradeOpen();
                    block.Erase();
                }
                foreach (var delId in delEntityIds)
                {
                    var entity = acdb.Element<DBObject>(delId);
                    entity.UpgradeOpen();
                    entity.Erase();
                }
            }
        }
        void InitData()
        {
            delOldBlockIds.Clear();
            delGroupIds.Clear();
            hisFanPipes.Clear();
            hisFanReducings.Clear();
            GetFanPipeConnect();
            hisIndoorFanVents = _indoorFanData.GetIndoorFanVent();
            hisIndoorFans = _indoorFanData.GetIndoorFanBlockModels();
            hisFanBoxPolylines = _indoorFanData.GetIndoorFanPolylines();
        }

        List<FanLayoutDetailed> CoilFanChange(List<IndoorFanBlock> roomFans,FanRectangle rectangle) 
        {
            var fanChanges = new List<FanLayoutDetailed>();
            var fanLoad = _allFanLoad.Where(c => c.FanNumber == rectangle.Name).FirstOrDefault();
            //计算每个风机的位置，大小,和对应的风口位置
            foreach (var item in roomFans)
            {
                delOldBlockIds.Add(item.FanBlockId);
                var posion = item.BlockPosion;
                var angle = item.FanBlock.Rotation;
                var dir = Vector3d.YAxis.RotateBy(angle, Vector3d.ZAxis);
                //获取，两个方向上的风机,送风口，回风口
                //送风口侧肯定有风箱，根据风箱的中心线找风口
                Line airSupplyLine = GetFanBoxPipe(posion,out double airSupplyLength);
                var ventPoints = GetVentPoints(airSupplyLine);
                //回风口侧，在一定范围内找风口,回风口处可能是风管，也可能是风箱
                var fanStartPoint = posion - dir.MultiplyBy(item.FanLength);
                var fanReturnVentPoints = GetReturnVentPoints(fanStartPoint, dir);
                foreach (var line in hisFanReducings)
                {
                    if (delGroupIds.Any(c => c.Equals(line.Key)))
                        continue;
                    var lineSp = line.Value.StartPoint;
                    var lineEp = line.Value.EndPoint;
                    if (lineSp.DistanceTo(posion) < 200 || lineSp.DistanceTo(fanStartPoint) < 200)
                    {
                        delGroupIds.Add(line.Key);
                    }
                    else if (lineEp.DistanceTo(posion) < 200 || lineEp.DistanceTo(fanStartPoint) < 200)
                    {
                        delGroupIds.Add(line.Key);
                    }
                }
                //回风侧找风箱，或风管计算长度
                double airReturnLength = GetReturnPipeBox(fanStartPoint, dir);
                //计算风机信息
                bool haveReturnVent = fanReturnVentPoints.Count > 0;
                var sp = fanStartPoint ;
                if (haveReturnVent)
                    sp -= dir.MultiplyBy(fanReturnVentPoints.First().DistanceTo(fanStartPoint) + fanLoad.ReturnAirSizeLength/2+100);
                var ep = posion + dir.MultiplyBy(airSupplyLength);
                var fanLayout = new FanLayoutDetailed(sp, ep, rectangle.Width, dir);
                fanLayout.FanLayoutName = rectangle.Name;
                fanLayout.HaveReturnVent = haveReturnVent;
                if(haveReturnVent)
                    fanLayout.FanReturnVentCenterPoint = fanReturnVentPoints.First();
                fanLayout.FanPoint = posion;
                foreach (var vent in ventPoints)
                {
                    fanLayout.FanInnerVents.Add(vent);
                }
                fanChanges.Add(fanLayout);
            }
            return fanChanges;
        }
        List<FanLayoutDetailed> VRFFanChange(List<IndoorFanBlock> roomFans, FanRectangle rectangle) 
        {
            //VRF管道机修改
            var fanChanges = new List<FanLayoutDetailed>();
            var fanLoad = _allFanLoad.Where(c => c.FanNumber == rectangle.Name).FirstOrDefault();
            //计算每个风机的位置，大小,和对应的风口位置
            foreach (var item in roomFans)
            {
                delOldBlockIds.Add(item.FanBlockId);
                var posion = item.BlockPosion;
                var angle = item.FanBlock.Rotation;
                var dir = Vector3d.YAxis.RotateBy(angle, Vector3d.ZAxis);
                //获取，两个方向上的风机,送风口，回风口
                //送风口侧肯定有风箱，根据风箱的中心线找风口
                Line airSupplyLine = GetFanBoxPipe(posion, out double airSupplyLength);
                var ventPoints = GetVentPoints(airSupplyLine);
                //回风口侧，在一定范围内找风口,回风口处可能是风管，也可能是风箱
                var fanStartPoint = posion - dir.MultiplyBy(item.FanLength);
                var fanReturnVentPoints = GetReturnVentPoints(fanStartPoint, dir);
                //回风侧找风箱，或风管计算长度
                double airReturnLength = GetReturnPipeBox(fanStartPoint, dir);
                //计算风机信息
                bool haveReturnVent = fanReturnVentPoints.Count > 0;
                var sp = fanStartPoint;
                if (haveReturnVent)
                    sp -= dir.MultiplyBy(fanReturnVentPoints.First().DistanceTo(fanStartPoint) + fanLoad.ReturnAirSizeLength / 2 + 100);
                var ep = posion + dir.MultiplyBy(airSupplyLength);
                var fanLayout = new FanLayoutDetailed(sp, ep, rectangle.Width, dir);
                fanLayout.FanLayoutName = rectangle.Name;
                fanLayout.HaveReturnVent = haveReturnVent;
                if (haveReturnVent)
                    fanLayout.FanReturnVentCenterPoint = fanReturnVentPoints.First();
                fanLayout.FanPoint = posion;
                foreach (var vent in ventPoints)
                {
                    fanLayout.FanInnerVents.Add(vent);
                }
                fanChanges.Add(fanLayout);
            }
            return fanChanges;
        }
        List<FanLayoutDetailed> VRFFourSideFanChange(List<IndoorFanBlock> roomFans, FanRectangle rectangle) 
        {
            var fanChanges = new List<FanLayoutDetailed>();
            var fanLoad = _allFanLoad.Where(c => c.FanNumber == rectangle.Name).FirstOrDefault();
            //计算每个风机的位置，大小,和对应的风口位置
            foreach (var item in roomFans)
            {
                delOldBlockIds.Add(item.FanBlockId);
                var posion = item.BlockPosion;
                var angle = item.FanBlock.Rotation;
                var dir = Vector3d.YAxis.RotateBy(angle, Vector3d.ZAxis);
                var fanLayout = new FanLayoutDetailed(posion, posion, rectangle.Width, dir);
                fanLayout.FanLayoutName = rectangle.Name;
                fanLayout.HaveReturnVent = false;
                fanLayout.FanPoint = posion;
                fanChanges.Add(fanLayout);
            }
            return fanChanges;
        }
        Line GetFanBoxPipe(Point3d fanPoint,out double airSupplyLength) 
        {
            Line airSupplyLine = null;
            airSupplyLength = 0.0;
            foreach (var line in hisFanPipes)
            {
                if (delGroupIds.Any(c => c.Equals(line.Key)))
                    continue;
                var lineSp = line.Value.StartPoint;
                var lineEp = line.Value.EndPoint;
                if (lineSp.DistanceTo(fanPoint) < 200)
                {
                    airSupplyLine = new Line(lineSp, lineEp);
                    airSupplyLength = lineEp.DistanceTo(fanPoint);
                    delGroupIds.Add(line.Key);
                    break;
                }
                else if (lineEp.DistanceTo(fanPoint) < 200)
                {
                    airSupplyLine = new Line(lineEp, lineSp);
                    airSupplyLength = lineSp.DistanceTo(fanPoint);
                    delGroupIds.Add(line.Key);
                    break;
                }
            }
            return airSupplyLine;
        }
        List<Point3d> GetVentPoints(Line pipeCenterLine)
        {
            var ventPoints = new List<Point3d>();
            if (null == pipeCenterLine)
                return ventPoints;
            //根据线获取长度，获取风口中心点位置
            foreach (var vent in hisIndoorFanVents)
            {
                if (string.IsNullOrEmpty(vent.VentName) || !airSupplyOutlet.Any(c=>vent.VentName.Contains(c)))
                    continue;
                if (!ThPointVectorUtil.PointInLineSegment(vent.BlockPosion, pipeCenterLine, 1, 100))
                    continue;
                delOldBlockIds.Add(vent.VentBlockId);
                var prjPoint = ThPointVectorUtil.PointToLine(vent.BlockPosion, pipeCenterLine);
                ventPoints.Add(prjPoint);
            }
            return ventPoints;
        }
        List<Point3d> GetReturnVentPoints(Point3d fanStartPoint,Vector3d dir) 
        {
            var fanReturnVentPoints = new List<Point3d>();//风口一般是只有一个
            foreach (var vent in hisIndoorFanVents)
            {
                if (string.IsNullOrEmpty(vent.VentName) || !vent.VentName.Contains("回"))
                    continue;
                var prjPoint = ThPointVectorUtil.PointToLine(vent.BlockPosion, fanStartPoint, dir);
                if (prjPoint.DistanceTo(vent.BlockPosion) > 100)
                    continue;
                if (prjPoint.DistanceTo(fanStartPoint) > 800)
                    continue;
                delOldBlockIds.Add(vent.VentBlockId);
                fanReturnVentPoints.Add(prjPoint);
                break;
            }
            return fanReturnVentPoints;
        }
        double GetReturnPipeBox(Point3d fanStartPoint,Vector3d dir) 
        {
            double airReturnLength = 0.0;
            foreach (var line in hisFanPipes)
            {
                if (delGroupIds.Any(c => c.Equals(line.Key)))
                    continue;
                //找风管
                var lineSp = line.Value.StartPoint;
                var lineEp = line.Value.EndPoint;
                if (lineSp.DistanceTo(fanStartPoint) < 200)
                {
                    airReturnLength = lineEp.DistanceTo(fanStartPoint);
                    delGroupIds.Add(line.Key);
                    break;
                }
                else if (lineEp.DistanceTo(fanStartPoint) < 200)
                {
                    airReturnLength = lineSp.DistanceTo(fanStartPoint);
                    delGroupIds.Add(line.Key);
                    break;
                }
            }
            if (airReturnLength > 100)
                return airReturnLength;
            //没有找到有风管。近一步判断是否有风箱
            foreach (var keyValue in hisFanBoxPolylines)
            {
                if (delGroupIds.Any(c => c.Equals(keyValue.Key)))
                    continue;
                var allPoints = IndoorFanCommon.GetPolylinePoints(keyValue.Value);
                var centerPoint = ThPointVectorUtil.PointsAverageValue(allPoints);
                var prjPoint = ThPointVectorUtil.PointToLine(centerPoint, fanStartPoint, dir);
                if (prjPoint.DistanceTo(centerPoint) > 100)
                    continue;
                if (prjPoint.DistanceTo(fanStartPoint) > 800)
                    continue;
                delEntityIds.Add(keyValue.Key);
                allPoints = ThPointVectorUtil.PointsOrderByDirection(allPoints, dir, false, fanStartPoint);
                airReturnLength = Math.Abs((allPoints.Last() - fanStartPoint).DotProduct(dir));
                break;
            }
            return airReturnLength;
        }
        List<IndoorFanBlock> GetRoomIndoorFans(Polyline roomPLine) 
        {
            var roomFans = new List<IndoorFanBlock>();
            if (null == hisIndoorFans || hisIndoorFans.Count < 1)
                return roomFans;
            foreach (var block in hisIndoorFans)
            {
                if (block.FanType != IndoorFanParameter.Instance.ChangeLayoutModel.FanType)
                    continue;
                if (delOldBlockIds.Any(c => c.Equals(block.FanBlockId)))
                    continue;
                var point = block.BlockPosion;
                if (!roomPLine.Contains(point))
                    continue;
                roomFans.Add(block);
            }
            return roomFans;
        }
        void GetFanPipeConnect() 
        {
            var allPipes = ThHvacGetComponent.ReadDuctIds();
            var allConnect = ThHvacGetComponent.ReadConnectorIds();
            foreach (var id in allPipes)
            {
                var param = ThHvacAnalysisComponent.GetDuctParamById(id);
                var sp = new Point3d(param.sp.X, param.sp.Y, 0);
                sp = _originTransformer.Transform(sp);
                var ep = new Point3d(param.ep.X, param.ep.Y, 0);
                ep = _originTransformer.Transform(ep);
                hisFanPipes.Add(id, new Line(sp, ep));
            }
            foreach (var id in allConnect)
            {
                if (delGroupIds.Any(c => c.Equals(id)))
                    continue;
                var param = ThHvacAnalysisComponent.GetConnectorParamById(id);
                if (param == null || param.portWidths == null || param.portWidths.Count < 1)
                    continue;
                if (param.type != "Reducing")
                    continue;
                var sp = param.portWidths.First().Key;
                var ep = param.portWidths.Last().Key;
                sp = new Point3d(sp.X, sp.Y, 0);
                sp = _originTransformer.Transform(sp);
                ep = new Point3d(ep.X, ep.Y, 0);
                ep = _originTransformer.Transform(ep);
                hisFanReducings.Add(id, new Line(sp, ep));
            }
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
                        IndoorFanParameter.Instance.ChangeLayoutModel.FanType,
                        IndoorFanParameter.Instance.ChangeLayoutModel.HotColdType,
                        IndoorFanParameter.Instance.ChangeLayoutModel.CorrectionFactor));
                }
                else if (item is VRFFan vrfFan)
                {
                    _allFanLoad.Add(new VRFImpellerFanLoad(
                        vrfFan,
                        IndoorFanParameter.Instance.ChangeLayoutModel.FanType,
                        IndoorFanParameter.Instance.ChangeLayoutModel.HotColdType,
                        IndoorFanParameter.Instance.ChangeLayoutModel.CorrectionFactor));
                }
            }
        }
    }
}
