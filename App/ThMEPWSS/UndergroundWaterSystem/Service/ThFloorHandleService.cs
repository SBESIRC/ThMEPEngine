using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.UndergroundWaterSystem.Service;

namespace ThMEPWSS.UndergroundWaterSystem.Tree
{
    public class ThFloorHandleService
    {
        /// <summary>
        /// 匹配立管和标注
        /// </summary>
        /// <param name="info"></param>
        public void MatchRiserMark(List<ThFloorModel> floorList)
        {
            //匹配立管和标注（让有标注的立管进行标注）
            foreach (var floor in floorList)
            {
                MatchRiserMark(floor.FloorInfo);
            }
        }
        public void MatchRiserMark(ThFloorInfo info)
        {
            var risers = info.RiserList;
            var markes = info.MarkList;
            foreach(var riser in risers)
            {
                foreach (var mark in markes)
                {
                    if(riser.Position.DistanceTo(mark.Poistion) < 50.0)
                    {
                        riser.MarkName = mark.MarkText;
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// 找到跨层立管,并将跨层立管合并成一个
        /// </summary>
        /// <param name="info"></param>
        public List<ThRiserInfo> MergeRiser(List<ThFloorModel> floorList)
        {
            var riserList = new List<ThRiserModel>();
            for (int i = 0; i < floorList.Count; i++)
            {
                riserList.AddRange(floorList[i].FloorInfo.RiserList);
            }

            var retList = new List<ThRiserInfo>();
            var noName = riserList.Where(o => o.MarkName == "").ToList();
            foreach(var riser in noName)
            {
                var riserInfo = new ThRiserInfo();
                riserInfo.MarkName = "";
                riserInfo.RiserPts.Add(riser.Position);
                retList.Add(riserInfo);
            }
            riserList = riserList.Except(noName).ToList();
            var groups = riserList.GroupBy(o => o.MarkName).ToList();
            foreach(var group in groups)
            {
                var riserInfo = new ThRiserInfo();
                riserInfo.MarkName = group.Key;
                var risers = group.ToList().OrderBy(o => o.FloorIndex);
                foreach(var riser in risers)
                {
                    riserInfo.RiserPts.Add(riser.Position);
                }
                retList.Add(riserInfo);
            }
            return retList;
        }
        public List<Line> GetPipeList(List<ThFloorInfo> floorInfos)
        {
            var retLine = new List<Line>();
            for (int i = 0; i < floorInfos.Count; i++)
            {
                retLine.AddRange(floorInfos[i].PipeLines);
            }
            return retLine;
        }
        public List<Line> GetRiserLine(List<ThRiserInfo> risers)
        {
            var retLine = new List<Line>();
            foreach (var riser in risers)
            {
                retLine.AddRange(GetRiserLine(riser));
            }
            return retLine;
        }
        public int ContainRiser(List<ThRiserModel> risers,ThRiserModel target)
        {
            int index = -1;
            if(target.MarkName.IsNull())
            {
                return index;
            }
            foreach(var r in risers)
            {
                index++;
                if (r.MarkName == target.MarkName)
                {
                    break;
                }
            }
            return index;
        }
        public List<Line> GetRiserLine(ThRiserInfo riser)
        {
            var retList = new List<Line>();
            for(int i = 0; i < riser.RiserPts.Count - 1;i++)
            {
                var line = new Line(riser.RiserPts[i], riser.RiserPts[i + 1]);
                retList.Add(line);
            }
            return retList;
        }
    }
}
