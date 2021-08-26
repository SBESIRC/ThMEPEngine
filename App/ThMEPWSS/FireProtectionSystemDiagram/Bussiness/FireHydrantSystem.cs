using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.FireProtectionSystemDiagram.Models;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.FireProtectionSystemDiagram.Bussiness
{
    class FireHydrantSystem: FireHydrantBase
    {
        int _ringCount = 0;
        int _maxRingCount = 0;
        double _lineOffSetDefault = 150; // 相交管线时，管线打断的偏移距离
        double _ringPipeMaxBottomLength = 900;//外层大回路下沉高度
        double _checkValveWidth = 300;//止回阀宽度
        double _connectionReservationWidth = 415;//接驳预留长度
        double _shutOffValveWidth = 300;//截止阀宽度
        double _scaleVerticalRingFloor = 4.0 / 5.0;//普通层底部成环高度比例
        double _valveToMainLineLength = 600.0;//主线的蝶阀连接到支线的阀和线的整体宽度
        double _bottomLineOverLenght = 300.0;//底部线突出高度
        double _valveToMainBottomHeight = 300.0;//蝶阀高度下沉高度
        double _xTextAddLength = 60;//图中的立管线X标注的长度增加
        Point3d _ringPoint;
        public FireHydrantSystem(FloorGroupData floorGroup, List<FloorDataModel> floorDatas, FireControlSystemDiagramViewModel vm)
            :base(floorGroup,floorDatas,vm)
        { }
        public double GetRaisePipeStart() 
        {
            return _raisePipeDistanceStartPoint;
        }
        public List<CreateBasicElement> LayoutPipeFireHydrant(Point3d point, out List<CreateDBTextElement> createDBTexts, out List<CreateBlockInfo> createBlocks) 
        {
            _createBasicElements.Clear();
            _createBlocks.Clear();
            _createDBTexts.Clear();
            _ringCount = 0;
            _maxRingCount = 0;
            createDBTexts = new List<CreateDBTextElement>();
            createBlocks = new List<CreateBlockInfo>();
            var createBasic = new List<CreateBasicElement>();

            _RaisePipe(point);
            if (null != _createBasicElements && _createBasicElements.Count > 0)
                createBasic.AddRange(_createBasicElements);
            if (null != _createBlocks && _createBlocks.Count > 0)
                createBlocks.AddRange(_createBlocks);
            if (null != _createDBTexts && _createDBTexts.Count > 0)
                createDBTexts.AddRange(_createDBTexts);
            return createBasic;
        }
        private void _RaisePipe(Point3d origin) 
        {
            //普通层的消火栓个数确定立管个数
            int group = 0;
            var pipeStartPoint = origin + _xAxis.MultiplyBy(_raisePipeDistanceStartPoint);
            _ringPoint = pipeStartPoint;
            foreach (var keyValue in _floorGroup.floorGroups) 
            {
                int startFloor = keyValue.Key;
                int endFloor = keyValue.Value;
                bool isFirst = group == 0;
                bool isLast = group == _floorGroup.floorGroups.Count() - 1;
                bool firstIsRefugeFloor = _floorDatas.Where(c => c.floorNum == startFloor).FirstOrDefault().isRefugeFloor;
                bool endIsRefugeFloor = isLast ? false : _floorDatas.Where(c => c.floorNum == endFloor).FirstOrDefault().isRefugeFloor;
                var topPoint = _GetVerticalFloorRaiseTopPoint(pipeStartPoint, endFloor, 0);
                var floorBottomPoint = pipeStartPoint + _yAxis.MultiplyBy((startFloor - _minFloor) * _floorSpace);
                string dnStr = _mainFirePipeDN;
                string loopDNStr = _mainFirePipeDN;
                if (null != _floorGroup.floorGroupDN)
                {
                    foreach (var item in _floorGroup.floorGroupDN)
                    {
                        if (item.Key == group && !string.IsNullOrEmpty(item.Value))
                            dnStr = item.Value;
                        else if (endIsRefugeFloor && item.Key == (group + 1) && !string.IsNullOrEmpty(item.Value))
                            loopDNStr = item.Value;
                    }
                }
                if (!isFirst && !firstIsRefugeFloor)
                {
                    //处理底部非避难层成环的问题
                    _BottomFloorCreateRing(floorBottomPoint,startFloor, group, dnStr);
                }
                //顶部的连线、蝶阀、连线等处理
                if (!isLast)
                {
                    if (endIsRefugeFloor)
                    {
                        //这里处理后下一分组就不需要处理避难层
                        var topFloorBottmPoint  = _GetVerticalFloorRaiseBottomPoint(pipeStartPoint, endFloor, 0);
                        _TopFloorRefugeRingFloor(topPoint, topFloorBottmPoint, endFloor,group+1, loopDNStr);
                    }
                    else
                    {
                        //非最后分组，非避难层
                        _TopFloorNotRefugeFloor(topPoint,true, dnStr,true);
                    }
                }
                else
                {
                    //顶层顶部处理
                    _RoofFloorRingInFloor(pipeStartPoint,topPoint, dnStr);
                }
                //创建该分区的立管线 添加文字X{i}，管径标注
                int floorCount = endFloor - startFloor;
                var lineElements = new List<LineElementCreate>();
                for (int i=0;i< _raisePipeCount; i++) 
                {
                    lineElements.Clear();
                    var levelPoint = pipeStartPoint + _xAxis.MultiplyBy(i * _raisePipeSpace);
                    if (_refugeLineInts.Any(c => c == i))
                    {
                        if (endIsRefugeFloor) 
                        {
                            //避难层中间多出的立管，只创建短线
                            var firstTopPoint = _GetVerticalFloorRaiseTopPoint(pipeStartPoint, endFloor, 0);
                            var shortLineTopPoint = new Point3d(levelPoint.X, firstTopPoint.Y, levelPoint.Z);
                            var shortLineBottomPoint = _GetFireHBottomPoint(levelPoint, endFloor);
                            _AddPipeLineToCreateElems(shortLineTopPoint, shortLineBottomPoint);
                        }
                        continue;
                    }
                    var lineSp = _GetVerticalFloorRaiseBottomPoint(pipeStartPoint, startFloor, i);
                    var lineEp = _GetVerticalFloorRaiseTopPoint(pipeStartPoint, endFloor, i);
                    _RasieLineCreateAndDimGroup(levelPoint, lineSp, lineEp, startFloor, endFloor,i,group,false,true, dnStr);
                }
                group += 1;
            }
        }
        #region 非顶层楼层的环处理
        private void _TopFloorNotRefugeFloor(Point3d topStartPoint,bool refugeMidAddValve,string DNStr,bool createDim=true) 
        {
            //分组顶层，非避难层，非屋面层连线
            //放置排气阀
            var exhaustValve = new CreateBlockInfo(ThWSSCommon.Layout_ExhaustValveSystemBlockName, ThWSSCommon.Layout_FireHydrantPipeLineLayerName, topStartPoint);
            exhaustValve.scaleNum = 0.5;
            _createBlocks.Add(exhaustValve);
            var startPoint = topStartPoint;
            if (createDim) 
            {
                var text = _AddTextToCreateElems(DNStr, startPoint,0,false);
                FireProtectionSysCommon.GetTextHeightWidth(new List<DBText> { text }, out double textHeight, out double textWidth);
                var textCreatePoint = startPoint + _xAxis.MultiplyBy(_raisePipeSpace);
                textCreatePoint += _yAxis.MultiplyBy(50) - _xAxis.MultiplyBy(textWidth / 2);
                _AddTextToCreateElems(DNStr, textCreatePoint, 0);
            }
            _RaisePipeConnectRingLineTwoSide(startPoint,true, false, false);
            _FloorRaiseButterflyValve(startPoint, 0, 0, refugeMidAddValve);
        }
        private void _TopFloorRefugeRingFloor(Point3d bottomFloorTopPoint, Point3d topFloorBottomPoint,int endFloor,int groupNum,string DNStr)
        {
            //楼层顶部成环(避难层成环)
            bool refugeMidAddValve = false;
            _TopFloorNotRefugeFloor(bottomFloorTopPoint, refugeMidAddValve, DNStr, false);
            var vlaveStartPoint = topFloorBottomPoint - _yAxis.MultiplyBy(_GetButterflyValveDistanceToLine());
            _ringPoint -= _xAxis.MultiplyBy(_ringPipeSpace);
            _ringCount += 1;
            var topPoint1 = vlaveStartPoint;//第一根立管顶部点
            var topPoint2 = topPoint1 + _xAxis.MultiplyBy((_raisePipeCount - 1) * _raisePipeSpace);//最后一根立管顶部点
            var ringTopStartPoint = new Point3d(_ringPoint.X, topPoint1.Y, topPoint1.Z);
            var ringTopEndPoint = topPoint2 + _xAxis.MultiplyBy(_ringCount* _ringPipeSpace);
            var ringEndPoint = new Point3d(ringTopEndPoint.X, _ringPoint.Y, _ringPoint.Z);//环的结束立管的点
            //添加DN标注
            var textCreatePoint = ringTopStartPoint + _xAxis.MultiplyBy(ringTopStartPoint.DistanceTo(topPoint1) / 2);
            FireProtectionSysCommon.GetTextHeightWidth(new List<string> { DNStr }, _textHeight, ThWSSCommon.Layout_TextStyle, out double textHeight, out double textWidth);
            textCreatePoint = textCreatePoint - _xAxis.MultiplyBy(textWidth / 2) + _yAxis.MultiplyBy(50);
            _AddTextToCreateElems(DNStr, textCreatePoint, 0);
            //如果避难层数量多与普通层中间再加DN标注
            if (_fireHCount < _refugeFireHCount) 
            {
                var midPoint = topPoint1 + _xAxis.MultiplyBy((topPoint2 - topPoint1).Length / 2) - _xAxis.MultiplyBy(textWidth / 2) + _yAxis.MultiplyBy(50);
                _AddTextToCreateElems(DNStr, midPoint, 0);
            }
            _RasieLineCreateAndDimGroup(_ringPoint, _ringPoint, ringTopStartPoint, _minFloor, endFloor, -1, groupNum, true,true, DNStr);
            _RasieLineCreateAndDimGroup(ringEndPoint, ringEndPoint, ringTopEndPoint, _minFloor, endFloor, _raisePipeCount+1, groupNum, true,true, DNStr);
            _FloorRaiseButterflyValve(topPoint1, _lineOffSetDefault, ringTopEndPoint.DistanceTo(topPoint2), false);
            _RaisePipeConnectRingLine(topPoint1, true, true, true);
            var lineElements = new List<LineElementCreate>();
            if (!_HaveMaxRing(endFloor))
            {
                _LineCreateElement(ringTopStartPoint, topPoint1 - _xAxis.MultiplyBy(_lineOffSetDefault), lineElements);
                return;
            }
            //添加止回阀
            lineElements.Add(new LineElementCreate(_AddBlockToCreateElement(ringTopStartPoint, ThWSSCommon.Layout_CheckValveBlockName, false), _checkValveWidth, 300, EnumPosition.LeftCenter));
            _LineCreateElement(ringTopStartPoint, topPoint1 - _xAxis.MultiplyBy(_lineOffSetDefault), lineElements);
            //外层回路
            topPoint1 = bottomFloorTopPoint;
            var innerRingTopPoint = ringTopStartPoint + _xAxis.MultiplyBy(_ringPipeMaxBottomLength);
            var innerRingLinePoint =new Point3d(_ringPoint.X, topPoint1.Y, topPoint1.Z);
            _ringPoint -= _xAxis.MultiplyBy(_ringPipeMaxSpace);
            ringTopStartPoint = new Point3d(_ringPoint.X, topPoint1.Y, topPoint1.Z);
            _RasieLineCreateAndDimGroup(_ringPoint, _ringPoint, ringTopStartPoint, _minFloor, endFloor, -1, -1,true,true, DNStr);
            //添加连接回路的竖线横线
            var connectInnerRingPoint = new Point3d(innerRingTopPoint.X, topPoint1.Y, topPoint1.Z);
            _AddPipeLineToCreateElems(innerRingTopPoint, connectInnerRingPoint);
            _AddPipeLineToCreateElems(innerRingLinePoint + _xAxis.MultiplyBy(_lineOffSetDefault), connectInnerRingPoint);
            //外层上方连接 截止阀止回阀
            //最上方的距离起点的距离850，两个阀间距200
            lineElements.Clear();
            lineElements.Add(new LineElementCreate(_AddBlockToCreateElement(ringTopStartPoint, ThWSSCommon.Layout_ShutOffValve, false),_shutOffValveWidth, 850, EnumPosition.Center));
            lineElements.Add(new LineElementCreate(_AddBlockToCreateElement(ringTopStartPoint, ThWSSCommon.Layout_CheckValveBlockName, false), _checkValveWidth, 200, EnumPosition.LeftCenter));
            _LineCreateElement(ringTopStartPoint, innerRingLinePoint - _xAxis.MultiplyBy(_lineOffSetDefault), lineElements);
            ////外层下方连接，连接到下方的距离起点终点
            var btUpDisToStartPoint = 365.0;
            var btUpStartPoint = ringTopStartPoint + _xAxis.MultiplyBy(btUpDisToStartPoint);
            var btUpEndPoint = innerRingLinePoint - _xAxis.MultiplyBy(btUpDisToStartPoint);
            var btStartPoint = btUpStartPoint - _yAxis.MultiplyBy(_ringPipeMaxBottomLength);
            var btEndPoint = btUpEndPoint - _yAxis.MultiplyBy(_ringPipeMaxBottomLength);
            _AddPipeLineToCreateElems(btUpStartPoint, btStartPoint);
            _AddPipeLineToCreateElems(btUpEndPoint, btEndPoint);
            lineElements.Clear();
            lineElements.Add(new LineElementCreate(_AddBlockToCreateElement(btStartPoint, ThWSSCommon.Layout_ShutOffValve,false), _shutOffValveWidth, 100, EnumPosition.Center));
            lineElements.Add(new LineElementCreate(_AddBlockToCreateElement(btStartPoint, ThWSSCommon.Layout_ConnectionReserveBlcokName,false), _connectionReservationWidth, 50, EnumPosition.LeftCenter));
            lineElements.Add(new LineElementCreate(_AddBlockToCreateElement(btStartPoint, ThWSSCommon.Layout_CheckValveBlockName,false), _checkValveWidth, 50, EnumPosition.LeftCenter));
            lineElements.Add(new LineElementCreate(_AddBlockToCreateElement(btStartPoint, ThWSSCommon.Layout_SafetyValve,false), 0, 75, EnumPosition.LeftCenter));
            lineElements.Add(new LineElementCreate(_AddBlockToCreateElement(btStartPoint, ThWSSCommon.Layout_ShutOffValve, false), _shutOffValveWidth, 75, EnumPosition.Center));
            _LineCreateElement(btStartPoint, btEndPoint, lineElements);
            _maxRingCount += 1;
        }
        
        private void _BottomFloorCreateRing(Point3d floorBottomPoint,int startFloor,int groupNum,string DNStr) 
        {
            //非避难层底部环生成
            _ringPoint -= _xAxis.MultiplyBy(_ringPipeSpace);
            _ringCount += 1;
            var topPoint1 = floorBottomPoint + _yAxis.MultiplyBy(_floorSpace* _scaleVerticalRingFloor);
            var topPoint2 = topPoint1 + _xAxis.MultiplyBy((_raisePipeCount-1)*_raisePipeSpace);
            var topStartPoint = new Point3d(_ringPoint.X, topPoint1.Y, topPoint1.Z);
            var topEndPoint = topPoint2 + _xAxis.MultiplyBy(_ringCount * _ringPipeSpace);
            var ringEndPoint = new Point3d(topEndPoint.X, _ringPoint.Y, _ringPoint.Z);
            //添加回线，添加标注
            _RasieLineCreateAndDimGroup(_ringPoint, _ringPoint, topStartPoint,_minFloor,startFloor,-1,groupNum,true,true, DNStr);
            _RasieLineCreateAndDimGroup(ringEndPoint, ringEndPoint, topEndPoint, _minFloor, startFloor, _raisePipeCount+1, groupNum,true,true, DNStr);
            _StartEndPointCreateLineValve(topStartPoint, 0, topPoint1, -_lineOffSetDefault, false, true, false);
            _StartEndPointCreateLineValve(topPoint2, _lineOffSetDefault, topEndPoint, 0, false, false, false);
            _RaisePipeConnectRingLineTwoSide(topPoint1, true, true, false);
            //添加DN标注
            var textCreatePoint = topPoint1 - _xAxis.MultiplyBy(_ringPipeSpace - _textOffSet * 2) + _yAxis.MultiplyBy(_textOffSet);
            _AddTextToCreateElems(DNStr, textCreatePoint, 0);
            var startPoint = topPoint1;
            for (int i = 1; i < _raisePipeCount; i++)
            {
                if (_refugeLineInts.Any(c => c == i))
                    continue;
                var endPoint = topPoint1 + _xAxis.MultiplyBy(i * _raisePipeSpace);
                _StartEndPointCreateLineValve(startPoint, _lineOffSetDefault, endPoint, -_lineOffSetDefault, true, false, false);
                startPoint = endPoint;
            }
        }
        #endregion

        #region 最顶层的环处理
        private void _RoofFloorRingInFloor(Point3d firstLevelPipePoint,Point3d topStartPoint,string DNStr) 
        {
            var realLength = _GetButterflyValveScaleWidth();
            var startPoint = topStartPoint;
            var endPoint = startPoint + _xAxis.MultiplyBy((_raisePipeCount-1)*_raisePipeSpace);
            bool refugeIsMore = _fireHCount != _raisePipeCount;
            var pipePoint = startPoint;
            var valvePoint = startPoint;
            bool midAddValve = false;
            var lineElements = new List<LineElementCreate>();
            for (int i = 1; i < _raisePipeCount; i++)
            {
                if (_refugeLineInts.Any(c => c == i))
                {
                    midAddValve = true;
                    continue;
                }
                var pipeCurrentPoint = startPoint + _xAxis.MultiplyBy(i * _raisePipeSpace);
                var butterflyValve = _AddButterflyToCreateElement(pipeCurrentPoint, false);
                var disPre =_valveToMainBottomHeight;
                if (!midAddValve)
                {
                    if (i == _raisePipeCount - 1)
                    {
                        disPre = pipeCurrentPoint.DistanceTo(valvePoint) - disPre - realLength;
                    }
                    else if(i>1)
                    {
                        var midPoint = pipePoint + _xAxis.MultiplyBy(pipeCurrentPoint.DistanceTo(pipePoint) / 2);
                        disPre = midPoint.DistanceTo(valvePoint) - realLength;
                    }
                }
                else 
                {
                    var midPoint = pipePoint + _xAxis.MultiplyBy(pipeCurrentPoint.DistanceTo(pipePoint) / 2);
                    disPre = midPoint.DistanceTo(valvePoint)- realLength;
                }
                lineElements.Add(new LineElementCreate(butterflyValve, realLength, disPre, EnumPosition.LeftCenter));
                midAddValve = false;
                pipePoint = pipeCurrentPoint;
                valvePoint = valvePoint + _xAxis.MultiplyBy(disPre+ realLength);
            }
            _LineCreateElement(startPoint, endPoint, lineElements);
            _RaisePipeConnectRingLineTwoSide(startPoint, !_topRingInRoof, false, false,_topRingInRoof?0.0: _valveToMainLineLength);
            if (_topRingInRoof && _fireHCount>2) 
            {
                var innerPoint= _GetVerticalFloorRaiseTopPoint(firstLevelPipePoint, _maxFloor, 1);
                //需要将线连接到内部的立管上
                double disToLine = _GetButterflyValveDistanceToLine();
                bool valveXNegate = false;
                for (int i = 1; i < _raisePipeCount-1; i++) 
                {
                    if (_refugeLineInts.Any(c => c == i))
                    {
                        valveXNegate = true;
                        continue;
                    }
                    var currentPipePoint = startPoint + _xAxis.MultiplyBy(i * _raisePipeSpace);
                    var currentInnerPoint = new Point3d(currentPipePoint.X, innerPoint.Y, innerPoint.Z);
                    var bottomEndPoint = currentPipePoint - _yAxis.MultiplyBy(disToLine);
                    var disToInnerPoint = currentInnerPoint.DistanceTo(currentPipePoint)- disToLine;
                    bottomEndPoint = valveXNegate ? (bottomEndPoint + _xAxis.MultiplyBy(_valveToMainLineLength)) : (bottomEndPoint - _xAxis.MultiplyBy(_valveToMainLineLength));
                    var bottomInnerPoint = bottomEndPoint - _yAxis.MultiplyBy(disToInnerPoint);
                    _AddPipeLineToCreateElems(bottomEndPoint, bottomInnerPoint);
                    _AddPipeLineToCreateElems(bottomInnerPoint, currentInnerPoint);
                }
                var dnDimPoint = topStartPoint + _xAxis.MultiplyBy(_raisePipeSpace);
                _AddTextToCreateElems(DNStr, dnDimPoint+_yAxis.MultiplyBy(_textOffSet), 0);
            }
        }
        #endregion
        private void _FloorRaiseButterflyValve(Point3d topStartPoint,double startOffSet,double endOffSet, bool refugeMidAddValve) 
        {
            var realLength = _GetButterflyValveScaleWidth();
            var valveStartPoint = topStartPoint;
            var prePoint = topStartPoint + _xAxis.MultiplyBy(startOffSet);
            var topEndPoint = topStartPoint + _xAxis.MultiplyBy((_raisePipeCount - 1) * _raisePipeSpace);
            var lineElements = new List<LineElementCreate>();
            bool refugeEnd = false;
            for (int i = 1; i < _raisePipeCount; i++)
            {
                if (_refugeLineInts.Any(c => c == i))
                {
                    refugeEnd = true;
                    continue;
                }
                var endPoint = topStartPoint + _xAxis.MultiplyBy(i * _raisePipeSpace);
                if (!refugeMidAddValve && refugeEnd) 
                {
                    valveStartPoint = endPoint;
                    refugeEnd = false;
                    continue;
                }
                var midPoint = valveStartPoint + _xAxis.MultiplyBy((endPoint - valveStartPoint).Length / 2);
                var butterflyValve = _AddButterflyToCreateElement(midPoint, false);
                lineElements.Add(new LineElementCreate(butterflyValve, realLength, midPoint.DistanceTo(prePoint) - (i == 1 ? realLength / 2 : realLength), EnumPosition.LeftCenter));
                valveStartPoint = endPoint;
                prePoint = midPoint;
            }
            _LineCreateElement(topStartPoint+ _xAxis.MultiplyBy(startOffSet), topEndPoint+ _xAxis.MultiplyBy(endOffSet), lineElements);
        }
        private void _RaisePipeConnectRingLineTwoSide(Point3d topStartPoint,bool leftFirst, bool startEndCreate,bool isUpLine,double moveOffSet= 600)
        {
            bool valveXNegate = false;
            for (int i = 0; i < _raisePipeCount; i++)
            {
                if (!startEndCreate && (i == 0 || i == _raisePipeCount - 1))
                    continue;
                if (_refugeLineInts.Any(c => c == i))
                {
                    valveXNegate = true;
                    continue;
                }
                var pipeTopPoint = topStartPoint + _xAxis.MultiplyBy(i * _raisePipeSpace);
                var addPoint = pipeTopPoint;
                if (leftFirst)
                {
                    addPoint = valveXNegate ? pipeTopPoint + _xAxis.MultiplyBy(moveOffSet) : pipeTopPoint - _xAxis.MultiplyBy(moveOffSet);
                }
                else 
                {
                    addPoint = valveXNegate ? pipeTopPoint + _xAxis.MultiplyBy(moveOffSet) : pipeTopPoint - _xAxis.MultiplyBy(moveOffSet);
                }
                _RingBranchRaisePipe(addPoint, leftFirst?valveXNegate:!valveXNegate, isUpLine);
            }
        }
        private void _RaisePipeConnectRingLine(Point3d topStartPoint,bool inPointRight ,bool startEndCreate, bool isUpLine)
        {
            for (int i = 0; i < _raisePipeCount; i++)
            {
                if ((!startEndCreate && (i == 0 || i == _raisePipeCount - 1)) || _refugeLineInts.Any(c => c == i))
                    continue;
                var pipeTopPoint = topStartPoint + _xAxis.MultiplyBy(i * _raisePipeSpace);
                var addPoint = inPointRight? pipeTopPoint + _xAxis.MultiplyBy(_valveToMainLineLength) : pipeTopPoint - _xAxis.MultiplyBy(_valveToMainLineLength);
                _RingBranchRaisePipe(addPoint, inPointRight, isUpLine);
            }
        }
        private void _RasieLineCreateAndDimGroup(Point3d pipeFirstLevelPoint,Point3d lineSPoint,Point3d lineEndPoint,int startFloor,int endFloor,int pipeNum,int gourpNum,bool bottomCreate,bool addAreaDim,string DNStr) 
        {
            bool isLeft = _FireInRaisePipeRight(pipeNum);
            _RasiePipeAddDNDim(pipeFirstLevelPoint, startFloor, endFloor, !bottomCreate? gourpNum == 0:true, isLeft, DNStr);
            string xStr = string.Format("X{0}", gourpNum + 1);
            var bottomPoint = lineSPoint;
            if (startFloor == _minFloor) 
            {
                //底部延申300，添加终端块
                bottomPoint -= _yAxis.MultiplyBy(_bottomLineOverLenght);
                var block = new CreateBlockInfo("水管中断", ThWSSCommon.Layout_FireHydrantEqumLayerName, bottomPoint);
                block.rotateAngle = _xAxis.GetAngleTo(Vector3d.YAxis, Vector3d.ZAxis);
                _createBlocks.Add(block);
            }
            var dbText = _AddTextToCreateElems(xStr, bottomPoint, 0, false, "W-FRPT-NOTE");
            FireProtectionSysCommon.GetTextHeightWidth(new List<DBText> { dbText }, out double textHeight, out double textWidth);
            textWidth += _xTextAddLength;
            int floorCount = endFloor - startFloor;
            var startLevelPoint = pipeFirstLevelPoint + _yAxis.MultiplyBy((startFloor - _minFloor)*_floorSpace);
            var lineElements = new List<LineElementCreate>();
            var areaStr = "";
            if (addAreaDim) 
            {
                string num = _refugeLineInts.Count > 0 && pipeNum > _refugeLineInts.Max()? (pipeNum - _refugeLineInts.Count + 1).ToString():(pipeNum + 1).ToString();
                if (gourpNum < 0)
                {
                    //大回路，只在左侧
                    areaStr = string.Format("{0}L{1}-{2}", xStr, _areaAttr, _maxRingCount +1);
                }
                else if (pipeNum < 0)
                {
                    areaStr = string.Format("{0}L{1}-A", xStr, _areaAttr);
                }
                else if (pipeNum > _raisePipeCount)
                {
                    areaStr = string.Format("{0}L{1}-B", xStr, _areaAttr);
                }
                else 
                {
                    areaStr = string.Format("{0}L{1}-{2}", xStr, _areaAttr, num);
                }
            }
            if (floorCount <= 7)
            {
                int addFloor = startFloor == _minFloor? 2: floorCount / 2;
                var textPoint1 = startLevelPoint + _yAxis.MultiplyBy(addFloor * _floorSpace + _floorSpace / 2);
                var dis = textPoint1.Y - bottomPoint.Y - textWidth/2;
                lineElements.Add(new LineElementCreate(new CreateDBTextElement(bottomPoint, dbText, ThWSSCommon.Layout_FireHydrantTextLayerName, ThWSSCommon.Layout_TextStyle), dis,textWidth,_xTextAddLength/2));
                //添加文字
                if (addAreaDim)
                {
                    var raiseFloorUpPoint = startLevelPoint + _yAxis.MultiplyBy(addFloor * _floorSpace + _floorSpace);
                    _RaiseLinePointAddGroupAreaNum(raiseFloorUpPoint, areaStr, isLeft);
                }
            }
            else
            {
                var textPoint1 = startLevelPoint + _yAxis.MultiplyBy(2 * _floorSpace + _floorSpace / 2);
                var firstDis = textPoint1.Y - bottomPoint.Y - textWidth/2;
                lineElements.Add(new LineElementCreate(new CreateDBTextElement(bottomPoint, dbText, ThWSSCommon.Layout_FireHydrantTextLayerName, ThWSSCommon.Layout_TextStyle), firstDis, textWidth, _xTextAddLength / 2));
                if (addAreaDim)
                {
                    var raiseFloorUpPoint = startLevelPoint + _yAxis.MultiplyBy(2 * _floorSpace + _floorSpace);
                    _RaiseLinePointAddGroupAreaNum(raiseFloorUpPoint, areaStr, isLeft);
                }
                if (pipeNum > -1 && pipeNum < _raisePipeCount) 
                {
                    var textPoint2 = startLevelPoint + _yAxis.MultiplyBy((floorCount - 2) * _floorSpace + _floorSpace / 2);
                    var secondDis = textPoint2.Y - bottomPoint.Y - firstDis - textWidth*3/2;
                    lineElements.Add(new LineElementCreate(new CreateDBTextElement(bottomPoint, dbText, ThWSSCommon.Layout_FireHydrantTextLayerName, ThWSSCommon.Layout_TextStyle), secondDis, textWidth, _xTextAddLength / 2));
                    if (addAreaDim)
                    {
                        var raiseFloorUpPoint = startLevelPoint + _yAxis.MultiplyBy((floorCount - 2) * _floorSpace + _floorSpace);
                        _RaiseLinePointAddGroupAreaNum(raiseFloorUpPoint, areaStr, isLeft);
                    }
                }
            }
            _LineCreateElement(bottomPoint, lineEndPoint, lineElements);
        }
        void _RaiseLinePointAddGroupAreaNum(Point3d raiseUpPoint,string areaStr, bool isLeft) 
        {
            FireProtectionSysCommon.GetTextHeightWidth(new List<string> { areaStr }, _textHeight, ThWSSCommon.Layout_TextStyle, out double areaTextHeight, out double areaTextWidth);
            var textLinePoint = raiseUpPoint - _yAxis.MultiplyBy(areaTextHeight + _textOffSet*2 + 150);
            var textLineStartPoint = textLinePoint + _yAxis.MultiplyBy(150) - (isLeft ? _xAxis.MultiplyBy(150) : _xAxis.Negate().MultiplyBy(150));
            var textLineEndPoint = textLineStartPoint + (isLeft ? _xAxis.Negate().MultiplyBy(areaTextWidth + _textOffSet*2) : _xAxis.MultiplyBy(areaTextWidth + _textOffSet*2));
            var textCreatePoint = (isLeft ? textLineEndPoint : textLineStartPoint) + _xAxis.MultiplyBy(_textOffSet*2) +_yAxis.MultiplyBy(_xTextAddLength/2);
            _AddTextToCreateElems(areaStr, textCreatePoint, 0, true, "W-FRPT-NOTE");
            _AddLineToCreateElems(textLinePoint, textLineStartPoint,true,"W-FRPT-NOTE");
            _AddLineToCreateElems(textLineStartPoint, textLineEndPoint, true, "W-FRPT-NOTE");
        }
        /// <summary>
        /// 根据起点终点创建蝶阀
        /// </summary>
        /// <param name="startPoint">线的起点</param>
        /// <param name="startOffSet"></param>
        /// <param name="endPoint">线的终点</param>
        /// <param name="endOffSet"></param>
        /// <param name="midValve">线的中间添加蝶阀</param>
        /// <param name="bottomValve">底部是否添加链接到中间的立管的支路蝶阀</param>
        /// <param name="valveXNegate">支路蝶阀X方向是否和X轴相反</param>
        /// <param name="valveYNegate">支路蝶阀在线的上方或下方</param>
        void _StartEndPointCreateLineValve(Point3d startPoint, double startOffSet, Point3d endPoint, double endOffSet, bool midValve, bool bottomValve,bool valveXNegate,bool valveYNegate=false)
        {
            var realLength = _GetButterflyValveScaleWidth();
            var dis = startPoint.DistanceTo(endPoint);
            var dirX = (endPoint - startPoint).GetNormal();
            var centrPoint = startPoint + dirX.MultiplyBy(dis / 2);
            var createPoint = centrPoint - _xAxis.MultiplyBy(realLength / 2);
            //放置主线中间蝶阀
            if (midValve)
            {
                _AddButterflyToCreateElement(createPoint, true);
                //添加线
                _AddPipeLineToCreateElems(startPoint + dirX.MultiplyBy(startOffSet), createPoint);
                _AddPipeLineToCreateElems(createPoint + dirX.MultiplyBy(realLength), endPoint + dirX.MultiplyBy(endOffSet));
            }
            else
            {
                _AddPipeLineToCreateElems(startPoint + dirX.MultiplyBy(startOffSet), endPoint + dirX.MultiplyBy(endOffSet));
            }
            if (!bottomValve)
                return;
            if (valveXNegate)
                dirX = dirX.Negate();
            _RingBranchRaisePipe(endPoint - dirX.MultiplyBy(_valveToMainLineLength), valveXNegate, valveYNegate);
        }
        void _RingBranchRaisePipe(Point3d ringPoint, bool valveXNegate, bool valveYNegate) 
        {
            var realLength = _GetButterflyValveScaleWidth();
            //这里只考虑X,Y轴方向，其它方向不考虑
            var dirX = _xAxis;
            var dirY = _yAxis;
            if (valveXNegate)
                dirX = dirX.Negate();
            //添加连接到上方下方的蝶阀和线
            var point = ringPoint;
            var lineLength = _GetButterflyValveDistanceToLine();
            if (valveYNegate)
                dirY = dirY.Negate();
            var btLineSp = point - dirY.MultiplyBy(lineLength);
            var btLineCenter = btLineSp + dirX.MultiplyBy(_valveToMainLineLength/2);
            var btLineEp = btLineSp + dirX.MultiplyBy(_valveToMainLineLength);
            var lineEpStart = btLineCenter + dirX.MultiplyBy(realLength / 2);
            var btCreatePoint = btLineCenter - dirX.MultiplyBy(realLength / 2);
            var lineSpStart = btCreatePoint;
            if (dirX.X < 0)
            {
                btCreatePoint = btLineCenter + dirX.MultiplyBy(realLength / 2);
                lineEpStart = btLineCenter + dirX.MultiplyBy(realLength / 2);
                lineSpStart = btLineCenter - dirX.MultiplyBy(realLength / 2);
            }
            _AddButterflyToCreateElement(btCreatePoint,true);
            _AddPipeLineToCreateElems(point, btLineSp);
            _AddPipeLineToCreateElems(btLineSp, lineSpStart);
            _AddPipeLineToCreateElems(lineEpStart, btLineEp);
        }
       
        /// <summary>
        /// 立管添加管径标注
        /// </summary>
        /// <param name="raiseBottomBasePoint">该立管在首层标高上的点</param>
        /// <param name="startFloor">该立管的开始楼层</param>
        /// <param name="endFloor">该立管的结束楼层</param>
        /// <param name="bottomAdd">底部是否添加管径表示</param>
        /// <param name="isLeft">管径标注是否在线的左侧</param>
        void _RasiePipeAddDNDim(Point3d raiseBottomBasePoint,int startFloor, int endFloor,bool bottomAdd,bool isLeft,string DNStr) 
        {
            //立管线添加管径标注，添加环路标注
            int floorCount = endFloor - startFloor;
            var startPoint = raiseBottomBasePoint + _yAxis.MultiplyBy((startFloor - _minFloor) *_floorSpace);
            var text = _AddTextToCreateElems(DNStr, startPoint, 0, false);
            FireProtectionSysCommon.GetTextHeightWidth(new List<DBText> { text }, out double textHeight, out double textWidth);
            if (bottomAdd)
            {
                var btCreatePoint = startPoint + _yAxis.MultiplyBy(_floorSpace / 2);
                btCreatePoint -= _yAxis.MultiplyBy(textWidth / 2);
                btCreatePoint = isLeft ? (btCreatePoint - _xAxis.MultiplyBy(_textOffSet)) : (btCreatePoint + _xAxis.MultiplyBy(_textOffSet + textHeight));
                _AddTextToCreateElems(DNStr, btCreatePoint, Math.PI / 2);
            }
            else if (floorCount <= 7)
            {
                var btCreatePoint = startPoint + _yAxis.MultiplyBy((floorCount / 2 -1) * _floorSpace + _floorSpace / 2);
                btCreatePoint -= _yAxis.MultiplyBy(textWidth / 2);
                btCreatePoint = isLeft ? btCreatePoint - _xAxis.MultiplyBy(_textOffSet) : btCreatePoint + _xAxis.MultiplyBy(_textOffSet + textHeight);
                _AddTextToCreateElems(DNStr, btCreatePoint, Math.PI / 2);
            }
            if(floorCount>7)
            {
                if (!bottomAdd) 
                {
                    var btCreatePoint1 = startPoint + _yAxis.MultiplyBy(3 * _floorSpace + _floorSpace / 2);
                    btCreatePoint1 -= _yAxis.MultiplyBy(textWidth / 2);
                    btCreatePoint1 = isLeft ? (btCreatePoint1 - _xAxis.MultiplyBy(_textOffSet)) : (btCreatePoint1 + _xAxis.MultiplyBy(_textOffSet + textHeight));
                    _AddTextToCreateElems(DNStr, btCreatePoint1, Math.PI / 2);
                }
                var btCreatePoint2 = startPoint + _yAxis.MultiplyBy((floorCount - 3) * _floorSpace + _floorSpace / 2);
                btCreatePoint2 -= _yAxis.MultiplyBy(textWidth / 2);
                btCreatePoint2 = isLeft ? (btCreatePoint2 - _xAxis.MultiplyBy(_textOffSet)) : (btCreatePoint2 + _xAxis.MultiplyBy(_textOffSet + textHeight));
                _AddTextToCreateElems(DNStr, btCreatePoint2, Math.PI / 2);
            }
        }
    }
}
