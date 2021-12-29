using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.IndoorFanLayout.Models;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class LayoutResultCheck
    {
        List<DivisionRoomArea> _layoutResult;
        double _roomNeedLoad;
        double _fanLoad;
        int _layoutFanCount;
        int _needFanCount;
        public LayoutResultCheck(List<DivisionRoomArea> layoutResult,double roomNeedLoad,double fanLoad) 
        {
            _layoutResult = new List<DivisionRoomArea>();
            foreach (var item in layoutResult) 
            {
                _layoutResult.Add(item);
            }
            _roomNeedLoad = roomNeedLoad;
            _fanLoad = fanLoad;
            _layoutFanCount = LayoutFanCount(layoutResult);
            _needFanCount = (int)Math.Ceiling(roomNeedLoad / fanLoad);
        }
        public List<string> GetDeleteFan() 
        {
            //这里只记录需要删除的Id,不进行删除操作
            var delIds = new List<string>();
            if (_layoutFanCount <= _needFanCount)
                return delIds;
            var ucsIds = _layoutResult.Select(c => c.UscGroupId).ToList().Distinct().ToList();
            var ucsRowFans = new List<LayoutRow>();
            var allLayoutFans = new List<FanLayoutArea>();
            foreach (var ucsId in ucsIds) 
            {
                var thisUcsGroup = _layoutResult.Where(c => c.UscGroupId == ucsId).ToList();
                var rowGroupIds = thisUcsGroup.Select(c => c.GroupId).ToList().Distinct().ToList();
                foreach (var rowId in rowGroupIds) 
                {
                    var thisRow = thisUcsGroup.Where(c => c.GroupId == rowId).ToList();
                    var thisRowNeedLoad = thisRow.Sum(c => c.NeedLoad);
                    var rowFan = new LayoutRow(ucsId, rowId, thisRowNeedLoad);
                    var thisRowFanCount = 0;
                    foreach (var item in thisRow)
                    {
                        var area = item.RealIntersectAreas.Sum(c => c.Area);
                        foreach (var res in item.FanLayoutAreaResult)
                        {
                            foreach (var fan in res.FanLayoutResult) 
                            {
                                var areaFan = new FanLayoutArea(item.divisionArea.Uid, area, fan);
                                allLayoutFans.Add(areaFan);
                            }
                            thisRowFanCount += res.FanLayoutResult.Count;
                        }
                    }
                    rowFan.RowLayoutFanCount = thisRowFanCount;
                    rowFan.RowLayoutLoad = thisRowFanCount * _fanLoad;
                    ucsRowFans.Add(rowFan);
                }
            }
            int needDelCount = _layoutFanCount - _needFanCount;
            allLayoutFans = allLayoutFans.OrderBy(c => c.LayoutArea).ToList();
            for (int i = 0; i < needDelCount; i++) 
            {
                delIds.Add(allLayoutFans[i].FanLayout.FanId);
            }
            return delIds;
        }
        private int LayoutFanCount(List<DivisionRoomArea> layoutResult) 
        {
            int count = 0;
            foreach (var item in layoutResult) 
            {
                foreach (var res in item.FanLayoutAreaResult) 
                {
                    count += res.FanLayoutResult.Count;
                }
            }
            return count;
        }
    }

    class LayoutRow 
    {
        public string UCSGroupId { get; }
        public string RowGroupId { get; }
        public double RowNeedLoad { get; }
        public int RowLayoutFanCount { get; set; }
        public double RowLayoutLoad { get; set; }
        public LayoutRow(string ucsId,string rowId,double rowNeedLoad) 
        {
            UCSGroupId = ucsId;
            RowGroupId = rowId;
            RowNeedLoad = rowNeedLoad;
        }
    }
    class FanLayoutArea 
    {
        public string UCSGroupId { get; }
        public string RowGroupId { get; }
        public string AreaId { get; }
        public double LayoutArea { get; }
        public FanLayoutRect FanLayout { get; }
        public FanLayoutArea(string areaId,double layoutArea,FanLayoutRect layoutRect) 
        {
            this.AreaId = areaId;
            this.LayoutArea = layoutArea;
            this.FanLayout = layoutRect;
        }
    }
}
