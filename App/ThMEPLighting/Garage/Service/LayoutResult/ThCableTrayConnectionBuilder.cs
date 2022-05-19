using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThCableTrayConnectionBuilder : ThLightWireBuilder,IPrinter
    {
        #region ---------- 外部传入 ----------       
        public List<Line> FdxLines { get; set; }
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
            var lines = new List<Line>();
            var crossLines = BuildCrossLinks();
            var tTypeLines = BuildTTypeLinks();
            // 修正lines
            var wireDict = CreateWireDict(Graphs.SelectMany(o=>o.GraphEdges).ToList());
            var wires = CutPortUnLinkWires(wireDict, ArrangeParameter.LampLength,FdxLines);

            lines.AddRange(wires.OfType<Line>());
            lines.AddRange(FdxLines);
            lines.AddRange(crossLines);
            lines.AddRange(tTypeLines);
            var cableTrayEngine = new ThCableTrayBuilder(lines, ArrangeParameter.Width);
            cableTrayEngine.Build();
            return cableTrayEngine;
        }

        private Dictionary<Line,Point3dCollection> CreateWireDict(List<ThLightEdge> edges)
        {
            var results = new Dictionary<Line, Point3dCollection>();
            edges.ForEach(o =>
            {
                var pts = new Point3dCollection();
                o.LightNodes.ForEach(n => pts.Add(n.Position));
                results.Add(o.Edge, pts);
            });
            return results;
        }

        private DBObjectCollection CutPortUnLinkWires(
            Dictionary<Line,Point3dCollection> wireDict,
            double lampLength,List<Line> fdxLines)
        {
            var handler = new ThCutCableTrayUnlinkWireService(wireDict, lampLength, fdxLines);
            return handler.Cut();
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
