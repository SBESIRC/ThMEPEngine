using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Common;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    class PipelineLabel
    {
        private double _textHeight1_50 = 175;
        private double _textHeight1_100 = 350;
        private double _textHeight1_150 = 525;
        private double _pipeLabelNearDistance = 500;
        private double _pipeLabelMaxDistance = 2000;
        private double _pipeLavelDirectionMoveStep = 100;
        FloorFramed _floor;
        List<CreateBlockInfo> _thisFloorPipes;
        private List<ObstacleEntity> _obstacleEntities;
        private List<Line> _obstacleLines;
        List<CreateBasicElement> createBasicElements;

        public PipelineLabel(FloorFramed floor, List<CreateBlockInfo> thisFloorBlocks, List<CreateBasicElement> thisBasicElement)
        {
            _floor = floor;
            _obstacleLines = new List<Line>();
            _obstacleEntities = new List<ObstacleEntity>();
            _thisFloorPipes = new List<CreateBlockInfo>();
            createBasicElements = new List<CreateBasicElement>();
            foreach (var cBlock in thisFloorBlocks)
            {
                if (!cBlock.floorId.Equals(floor.floorUid))
                    continue;
                if (cBlock.equipmentType != EnumEquipmentType.floorDrain && 
                    cBlock.equipmentType != EnumEquipmentType.riser &&
                    cBlock.equipmentType != EnumEquipmentType.balconyRiser && 
                    cBlock.equipmentType != EnumEquipmentType.condensateRiser && 
                    cBlock.equipmentType != EnumEquipmentType.roofRainRiser)
                    continue;
                //将地漏，立管加入到避让信息中,这里认为是矩形，这个时候没有在图纸中生成，没有拿到具体的大小
                var point = cBlock.createPoint;
                point = point - Vector3d.XAxis.MultiplyBy(150);
                point = point - Vector3d.YAxis.MultiplyBy(150);
                var pline = TextOutPolyLine(point, Vector3d.XAxis, 300, Vector3d.YAxis, 300);
                _obstacleEntities.Add(new ObstacleEntity(pline));
                if (string.IsNullOrEmpty(cBlock.tag))
                    continue;
                if (cBlock.tag.ToUpper().Equals("Y1L") || cBlock.tag.ToUpper().Equals("Y2L") || cBlock.tag.ToUpper().Equals("NL")
                    || cBlock.tag.ToUpper().Equals("FL") || cBlock.tag.ToUpper().Equals("PL") || cBlock.tag.ToUpper().Equals("TL") || cBlock.tag.ToUpper().Equals("DL"))
                    _thisFloorPipes.Add(cBlock);
            }
        }
        public void AddObstacleEntity(Entity entity)
        {
            _obstacleEntities.Add(new ObstacleEntity(entity));
        }
        public void AddObstacleEntity(Polyline pline)
        {
            _obstacleEntities.Add(new ObstacleEntity(pline));
        }
        public void AddObstacleEntitys(List<Entity> entitys)
        {
            if (null == entitys || entitys.Count < 1)
                return;
            foreach(var entity in entitys)
                _obstacleEntities.Add(new ObstacleEntity(entity));
        }
        public void AddObstacleEntitys(List<Polyline> polylines)
        {
            if (null == polylines || polylines.Count < 1)
                return;
            foreach (var pline in polylines)
                _obstacleEntities.Add(new ObstacleEntity(pline));
        }
        public void AddObstacleEntity(ObstacleEntity obsEntity)
        {
            _obstacleEntities.Add(obsEntity);
        }
        public void ClearObstacle()
        {
            _obstacleEntities.Clear();
        }
        public List<CreateDBTextElement> SpliteFloorSpace(out List<CreateBasicElement> createBasics)
        {
            createBasicElements.Clear();
            createBasics = new List<CreateBasicElement>();
            var allTexts = new List<CreateDBTextElement>();
            List<Line> spliteLines = FramedReadUtil.FloorFrameSpliteLines(_floor);
            List<double> spliteX = spliteLines.Select(c => c.StartPoint.X).ToList();
            double floorStartX = _floor.floorBlock.Position.X;
            double floorEndX = floorStartX + _floor.width;
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
                bool isClockwise = i % 2 == 1;
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
        List<CreateDBTextElement> LayoutTextAvoidObstacleEntity(double minX, double maxX,List<LablePipe> areaAllPipe)
        {
            var retText = new List<CreateDBTextElement>();
            if (areaAllPipe ==null || areaAllPipe.Count < 1)
                return retText;
            List<string> hisPipes = new List<string>();
            var xAxis = Vector3d.XAxis;
            var yAxis = Vector3d.YAxis;
            var xy13 = (xAxis + yAxis).GetNormal();
            var xy24 = (xAxis.Negate() + yAxis).GetNormal();
            //标注线的方向沿Y轴，
            //所有立管全部布置完毕后，以立管的圆心（容差10）为起点在Y轴正负方向找距离500范围内的的其他立管。若500范围内找到了立管则继续找，直到找不到为止，将找到的组成一队。
            var tempPipes = new List<LablePipe>();
            areaAllPipe.ForEach(c => tempPipes.Add(c));
            while (tempPipes.Count > 0) 
            {
                var basePipe = tempPipes.FirstOrDefault();
                var thisLinePipes = GetLinePipe(basePipe.pipeCenterPoint, tempPipes,Vector3d.YAxis,500,10);
                hisPipes.AddRange(thisLinePipes.Select(c => c.createBlockUid).ToList());
                tempPipes = tempPipes.Where(c => !hisPipes.Any(x => x.Equals(c.createBlockUid))).ToList();
                var centerPoint = PointVectorUtil.PointsAverageValue(thisLinePipes.Select(c => c.pipeCenterPoint).ToList());
                GetTextHeightWidth(thisLinePipes, out double textHeight, out double textWidth);
                textHeight += 150;
                textWidth += 100;
                bool canCreate = false;
                var createPoint = new Point3d();
                var checkDirections = new List<CheckDirection>();
                CheckDirection checkDirection = null;
                if (thisLinePipes.Count > 1)
                {
                    //多个时只能与垂直方向的可布置区域
                    if(centerPoint.X+textWidth<maxX)
                        checkDirections.Add(new CheckDirection(yAxis.Negate(), xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X - textWidth >=minX)
                        checkDirections.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X + textWidth < maxX)
                        checkDirections.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    if (centerPoint.X - textWidth >= minX)
                        checkDirections.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                }
                else 
                {
                    //有多个区域可以布置
                    checkDirections.Add(new CheckDirection(yAxis.Negate(), xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    checkDirections.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    checkDirections.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    checkDirections.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    checkDirections.Add(new CheckDirection(xy24.Negate(), xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    checkDirections.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    checkDirections.Add(new CheckDirection(xy13, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    checkDirections.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                }
                foreach (var item in checkDirections) 
                {
                    if (canCreate)
                        break;
                    checkDirection = item;
                    canCreate = CanAddTextInDir(centerPoint, item, textHeight, textWidth, out createPoint);
                }
                if (!canCreate)
                {
                    //没有满足条件的可布置点。
                    checkDirection = checkDirections.FirstOrDefault();
                    createPoint = centerPoint + checkDirection.direction.MultiplyBy(checkDirection.maxDistance / 2);
                }
                //添加文字，并将文字加入到躲避区域中，后续要躲避该文字
                var textPLine = TextOutPolyLine(createPoint, checkDirection.outDirection, textWidth, Vector3d.YAxis, textHeight);
                _obstacleEntities.Add(new ObstacleEntity(textPLine));
                if (checkDirection.direction.Y < 0)
                    thisLinePipes = thisLinePipes.OrderByDescending(c => c.pipeCenterPoint.Y).ToList();
                else
                    thisLinePipes = thisLinePipes.OrderBy(c => c.pipeCenterPoint.Y).ToList();
                var startPoint = createPoint;
                string blIds = "";
                for (int i = 0; i < thisLinePipes.Count; i++)
                {
                    var pipe = thisLinePipes[i];
                    blIds += pipe.createBlockUid + ",";
                    var textPoint = startPoint;
                    if (checkDirection.outDirection.X < 0)
                    {
                        textPoint = textPoint + checkDirection.outDirection.MultiplyBy(textWidth);
                    }
                    string txtLayer = ThWSSCommon.Layout_PipeRainTextLayerName;
                    if (pipe.pipeAttrTag.ToUpper().Equals("FL") || pipe.pipeAttrTag.ToUpper().Equals("PL") || pipe.pipeAttrTag.ToUpper().Equals("TL"))
                        txtLayer = ThWSSCommon.Layout_PipeWastDrainTextLayerName;
                    var textCreatePoint = textPoint + Vector3d.YAxis.MultiplyBy(50);
                    textCreatePoint += Vector3d.XAxis.MultiplyBy(50);
                    var text = CreateDBText(pipe.pipeNumText, textCreatePoint, txtLayer);
                    if (i != thisLinePipes.Count - 1)
                    {
                        var maxPoint = text.GeometricExtents.MaxPoint;
                        var minPoint = text.GeometricExtents.MinPoint;
                        var xDis = Math.Abs(maxPoint.X - minPoint.X);
                        var yDis = Math.Abs(maxPoint.Y - minPoint.Y);
                        startPoint = startPoint + checkDirection.direction.MultiplyBy(yDis + 200);
                    }
                    var s = new CreateBasicElement(_floor.floorUid, new Line(textPoint, textPoint + Vector3d.XAxis.MultiplyBy(textWidth)), ThWSSCommon.Layout_FloorDrainBlockRainLayerName, pipe.createBlockUid, "LG_BSLJX");
                    createBasicElements.Add(s);
                    retText.Add(new CreateDBTextElement(_floor.floorUid, textPoint, text, pipe.createBlockUid, txtLayer, ThWSSCommon.Layout_TextStyle));
                }
                var lineSp = new Point3d(centerPoint.X, thisLinePipes.First().pipeCenterPoint.Y, 0);
                blIds = blIds.Substring(0, blIds.Length - 1);
                var addLine = new CreateBasicElement(_floor.floorUid, new Line(lineSp, startPoint), ThWSSCommon.Layout_FloorDrainBlockRainLayerName, blIds, "LG_BSLJX");
                createBasicElements.Add(addLine);
                //将主线加入到后续的避让线中
                _obstacleLines.Add(new Line(lineSp, startPoint));
            }
            return retText;
        }
        void GetTextHeightWidth(List<LablePipe> lablePipes,out double height,out double width) 
        {
            height = 0;
            width = 0;
            foreach (var item in lablePipes) 
            {
                var text = CreateDBText(item.pipeNumText, item.pipeCenterPoint,"");
                var maxPoint = text.GeometricExtents.MaxPoint;
                var minPoint = text.GeometricExtents.MinPoint;
                var xDis =Math.Abs(maxPoint.X - minPoint.X);
                var yDis = Math.Abs(maxPoint.Y - minPoint.Y);
                height += yDis;
                width = Math.Max(width, xDis);
            }
        }
        bool CanAddTextInDir(Point3d centerPoint, CheckDirection checkDirection, double textHeight,double textLength,out Point3d createPoint) 
        {
            var lineDir = checkDirection.direction;
            var sideDir = checkDirection.outDirection;
            createPoint = new Point3d();
            //以一组立管的几何中心点为起点，以2000+最长文字下划线长度为半径画圆。圆心内先扣掉一个半径为500的同心圆，然后扣掉所有要躲避的图元。
            double disStart = checkDirection.minDistance;
            double step = checkDirection.dirSetp;
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
                if (CheckMainLineObstacleLines(mainLine))
                {
                    //和其它线有共线的情况
                    return false;
                }
                var textPLine= TextOutPolyLine(startPoint, sideDir, textLength, Vector3d.YAxis, textHeight);
                if (CheckTextOverObstacleEntities(textPLine)) 
                {
                    //可进一步判断辅线X轴方向是否符合要求
                    startPoint += lineDir.MultiplyBy(step);
                    continue;
                }
                canAdd = true;
            }
            if (canAdd)
                createPoint = startPoint;
            return canAdd;
        }
        bool CheckTextOverObstacleEntities(Polyline textPLine) 
        {
            var textGeo = textPLine.ToNTSGeometry();
            bool isIntersect = false;
            foreach (var item in _obstacleEntities) 
            {
                if (isIntersect)
                    break;
                if (item.outPolyLine == null || item.outPolyLine.Area < 10)
                    continue;
                var itemGeo = item.outPolyLine.ToNTSGeometry();
                isIntersect = textGeo.Intersects(itemGeo);
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
            Point2d sp2dNext = new Point2d(textPoint3.X, textPoint3.Y);
            Point2d ep2dNext = new Point2d(textPoint4.X, textPoint4.Y);
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
                double midY = orderPipes.Sum(c => c.createPoint.Y) / orderPipes.Count;
                var upPipes = orderPipes.Where(c => c.createPoint.Y >= midY).ToList();
                var downPipes = orderPipes.Where(c => !upPipes.Any(x => x.uid.Equals(c.uid))).ToList();
                if (isClockwise)
                {
                    upPipes = upPipes.OrderBy(c => c.createPoint.Y).ToList();
                    downPipes = downPipes.OrderByDescending(c => c.createPoint.Y).ToList();
                }
                else
                {
                    upPipes = upPipes.OrderByDescending(c => c.createPoint.Y).ToList();
                    downPipes = downPipes.OrderBy(c => c.createPoint.Y).ToList();
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

        DBText CreateDBText(string str, Point3d position,string layerName)
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
            DBText infortext = new DBText()
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
                infortext.Layer = layerName;
            return infortext;
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
            this.pipeCenterPoint = blockInfo.createPoint;
            this.pipeNumText = numText;
        }
    }
    class ObstacleEntity
    {
        public string uid { get; }
        public Polyline outPolyLine { get; }
        public Entity entity { get; }
        public ObstacleEntity(Entity entity) 
        {
            this.uid = Guid.NewGuid().ToString();
            this.entity = entity;
            if (entity is BlockReference)
            {
                var block = entity as BlockReference;
                var ntsPLine = entity.ToNTSPolygon();
                this.outPolyLine = ntsPLine.ToDbPolylines().FirstOrDefault();
            }
            else if (entity is Polyline pLine) 
            {
                if (pLine.Area < 0.001)
                    return;
                this.outPolyLine = pLine.CalObb();
            }
            else if (entity is Circle || entity is Arc)
            {
                this.outPolyLine = entity.GeometricExtents.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
            }
            else if (entity is Line)
            {
                this.outPolyLine = (entity as Line).Buffer(10);
            }
            else if (entity is DBText || entity is MText)
            {
                this.outPolyLine = entity.GeometricExtents.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
            }
        }
        public ObstacleEntity(Polyline polyline)
        {
            if (polyline == null || polyline.Area < 0.001)
                return;
            this.uid = Guid.NewGuid().ToString();
            this.entity = null;
            this.outPolyLine = polyline;
        }
    }
}
