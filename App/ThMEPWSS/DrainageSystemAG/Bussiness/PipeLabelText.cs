using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Common;
using ThCADCore.NTS;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;
using ThMEPEngineCore.CAD;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    class PipeLabelText
    {
        FloorFramed _spliteFloor;
        FloorFramed _createFloor;
        double _floorSpliteY;
        double _createFloorSpliteY;
        List<Line> _floorSpliteLines;
        List<double> _floorSpliteX;
        List<RoomModel> _cretateFloorRooms;
        List<Polyline> _roomTypeSplitLines = new List<Polyline>();//楼层框定户型分隔线
        Polyline _floorFramedBound { get; set; }
        public PipeLabelText(FloorFramed spliterfloor, double spliterY, List<Polyline> roomTypeSplitLines)
        {
            _spliteFloor = spliterfloor;
            _floorSpliteY = spliterY;
            _floorSpliteLines = FramedReadUtil.FloorFrameSpliteLines(spliterfloor);
            _floorSpliteX = _floorSpliteLines.Select(c => c.StartPoint.X).ToList();
            _cretateFloorRooms = new List<RoomModel>();
            _roomTypeSplitLines = new List<Polyline>(roomTypeSplitLines);
            _floorFramedBound = spliterfloor.outPolyline;
        }
        public void InitFloorData(FloorFramed layerFloor, List<RoomModel> thisFloorRooms)
        {
            _createFloor = layerFloor;
            _cretateFloorRooms = new List<RoomModel>();
            if (null != thisFloorRooms && thisFloorRooms.Count > 0)
                _cretateFloorRooms.AddRange(thisFloorRooms);
        }
        public List<PointLabelInfo> ClockwiseLabelInfo(List<CreateBlockInfo> thisFloorPipes,string suffix="") 
        {
            var pointLabelInfos = new List<PointLabelInfo>();
            if (null == thisFloorPipes || thisFloorPipes.Count < 1)
                return pointLabelInfos;
            var areaPipes = GetPipeAreaInfo(thisFloorPipes);
            foreach (var keyValue in areaPipes) 
            {
                if (keyValue.Value == null || keyValue.Value.Count < 1)
                    continue;
                bool isClockwise = (keyValue.Key + 1) % 2 == 1;
                List<string> typeNames = keyValue.Value.Select(c => c.tag).ToList();
                typeNames = typeNames.Distinct().ToList();
                foreach (var name in typeNames)
                {
                    var typePipes = keyValue.Value.Where(c => c.tag.Equals(name)).ToList();
                    var pipeIdNums = PipeIdNumber(typePipes, isClockwise);
                    var pipeNum = string.Format("{0}{1}-", name, (keyValue.Key + 1));
                    foreach (var pipe in typePipes)
                    {
                        var num = pipeIdNums.Where(c => c.Key.Equals(pipe.uid)).FirstOrDefault().Value;
                        var realNum = string.Format("{0}{1}{2}", pipeNum, num,suffix);
                        var createPoint = new Point3d(pipe.createPoint.X, pipe.createPoint.Y, pipe.createPoint.Z);
                        var pipeLabel = new PointLabelInfo(createPoint, pipe.uid, num, realNum);
                        pointLabelInfos.Add(pipeLabel);
                    }
                }
            }
            return pointLabelInfos;
        }
        public List<PointLabelInfo> OrderLabelInfo(List<CreateBlockInfo> thisFloorPipes, string suffix = "") 
        {
            var pointLabelInfos = new List<PointLabelInfo>();
            if (null == thisFloorPipes || thisFloorPipes.Count < 1)
                return pointLabelInfos;
            var areaPipes = GetPipeAreaInfo(thisFloorPipes);
            foreach (var keyValue in areaPipes)
            {
                if (keyValue.Value == null || keyValue.Value.Count < 1)
                    continue;
                List<string> typeNames = keyValue.Value.Select(c => c.tag).ToList();
                typeNames = typeNames.Distinct().ToList();
                foreach (var name in typeNames)
                {
                    var typePipes = keyValue.Value.Where(c => c.tag.Equals(name)).ToList();
                    bool isOrder = (keyValue.Key + 1) % 2 == 1;
                    typePipes = isOrder ? typePipes.OrderBy(c => c.createPoint.X).ToList() : typePipes.OrderByDescending(c => c.createPoint.X).ToList();
                    var pipeNum = string.Format("{0}{1}-", name, (keyValue.Key + 1));
                    for (int i = 0; i < typePipes.Count; i++) 
                    {
                        var num = i+1;
                        var realNum = string.Format("{0}{1}{2}", pipeNum, num, suffix);
                        var pipe = typePipes[i];
                        var createPoint = new Point3d(pipe.createPoint.X, pipe.createPoint.Y, pipe.createPoint.Z);
                        var pipeLabel = new PointLabelInfo(createPoint, pipe.uid, num, realNum);
                        pointLabelInfos.Add(pipeLabel);
                    }
                }
            }
            return pointLabelInfos;
        }
        public List<double> GetSpliteXBySpliteFloor()
        {
            _createFloorSpliteY = GetCreateFloorSpliteY();
            List<double> createSpliteX = new List<double>();
            if (null == _floorSpliteX || _floorSpliteX.Count < 1)
                return createSpliteX;
            var oldPoistion = _spliteFloor.datumPoint.X;
            var newPoistion = _createFloor.datumPoint.X;
            foreach (var item in _floorSpliteX)
            {
                var x = newPoistion + (item - oldPoistion);
                createSpliteX.Add(x);
            }
            createSpliteX = createSpliteX.OrderBy(c => c).ToList();
            return createSpliteX;
        }
        public double GetCreateFloorSpliteY() 
        {
            var spliteY = _createFloor.datumPoint.Y + (_floorSpliteY - _spliteFloor.datumPoint.Y);
            return spliteY;
        }
        Dictionary<int, List<CreateBlockInfo>> GetPipeAreaInfo(List<CreateBlockInfo> thisFloorPipes)
        {
            var pipeArea = new Dictionary<int, List<CreateBlockInfo>>();
            //提取到属于该楼层框的户型分割线
            _roomTypeSplitLines = _roomTypeSplitLines.Where(e => _floorFramedBound.Contains(ThCADExtension.ThCurveExtension.GetMidpoint(e)) || _floorFramedBound.IntersectWithEx(e).Count > 0).ToList();
            if (_roomTypeSplitLines.Count >= 1)
            {
                var floorSpceRegions = FloorFramedSpliter.ConvertToCorrectSpliteLines(_roomTypeSplitLines, _floorFramedBound);
                for (int i = 0; i < floorSpceRegions.Count; i++)
                {
                    var spacePipes = thisFloorPipes.Where(c => floorSpceRegions[i].Contains(c.createPoint)).ToList();
                    if (spacePipes == null || spacePipes.Count < 1)
                        continue;
                    pipeArea.Add(i, spacePipes);
                }
            }
            else
            {
                //支持老版
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
                for (int i = 0; i < floorSpaceX.Count - 1; i++)
                {
                    double minX = floorSpaceX[i];
                    double maxX = floorSpaceX[i + 1];
                    //获取该区域内的立管
                    var spacePipes = thisFloorPipes.Where(c => c.createPoint.X > minX && c.createPoint.X < maxX).ToList();
                    if (spacePipes == null || spacePipes.Count < 1)
                        continue;
                    pipeArea.Add(i, spacePipes);
                }
            }
            return pipeArea;
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
                double midY = _createFloorSpliteY; 
                var upPipes = new List<CreateBlockInfo>();
                var downPipes = new List<CreateBlockInfo>();
                foreach (var pipe in orderPipes) 
                {
                    if (PipeInUp(pipe))
                        upPipes.Add(pipe);
                    else
                        downPipes.Add(pipe);
                }
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

        bool PipeInUp(CreateBlockInfo createBlock) 
        {
            if (null != _cretateFloorRooms) 
            {
                foreach (var room in _cretateFloorRooms) 
                {
                    if (room.GetRoomOBBPolyline().Contains(createBlock.createPoint))
                        return RoomInUp(room);
                }
            }
            return createBlock.createPoint.Y >= _createFloorSpliteY;

        }
        bool RoomInUp(RoomModel room) 
        {
            var roomCenter = room.GetRoomCenterPoint();
            return roomCenter.Y > _createFloorSpliteY;
        }
    }
}
