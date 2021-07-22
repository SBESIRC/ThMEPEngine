using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.FireProtectionSystemDiagram.Models;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.FireProtectionSystemDiagram.Bussiness
{
    class FireHydrantLayout: FireHydrantBase
    {
        double _testFireHydrantDistanceToFirstPipe = 1800;//试验消火栓中起点距离最左侧的立管距离
        double _testFireHydrantVentDistanceStart = 1200;//试验消火栓中的排气阀距离起点的距离
        double _testFireHydrantVentDistanceLine = 450;//实验消火栓中排气阀距离管线的距离
        double _testFireHydrantButterflyValveDisctanceToStart = 700;//实验消火栓中蝶阀距离起点的距离
        double _roofTestFireHydrantDistanecToTopLine = 300;//屋面试验消火栓距离顶部线距离
        double _roofTestFireHydrantDistanceToTopLevel = 700;//楼层消火栓线距离屋顶标高距离
        public FireHydrantLayout(FloorGroupData floorGroup, List<FloorDataModel> floorDatas, FireControlSystemDiagramViewModel vm)
            : base(floorGroup, floorDatas, vm)
        {}
        public List<CreateBasicElement> FireHydrantBlocks(Point3d origin, out List<CreateDBTextElement> createDBTexts, out List<CreateBlockInfo> createBlocks)
        {
            _createBasicElements.Clear();
            _createBlocks.Clear();
            _createDBTexts.Clear();
            createDBTexts = new List<CreateDBTextElement>();
            createBlocks = new List<CreateBlockInfo>();
            var createBasic = new List<CreateBasicElement>();
            var lineDir = _xAxis;
            var lineStartPoint = origin + _xAxis.MultiplyBy(_raisePipeDistanceStartPoint);
            var startFloor = _floorDatas.Min(c => c.floorNum);
            for (int i = 0; i < _raisePipeCount; i++)
            {
                if (_fireHCount == 1 && i == _raisePipeCount - 1)
                    continue;
                var lineSp = lineStartPoint + _xAxis.MultiplyBy(i * _raisePipeSpace);
                lineDir = _FireInRaisePipeRight(i) ? _xAxis : _xAxis.Negate();
                bool isRefuge = _refugeLineInts.Any(c => c == i);
                _FireHydrantBlocks(lineSp, lineDir, isRefuge);
            }

            //实验消火栓部分
            var pipeStartPoint = origin + _xAxis.MultiplyBy(_raisePipeDistanceStartPoint);
            var topPoint = _GetVerticalFloorRaiseTopPoint(pipeStartPoint, _maxFloor, 0);
            _RoofFloorTestFireHydrant(topPoint);

            if (null != _createBasicElements && _createBasicElements.Count > 0)
                createBasic.AddRange(_createBasicElements);
            if (null != _createBlocks && _createBlocks.Count > 0)
                createBlocks.AddRange(_createBlocks);
            if (null != _createDBTexts && _createDBTexts.Count > 0)
                createDBTexts.AddRange(_createDBTexts);
            return createBasic;
        }
        private void _FireHydrantBlocks(Point3d firstFloorRaisePoint, Vector3d layoutSideDir, bool onlyRefuge)
        {
            //创建消火栓
            var startFloor = _floorDatas.Min(c => c.floorNum);
            while (startFloor <= _maxFloor)
            {
                var floor = _floorDatas.Where(c => c.floorNum == startFloor).FirstOrDefault();
                if (null == floor || (onlyRefuge && !floor.isRefugeFloor))
                {
                    startFloor += 1;
                    continue;
                }
                var lineSp = _GetFireHBottomPoint(firstFloorRaisePoint, startFloor);
                var lineEp = lineSp + layoutSideDir.MultiplyBy(_fireHydrantOutRaisePipe);
                _AddPipeLineToCreateElems(lineSp, lineEp);
                _AddFireHydrantToCreate(lineEp, _fireHydrantType);
                //横管管径标注
                var text = _AddTextToCreateElems(_secondFirePipeDN, lineEp, 0,false);
                FireProtectionSysCommon.GetTextHeightWidth(new List<DBText> { text }, out double textHeight, out double textWidth);
                var textCreatePoint = lineSp + layoutSideDir.MultiplyBy(_textOffSet);
                textCreatePoint += _yAxis.Negate().MultiplyBy(textHeight + _textOffSet+30);
                if (layoutSideDir.X < 0)
                {
                    textCreatePoint += layoutSideDir.MultiplyBy(textWidth);
                }
                _AddTextToCreateElems(_secondFirePipeDN, textCreatePoint, 0);
                startFloor += 1;
            }
        }
        private void _RoofFloorTestFireHydrant(Point3d topStartPoint)
        {
            if (!_haveTestFireHydrant)
                return;
            var upDis = _topRingInRoof ? _roofTestFireHydrantDistanecToTopLine : _roofTestFireHydrantDistanceToTopLevel + (_floorSpace - _scaleTopFloorTopLine * _floorSpace);
            var testFireHBottomPoint = _raisePipeCount > 1 ? topStartPoint + _xAxis.MultiplyBy(_raisePipeSpace / 2) : topStartPoint;
            var testFireHUpPoint = testFireHBottomPoint + _yAxis.MultiplyBy(upDis);
            var testFireHUpStartPoint = testFireHUpPoint - _xAxis.MultiplyBy(_testFireHydrantDistanceToFirstPipe + _raisePipeSpace / 2);
            _AddPipeLineToCreateElems(testFireHUpPoint, testFireHBottomPoint);
            //在距离起点1200处放排气阀 添加文字
            var valveBottomPoint = testFireHUpStartPoint + _xAxis.MultiplyBy(_testFireHydrantVentDistanceStart);
            var valveCreatePoint = valveBottomPoint + _yAxis.MultiplyBy(_testFireHydrantVentDistanceLine);
            _createBlocks.Add(new CreateBlockInfo(ThWSSCommon.Layout_ExhaustValveSystemBlockName, ThWSSCommon.Layout_FireHydrantPipeLineLayerName, valveCreatePoint));
            _AddPipeLineToCreateElems(valveBottomPoint, valveCreatePoint);
            var valveTxtBottomPoint = valveCreatePoint + _yAxis.MultiplyBy(562) + _xAxis.MultiplyBy(92);
            var xy = (_xAxis + _yAxis).GetNormal();
            var valveTxtLineStartPoint = valveTxtBottomPoint + xy.MultiplyBy(300);
            FireProtectionSysCommon.GetTextHeightWidth(new List<string> { "自动排气阀DN25，余同 ", "该排气阀高度距地1.5m" }, _textHeight, ThWSSCommon.Layout_TextStyle, out double textHeight, out double textWidth);
            var valceTxtLineEndPoint = valveTxtLineStartPoint + _xAxis.MultiplyBy(textWidth + _textOffSet*2);
            _createBasicElements.Add(new CreateBasicElement(new Line(valveTxtBottomPoint, valveTxtLineStartPoint), ThWSSCommon.Layout_FireHydrantDescriptionLayerName));
            _createBasicElements.Add(new CreateBasicElement(new Line(valveTxtLineStartPoint, valceTxtLineEndPoint), ThWSSCommon.Layout_FireHydrantDescriptionLayerName));
            var textCreatePoint1 = valveTxtLineStartPoint + _xAxis.MultiplyBy(_textOffSet) + _yAxis.MultiplyBy(_textOffSet);
            _AddTextToCreateElemsWSUP("自动排气阀DN25，余同", textCreatePoint1, 0, true);
            var textCreatePoint2 = valveTxtLineStartPoint + _xAxis.MultiplyBy(_textOffSet) - _yAxis.MultiplyBy(textHeight / 2 + _textOffSet);
            _AddTextToCreateElemsWSUP("该排气阀高度距地1.5m", textCreatePoint2, 0, true);
            //起点添加实验消火栓  添加文字
            _AddFireHydrantToCreate(testFireHUpStartPoint, "试验消火栓");
            var txtLineTopPoint = testFireHUpStartPoint + _yAxis.MultiplyBy(500);
            var txtLineEndPoint = txtLineTopPoint - _yAxis.MultiplyBy(400) - _xAxis.MultiplyBy(400);
            FireProtectionSysCommon.GetTextHeightWidth(new List<string> { "试验消火栓" }, _textHeight, ThWSSCommon.Layout_TextStyle, out textHeight, out textWidth);
            var txtLineStartPoint = txtLineEndPoint - _xAxis.MultiplyBy(textWidth + _textOffSet*2);
            _createBasicElements.Add(new CreateBasicElement(new Line(txtLineStartPoint, txtLineEndPoint), ThWSSCommon.Layout_FireHydrantDescriptionLayerName));
            _createBasicElements.Add(new CreateBasicElement(new Line(txtLineEndPoint, txtLineTopPoint), ThWSSCommon.Layout_FireHydrantDescriptionLayerName));
            _AddTextToCreateElemsWSUP("试验消火栓", txtLineStartPoint + _xAxis.MultiplyBy(_textOffSet), 0, true);
            var lineElements = new List<LineElementCreate>();
            var butterflyValve = _AddButterflyToCreateElement(testFireHUpStartPoint, false);
            lineElements.Add(new LineElementCreate(butterflyValve, _GetButterflyValveScaleWidth(), _testFireHydrantButterflyValveDisctanceToStart, EnumPosition.LeftCenter));
            _LineCreateElement(testFireHUpStartPoint, testFireHUpPoint, lineElements);
        }
        private void _AddFireHydrantToCreate(Point3d fireHCreatePoint, string fireHType)
        {
            var addFireH = new CreateBlockInfo(ThWSSCommon.Layout_FireHydrantBlockName, ThWSSCommon.Layout_FireHydrantLayerName, fireHCreatePoint);
            if (!string.IsNullOrEmpty(fireHType))
                addFireH.dymBlockAttr.Add("可见性", fireHType);
            _createBlocks.Add(addFireH);
        }
    }
}
