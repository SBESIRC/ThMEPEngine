using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;

using ThMEPEngineCore.CAD;
using ThMEPWSS.Common;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    /// <summary>
    /// 立管文字标注
    /// </summary>
    class PipeLineLabelLayout
    {

        private double _pipeYAxisGroupDistance = 1200;//Y轴方向上立管分组距离
        private double _pipeXAxisGroupDistance = 10;//Y轴分组是X轴方向上允许的误差范围

        private double _pipeLabelNearDistance = 500;//文字距离立管最短距离
        private double _pipeLabelMaxDistance = 3500;//文字距离立管最大距离
        private double _pipeLavelDirectionMoveStep = 300;//文字沿着线方向移动步长
        private double _pipeLabelXDirectionMaxDistance = 200;//文字X轴方向距离主线的最大距离
        private double _pipeLalelXDirectionMoveStep = 200;//文字沿着X轴方向移动的步长
        private double _checkNearPipeDistance = 1000;//布置检查附近是否有立管的范围，用于确定排布方向的先后顺序

        private double _labelTextYSpace = 150;
        private double _labelTextXSpace = 100;

        List<CreateBlockInfo> _thisFloorPipes;
        List<CreateBlockInfo> _orderLabelPipes;
        List<RoofPointInfo> _labelRoofDrains;
        ObstacleEntities _obstacleEntities;
        List<CreateBasicElement> createBasicElements;
        PipeLabelText _pipeLabelText;
        List<RoomModel> _cretateFloorRooms;
        FloorFramed _createFloor;
        double _createFloorSpliteY;
        List<Polyline> _roomTypeSplitLines = new List<Polyline>();//楼层框定户型分隔线
        Polyline _floorFramedBound { get; set; }
        List<FloorFramed> _floorFrameds { get; set; }
        public PipeLineLabelLayout(FloorFramed spliterfloor, double spliterY, List<Polyline> roomTypeSplitLines, List<FloorFramed> floorFrameds)
        {
            _thisFloorPipes = new List<CreateBlockInfo>();
            _orderLabelPipes = new List<CreateBlockInfo>();
            _labelRoofDrains = new List<RoofPointInfo>();
            createBasicElements = new List<CreateBasicElement>();
            _obstacleEntities = new ObstacleEntities();
            _pipeLabelText = new PipeLabelText(spliterfloor, spliterY, roomTypeSplitLines);
            _cretateFloorRooms = new List<RoomModel>();
            _roomTypeSplitLines = new List<Polyline>(roomTypeSplitLines);
            _floorFramedBound = spliterfloor.outPolyline;
            _floorFrameds= floorFrameds;
        }
        public void AddObstacleEntitys(List<Entity> entitys)
        {
            if (null == entitys || entitys.Count < 1)
                return;
            _obstacleEntities.AddObstacleEntitys(entitys);
        }
        public void InitFloorData(FloorFramed layerFloor,List<CreateBlockInfo> thisFloorBlocks, List<RoomModel> thisFloorRooms) 
        {
            _createFloor = layerFloor;
            _thisFloorPipes.Clear();
            createBasicElements.Clear();
            _cretateFloorRooms.Clear();
            if (null != thisFloorRooms && thisFloorRooms.Count > 0)
                _cretateFloorRooms.AddRange(thisFloorRooms);
            var pipeTags = new List<string>
            {
                "Y1L","Y2L","FyL","FcL", "NL","FL","PL","TL","DL","WL"
            };
            foreach (var cBlock in thisFloorBlocks)
            {
                if (!cBlock.floorId.Equals(layerFloor.floorUid))
                    continue;
                bool isAddToObstacle = false;
                //将地漏，立管加入到避让信息中,这里认为是矩形，这个时候没有在图纸中生成，没有拿到具体的大小
                if (cBlock.equipmentType == EnumEquipmentType.floorDrain) 
                {
                    isAddToObstacle = true;
                }
                if (!string.IsNullOrEmpty(cBlock.tag) && pipeTags.Any(c => c.Equals(cBlock.tag)))
                {
                    isAddToObstacle = true;
                    _thisFloorPipes.Add(cBlock);
                }
                if (isAddToObstacle) 
                {
                    var point = new Point3d(cBlock.createPoint.X, cBlock.createPoint.Y, cBlock.createPoint.Z);
                    point = point - Vector3d.XAxis.MultiplyBy(150);
                    point = point - Vector3d.YAxis.MultiplyBy(150);
                    var pline = TextOutPolyLine(point, Vector3d.XAxis, 300, Vector3d.YAxis, 300);
                    _obstacleEntities.AddObstacleEntity(pline,false);
                }
            }
        }
        public void InitRoofFloorPipes(List<CreateBlockInfo> roofY1Pipes,List<RoofPointInfo> thisFloorRoofDrain) 
        {
            if (null != roofY1Pipes && roofY1Pipes.Count > 0)
                _orderLabelPipes.AddRange(roofY1Pipes);
            if (null != thisFloorRoofDrain && thisFloorRoofDrain.Count > 0)
                _labelRoofDrains.AddRange(thisFloorRoofDrain);
        }
        public static Vector3d CreateVector(Point3d ps, Point3d pe)
        {
            return new Vector3d(pe.X - ps.X, pe.Y - ps.Y, pe.Z - ps.Z);
        }
        public List<CreateDBTextElement> SpliteFloorSpace(out List<CreateBasicElement> createBasics)
        {
            _pipeLabelText.InitFloorData(_createFloor, _cretateFloorRooms);
            _createFloorSpliteY = _pipeLabelText.GetCreateFloorSpliteY();
            createBasicElements.Clear();
            createBasics = new List<CreateBasicElement>();
            var allTexts = new List<CreateDBTextElement>();
            List<double> spliteX = _pipeLabelText.GetSpliteXBySpliteFloor();
            double floorStartX = _createFloor.floorBlock.Position.X;
            double floorEndX = floorStartX + _createFloor.width;
            List<double> floorSpaceX = new List<double>();
            floorSpaceX.Add(floorStartX);
            floorSpaceX.Add(floorEndX);
            foreach (var x in spliteX)
            {
                if (x <= floorStartX || x >= floorEndX)
                    continue;
                floorSpaceX.Add(x);
            }
            floorSpaceX = floorSpaceX.OrderBy(c => c).ToList();
            var allPipeLabels = GetThisFloorAllLables();
            //获取顶层到该楼层的偏移矩阵
            var crossMat = new Matrix3d();
            if (allPipeLabels.Any())
            {
                var thisFloorFramed = _floorFrameds.Where(e => e.outPolyline.Contains(allPipeLabels[0].BasePoint)).ToList();
                var topFloorFramed = _floorFrameds.Where(e => e.outPolyline.Contains(_floorFramedBound.GetCentroidPoint())).ToList();
                if (thisFloorFramed.Any() && topFloorFramed.Any())
                    crossMat = Matrix3d.Displacement(CreateVector(topFloorFramed.First().datumPoint, thisFloorFramed.First().datumPoint));
            }
            //提取到属于该楼层框的户型分割线
            double refY = _floorFramedBound.GetCentroidPoint().Y;
            var eldSpaceX = new List<double>(floorSpaceX);
            if (eldSpaceX.Count > 2)
            {
                eldSpaceX.RemoveAt(0);
                eldSpaceX.RemoveAt(eldSpaceX.Count - 1);
            }
            eldSpaceX.ForEach(d => _roomTypeSplitLines.Add(FloorFramedSpliter.PolyFromLine(new Line(new Point3d(d, refY - 1, 0), new Point3d(d, refY + 1, 0)))));
            _roomTypeSplitLines = _roomTypeSplitLines.Where(e => _floorFramedBound.Contains(ThCADExtension.ThCurveExtension.GetMidpoint(e)) || _floorFramedBound.IntersectWithEx(e).Count > 0).ToList();
            if (_roomTypeSplitLines.Count >= 1)
            {
                var floorSpceRegions = FloorFramedSpliter.ConvertToCorrectSpliteLines(_roomTypeSplitLines, _floorFramedBound);
                for (int i = 0; i < floorSpceRegions.Count; i++)
                {
                    double minX = floorSpceRegions[i].EntityVertices().Cast<Point3d>().Select(p => p.X).OrderBy(d=>d).First();
                    double maxX = floorSpceRegions[i].EntityVertices().Cast<Point3d>().Select(p => p.X).OrderBy(d => d).Last();
                    var offset_region = floorSpceRegions[i].Clone() as Polyline;
                    offset_region.TransformBy(crossMat);
                    //获取该区域内的立管
                    var spacePipes = allPipeLabels.Where(c => offset_region.Contains(c.BasePoint)).ToList();
                    if (spacePipes == null || spacePipes.Count < 1)
                        continue;
                    var tmpBaseElements = createBasicElements;
                    var addText = LayoutTextAvoidObstacleEntity(minX, maxX, spacePipes);
                    tmpBaseElements = createBasicElements.Except(tmpBaseElements).ToList();
                    if (null != addText && addText.Count > 0)
                    {
                        if (addText.Any(e => e.dbText.TextString.Contains("雨水斗")))
                        {
                            addText.ForEach(e => e.ConvertToTCHElement = true);
                        }
                        allTexts.AddRange(addText);
                    }
                }
            }
            else
            {
                //支持老版
                for (int i = 0; i < floorSpaceX.Count - 1; i++)
                {
                    double minX = floorSpaceX[i];
                    double maxX = floorSpaceX[i + 1];
                    //获取该区域内的立管
                    var spacePipes = allPipeLabels.Where(c => c.BasePoint.X > minX && c.BasePoint.X < maxX).ToList();
                    if (spacePipes == null || spacePipes.Count < 1)
                        continue;
                    var tmpBaseElements = createBasicElements;
                    var addText = LayoutTextAvoidObstacleEntity(minX, maxX, spacePipes);
                    tmpBaseElements = createBasicElements.Except(tmpBaseElements).ToList();
                    if (null != addText && addText.Count > 0)
                    {
                        if (addText.Any(e => e.dbText.TextString.Contains("雨水斗")))
                        {
                            addText.ForEach(e => e.ConvertToTCHElement = true);
                        }
                        allTexts.AddRange(addText);
                    }
                }
            }
            if (createBasicElements.Count > 0)
                createBasics.AddRange(createBasicElements);
            return allTexts;
        }

        List<PointLabelInfo> GetThisFloorAllLables()
        {
            var allLabels = new List<PointLabelInfo>();
            //获取顺逆时针排布的立管
            var clockwiseLabels = _pipeLabelText.ClockwiseLabelInfo(_thisFloorPipes);
            if (clockwiseLabels.Count > 0)
                allLabels.AddRange(clockwiseLabels);
            var orderLabels = _pipeLabelText.OrderLabelInfo(_orderLabelPipes,"a");
            if (orderLabels.Count > 0)
                allLabels.AddRange(orderLabels);
            //雨水斗表述信息
            if (null != _labelRoofDrains && _labelRoofDrains.Count > 0) 
            {
                bool isMaxRoof = _createFloor.floorType.Contains("大屋面");
                string gravityDN = isMaxRoof ? SetServicesModel.Instance.maxRoofGravityRainBucketRiserPipeDiameter.ToString() : SetServicesModel.Instance.minRoofGravityRainBucketRiserPipeDiameter.ToString();
                string sideDN = isMaxRoof ? SetServicesModel.Instance.maxRoofSideDrainRiserPipeDiameter.ToString() : SetServicesModel.Instance.minRoofSideDrainRiserPipeDiameter.ToString();
                foreach (var roofDrain in _labelRoofDrains) 
                {
                    if (!roofDrain.roofUid.Equals(_createFloor.floorUid))
                        continue;
                    if (roofDrain.equipmentType == EnumEquipmentType.gravityRainBucket)
                    {
                        //重力雨水斗
                        PointLabelInfo pointLabel = new PointLabelInfo(roofDrain.centerPoint, Guid.NewGuid().ToString(), -1, "重力雨水斗");
                        pointLabel.BottomText = gravityDN;
                        allLabels.Add(pointLabel);
                    }
                    else if (roofDrain.equipmentType == EnumEquipmentType.sideRainBucket) 
                    {
                        //侧入雨水斗
                        PointLabelInfo pointLabel = new PointLabelInfo(roofDrain.centerPoint, Guid.NewGuid().ToString(), -1, "侧入雨水斗");
                        pointLabel.BottomText = sideDN;
                        allLabels.Add(pointLabel);
                    }
                }
            }
            return allLabels;
        }
        List<CreateDBTextElement> LayoutTextAvoidObstacleEntity(double minX, double maxX, List<PointLabelInfo> pointLabelInfos)
        {
            var retText = new List<CreateDBTextElement>();
            if (pointLabelInfos == null || pointLabelInfos.Count < 1)
                return retText;
            var pipeLayoutDir = new PipeLabelLayoutDirection(pointLabelInfos, minX, maxX, _createFloorSpliteY);
            pipeLayoutDir.InitData(_pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep);
            List<string> hisPipes = new List<string>();
            var yAxis = Vector3d.YAxis;
            //标注线的方向沿Y轴，
            //所有立管全部布置完毕后，以立管的圆心（容差10）为起点在Y轴正负方向找距离500范围内的的其他立管。若500范围内找到了立管则继续找，直到找不到为止，将找到的组成一队。
            var tempPipes = new List<PointLabelInfo>();
            pointLabelInfos.ForEach(c => tempPipes.Add(c));
            //先进行分组，优先排布分组个数多的
            List<List<PointLabelInfo>> groupPipeLabels = new List<List<PointLabelInfo>>();
            while (tempPipes.Count > 0) 
            {
                var basePipe = tempPipes.FirstOrDefault();
                var thisLinePipes = GetLinePipe(basePipe.BasePoint, tempPipes, Vector3d.YAxis, _pipeYAxisGroupDistance,_pipeXAxisGroupDistance);
                hisPipes.AddRange(thisLinePipes.Select(c => c.BelongId).ToList());
                tempPipes = tempPipes.Where(c => !hisPipes.Any(x => x.Equals(c.BelongId))).ToList();
                groupPipeLabels.Add(thisLinePipes);
            }
            groupPipeLabels = groupPipeLabels.OrderByDescending(c => c.Count).ToList();
            foreach (var listPipe in groupPipeLabels) 
            {
                var thisLinePipes = new List<PointLabelInfo>();
                thisLinePipes.AddRange(listPipe);
                var centerPoint = PointVectorUtil.PointsAverageValue(thisLinePipes.Select(c => c.BasePoint).ToList());
                GetTextHeightWidth(thisLinePipes, out double textHeight, out double textWidth);
                textHeight += thisLinePipes.Count * _labelTextYSpace;
                textWidth += _labelTextXSpace;
                bool canCreate = false;
                var createPoint = new Point3d();
                var outXLength = 0.0;
                var layoutDiections = pipeLayoutDir.GetLayoutDirections(thisLinePipes, centerPoint, textWidth, textHeight, _checkNearPipeDistance);
                CheckDirection layoutDir = null;
                foreach (var item in layoutDiections)
                {
                    if (canCreate)
                        break;
                    layoutDir = item;
                    canCreate = CanAddTextInDir(centerPoint, item, textHeight, textWidth, out createPoint, out outXLength);
                }
                if (!canCreate)
                {
                    //没有满足条件的可布置点。
                    layoutDir = layoutDiections.FirstOrDefault();
                    var moveOutDis = layoutDir.direction.Y > 0 ? _pipeLabelNearDistance : _pipeLabelNearDistance + textHeight;
                    createPoint = centerPoint + layoutDir.direction.MultiplyBy(moveOutDis);
                    outXLength = 0.0;
                }

                //获取的的可布置点为最高点，或最低点
                thisLinePipes = thisLinePipes.OrderBy(c => c.BasePoint.Y).ToList();

                var lineStartPoint = createPoint;
                string connectPipeIds = string.Join(",", thisLinePipes.Select(c => c.BelongId).ToArray());

                var textStartPoint = lineStartPoint + layoutDir.outDirection.MultiplyBy(outXLength);
                if (layoutDir.outDirection.X < 0)
                    textStartPoint = textStartPoint + layoutDir.outDirection.MultiplyBy(textWidth);
                var textPLine = TextOutPolyLine(textStartPoint, Vector3d.XAxis, textWidth, Vector3d.YAxis, textHeight);
                _obstacleEntities.AddObstacleEntity(textPLine, true);
                int plCount = 0;
                for (int i = 0; i < thisLinePipes.Count; i++)
                {
                    var pipe = thisLinePipes[i];
                    var pipeNames = new string[] { "FL", "PL", "TL", "FCL", "FYL", "WL" };
                    string txtLineLayer = ThWSSCommon.Layout_PipeRainTextLayerName;
                    var matched_pipeName = false;
                    foreach (var name in pipeNames)
                    {
                        if (pipe.UpText.ToUpper().Contains(name))
                        {
                            matched_pipeName = true;
                            break;
                        }
                    }
                    if (matched_pipeName)
                    {
                        plCount += 1;
                        txtLineLayer = ThWSSCommon.Layout_PipeWastDrainTextLayerName;
                    }
                    if (!string.IsNullOrEmpty(pipe.BottomText) && layoutDir.direction.Y > 0)
                    {
                        lineStartPoint = lineStartPoint + yAxis.MultiplyBy(textHeight / 2);
                        textStartPoint = textStartPoint + yAxis.MultiplyBy(textHeight / 2);
                    }
                    var textCreatePoint = textStartPoint + Vector3d.YAxis.MultiplyBy(_labelTextYSpace/3) +Vector3d.XAxis.MultiplyBy(_labelTextXSpace/2);
                    var text = DrainSysAGCommon.CreateDBText(pipe.UpText, textCreatePoint, txtLineLayer, ThWSSCommon.Layout_TextStyle);
                    DBText btText = null;
                    if (!string.IsNullOrEmpty(pipe.BottomText))
                    {
                        btText = DrainSysAGCommon.CreateDBText(pipe.BottomText, textCreatePoint - yAxis.MultiplyBy(textHeight / 2 + 50), txtLineLayer, ThWSSCommon.Layout_TextStyle);
                    }
                    var textLineEp = layoutDir.outDirection.X < 0 ? textStartPoint : textStartPoint + Vector3d.XAxis.MultiplyBy(textWidth);
                    var s = new CreateBasicElement(_createFloor.floorUid, new Line(lineStartPoint, textLineEp), txtLineLayer, pipe.BelongId, "LG_BSLJX");
                    createBasicElements.Add(s);
                    var _mainLine = new Line(pipe.BasePoint, lineStartPoint);
                    if (matched_pipeName)
                    {
                        var addLine1 = new CreateBasicElement(_createFloor.floorUid, _mainLine, ThWSSCommon.Layout_PipeWastDrainTextLayerName, pipe.BelongId, "LG_BSLJX");
                        createBasicElements.Add(addLine1);
                    }
                    else
                    {
                        var addLine = new CreateBasicElement(_createFloor.floorUid, _mainLine, ThWSSCommon.Layout_PipeRainTextLayerName, pipe.BelongId, "LG_BSLJX");
                        createBasicElements.Add(addLine);
                    }
                    if (i != thisLinePipes.Count - 1)
                    {
                        var maxPoint = text.GeometricExtents.MaxPoint;
                        var minPoint = text.GeometricExtents.MinPoint;
                        var xDis = Math.Abs(maxPoint.X - minPoint.X);
                        var yDis = Math.Abs(maxPoint.Y - minPoint.Y);
                        textStartPoint = textStartPoint + yAxis.MultiplyBy(yDis + _labelTextYSpace);
                        lineStartPoint = lineStartPoint + yAxis.MultiplyBy(yDis + _labelTextYSpace);
                    }
                    retText.Add(new CreateDBTextElement(_createFloor.floorUid, textStartPoint, text, pipe.BelongId, txtLineLayer, ThWSSCommon.Layout_TextStyle));
                    if (btText != null) 
                    {
                        retText.Add(new CreateDBTextElement(_createFloor.floorUid, btText.Position, btText, pipe.BelongId, txtLineLayer, ThWSSCommon.Layout_TextStyle));
                    }
                }                          
                var startPipe = layoutDir.direction.Y < 0 ? thisLinePipes.Last() : thisLinePipes.First();
                var lineSp = new Point3d(centerPoint.X, startPipe.BasePoint.Y, 0);
                var lineEp = layoutDir.direction.Y < 0 ? createPoint : lineStartPoint;
                var mainLine = new Line(lineSp, lineEp);
                //将主线加入到后续的避让线中
                _obstacleEntities.AddMainLine(mainLine);
            }
            return retText;
        }
        void GetTextHeightWidth(List<PointLabelInfo> lablePipes,out double height,out double width) 
        {
            height = 0;
            width = 0;
            foreach (var item in lablePipes) 
            {
                if (!string.IsNullOrEmpty(item.UpText)) 
                {
                    var text = DrainSysAGCommon.CreateDBText(item.UpText, item.BasePoint, "", ThWSSCommon.Layout_TextStyle);
                    var maxPoint = text.GeometricExtents.MaxPoint;
                    var minPoint = text.GeometricExtents.MinPoint;
                    var xDis = Math.Abs(maxPoint.X - minPoint.X);
                    var yDis = Math.Abs(maxPoint.Y - minPoint.Y);
                    height += yDis;
                    width = Math.Max(width, xDis);
                }
                if (!string.IsNullOrEmpty(item.BottomText)) 
                {
                    var text = DrainSysAGCommon.CreateDBText(item.BottomText, item.BasePoint, "", ThWSSCommon.Layout_TextStyle);
                    var maxPoint = text.GeometricExtents.MaxPoint;
                    var minPoint = text.GeometricExtents.MinPoint;
                    var xDis = Math.Abs(maxPoint.X - minPoint.X);
                    var yDis = Math.Abs(maxPoint.Y - minPoint.Y);
                    height += yDis;
                    width = Math.Max(width, xDis);
                }
            }
        }
        bool CanAddTextInDir(Point3d centerPoint, CheckDirection checkDirection, double textHeight,double textLength,out Point3d createPoint,out double xOutLength) 
        {
            var lineDir = checkDirection.direction;
            var sideDir = checkDirection.outDirection;
            xOutLength = 0;
            createPoint = new Point3d();
            //以一组立管的几何中心点为起点，以2000+最长文字下划线长度为半径画圆。圆心内先扣掉一个半径为500的同心圆，然后扣掉所有要躲避的图元。
            double disStart = checkDirection.minDistance;
            double step = checkDirection.dirSetp;
            double xStep = _pipeLalelXDirectionMoveStep;
            double xMaxLength = _pipeLabelXDirectionMaxDistance;
            var startPoint = centerPoint + lineDir.MultiplyBy(disStart);
            bool canAdd = false;
            while (true) 
            {
                //主线Y轴方向判断是否符合要求
                if (canAdd)
                    break;
                if (startPoint.DistanceTo(centerPoint) >= checkDirection.maxDistance)
                    break;
                Line mainLine = new Line(centerPoint, startPoint);
                var xStartPoint = startPoint;
                if (_obstacleEntities.CheckMainLineObstacle(mainLine))
                {
                    //和其它线有共线的情况
                    return false;
                }
                while (true) 
                {
                    if (canAdd)
                        break;
                    if (xStartPoint.DistanceTo(startPoint) > xMaxLength)
                        break;
                    var textPLine = TextOutPolyLine(xStartPoint, sideDir, textLength, Vector3d.YAxis, textHeight);
                    if (_obstacleEntities.CheckBySpaceIndex(textPLine))
                    {
                        //可进一步判断辅线X轴方向是否符合要求
                        xStartPoint += sideDir.MultiplyBy(xStep);
                        continue;
                    }
                    canAdd = true;
                }
                if (canAdd)
                {
                    xOutLength = xStartPoint.DistanceTo(startPoint);
                    break;
                }
                startPoint += lineDir.MultiplyBy(step);
            }
            if (canAdd)
                createPoint = startPoint;
            return canAdd;
        }
        Polyline TextOutPolyLine(Point3d point,Vector3d xAxis,double xLength,Vector3d yAxis,double yLength) 
        {
            var textPoint1 = point;
            var textPoint2 = textPoint1 + xAxis.MultiplyBy(xLength);
            var textPoint3 = textPoint2 + yAxis.MultiplyBy(yLength);
            var textPoint4 = textPoint1 + yAxis.MultiplyBy(yLength);
            Point2d sp2d = new Point2d(textPoint1.X, textPoint1.Y);
            Point2d ep2d = new Point2d(textPoint2.X, textPoint2.Y);
            Point2d ep2dNext = new Point2d(textPoint3.X, textPoint3.Y);
            Point2d sp2dNext = new Point2d(textPoint4.X, textPoint4.Y);
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(0, sp2d, 0, 0, 0);
            polyline.AddVertexAt(1, ep2d, 0, 0, 0);
            polyline.AddVertexAt(2, ep2dNext, 0, 0, 0);
            polyline.AddVertexAt(3, sp2dNext, 0, 0, 0);
            polyline.Closed = true;
            return polyline;
        }
        

        List<PointLabelInfo> GetLinePipe(Point3d basePoint,List<PointLabelInfo> areaAllPipe,Vector3d orderDir,double dirTolerance,double outTolerance)
        {
            var points = areaAllPipe.Select(c => c.BasePoint).ToList();
            var targetPoints = new List<Point3d>();
            foreach (var point in points) 
            {
                var prjPoint = point.PointToLine(basePoint, orderDir);
                if (prjPoint.DistanceTo(point) < outTolerance)
                    targetPoints.Add(point);
            }
            targetPoints = PointVectorUtil.PointsOrderByDirection(targetPoints, orderDir, false);
            var nearPoints =new List<Point3d>();
            nearPoints.Add(basePoint);
            while (true) 
            {
                bool isAdd = false;
                targetPoints = targetPoints.Where(c => !nearPoints.Any(x => x.DistanceTo(c) < 1)).ToList();
                if (targetPoints.Count < 1)
                    break;
                nearPoints = PointVectorUtil.PointsOrderByDirection(nearPoints, orderDir, false);
                var first = nearPoints.First();
                var last = nearPoints.Last();
                var dicLast = PointVectorUtil.PointsOrderByDirection(targetPoints, orderDir, last);
                dicLast = dicLast.OrderBy(c => c.Value).ToDictionary(c=>c.Key,x=>x.Value);
                foreach (var item in dicLast) 
                {
                    if (item.Value >= 0 && item.Value <= dirTolerance)
                    {
                        isAdd = true;
                        nearPoints.Add(item.Key);
                        break;
                    }
                }
                dicLast = PointVectorUtil.PointsOrderByDirection(targetPoints, orderDir.Negate(), first);
                dicLast = dicLast.OrderBy(c => c.Value).ToDictionary(c => c.Key, x => x.Value);
                foreach (var item in dicLast)
                {
                    if (item.Value >= 0 && item.Value <= dirTolerance)
                    {
                        isAdd = true;
                        nearPoints.Add(item.Key);
                        break;
                    }
                }
                if (!isAdd)
                    break;
            }
            var resPipe = new List<PointLabelInfo>();
            foreach (var item in areaAllPipe) 
            {
                if (nearPoints.Any(c => c.DistanceTo(item.BasePoint) < 1))
                    resPipe.Add(item);
            }
            return resPipe;
        }

    }
}
