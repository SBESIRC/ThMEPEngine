using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
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
            //这里数据一般是一个UCS
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
        public List<string> GetDeleteFanByMinArea() 
        {
            //这里只记录需要删除的Id,不进行删除操作
            var delIds = new List<string>();
            if (_layoutFanCount <= _needFanCount)
                return delIds;
            var allLayoutFans = new List<FanLayoutArea>();
            var ucsRowFans = GetLayoutRowInfo(out allLayoutFans);
            int needDelCount = _layoutFanCount - _needFanCount;
            allLayoutFans = allLayoutFans.OrderBy(c => c.LayoutArea).ToList();
            for (int i = 0; i < needDelCount; i++) 
            {
                delIds.Add(allLayoutFans[i].FanLayout.FanId);
            }
            return delIds;
        }

        public List<DeleteFan> GetDeleteFanByRow() 
        {
            var delFans = new List<DeleteFan>();
            if (_layoutFanCount <= _needFanCount)
                return delFans;
            var allLayoutFans = new List<FanLayoutArea>();
            var ucsRowFans = GetLayoutRowInfo(out allLayoutFans);
            int needDelCount = _layoutFanCount - _needFanCount;
            allLayoutFans = allLayoutFans.OrderBy(c => c.LayoutArea).ToList();
            for (int i = 0; i < needDelCount; i++)
            {
                ucsRowFans = ucsRowFans.OrderByDescending(c => c.RowLayoutDiffNeed).ToList();
                //该行中删除负荷超出最多的单元格中的一个
                var delOneRow = ucsRowFans.First();
                var rowCells = delOneRow.RowCells;
                rowCells = rowCells.OrderByDescending(c => c.CellLayoutDiffNeed).ToList();
                var rowFans = allLayoutFans.Where(c => c.RowGroupId == delOneRow.RowGroupId).ToList();
                var delCell = rowCells.First();
                //计算要删除的风机
                foreach (var item in rowFans) 
                {
                    if (item.AreaId != delCell.CellId)
                        continue;
                    string fanId = item.FanLayout.FanId;
                    if (delFans.Any(c => c.FanId == item.FanLayout.FanId))
                        continue;
                    delFans.Add(new DeleteFan(delOneRow.UCSGroupId,delOneRow.RowGroupId,delCell.CellId,fanId));
                    break;
                }
                //删除后修改相应的负荷数据
                delOneRow.RowLayoutLoad -= _fanLoad;
                delOneRow.RowLayoutDiffNeed -= _fanLoad;
                delCell.CellLayoutLoad -= _fanLoad;
                delCell.CellLayoutDiffNeed -= _fanLoad;
            }
            return delFans;
        }

        public List<int> RowAddFan(List<string> rowOrderIds, out List<LayoutRow> ucsRowFans) 
        {
            ucsRowFans = new List<LayoutRow>();
            //这里数据是一个UCS,
            var rowAddCounts = new List<int>();
            if (_layoutFanCount >= _needFanCount)
                return rowAddCounts;
            int needAddCount = _needFanCount - _layoutFanCount;
            var allLayoutFans = new List<FanLayoutArea>();
            ucsRowFans = GetLayoutRowInfo(out allLayoutFans);
            var rowOverLoads = new List<double>();
            var rowAddLoads = new List<double>();
            for (int i = 0; i < rowOrderIds.Count; i++)
            {
                var rowId = rowOrderIds[i];
                var rowFans = ucsRowFans.Where(c => c.RowGroupId == rowId).FirstOrDefault();
                if (rowFans == null)
                {
                    rowOverLoads.Add(0);
                    rowAddLoads.Add(0);
                    continue;
                }
                double overLoad = rowFans.RowLayoutLoad - rowFans.RowNeedLoad;
                rowOverLoads.Add(overLoad);
                rowAddLoads.Add(0);
            }
            double needAddLoad = 0.0;
            for (int i = 0; i < rowAddLoads.Count; i++) 
            {
                double overLoad = rowOverLoads[i];
                if (overLoad > 0)
                    continue;
                needAddLoad += overLoad;
                //排负荷不够，计算是否往相邻排补,如果相邻排无法补，继续往相邻排补
                int j = 1;
                while (true)
                {
                    if (i - j < 0 && i + j > rowAddLoads.Count)
                        break;
                    int upRow = i - j;
                    int downRow = i + j;
                    bool isBreak = false;
                    if (upRow > 0)
                    {
                        //有上一排,如果上一排负荷不够，不进行添加
                        var upRowOverLoad = rowOverLoads[upRow];
                        if (upRowOverLoad > 0)
                        {
                            rowOverLoads[upRow] = 0;
                            rowAddLoads[upRow] += needAddLoad;
                            needAddLoad -= overLoad;
                            isBreak = true;
                        }
                    }
                    if (isBreak)
                        break;
                    if (downRow < rowOrderIds.Count)
                    {
                        //没有上一排，有下一排
                        var downRowOverLoad = rowOverLoads[downRow];
                        if (downRowOverLoad > 0)
                        {
                            rowOverLoads[downRow] = 0;
                            rowAddLoads[downRow] += needAddLoad;
                            needAddLoad -= overLoad;
                            isBreak = true;
                        }
                    }
                    if (isBreak)
                        break;
                    j += 1;
                }
            }
            int hisAddCount = 0;
            for (int i = 0; i < rowAddLoads.Count; i++) 
            {
                if (hisAddCount > needAddCount)
                    break;
                double overLoad = rowOverLoads[i];
                if (overLoad < 0)
                {
                    rowAddCounts.Add(0);
                    continue; 
                }
                var addLoad = rowAddLoads[i];
                double add = overLoad + addLoad;
                if (add > 0)
                {
                    rowAddCounts.Add(0);
                    continue;
                }
                add = Math.Abs(add);
                int addCount = (int)Math.Ceiling(add / _fanLoad);
                hisAddCount += addCount;
                rowAddCounts.Add(addCount);
            }
            return rowAddCounts;
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

        List<LayoutRow> GetLayoutRowInfo(out List<FanLayoutArea> allLayoutFans) 
        {
            var ucsIds = _layoutResult.Select(c => c.UscGroupId).ToList().Distinct().ToList();
            var ucsRowFans = new List<LayoutRow>();
            allLayoutFans = new List<FanLayoutArea>();
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
                        //计算Cell负荷信息
                        int cellFanCount = 0;
                        var area = item.RealIntersectAreas.Sum(c => c.Area);
                        foreach (var res in item.FanLayoutAreaResult)
                        {
                            foreach (var fan in res.FanLayoutResult)
                            {
                                var areaFan = new FanLayoutArea(ucsId,rowId,item.divisionArea.Uid, area, fan);
                                allLayoutFans.Add(areaFan);
                            }
                            cellFanCount+= res.FanLayoutResult.Count;
                        }
                        var cell = new LayoutCell(item.divisionArea.Uid, item.NeedLoad);
                        cell.CellLayoutFanCount = cellFanCount;
                        cell.CellLayoutLoad = cellFanCount * _fanLoad;
                        cell.CellLayoutDiffNeed = cell.CellLayoutLoad - cell.CellNeedLoad;
                        rowFan.RowCells.Add(cell);
                        thisRowFanCount += cellFanCount;
                    }
                    rowFan.RowLayoutFanCount = thisRowFanCount;
                    rowFan.RowLayoutLoad = thisRowFanCount * _fanLoad;
                    rowFan.RowLayoutDiffNeed = rowFan.RowLayoutLoad - rowFan.RowNeedLoad; 
                    ucsRowFans.Add(rowFan);
                }
            }
            return ucsRowFans;
        }
    }

    class LayoutRow 
    {
        public string UCSGroupId { get; }
        public string RowGroupId { get; }
        public double RowNeedLoad { get; }
        public int RowLayoutFanCount { get; set; }
        public double RowLayoutLoad { get; set; }
        public double RowLayoutDiffNeed { get; set; }
        public List<LayoutCell> RowCells { get; }
        public LayoutRow(string ucsId,string rowId,double rowNeedLoad) 
        {
            UCSGroupId = ucsId;
            RowGroupId = rowId;
            RowNeedLoad = rowNeedLoad;
            RowCells = new List<LayoutCell>();
        }
    }
    class LayoutCell 
    {
        public string CellId { get; }
        public double CellNeedLoad { get; }
        public int CellLayoutFanCount { get; set; }
        public double CellLayoutLoad { get; set; }
        public double CellLayoutDiffNeed { get; set; }
        public LayoutCell(string cellId, double cellNeedLoad)
        {
            CellId = cellId;
            CellNeedLoad = cellNeedLoad;
        }
    }
    class FanLayoutArea 
    {
        public string UCSGroupId { get; }
        public string RowGroupId { get; }
        public string AreaId { get; }
        public double LayoutArea { get; }
        public FanLayoutRect FanLayout { get; }
        public FanLayoutArea(string uscId,string rowId, string areaId, double layoutArea,FanLayoutRect layoutRect) 
        {
            this.UCSGroupId = uscId;
            this.RowGroupId = rowId;
            this.AreaId = areaId;
            this.LayoutArea = layoutArea;
            this.FanLayout = layoutRect;
        }
    }

    class DeleteFan 
    {
        public string UCSId { get; }
        public string GroupRowId { get; }
        public string CellId { get; }
        public string FanId { get; }
        public DeleteFan(string ucsId,string rowId,string cellId,string fanId) 
        {
            this.UCSId = ucsId;
            this.GroupRowId = rowId;
            this.CellId = cellId;
            this.FanId = fanId;
        }
    }
}
