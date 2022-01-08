using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThCableTrayConnectionBuilder : ThLightWireBuilder,IPrinter
    {
        #region ---------- 外部传入 ----------       
        public List<Line> FdxLines { get; set; }
        public List<Line> SingleRowCableTrunking { get; set; }
        public bool YnBuildCableTray { get; set; } = true;
        #endregion

        #region ---------- 生成结果 ----------  
        public ObjectIdList ObjIds { get ; }
        private List<Line> CableTraySides { get; set; }
        private List<Line> CableTrayCenters { get; set; }
        private Dictionary<Line, List<Line>> CableTrayGroups { get; set; }
        private Dictionary<Line, List<Line>> CableTrayPorts { get; set; }
        #endregion

        public ThCableTrayConnectionBuilder(List<ThLightGraphService> graphs):base(graphs)
        {
            ObjIds = new ObjectIdList();
            FdxLines = new List<Line>();
            CableTraySides = new List<Line>();
            CableTrayCenters = new List<Line>();
            SingleRowCableTrunking = new List<Line>();
            CableTrayPorts = new Dictionary<Line, List<Line>>();
            CableTrayGroups = new Dictionary<Line, List<Line>>();
        }

        public override void Build()
        {

            // 灯线线槽
            if(YnBuildCableTray)
            {
                var cableTrayBuildEngine = BuildCableTray();
                CableTrayCenters = cableTrayBuildEngine.SplitCenters;
                CableTraySides = cableTrayBuildEngine.SplitSides;
                CableTrayGroups = cableTrayBuildEngine.CenterWithSides;
                CableTrayPorts = cableTrayBuildEngine.CenterWithPorts;
            }

            // 灯编号
            NumberTexts = BuildNumberText(
                ArrangeParameter.Width/2.0,
                ArrangeParameter.LightNumberTextGap,
                ArrangeParameter.LightNumberTextHeight, 
                ArrangeParameter.LightNumberTextWidthFactor);

            // 布灯点
            LightPositionDict = BuildLightPos();
        }

        private List<Line> BuildCrossLinks()
        {
            var results = new List<Line>();
            if(CenterSideDicts.Count==0 || CenterGroupLines.Count==0)
            {
                return results;
            }
            var calulator = new ThCrossLinkCalculator(CenterSideDicts, CenterGroupLines);
            var crossLinks = calulator.LinkCableTrayCross();
            crossLinks.ForEach(o => results.AddRange(o));
            return results.Where(o=>o.Length>1e-6).ToList();
        }

        private List<Line> BuildTTypeLinks()
        {
            var results = new List<Line>();
            if (CenterSideDicts.Count == 0 || CenterGroupLines.Count == 0)
            {
                return results;
            }
            var calulator = new ThCrossLinkCalculator(CenterSideDicts, CenterGroupLines);
            var crossLinks = calulator.LinkCableTrayTType();
            crossLinks.ForEach(o => results.AddRange(o));
            return results.Where(o => o.Length > 1e-6).ToList();
        }

        private ThCableTrayBuilder BuildCableTray()
        {
            var crossLines = BuildCrossLinks();
            var tTypeLines = BuildTTypeLinks();
            var lines = Graphs.SelectMany(g => g.GraphEdges).Where(o=>o.IsDX).Select(o=>o.Edge.Clone() as Line).ToList();
            lines.AddRange(FdxLines.Select(o=>o.Clone() as Line).ToList());
            lines.AddRange(crossLines);
            lines.AddRange(tTypeLines);
            lines.AddRange(SingleRowCableTrunking.Select(o => o.Clone() as Line).ToList());
            var cableTrayEngine = new ThCableTrayBuilder(lines, ArrangeParameter.Width);
            cableTrayEngine.Build();
            return cableTrayEngine;
        }

        public void Print(Database db)
        {
            SetDatabaseDefault(db);
            ObjIds.AddRange(PrintNumberTexts(db));
            ObjIds.AddRange(YnBuildCableTray?PrintCableTray(db):new ObjectIdList());
            ObjIds.AddRange(PrintLightBlocks(db));
        }

        private ObjectIdList PrintCableTray(Database db)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                var objIds = new ObjectIdList();
                CableTrayCenters.ForEach(o => o.Layer = CableTrayParameter.CenterLineParameter.Layer);
                CableTrayCenters.ForEach(o => o.ColorIndex = (int)ColorIndex.BYLAYER);
                CableTrayCenters.ForEach(o => objIds.Add(acadDatabase.ModelSpace.Add(o)));

                CableTraySides.ForEach(o => o.Layer = CableTrayParameter.SideLineParameter.Layer);
                CableTraySides.ForEach(o => o.ColorIndex = (int)ColorIndex.BYLAYER);
                CableTraySides.ForEach(o => objIds.Add(acadDatabase.ModelSpace.Add(o)));

                CableTrayGroups.ForEach(o =>
                {
                    var groupIds = new ObjectIdList();
                    o.Value.ForEach(v => groupIds.Add(v.Id));
                    groupIds.Add(o.Key.Id);
                    var ports = FindPorts(o.Key, CableTrayPorts);
                    ports.ForEach(p => groupIds.Add(p.Id));
                    var groupName = Guid.NewGuid().ToString();
                    GroupTools.CreateGroup(acadDatabase.Database, groupName, groupIds);
                });
                return objIds;
            }
        }

        private List<Line> FindPorts(Line center, Dictionary<Line, List<Line>> centerPorts)
        {
            if (centerPorts.ContainsKey(center))
            {
                return centerPorts[center];
            }
            else
            {
                foreach (var item in centerPorts)
                {
                    if (center.IsCoincide(item.Key, 1.0))
                    {
                        return item.Value;
                    }
                }
            }
            return new List<Line>();
        }

        public override void Reset()
        {
            ResetObjIds(ObjIds);
        }
    }
}
