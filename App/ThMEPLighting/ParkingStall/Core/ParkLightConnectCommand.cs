using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Algorithm.GraphDomain;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Engine;
using ThMEPLighting.ParkingStall.CAD;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ParkingStall.Worker.LightConnect;
using ThMEPLighting.ParkingStall.Worker.LightConnectAdjust;
using ThMEPLighting.ServiceModels;

namespace ThMEPLighting.ParkingStall.Core
{
    class ParkLightConnectCommand : ThMEPBaseCommand, IDisposable
    {
        List<LightBlockReference> _areaLightBlocks;//灯块
        BaseElement _baseElement;
        List<Line> _laneLines;//车道线
        List<Line> _trunkingLines;//线槽线
        List<BlockReference> _alBlocks;
        double _dbScanClusterRadius = 10000;
        int _dbScanClusterSingleMaxCount = 25;
        PolygonInfo _polygonInfo;
        Point3d _alPoint;
        ThWallColumnsEngine _thWallColumns;
        private ThMEPOriginTransformer _originTransformer;
        public List<string> ErrorMsgs;
        List<Line> _allCalcALLines;
        List<GraphRoute> _allGraphRoutes;
        public void Dispose()
        { }
        public ParkLightConnectCommand(PolygonInfo polygonInfo,Point3d alPoint) 
        {
            _polygonInfo = polygonInfo;
            _alPoint = alPoint;
            ErrorMsgs = new List<string>();
            _allCalcALLines = new List<Line>();
            _allGraphRoutes = new List<GraphRoute>();
            _dbScanClusterSingleMaxCount = ThParkingStallService.Instance.GroupMaxLightCount;
            CommandName = "THCWZMLX";
            ActionName = "车位照明连线";
        }
        public override void SubExecute()
        {
            if (null == _polygonInfo)
                return;
            _allCalcALLines.Clear();
            _allGraphRoutes.Clear();
            ErrorMsgs.Clear();
            _areaLightBlocks = new List<LightBlockReference>();
            _laneLines = new List<Line>();
            _trunkingLines = new List<Line>();
            _alBlocks = new List<BlockReference>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                //进行偏移，超远距离处理
                var pt = _polygonInfo.ExternalProfile.StartPoint;
                _originTransformer = new ThMEPOriginTransformer(pt);
                var outPLine = (Polyline)_polygonInfo.ExternalProfile.Clone();
                _originTransformer.Transform(outPLine);
                var innerPLines = new List<Polyline>();
                if (null != _polygonInfo.InnerProfiles && _polygonInfo.InnerProfiles.Count > 0)
                {
                    foreach (var pl in _polygonInfo.InnerProfiles)
                    {
                        var copy = (Polyline)pl.Clone();
                        _originTransformer.Transform(copy);
                        innerPLines.Add(copy);
                    }
                }
                var alPoint = new Point3d(_alPoint.X, _alPoint.Y, 0);
                alPoint = _originTransformer.Transform(alPoint);
                alPoint = new Point3d(alPoint.X, alPoint.Y, 0);

                _baseElement = new BaseElement(acdb.Database, _originTransformer);
                if (!BaseCheck(outPLine, innerPLines, alPoint))
                    return;

                try 
                {
                    _thWallColumns = new ThWallColumnsEngine(_originTransformer);
                }
                catch
                {
                    //柱梁获取失败不进行报错，后面不进行躲避
                    ErrorMsgs.Add("柱墙提取失败");
                }
                
                LoadCraterClear.LoadBlockLayerToDocument(acdb.Database);
                LoadCraterClear.ClearHistoryLines(acdb.Database, ParkingStallCommon.PARK_LIGHT_CONNECT_LAYER, outPLine, innerPLines, _originTransformer);

                var connectGroup = AreaLightConnect(outPLine,innerPLines, alPoint);
                if (null == connectGroup || connectGroup.Count<1)
                    return;
                var lines = new List<Line>();
                foreach (var group in connectGroup)
                {
                    var groupLines = new List<Line>();
                    foreach (var item in group.LightGroups)
                    {
                        if (item == null || item.LightPoints == null || item.LightPoints.Count < 1)
                            continue;
                        foreach (var lightLines in item.LightConnectLines)
                        {
                            if (null == lightLines.ConnectLines || lightLines.ConnectLines.Count < 1)
                                continue;
                            groupLines.AddRange(lightLines.ConnectLines);
                        }
                        if (string.IsNullOrEmpty(item.ParentId) || item.ParentId == "0" || item.ConnectParent == null)
                            continue;
                        groupLines.AddRange(item.ConnectParent.ConnectLines);
                    }
                    foreach (var line in group.ConnectLines)
                    {
                        var addLine = new Line(line.StartPoint, line.EndPoint);
                        groupLines.Add(addLine);
                    }
                    double r = 25;
                    Point3d cc = group.WireTroughLinePoint;
                    Point2d p1 = new Point2d(cc.X + r, cc.Y);
                    Point2d p2 = new Point2d(cc.X - r, cc.Y);
                    Polyline poly = new Polyline();
                    poly.Closed = false;
                    poly.Layer = ParkingStallCommon.PARK_LIGHT_CONNECT_LAYER;
                    poly.ColorIndex = (int)ColorIndex.BYLAYER;
                    poly.AddVertexAt(0, p1, 1, 0, 0);
                    poly.AddVertexAt(1, p2, 1, 0, 0);
                    poly.AddVertexAt(2, p1, 1, 0, 0);
                    poly.ConstantWidth = 50;
                    _originTransformer.Reset(poly);
                    acdb.ModelSpace.Add(poly);
                    lines.AddRange(groupLines);
                }
                foreach (var line in lines)
                {
                    var copyLine = (Line)line.Clone();
                    copyLine.Layer = ParkingStallCommon.PARK_LIGHT_CONNECT_LAYER;
                    copyLine.ColorIndex = (int)ColorIndex.BYLAYER;
                    _originTransformer.Reset(copyLine);
                    acdb.ModelSpace.Add(copyLine);
                }
            }
        }
        bool BaseCheck(Polyline outPolylin, List<Polyline> innerPolylines, Point3d alPoint) 
        {
            //提取车位灯、车道线、线槽线等相关信息
            InitPolyLineData(outPolylin, innerPolylines);
            if (_areaLightBlocks == null || _areaLightBlocks.Count < 1 || _trunkingLines == null || _trunkingLines.Count < 1)
                return false;

            //处理线槽线,合并计算最短路径
            var objs = new DBObjectCollection();
            _trunkingLines.ForEach(x => objs.Add(x));
            var firstStep = ThMEPLineExtension.LineSimplifier(objs, 5.0, 2.0, 2.0, Math.PI / 180.0).Cast<Line>().ToList();
            objs.Clear();
            firstStep.ForEach(x =>
            {
                if (x.GetLength() > 310)
                    objs.Add(x);
            });
            _allCalcALLines = ThMEPLineExtension.LineSimplifier(objs, 5.0, 400.0, 10.0, Math.PI*5.0 / 180.0).Cast<Line>().ToList();

            //根据车道线，配电箱构造车道线上各个点到出口的路径
            var routeServices = new LaneLineRoute(outPolylin, _allCalcALLines, alPoint);
            var exitPoint = routeServices.CalcALPoint();
            _allGraphRoutes = routeServices.GetAllGraphRoute(true);
            //检查是否有线无法到达配电箱的，如果有，不进行后续的操作
            var allPoints = new List<Point3d>();
            foreach (var line in _allCalcALLines)
            {
                var sp = line.StartPoint;
                var ep = line.EndPoint;
                if (!allPoints.Any(c => c.DistanceTo(sp) < 1))
                    allPoints.Add(sp);
                if (!allPoints.Any(c => c.DistanceTo(ep) < 1))
                    allPoints.Add(ep);
            }
            bool isExit = false;
            foreach (var point in allPoints)
            {
                if (point.DistanceTo(exitPoint) < 10)
                    continue;
                if (isExit)
                    break;
                bool inRoutes = false;
                foreach (var route in _allGraphRoutes)
                {
                    if (inRoutes)
                        break;
                    var checkPoint = (Point3d)route.currentNode.GraphNode;
                    inRoutes = checkPoint.DistanceTo(point) < 10;
                }
                isExit = !inRoutes;
            }
            if (isExit)
            {
                ErrorMsgs.Add("有不连通的线槽，请修改后再次进行相应的操作");
                return false;
            }
            return true;
        }
        List<MaxGroupLight> AreaLightConnect(Polyline outPolylin,List<Polyline> innerPolylines,Point3d alPoint) 
        {
            var allColumns = new List<Polyline>();
            var allWalls = new List<Polyline>();
            try
            {
                //柱墙获取不在进行报错处理，后面如果获取不到就不进行躲避
                _thWallColumns.GetStructureInfo(outPolylin, out allColumns, out allWalls);
            }
            catch 
            {
                ErrorMsgs.Add("柱墙获取数据失败");
            }
            //根据线槽线、车道线进行密度聚类对灯进行聚类预分组
            var preLightGroups = LightPreGroup(outPolylin, innerPolylines);

            //将分组进行合并然后进行连接
            var groupMerge = new LightGroupLane(outPolylin, innerPolylines, preLightGroups, ExtendLaneLine(_laneLines), _dbScanClusterSingleMaxCount);
            var lightGroups = groupMerge.FirstStepGroup();
            lightGroups = groupMerge.SecondStepGroup(lightGroups);

            //分组后根据路径计算每组中最近的连接点,合并同一分组同一线上的分组
            var routeGroup = new LightGroupByRoute(lightGroups, _allGraphRoutes, _allCalcALLines, _trunkingLines);
            routeGroup.InitPolylines(outPolylin, innerPolylines);
            var maxGroups = routeGroup.GroupMergeConveter(15000);

            //合并转换后的分组信息，计算每个大分组中的连接信息
            var connect = new LightGroupConnect(maxGroups, _areaLightBlocks, outPolylin, innerPolylines);
            connect.InitData(allWalls, allColumns, _allCalcALLines);
            var connectGroup = connect.CalcGroupConnect();
            //return connectGroup;
            //根据连接线信息进行调整连接调整修正
            LightAdjustConnectPoint connectAdjust = new LightAdjustConnectPoint(connectGroup, _areaLightBlocks, outPolylin, innerPolylines);
            connectAdjust.InitData(allWalls, allColumns, _allCalcALLines);
            connectAdjust.AdjustMaxGroupConnect();
            //return connectGroup;
            LightAdjustConnectLine connectAdjustLine = new LightAdjustConnectLine(connectGroup, _areaLightBlocks, outPolylin, innerPolylines);
            connectAdjustLine.InitData(allWalls, allColumns, _allCalcALLines);
            return connectAdjustLine.AdjustMaxGroupConnect();
        }
        void InitPolyLineData(Polyline outPolyline,List<Polyline> innerPolylines) 
        {
            _areaLightBlocks.Clear();
            _laneLines.Clear();
            _trunkingLines.Clear();
            _alBlocks.Clear();
            //提取车位灯
            var blks = _baseElement.GetAreaLights(outPolyline, innerPolylines);
            blks.ForEach(c => _areaLightBlocks.Add(new LightBlockReference(c)));
            _laneLines = _baseElement.GetLaneLines(outPolyline);//获取车道线
            _trunkingLines = _baseElement.GetLayerLines(outPolyline, new List<string>() { "E-UNIV-EL2", "E-LITE-CMTB" });//获取线槽线
            _alBlocks = _baseElement.GetAreaDistributionBox(outPolyline, innerPolylines);
        }
        List<LightGroup> LightPreGroup(Polyline outPolyline,List<Polyline> innerPolylines) 
        {
            var notInsertLines = new List<Line>();
            notInsertLines.AddRange(_trunkingLines);
            if (innerPolylines.Count > 0)
            {
                var liens = ThMEPLineExtension.ExplodeCurves(innerPolylines.ToCollection()).Where(c => c is Line).Cast<Line>().ToList();
                notInsertLines.AddRange(liens);
            }
            var tempLines = ExtendLaneLine(_laneLines);
            notInsertLines.AddRange(tempLines);
            
            //车位灯预分组
            var lightPreGroup = new LightBeforehandGroup(_areaLightBlocks, notInsertLines);
            var drawInput = lightPreGroup.GetPreGroupPoints(_dbScanClusterRadius, 0, _dbScanClusterSingleMaxCount);
            //根据预分组，线将组内进行分组
            var innerGroupConnect = new LightInnerGroupConnect(outPolyline, innerPolylines);
            return innerGroupConnect.GroupInnerConnect(drawInput, _areaLightBlocks, 2000);
        }
        List<Line> ExtendLaneLine(List<Line> tartgetLines) 
        {
            var lines = new List<Line>();
            foreach (var line in tartgetLines)
            {
                var sp = line.StartPoint;
                var ep = line.EndPoint;
                var dir = (ep - sp).GetNormal();
                var spLines = tartgetLines.Where(c => sp.PointInLineSegment(c, 5, 5)).ToList();
                var epLines = tartgetLines.Where(c => ep.PointInLineSegment(c, 5, 5)).ToList();
                if (spLines.Count < 2)
                {
                    sp -= dir.MultiplyBy(3000);
                }
                if (epLines.Count < 2)
                {
                    ep += dir.MultiplyBy(3000);
                }
                lines.Add(new Line(sp, ep));
            }
            return lines;
        }
    }
}
