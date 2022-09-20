using System;
using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPEngineCore.Algorithm;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThCableTrayConnectionBuilder : ThLightWireBuilder, IPrinter
    {
        #region ---------- 外部传入 ----------       
        public List<Line> FdxLines { get; set; }
        public bool YnBuildCableTray { get; set; } = true;
        #endregion

        #region ---------- 生成结果 ----------  
        public ObjectIdList ObjIds { get; }
        private List<Line> CableTraySides { get; set; }
        private List<Line> CableTrayCenters { get; set; }
        private Dictionary<Line, List<Line>> CableTrayGroups { get; set; }
        private Dictionary<Line, List<Line>> CableTrayPorts { get; set; }
        #endregion

        public ThCableTrayConnectionBuilder(List<ThLightGraphService> graphs) : base(graphs)
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
            if (YnBuildCableTray && !ArrangeParameter.IsTCHCableTray)
            {
                var cableTrayBuildEngine = BuildCableTray();
                CableTrayCenters = cableTrayBuildEngine.SplitCenters;
                CableTraySides = cableTrayBuildEngine.SplitSides;
                CableTrayGroups = cableTrayBuildEngine.CenterWithSides;
                CableTrayPorts = cableTrayBuildEngine.CenterWithPorts;
            }

            // 布灯点
            LightPositionDict = BuildLightPos();

            // 灯编号
            NumberTexts = BuildNumberText(
                ArrangeParameter.Width / 2.0,
                ArrangeParameter.LightNumberTextGap,
                ArrangeParameter.LightNumberTextHeight,
                ArrangeParameter.LightNumberTextWidthFactor);
        }

        private List<Line> BuildCrossLinks()
        {
            var results = new List<Line>();
            if (CenterSideDicts.Count == 0 || CenterGroupLines.Count == 0)
            {
                return results;
            }
            var calulator = new ThCrossLinkCalculator(CenterSideDicts, CenterGroupLines);
            var crossLinks = calulator.LinkCableTrayCross();
            crossLinks.ForEach(o => results.AddRange(o));
            return results.Where(o => o.Length > 1e-6).ToList();
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
            //var crossLines = BuildCrossLinks();
            //var tTypeLines = BuildTTypeLinks();
            // 修正lines
            var crossLinks = new List<Line>();
            var wires = CutTerminal(crossLinks);

            lines.AddRange(wires);
            lines.AddRange(crossLinks);
            var cableTrayEngine = new ThCableTrayBuilder(lines, ArrangeParameter.Width);
            cableTrayEngine.Build();
            return cableTrayEngine;
        }

        private List<Line> CutTerminal(List<Line> crossLinks)
        {
            crossLinks.AddRange(FdxLines);
            var wireDict = CreateWireDict(Graphs.SelectMany(o => o.GraphEdges).ToList());
            return CutPortUnLinkWires(wireDict, ArrangeParameter.LampLength, crossLinks).OfType<Line>().ToList();
        }

        private Dictionary<Line, Point3dCollection> CreateWireDict(List<ThLightEdge> edges)
        {
            var results = new Dictionary<Line, Point3dCollection>();
            edges.ForEach(o =>
            {
                var pts = new Point3dCollection();
                o.LightNodes.ForEach(n => pts.Add(n.Position));
                if (!results.ContainsKey(o.Edge))
                {
                    results.Add(o.Edge, pts);
                }
            });
            return results;
        }

        private DBObjectCollection CutPortUnLinkWires(
            Dictionary<Line, Point3dCollection> wireDict,
            double lampLength, List<Line> fdxLines)
        {
            var handler = new ThCutCableTrayUnlinkWireService(wireDict, lampLength, fdxLines);
            return handler.Cut();
        }

        public void Print(Database db)
        {
            SetDatabaseDefault(db);
            ObjIds.AddRange(PrintNumberTexts(db));
            if (!ArrangeParameter.IsTCHCableTray)
            {
                ObjIds.AddRange(YnBuildCableTray ? PrintCableTray(db) : new ObjectIdList());
            }
            ObjIds.AddRange(PrintLightBlocks(db));
        }

        private void PrintTCHCableTray(List<Line> lines, ThMEPOriginTransformer transformer)
        {
            var service = new ThDrawTCHCableTrayService();
            service.Width = ArrangeParameter.Width;
            service.Height = ArrangeParameter.Height;
            service.Draw(lines, transformer);
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
            // 打印天正桥架(因为天正桥架获取不到它的ObjectId)
            if (ArrangeParameter.IsTCHCableTray)
            {
                var lines = CutLightingLines(FdxLines);
                lines.AddRange(FdxLines);
                ResetObjIds(ObjIds);
                PrintTCHCableTray(lines, Transformer);
                Transformer.Reset(lines.ToCollection());
            }
            else
            {
                ResetObjIds(ObjIds);
            }
        }

        private List<Line> CutLightingLines(List<Line> nonLightinglines, double tolerance = 10.0)
        {
            var results = new List<Line>();
            var wireDict = CreateWireDict(Graphs.SelectMany(o => o.GraphEdges).ToList());
            var unLinkWires = CutPortUnLinkWires(wireDict, ArrangeParameter.LampLength, FdxLines).OfType<Line>().ToList();
            var firstLines = new List<Line>();
            var secondLines = new List<Line>();
            var grapgEdges = Graphs.SelectMany(o => o.GraphEdges);
            var firstEdges = grapgEdges.Where(o => o.EdgePattern.Equals(EdgePattern.First)).Select(o => o.Edge).ToList();
            unLinkWires.ForEach(o =>
            {
                var center = o.GetCenter();
                var direction = o.LineDirection();
                var tag = false;
                foreach (var edge in firstEdges)
                {
                    if (edge.DistanceTo(center, false) < 10.0
                        && Math.Abs(edge.LineDirection().DotProduct(direction)) > Math.Cos(1.0 / 180 * Math.PI))
                    {
                        firstLines.Add(o);
                        tag = true;
                        break;
                    }
                }
                if (!tag)
                {
                    secondLines.Add(o);
                }
            });

            var nonLightingLineIndex = new ThCADCoreNTSSpatialIndex(nonLightinglines.ToCollection());
            var firstEdgesIndex = new ThCADCoreNTSSpatialIndex(firstLines.ToCollection());
            var secondEdgesIndex = new ThCADCoreNTSSpatialIndex(secondLines.ToCollection());

            CutLightingLines(results, firstLines, nonLightingLineIndex, secondEdgesIndex, tolerance);
            CutLightingLines(results, secondLines, nonLightingLineIndex, firstEdgesIndex, tolerance);
            results.RemoveAll(o => o.Length < 10.0);
            return results;
        }

        private void CutLightingLines(List<Line> results, List<Line> edges, ThCADCoreNTSSpatialIndex nonLightingLineIndex,
            ThCADCoreNTSSpatialIndex otherEdgesIndex, double tolerance)
        {
            edges.ForEach(o =>
            {
                var direction = o.LineDirection();
                var reduceLine = new Line(o.StartPoint + tolerance * direction, o.EndPoint - tolerance * direction);
                var buffer = reduceLine.Buffer(tolerance);
                var nonLightingLineFilter = nonLightingLineIndex.SelectCrossingPolygon(buffer).OfType<Line>().ToList();
                var edgeFilter = otherEdgesIndex.SelectCrossingPolygon(buffer).OfType<Line>().ToList();

                var points = new List<Point3d>();
                GetIntersectPts(nonLightingLineFilter, o, points);
                GetIntersectPts(edgeFilter, o, points);

                points.Add(o.StartPoint);
                points.Add(o.EndPoint);
                points = points.OrderBy(pt => pt.DistanceTo(o.StartPoint)).ToList();
                for (var i = 0; i < points.Count - 1; i++)
                {
                    results.Add(new Line(points[i], points[i + 1]));
                }
            });
        }

        private void GetIntersectPts(List<Line> filter, Line line, List<Point3d> points)
        {
            filter.ForEach(l =>
            {
                var intersection = GetIntersectPts(l, line);
                if (intersection.Count == 1)
                {
                    points.Add(intersection[0]);
                }
            });
        }

        private Point3dCollection GetIntersectPts(Line first, Line second)
        {
            return first.IntersectWithEx(second, Intersect.ExtendBoth);
        }
    }
}
