using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.FireProtectionSystemDiagram.Models;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.FireProtectionSystemDiagram.Bussiness
{
    abstract class FireHydrantBase
    {
        protected Vector3d _xAxis;
        protected Vector3d _yAxis;
        protected int _minFloor;//最低楼层
        protected int _maxFloor;//最高楼层
        protected int _fireHCount;//普通层消火栓个数
        protected int _refugeFireHCount;//避难层的消火栓个数
        protected FloorGroupData _floorGroup;
        protected List<FloorDataModel> _floorDatas;
        protected string _mainFirePipeDN = "DN100";//主管路的管线直径
        protected string _secondFirePipeDN = "DN65";//支管的管线直径
        protected double _textHeight = 350;//文字高度
        protected double _textOffSet = 50.0;
        protected double _fireHydrantOutRaisePipe = 600;//消火栓距离立管就距离
        protected double _raisePipeDistanceStartPoint = 10000;
        protected double _ringPipeSpace = 1500;//环路的间距
        protected double _ringPipeMaxSpace = 2500;//环路中的大环管距离环路距离
        protected int _raisePipeCount;//根据消火栓确定的立管个数
        protected List<int> _refugeLineInts;
        protected string _fireHydrantType = "";
        protected List<CreateBasicElement> _createBasicElements;
        protected List<CreateDBTextElement> _createDBTexts;
        protected List<CreateBlockInfo> _createBlocks;
        protected double _floorSpace = 1800.0;//楼层间距
        protected double _raisePipeSpace = 2000;//立管间距
        protected bool _topRingInRoof = false;
        protected bool _haveTestFireHydrant = false;
        protected double _scaleTopFloorTopLine = 17.0 / 20.0;//顶部楼层成环，立管突出比例
        double _scaleRefugeFireH = 1.0 / 5.0;//避难层，消火栓突出楼层比例 400/2000
        double _scaleVerticalFireH = 3.0 / 10.0;//普通层，消火栓突出楼层线比例 600/2000
        double _scaleRoofTopLine = 3.0 / 10.0;//顶部屋面成环，立管突出屋面比例
        double _scaleRoofValaeTopLine = 3.0 / 20.0;
        double _scaleTopFloorInnerTopLine = 7.0 / 10.0;//顶部楼层成环，内部立管突出比例
        double _scaleRefugeFloorTopLine = 3.0 / 5.0;//避难层，顶部线突出楼层比例 1200/2000
        double _scaleRefugeFloorTopInnerLine = 9.0 / 20.0; //普通层，内部立管线突出楼层线比例900/2000
        double _scaleVerticalFloorTopLine = 4.0 / 5.0;//普通层，顶部线突出楼层比例
        double _scaleVerticalFloorInnerTopLine = 13.0 / 20.0; //普通层，内部立管线突出楼层线比例1300/2000
        double _scaleRefugeFloorBottomLine = 1.0 / 8.0;//避难层，底部线下沉比例
        protected double _butterflyValveWidth = 240;//蝶阀宽度
        protected double _raiseDistanceToStartDefault = 5000;//竖直立管距离起点距离
        protected string _areaAttr = "";
        protected bool _haveHandPumpConnection = false;
        public FireHydrantBase(FloorGroupData floorGroup, List<FloorDataModel> floorDatas, FireControlSystemDiagramViewModel vm)
        {
            _floorGroup = floorGroup;
            _floorDatas = new List<FloorDataModel>();
            if (null != floorDatas && floorDatas.Count > 0)
            {
                floorDatas.ForEach(c =>
                {
                    if (c != null)
                        _floorDatas.Add(c);
                });
            }
            _floorDatas = _floorDatas.OrderBy(c => c.floorNum).ToList();
            _minFloor = _floorDatas.Min(c => c.floorNum);
            _maxFloor = _floorDatas.Max(c => c.floorNum);
            _createDBTexts = new List<CreateDBTextElement>();
            _createBasicElements = new List<CreateBasicElement>();
            _createBlocks = new List<CreateBlockInfo>();
            _xAxis = Vector3d.XAxis;
            _yAxis = Vector3d.YAxis;
            _haveHandPumpConnection = vm.HaveHandPumpConnection;
            InitData(vm);
        }
        void InitData(FireControlSystemDiagramViewModel vm)
        {
            _fireHCount = vm.CountsGeneral;
            _refugeFireHCount = vm.CountsRefuge;
            _floorSpace = vm.FaucetFloor;
            _refugeLineInts = new List<int>();
            _raisePipeCount = Math.Max(_fireHCount, _refugeFireHCount);
            var mid = (int)_fireHCount / 2;
            mid = mid >= 1 ? mid : 1;
            _raisePipeCount += _fireHCount == 1 ? 1 : 0;
            for (int i = 0; i < _refugeFireHCount - _fireHCount; i++)
                _refugeLineInts.Add(mid + i);
            _topRingInRoof = vm.IsRoofRing;
            _haveTestFireHydrant = vm.HaveTestFireHydrant;
            _areaAttr = vm.Serialnumber;
            _fireHydrantType = vm.ComBoxFireTypeSelectItem.Name.ToString();

            var width = GetAllWidth();
            if (_floorDatas.Any(c => c.isRefugeFloor))
            {
                _raisePipeDistanceStartPoint = width / 2 + _raiseDistanceToStartDefault;
            }
            else 
            {
                _raisePipeDistanceStartPoint = (width - (_raisePipeCount - 1) * _raisePipeSpace) / 2 + _raiseDistanceToStartDefault;
            }
        }
        protected bool _HaveMaxRing(int floor)
        {
            //避难层30层以下不添加大回路
            return _haveHandPumpConnection && floor >= 30;
        }
        protected double _GetButterflyValveDistanceToLine()
        {
            return _floorSpace * _scaleRoofValaeTopLine;
        }
        public double GetAllWidth()
        {
            double width = (_raisePipeCount - 1) * _raisePipeSpace;
            //根据环路计算相应的宽度
            int group = 0;
            foreach (var keyValue in _floorGroup.floorGroups)
            {
                int startFloor = keyValue.Key;
                int endFloor = keyValue.Value;
                bool isFirst = group == 0;
                bool firstIsRefugeFloor = _floorDatas.Where(c => c.floorNum == startFloor).FirstOrDefault().isRefugeFloor;
                if (!isFirst)
                {
                    width += _ringPipeSpace * 2;
                    if (firstIsRefugeFloor && _HaveMaxRing(endFloor))
                        width += _ringPipeMaxSpace;
                }
                group += 1;
            }
            return width;
        }
        /// <summary>
        /// 获取消火栓底部在立管线上的点
        /// </summary>
        /// <param name="raiseOrigin"></param>
        /// <param name="topFloor"></param>
        /// <returns></returns>
        protected Point3d _GetFireHBottomPoint(Point3d raiseOrigin, int topFloor)
        {

            var topPoint = raiseOrigin + Vector3d.YAxis.MultiplyBy((topFloor - 1) * _floorSpace);
            double pointDisLine = 0;
            if (topFloor == _maxFloor)
            {
                pointDisLine = _topRingInRoof ? (_floorSpace * _scaleVerticalFireH) : (_floorSpace * _scaleRefugeFireH);
            }
            else
            {
                bool isRefugeFloor = _floorDatas.Where(c => c.floorNum == topFloor).FirstOrDefault().isRefugeFloor;
                pointDisLine = isRefugeFloor ? (_floorSpace * _scaleRefugeFireH) : (_floorSpace * _scaleVerticalFireH);
            }
            topPoint += Vector3d.YAxis.MultiplyBy(pointDisLine);
            return topPoint;
        }

        /// <summary>
        /// 获取立管的顶部点
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="topFloor"></param>
        /// <param name="raiseNum"></param>
        /// <returns></returns>
        protected Point3d _GetVerticalFloorRaiseTopPoint(Point3d origin, int topFloor, int raiseNum)
        {
            var topPoint = origin + Vector3d.XAxis.MultiplyBy(raiseNum * _raisePipeSpace);
            topPoint += Vector3d.YAxis.MultiplyBy((topFloor - 1) * _floorSpace);
            //是否是顶层
            double pointDisLine = 0;
            if (topFloor == _maxFloor)
            {
                if (_topRingInRoof)
                {
                    pointDisLine = _floorSpace + _floorSpace * _scaleRoofTopLine;
                    if (raiseNum > 0 && raiseNum < _raisePipeCount - 1)
                        pointDisLine = _floorSpace * _scaleTopFloorTopLine;
                }
                else
                {
                    pointDisLine = _floorSpace * _scaleTopFloorTopLine;
                    if (raiseNum > 0 && raiseNum < _raisePipeCount - 1)
                        pointDisLine = _floorSpace * _scaleTopFloorInnerTopLine;
                }
            }
            else
            {
                //非顶层，判断是否是避难层
                bool isRefugeFloor = _floorDatas.Where(c => c.floorNum == topFloor).FirstOrDefault().isRefugeFloor;
                if (isRefugeFloor)
                {
                    //避难层
                    pointDisLine = _floorSpace * _scaleRefugeFloorTopLine;
                    if (raiseNum > 0 && raiseNum < _raisePipeCount - 1)
                        pointDisLine = _floorSpace * _scaleRefugeFloorTopInnerLine;
                }
                else
                {
                    //非避难层
                    pointDisLine = _floorSpace * _scaleVerticalFloorTopLine;
                    if (raiseNum > 0 && raiseNum < _raisePipeCount - 1)
                        pointDisLine = _floorSpace * _scaleVerticalFloorInnerTopLine;
                }
            }
            topPoint += Vector3d.YAxis.MultiplyBy(pointDisLine);
            return topPoint;
        }
        /// <summary>
        /// 获取立管的底部点
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="bottomFloor"></param>
        /// <param name="rasieNum"></param>
        /// <returns></returns>
        protected Point3d _GetVerticalFloorRaiseBottomPoint(Point3d origin, int bottomFloor, int rasieNum)
        {
            var bottomPoint = origin + Vector3d.XAxis.MultiplyBy(rasieNum * _raisePipeSpace);
            bottomPoint += Vector3d.YAxis.MultiplyBy((bottomFloor - 1) * _floorSpace);
            double pointDisLine = 0;
            if (bottomFloor != _minFloor)
            {
                //非首层，判断是否是避难层
                bool isRefugeFloor = _floorDatas.Where(c => c.floorNum == bottomFloor).FirstOrDefault().isRefugeFloor;
                pointDisLine = isRefugeFloor ? (_floorSpace - _floorSpace * _scaleRefugeFloorBottomLine) :(_floorSpace * _scaleVerticalFireH);
            }
            bottomPoint += Vector3d.YAxis.MultiplyBy(pointDisLine);
            return bottomPoint;
        }
        /// <summary>
        /// 消火栓是否在该立管的右侧
        /// </summary>
        /// <param name="raiseNum"></param>
        /// <returns></returns>
        protected bool _FireInRaisePipeRight(int raiseNum)
        {
            if (null == _refugeLineInts || _refugeLineInts.Count < 1)
                return true;
            bool isRefuge = _refugeLineInts.Any(c => c == raiseNum);
            if (isRefuge)
                return true;
            if (raiseNum < _refugeLineInts.Min())
                return true;
            return raiseNum <= _refugeLineInts.Max();
        }
        /// <summary>
        /// 获取蝶阀的宽度
        /// </summary>
        /// <param name="scaleNum"></param>
        /// <returns></returns>
        protected double _GetButterflyValveScaleWidth(double scaleNum = 1.2)
        {
            return _butterflyValveWidth * scaleNum;
        }
        protected void _AddPipeLineToCreateElems(Point3d lineStartPoint, Point3d lineEndPoint)
        {
            _AddLineToCreateElems(lineStartPoint, lineEndPoint, true, ThWSSCommon.Layout_FireHydrantPipeLineLayerName);
        }
        protected CreateBasicElement _AddLineToCreateElems(Point3d lineStartPoint, Point3d lineEndPoint,bool addToCreate,string layerName) 
        {
            var basieElems = new CreateBasicElement(new Line(lineStartPoint, lineEndPoint), layerName);
            if (addToCreate)
                _createBasicElements.Add(basieElems);
            return basieElems;
        }
        protected DBText _AddTextToCreateElems(string text, Point3d textCreatePoint, double angle, bool addToCreate = true, string layerName = ThWSSCommon.Layout_FireHydrantTextLayerName)
        {
            var addText = FireProtectionSysCommon.GetAddDBText(text, _textHeight, textCreatePoint, layerName, ThWSSCommon.Layout_TextStyle);
            addText.Rotation = angle;
            if (addToCreate)
                _createDBTexts.Add(new CreateDBTextElement(textCreatePoint, addText, layerName, ThWSSCommon.Layout_TextStyle));
            return addText;
        }
        protected void _LineCreateElement(Point3d startPoint, Point3d endPoint, List<LineElementCreate> lineElements)
        {
            var lineDir = (endPoint - startPoint).GetNormal();
            var yAxis = Vector3d.ZAxis.CrossProduct(lineDir).GetNormal();
            var point = startPoint;
            foreach (var item in lineElements)
            {
                var blockStartPoint = point + lineDir.MultiplyBy(item.marginPrevious);
                var blockEndPoint = blockStartPoint + lineDir.MultiplyBy(item.width);
                if (item.enumElement == EnumElementType.Text)
                {
                    FireProtectionSysCommon.GetTextHeightWidth(new List<DBText> { item.createDBText.dbText }, out double textHeight, out double textWidth);
                    if (item.width < 0) 
                    {
                        blockEndPoint = blockStartPoint + lineDir.MultiplyBy(textWidth);
                    }
                    var textCreatePoint = blockStartPoint + lineDir.MultiplyBy(item.previousOffSet) - yAxis.MultiplyBy(textHeight / 2);
                    var addText = FireProtectionSysCommon.GetAddDBText(item.createDBText.dbText.TextString, item.createDBText.dbText.Height, textCreatePoint, item.createDBText.layerName, item.createDBText.textStyle);
                    addText.Rotation = lineDir.GetAngleTo(_xAxis, -Vector3d.ZAxis);
                    _createDBTexts.Add(new CreateDBTextElement(textCreatePoint, addText, item.createDBText.layerName, item.createDBText.textStyle));
                }
                else if (item.enumElement == EnumElementType.Block)
                {
                    blockEndPoint = blockStartPoint + lineDir.MultiplyBy(item.width);
                    var blockCreatePoint = item.enumPosition == EnumPosition.LeftCenter ? blockStartPoint : blockStartPoint + lineDir.MultiplyBy(item.width / 2);
                    var addBlock = new CreateBlockInfo(item.createBlock.blockName, item.createBlock.layerName, blockCreatePoint);
                    addBlock.scaleNum = item.createBlock.scaleNum;
                    addBlock.rotateAngle = lineDir.GetAngleTo(_xAxis, -Vector3d.ZAxis);
                    if (item.createBlock.dymBlockAttr.Count > 0)
                    {
                        foreach (var keyValue in item.createBlock.dymBlockAttr)
                            addBlock.dymBlockAttr.Add(keyValue.Key, keyValue.Value);
                    }
                    _createBlocks.Add(addBlock);
                }
                _AddPipeLineToCreateElems(point, blockStartPoint);
                point = blockEndPoint;
            }
            _AddPipeLineToCreateElems(point, endPoint);
        }

        protected DBText _AddTextToCreateElemsWSUP(string text, Point3d textCreatePoint, double angle, bool addToCreate)
        {
            return _AddTextToCreateElems(text, textCreatePoint, angle, addToCreate, ThWSSCommon.Layout_FireHydrantDescriptionLayerName);
        }
        protected CreateBlockInfo _AddButterflyToCreateElement(Point3d createPoint, bool addToCreate = true)
        {
            var butterflyValve = _AddBlockToCreateElement(createPoint, ThWSSCommon.Layout_ButterflyValveBlcokName, false);
            butterflyValve.scaleNum = 1.2;
            if (addToCreate)
                _createBlocks.Add(butterflyValve);
            return butterflyValve;
        }
        protected CreateBlockInfo _AddBlockToCreateElement(Point3d createPoint, string blockName, bool addToCreate = true)
        {
            var block = new CreateBlockInfo(blockName, ThWSSCommon.Layout_FireHydrantEqumLayerName, createPoint);
            if (addToCreate)
                _createBlocks.Add(block);
            return block;
        }
    }
}
