using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Assistant;
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
        private double _textHeight1_50 = 175;//文字高度 在图纸比例 1：50
        private double _textHeight1_100 = 350;//文字高度 在图纸比例 1：100
        private double _textHeight1_150 = 525;//文字高度 在图纸比例 1：150

        private double _pipeYAxisGroupDistance = 500;//Y轴方向上立管分组距离
        private double _pipeXAxisGroupDistance = 10;//Y轴分组是X轴方向上允许的误差范围

        private double _pipeLabelNearDistance = 500;//文字距离立管最短距离
        private double _pipeLabelMaxDistance = 3500;//文字距离立管最大距离
        private double _pipeLavelDirectionMoveStep = 300;//文字沿着线方向移动步长
        private double _pipeLabelXDirectionMaxDistance = 200;//文字X轴方向距离主线的最大距离
        private double _pipeLalelXDirectionMoveStep = 200;//文字沿着X轴方向移动的步长

        private double _labelTextYSpace = 150;
        private double _labelTextXSpace = 100;

        List<CreateBlockInfo> _thisFloorPipes;
        public ThCADCoreNTSSpatialIndex _obstacleSpatialIndex;
        public List<Polyline> _obstacleEntities;
        private List<Line> _obstacleLines;
        public List<Polyline> _obstacleCreateEntities;
        List<CreateBasicElement> createBasicElements;

        List<Line> _floorSpliteLines;
        List<double> _floorSpliteX;
        double _floorSpliteY;
        FloorFramed _spliteFloor;
        FloorFramed _createFloor;
        double _createFloorSpliteY;

        public PipeLineLabelLayout(FloorFramed spliterfloor,double spliterY)
        {
            _spliteFloor = spliterfloor;
            _floorSpliteY = spliterY;
            _floorSpliteLines = FramedReadUtil.FloorFrameSpliteLines(spliterfloor);
            _floorSpliteX = _floorSpliteLines.Select(c => c.StartPoint.X).ToList();
            _thisFloorPipes = new List<CreateBlockInfo>();
            _obstacleEntities = new List<Polyline>();
            _obstacleCreateEntities = new List<Polyline>();
            _obstacleLines = new List<Line>();
            createBasicElements = new List<CreateBasicElement>();
        }

        public void InitFloorData(FloorFramed layerFloor,List<CreateBlockInfo> thisFloorBlocks, List<CreateBasicElement> thisBasicElement) 
        {
            _createFloor = layerFloor;
            ClearObstacle();
            _thisFloorPipes.Clear();
            createBasicElements.Clear();
            var pipeTags = new List<string>
            {
                "Y1L","Y2L", "NL","FL","PL","TL","DL"
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
                    _obstacleEntities.Add(pline);
                }
            }
            if (null != thisBasicElement && thisBasicElement.Count > 0) 
            {
                //foreach (var item in thisBasicElement) 
                //{
                //    var isLine = item.baseCurce is Line;
                //    if (!isLine)
                //        continue;
                //    var line = item.baseCurce as Line;
                //    _obstacleLines.Add(line);
                //}
            }
        }
        public void AddObstacleEntity(Entity entity)
        {
            AddObstacle(entity);
        }
        public void AddObstacleEntity(Polyline pline)
        {
            if (null != pline && pline.Area < 10)
                return;
            _obstacleEntities.Add(pline);
        }
        public void AddObstacleEntitys(List<Entity> entitys)
        {
            if (null == entitys || entitys.Count < 1)
                return;
            foreach(var entity in entitys)
                AddObstacle(entity);
        }
        public void AddObstacleEntitys(List<Polyline> polylines)
        {
            if (null == polylines || polylines.Count < 1)
                return;
            foreach (var pline in polylines) 
            {
                if (pline == null || pline.Area < 10)
                    continue;
                _obstacleEntities.Add(pline);
            }
            
        }

        void AddObstacle(Entity entity) 
        {
            Polyline polyline = null;
            try
            {
                if (entity is BlockReference)
                {
                    var block = entity as BlockReference;
                    var ntsPLine = entity.GeometricExtents.ToNTSPolygon();
                    polyline = ntsPLine.ToDbPolylines().FirstOrDefault();
                }
                else if (entity is Polyline pLine)
                {
                    if (pLine.Area < 0.001)
                        return;
                    polyline = pLine;
                }
                else if (entity is Circle || entity is Arc)
                {
                    polyline = entity.GeometricExtents.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
                }
                else if (entity is Line)
                {
                    polyline = (entity as Line).Buffer(10);
                }
                else if (entity is DBText || entity is MText)
                {
                    polyline = entity.GeometricExtents.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
                }
            }
            catch (Exception ex) 
            {
                polyline = null;
            }
            if(null != polyline)
            {
                _obstacleEntities.Add(polyline);
            }
        }
        public void ClearObstacle()
        {
            _obstacleEntities.Clear();
            _obstacleLines.Clear();
            _obstacleCreateEntities.Clear();
        }
        public List<CreateDBTextElement> SpliteFloorSpace(out List<CreateBasicElement> createBasics)
        {
            var addDBColl = new DBObjectCollection();
            _obstacleEntities.ForEach(c =>
            {
                if (null != c && c.Area > 10)
                    addDBColl.Add(c);
            });
            _obstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(addDBColl);


            createBasicElements.Clear();
            createBasics = new List<CreateBasicElement>();
            var allTexts = new List<CreateDBTextElement>();
            List<double> spliteX = GetSpliteXBySpliteFloor();
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
            var areaPipes = new List<LablePipe>();
            for (int i = 0; i < floorSpaceX.Count - 1; i++)
            {
                areaPipes.Clear();
                double minX = floorSpaceX[i];
                double maxX = floorSpaceX[i + 1];
                //获取该区域内的立管
                var spacePipes = _thisFloorPipes.Where(c => c.createPoint.X > minX && c.createPoint.X < maxX).ToList();
                if (spacePipes == null || spacePipes.Count < 1)
                    continue;
                bool isClockwise = (i+1) % 2 == 1;
                List<string> typeNames = spacePipes.Select(c => c.tag).ToList();
                typeNames = typeNames.Distinct().ToList();
                foreach (var name in typeNames)
                {
                    var typePipes = spacePipes.Where(c => c.tag.Equals(name)).ToList();
                    var pipeIdNums = PipeIdNumber(typePipes, isClockwise);
                    var pipeNum = string.Format("{0}{1}-", name, (i + 1));
                    foreach (var pipe in typePipes)
                    {
                        var num = pipeIdNums.Where(c => c.Key.Equals(pipe.uid)).FirstOrDefault().Value;
                        var realNum = string.Format("{0}{1}", pipeNum, num);
                        areaPipes.Add(new LablePipe(pipe, realNum));
                    }
                }
                var addText = LayoutTextAvoidObstacleEntity(minX, maxX, areaPipes);
                if (null != addText && addText.Count > 0)
                    allTexts.AddRange(addText);
            }
            if (createBasicElements.Count > 0)
                createBasics.AddRange(createBasicElements);
            return allTexts;
        }

        List<double> GetSpliteXBySpliteFloor()
        {
            _createFloorSpliteY = _createFloor.datumPoint.Y + (_floorSpliteY - _spliteFloor.datumPoint.Y);
            List<double> createSpliteX = new List<double>();
            if (null == _floorSpliteX || _floorSpliteX.Count < 1)
                return createSpliteX;
            var oldPoistion = _spliteFloor.datumPoint.X;
            var newPoistion = _createFloor.datumPoint.X;
            foreach (var item in _floorSpliteX) 
            {
                var x = newPoistion + (item -oldPoistion);
                createSpliteX.Add(x);
            }
            createSpliteX = createSpliteX.OrderBy(c => c).ToList();
            return createSpliteX;
        } 
        List<CreateDBTextElement> LayoutTextAvoidObstacleEntity(double minX, double maxX,List<LablePipe> areaAllPipe)
        {
            var retText = new List<CreateDBTextElement>();
            if (areaAllPipe ==null || areaAllPipe.Count < 1)
                return retText;
            List<string> hisPipes = new List<string>();
            var yAxis = Vector3d.YAxis;
            //标注线的方向沿Y轴，
            //所有立管全部布置完毕后，以立管的圆心（容差10）为起点在Y轴正负方向找距离500范围内的的其他立管。若500范围内找到了立管则继续找，直到找不到为止，将找到的组成一队。
            var tempPipes = new List<LablePipe>();
            areaAllPipe.ForEach(c => tempPipes.Add(c));
            //先进行分组，优先排布分组个数多的
            List<List<LablePipe>> groupPipeLabels = new List<List<LablePipe>>();
            while (tempPipes.Count > 0) 
            {
                var basePipe = tempPipes.FirstOrDefault();
                var thisLinePipes = GetLinePipe(basePipe.pipeCenterPoint, tempPipes, Vector3d.YAxis, _pipeYAxisGroupDistance,_pipeXAxisGroupDistance);
                hisPipes.AddRange(thisLinePipes.Select(c => c.createBlockUid).ToList());
                tempPipes = tempPipes.Where(c => !hisPipes.Any(x => x.Equals(c.createBlockUid))).ToList();
                groupPipeLabels.Add(thisLinePipes);
            }
            groupPipeLabels = groupPipeLabels.OrderByDescending(c => c.Count).ToList();
            foreach (var listPipe in groupPipeLabels) 
            {
                var thisLinePipes = new List<LablePipe>();
                thisLinePipes.AddRange(listPipe);
                var centerPoint = PointVectorUtil.PointsAverageValue(thisLinePipes.Select(c => c.pipeCenterPoint).ToList());
                GetTextHeightWidth(thisLinePipes, out double textHeight, out double textWidth);
                textHeight += thisLinePipes.Count * _labelTextYSpace;
                textWidth += _labelTextXSpace;
                bool canCreate = false;
                var createPoint = new Point3d();
                var outXLength = 0.0;
                var layoutDiections = GetLayoutDirections(thisLinePipes, centerPoint, minX, maxX, textWidth, textHeight);
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
                thisLinePipes = thisLinePipes.OrderBy(c => c.pipeCenterPoint.Y).ToList();

                var lineStartPoint = createPoint;
                string connectPipeIds = string.Join(",", thisLinePipes.Select(c => c.createBlockUid).ToArray());

                var textStartPoint = lineStartPoint + layoutDir.outDirection.MultiplyBy(outXLength);
                if (layoutDir.outDirection.X < 0)
                    textStartPoint = textStartPoint + layoutDir.outDirection.MultiplyBy(textWidth);
                var textPLine = TextOutPolyLine(textStartPoint, Vector3d.XAxis, textWidth, Vector3d.YAxis, textHeight);
                _obstacleCreateEntities.Add(textPLine);
                for (int i = 0; i < thisLinePipes.Count; i++)
                {
                    var pipe = thisLinePipes[i];
                    string txtLayer = ThWSSCommon.Layout_PipeRainTextLayerName;
                    if (pipe.pipeAttrTag.ToUpper().Equals("FL") || pipe.pipeAttrTag.ToUpper().Equals("PL") || pipe.pipeAttrTag.ToUpper().Equals("TL"))
                        txtLayer = ThWSSCommon.Layout_PipeWastDrainTextLayerName;
                    var textCreatePoint = textStartPoint + Vector3d.YAxis.MultiplyBy(_labelTextYSpace/3) +Vector3d.XAxis.MultiplyBy(_labelTextXSpace/2);
                    var text = CreateDBText(pipe.pipeNumText, textCreatePoint, txtLayer, ThWSSCommon.Layout_TextStyle);

                    var textLineEp = layoutDir.outDirection.X < 0 ? textStartPoint : textStartPoint + Vector3d.XAxis.MultiplyBy(textWidth);
                    var s = new CreateBasicElement(_createFloor.floorUid, new Line(lineStartPoint, textLineEp), ThWSSCommon.Layout_FloorDrainBlockRainLayerName, pipe.createBlockUid, "LG_BSLJX");
                    createBasicElements.Add(s);
                    if (i != thisLinePipes.Count - 1)
                    {
                        var maxPoint = text.GeometricExtents.MaxPoint;
                        var minPoint = text.GeometricExtents.MinPoint;
                        var xDis = Math.Abs(maxPoint.X - minPoint.X);
                        var yDis = Math.Abs(maxPoint.Y - minPoint.Y);
                        textStartPoint = textStartPoint + yAxis.MultiplyBy(yDis + _labelTextYSpace);
                        lineStartPoint = lineStartPoint + yAxis.MultiplyBy(yDis + _labelTextYSpace);
                    }
                    retText.Add(new CreateDBTextElement(_createFloor.floorUid, textStartPoint, text, pipe.createBlockUid, txtLayer, ThWSSCommon.Layout_TextStyle));
                }
                var startPipe = layoutDir.direction.Y < 0 ? thisLinePipes.Last() : thisLinePipes.First();
                var lineSp = new Point3d(centerPoint.X, startPipe.pipeCenterPoint.Y, 0);
                var lineEp = layoutDir.direction.Y < 0 ? createPoint : lineStartPoint;
                var mainLine = new Line(lineSp, lineEp);
                var addLine = new CreateBasicElement(_createFloor.floorUid, mainLine, ThWSSCommon.Layout_FloorDrainBlockRainLayerName, connectPipeIds, "LG_BSLJX");
                createBasicElements.Add(addLine);
                //将主线加入到后续的避让线中
                _obstacleLines.Add(mainLine);
            }
            return retText;
        }
        List<CheckDirection> GetLayoutDirections(List<LablePipe> thisLinePipes,Point3d centerPoint, double minX,double maxX,double textWidth,double textHeight) 
        {
            var layoutDirs = new List<CheckDirection>();
            var xAxis = Vector3d.XAxis;
            var yAxis = Vector3d.YAxis;
            var xy13 = (xAxis + yAxis).GetNormal();
            var xy24 = (xAxis.Negate() + yAxis).GetNormal();
            var startHeight = _pipeLabelNearDistance + textHeight;
            if (thisLinePipes.Count > 1)
            {
                //多个时只能与垂直方向的可布置区域
                if (centerPoint.Y >= _createFloorSpliteY)
                {
                    if (centerPoint.X + textWidth < maxX)
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X - textWidth >= minX)
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X + textWidth < maxX)
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X - textWidth >= minX)
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                }
                else 
                {
                    if (centerPoint.X + textWidth < maxX)
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X - textWidth >= minX)
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X + textWidth < maxX)
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X - textWidth >= minX)
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                }
                
            }
            else
            {
                //有多个区域可以布置
                if (centerPoint.Y >= _createFloorSpliteY)
                {
                    if (centerPoint.X + textWidth < maxX)
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X - textWidth > minX)
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X - textWidth > minX)
                    {
                        layoutDirs.Add(new CheckDirection(xy13, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }

                    if (centerPoint.X + textWidth < maxX)
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X - textWidth > minX)
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    
                    if (centerPoint.X + textWidth < maxX)
                    {
                        layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                }
                else 
                {
                    if (centerPoint.X + textWidth < maxX)
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X - textWidth > minX)
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X + textWidth < maxX)
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X - textWidth > minX)
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X + textWidth < maxX)
                    {
                        layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                    if (centerPoint.X - textWidth > minX)
                    {
                        layoutDirs.Add(new CheckDirection(xy13, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                }
            }
            return layoutDirs;
        }
        void GetTextHeightWidth(List<LablePipe> lablePipes,out double height,out double width) 
        {
            height = 0;
            width = 0;
            foreach (var item in lablePipes) 
            {
                var text = CreateDBText(item.pipeNumText, item.pipeCenterPoint,"",ThWSSCommon.Layout_TextStyle);
                var maxPoint = text.GeometricExtents.MaxPoint;
                var minPoint = text.GeometricExtents.MinPoint;
                var xDis =Math.Abs(maxPoint.X - minPoint.X);
                var yDis = Math.Abs(maxPoint.Y - minPoint.Y);
                height += yDis;
                width = Math.Max(width, xDis);
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
                if (CheckMainLineObstacleLines(mainLine))
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
                    if (CheckBySpaceIndex(textPLine))
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
        bool CheckBySpaceIndex(Polyline textPLine)
        {
            bool isIntersect = false;
            if (null == _obstacleSpatialIndex || textPLine == null || textPLine.Area < 10)
                return isIntersect;
            var crossPLines= _obstacleSpatialIndex.SelectCrossingPolygon(textPLine).Cast<Entity>();
            isIntersect = crossPLines != null && crossPLines.Count() > 0;
            var textGeo = textPLine.ToNTSPolygon();
            foreach (var item in _obstacleCreateEntities)
            {
                if (isIntersect)
                    break;
                if (item == null || item.Area < 10)
                    continue;
                var itemGeo = item.ToNTSPolygon();
                isIntersect = textGeo.Intersects(itemGeo) || textGeo.Crosses(itemGeo);
            }
            return isIntersect;
        }
        
        bool CheckMainLineObstacleLines(Line mainLine) 
        {
            if (_obstacleLines == null || _obstacleLines.Count < 1)
                return false;
            var mainDir = (mainLine.EndPoint - mainLine.StartPoint).GetNormal();
            bool isColl = false;
            foreach (var line in _obstacleLines) 
            {
                if (isColl)
                    break;
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                //先判断方向
                var dotDir = lineDir.DotProduct(mainDir);
                if (Math.Abs(dotDir) < 0.95)
                    continue;
                //再判断是否共线，将线投影到mainLine上，再进行精确判断
                var prjSp = line.StartPoint.PointToLine(mainLine);
                var prjEp = line.EndPoint.PointToLine(mainLine);
                if (prjSp.DistanceTo(line.StartPoint) > 10)
                    continue;
                var listPoints = new List<Point3d>(){ prjSp,prjEp };
                listPoints = PointVectorUtil.PointsOrderByDirection(listPoints, mainDir,false).ToList();
                var spVector = listPoints.First() - mainLine.StartPoint;
                var epVector = listPoints.Last() - mainLine.EndPoint;
                var spDot = spVector.DotProduct(mainDir);
                if (spDot > -0.0001)
                {
                    if (spVector.Length < line.Length - 1)
                    {
                        isColl = true;
                        break;
                    }
                }
                else 
                {
                    var spDotEp = spVector.DotProduct(epVector);
                    if (spDotEp < 0)
                    {
                        isColl = true;
                        break;
                    }
                }
            }
            return isColl;
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
        Dictionary<string, int> PipeIdNumber(List<CreateBlockInfo> orderPipes, bool isClockwise)
        {
            Dictionary<string, int> idNums = new Dictionary<string, int>();
            if (orderPipes == null || orderPipes.Count < 1)
                return idNums;
            if (orderPipes.Count == 1)
            {
                idNums.Add(orderPipes.First().uid, 1);
            }
            else
            {
                double midY = _createFloorSpliteY; //orderPipes.Sum(c => c.createPoint.Y) / orderPipes.Count;
                var upPipes = orderPipes.Where(c => c.createPoint.Y >= midY).ToList();
                var downPipes = orderPipes.Where(c => !upPipes.Any(x => x.uid.Equals(c.uid))).ToList();
                if (isClockwise)
                {
                    upPipes = upPipes.OrderBy(c => c.createPoint.X).ToList();
                    downPipes = downPipes.OrderByDescending(c => c.createPoint.X).ToList();
                }
                else
                {
                    upPipes = upPipes.OrderByDescending(c => c.createPoint.X).ToList();
                    downPipes = downPipes.OrderBy(c => c.createPoint.X).ToList();
                }
                int i = 1;
                foreach (var pipe in upPipes)
                {
                    idNums.Add(pipe.uid, i);
                    i += 1;
                }
                foreach (var pipe in downPipes)
                {
                    idNums.Add(pipe.uid, i);
                    i += 1;
                }
            }
            return idNums;
        }

        DBText CreateDBText(string str, Point3d position,string layerName,string styleName)
        {
            double height = _textHeight1_50;
            switch (SetServicesModel.Instance.drawingScale)
            {
                case EnumDrawingScale.DrawingScale1_50:
                    height = _textHeight1_50;
                    break;
                case EnumDrawingScale.DrawingScale1_100:
                    height = _textHeight1_100;
                    break;
                case EnumDrawingScale.DrawingScale1_150:
                    height = _textHeight1_150;
                    break;
            }
            DBText infotext = new DBText()
            {
                TextString = str,
                Height = height,
                WidthFactor = 0.7,
                HorizontalMode = TextHorizontalMode.TextLeft,
                Oblique = 0,
                Position = position,
                Rotation = 0,
            };
            if (!string.IsNullOrEmpty(layerName))
                infotext.Layer = layerName;
            if (!string.IsNullOrEmpty(styleName)) 
            {
                var styleId = DrawUtils.GetTextStyleId(styleName);
                if (null != styleId && styleId.IsValid) 
                {
                    infotext.TextStyleId = styleId;
                }
            }
            return infotext;
        }

        List<LablePipe> GetLinePipe(Point3d basePoint,List<LablePipe> areaAllPipe,Vector3d orderDir,double dirTolerance,double outTolerance)
        {
            var points = areaAllPipe.Select(c => c.pipeCenterPoint).ToList();
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
            var resPipe = new List<LablePipe>();
            foreach (var item in areaAllPipe) 
            {
                if (nearPoints.Any(c => c.DistanceTo(item.pipeCenterPoint) < 1))
                    resPipe.Add(item);
            }
            return resPipe;
        }

    }
    class CheckDirection 
    {
        public Vector3d direction { get; }
        public Vector3d outDirection { get; }
        public double minDistance { get; }
        public double maxDistance { get; }
        public double dirSetp { get; set; }
        public CheckDirection(Vector3d direction, Vector3d outDirection,double startDis,double maxDis,double step) 
        {
            this.direction = direction;
            this.outDirection = outDirection;
            this.minDistance = startDis<=0?0: startDis;
            this.maxDistance = maxDis >= this.minDistance ? maxDis : this.minDistance;
            this.dirSetp = step <= 0 ? 5 : step;
        }
    }
    class LablePipe
    {
        public string createBlockUid { get; }
        public string pipeAttrTag { get; }
        public string pipeNumText { get; }
        public Point3d pipeCenterPoint { get; }
        public LablePipe(CreateBlockInfo blockInfo,string numText) 
        {
            this.createBlockUid = blockInfo.uid;
            this.pipeAttrTag = blockInfo.tag;
            this.pipeCenterPoint = new Point3d(blockInfo.createPoint.X, blockInfo.createPoint.Y, blockInfo.createPoint.Z);
            this.pipeNumText = numText;
        }
    }

}
