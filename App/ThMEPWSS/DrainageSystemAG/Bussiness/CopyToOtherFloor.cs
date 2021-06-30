using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    class CopyToOtherFloor
    {
        private List<CreateBlockInfo> _thisFloorBlocks;
        private List<CreateBasicElement> _thisFloorBasicElems;
        private List<CreateDBTextElement> _thisFloorTextElems;
        private FloorFramed _baseFloor;
        private Point3d _thisFloorBasePoint;
        public CopyToOtherFloor(FloorFramed baseFloor, List<CreateBlockInfo> thisFloorBlocks, List<CreateBasicElement> thisFloorBasicElement, List<CreateDBTextElement> thisFloorTextElems) 
        {
            _thisFloorBlocks = new List<CreateBlockInfo>();
            _thisFloorBasicElems = new List<CreateBasicElement>();
            _thisFloorTextElems = new List<CreateDBTextElement>();
            _thisFloorBasePoint = baseFloor.datumPoint;
            if (null != thisFloorBlocks && thisFloorBlocks.Count > 0) 
                thisFloorBlocks.ForEach(c => { if (c != null) _thisFloorBlocks.Add(c); });
            if (null != thisFloorBasicElement && thisFloorBasicElement.Count > 0)
                thisFloorBasicElement.ForEach(c => { if (c != null) _thisFloorBasicElems.Add(c); });
            if (null != thisFloorTextElems && thisFloorTextElems.Count > 0)
                thisFloorTextElems.ForEach(c => { if (c != null) _thisFloorTextElems.Add(c); });
        }
        public List<CreateBlockInfo> CopyAllToFloor(FloorFramed floor,out List<CreateBasicElement> copyBasicElements,out List<CreateDBTextElement> copyTexts) 
        {
            var copyBlocks = CopyBlockToFloor(floor);
            copyBasicElements = CopyBasicElementToFloor(floor);
            copyTexts = CopyDBTextToFloor(floor);
            return copyBlocks;
        }
        public List<CreateBlockInfo> CopyBlockToFloor(FloorFramed floor) 
        {
            List<CreateBlockInfo> copyBlocks = new List<CreateBlockInfo>();
            if (null == _thisFloorBlocks || _thisFloorBlocks.Count < 1)
                return copyBlocks;
            foreach (var item in _thisFloorBlocks) 
                copyBlocks.Add(DrainSysAGCommon.CopyOneBlock(item, _thisFloorBasePoint, floor.datumPoint, floor.floorUid));
            return copyBlocks;
        }
        public List<CreateBasicElement> CopyBasicElementToFloor(FloorFramed floor) 
        {
            List<CreateBasicElement> copyElems = new List<CreateBasicElement>();
            if (null == _thisFloorBasicElems || _thisFloorBasicElems.Count < 1)
                return copyElems;
            foreach (var item in _thisFloorBasicElems) 
            {
                var copy = DrainSysAGCommon.CopyBaseElement(item, _thisFloorBasePoint, floor.datumPoint, floor.floorUid);
                if (null == copy)
                    continue;
                copyElems.Add(copy);
            }
            return copyElems;
        }
        public List<CreateDBTextElement> CopyDBTextToFloor(FloorFramed floor) 
        {
            List<CreateDBTextElement> copyElems = new List<CreateDBTextElement>();
            if (null == _thisFloorTextElems || _thisFloorTextElems.Count < 1)
                return copyElems;
            foreach (var item in _thisFloorTextElems)
            {
                var copy = DrainSysAGCommon.CopyTextElement(item, _thisFloorBasePoint, floor.datumPoint, floor.floorUid);
                if (null == copy)
                    continue;
                copyElems.Add(copy);
            }
            return copyElems;
        }

        public List<CreateBasicElement> CopyFloorLabelTextToMaxRoof(FloorFramed maxRoofFloor,List<CreateBlockInfo> roofPipes, out List<CreateDBTextElement> copyDbTexts) 
        {
            copyDbTexts = new List<CreateDBTextElement>();
            //大屋面上立管，本身转换、小屋面复制过来、住人顶层复制过来的
            var copyElems = new List<CreateBasicElement>();
            foreach (var roofPipe in roofPipes) 
            {
                CreateBlockInfo floorPipe = null;
                if (roofPipe.tag.ToUpper().Equals("Y1L"))
                {
                    //屋面雨水立管，可能是小屋面复制，也可能是自己转换的
                    floorPipe = _thisFloorBlocks.Where(c => c.copyId.Equals(roofPipe.uid)).FirstOrDefault();
                }
                else 
                {
                    //楼层复制过来的
                    floorPipe = _thisFloorBlocks.Where(c => c.uid.Equals(roofPipe.copyId)).FirstOrDefault();
                }
                if (floorPipe == null)
                    //没有找到楼层上的立管，不进行操作
                    continue;
                var lines = _thisFloorBasicElems.Where(c => !string.IsNullOrEmpty(c.curveTag) && c.curveTag.ToUpper().Equals("LG_BSLJX") && c.belongBlockId.Equals(floorPipe.uid)).ToList();
                if (lines.Count < 1)
                    continue;
                var text = _thisFloorTextElems.Where(c => c.belongBlockId.Equals(floorPipe.uid)).FirstOrDefault();
                //线进一步判断长度是否有多余的
                //直接进行复制操作，不需要判断线信息

                CreateBasicElement baseCopy = null;
                foreach (var line in lines)
                {
                    baseCopy = DrainSysAGCommon.CopyBaseElement(line, _thisFloorBasePoint, maxRoofFloor.datumPoint, maxRoofFloor.floorUid);
                    copyElems.Add(baseCopy);
                }
                if (lines.Count ==1)
                {
                    var lineSp = baseCopy.baseCurce.StartPoint;
                    var lineEp = baseCopy.baseCurce.EndPoint;
                    var newSp = roofPipe.createPoint;
                    var newEp = lineSp.DistanceTo(newSp) > lineEp.DistanceTo(newSp) ? lineEp : lineSp;
                    var s = new CreateBasicElement(maxRoofFloor.floorUid, new Line(newSp, newEp), baseCopy.layerName, roofPipe.uid, "LG_BSLJX");
                    copyElems.Add(s);
                }
                
                copyDbTexts.Add(DrainSysAGCommon.CopyTextElement(text, _thisFloorBasePoint, maxRoofFloor.datumPoint, maxRoofFloor.floorUid));
            }
            return copyElems;
        }
        public List<CreateBasicElement> CopyFloorLabelTextToMinRoof(FloorFramed minRoofFloor,List<CreateBlockInfo> roofPipes,
            FloorFramed maxRoofFloor,List<CreateBlockInfo> maxRoofBlocks,
            List<CreateBasicElement> maxRoofElems,List<CreateDBTextElement> maxRoofText,out List<CreateDBTextElement> copyDbTexts) 
        {
            var copyElems = new List<CreateBasicElement>();
            copyDbTexts = new List<CreateDBTextElement>();
            //小屋面的立管，可能来自自己，也有可能来自大屋面
            foreach (var roofPipe in roofPipes) 
            {
                CreateBlockInfo maxPipe = null;
                if (string.IsNullOrEmpty(roofPipe.copyId))
                {
                    //本身生成的数据
                    maxRoofBlocks.Where(c => c.copyId.Equals(roofPipe.uid));
                }
                else 
                {
                    //大屋面复制过来的立管
                    maxRoofBlocks.Where(c => c.uid.Equals(roofPipe.copyId));
                }
                if (maxPipe == null)
                    //没有找到楼层上的立管，不进行操作
                    continue;
                var lines = maxRoofElems.Where(c => !string.IsNullOrEmpty(c.curveTag) && c.curveTag.ToUpper().Equals("LG_BSLJX") && c.belongBlockId.Contains(maxPipe.uid)).ToList();
                if (lines.Count < 1)
                    continue;
                var text = maxRoofText.Where(c => c.belongBlockId.Equals(maxPipe.uid)).FirstOrDefault();
                //直接进行复制操作，不需要判断线信息
                foreach (var line in lines)
                    copyElems.Add(DrainSysAGCommon.CopyBaseElement(line, maxRoofFloor.datumPoint, minRoofFloor.datumPoint, minRoofFloor.floorUid));
                copyDbTexts.Add(DrainSysAGCommon.CopyTextElement(text, minRoofFloor.datumPoint, minRoofFloor.datumPoint, minRoofFloor.floorUid));
            }
            return copyElems;
        }
    }
}
