using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using System;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThChainConnectionBuilder : ThLightWireBuilder, IPrinter
    {
        public ThChainConnectionBuilder(List<ThLightGraphService> graphs):base(graphs)
        {
        }
        public override void Build()
        {
            // 布灯点
            LightPositionDict = BuildLightPos();

            // 连线
            if (ArrangeParameter.IsSingleRow)
            {
                BuildSingleRow(); // 单排布置
            }
            else
            {
                BuildDoubleRow(); // 双排布置
            }

            // 创建灯文字
            NumberTexts = BuildNumberText(
                ArrangeParameter.JumpWireOffsetDistance,
                ArrangeParameter.LightNumberTextGap,
                ArrangeParameter.LightNumberTextHeight,
                ArrangeParameter.LightNumberTextWidthFactor);
        }

        private void BuildSingleRow()
        {
            // 创建连接线，按照灯长度把灯所在的边打断
            var edges = GetEdges();
            var linkWireObjs = CreateSingleRowLinkWire(edges);

            // 建议允许最大的回路编号是4
            var samePathJumpWireRes = CreateSingleRowJumpWire(Graphs);
            var branchCornerJumpWireRes = CreateSingleRowBranchCornerJumpWire(Graphs);

            // 过滤分支上的连线
            var totalEdges = GetEdges();
            linkWireObjs = FilterSingleRowLinkWire(linkWireObjs, totalEdges, branchCornerJumpWireRes.Item2);

            // 收集创建的线            
            Wires = Wires.Union(samePathJumpWireRes);
            Wires = Wires.Union(branchCornerJumpWireRes.Item1);

            Wires = Wires.Union(linkWireObjs);
            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 打断
            Wires = MergeWire(Wires);
        }

        private void BuildDoubleRow()
        {
            var totalEdges = GetEdges();

            // 将1、2线边上的灯线用灯块打断，并过滤末端
            var linkWireObjs = CreateDoubleRowLinkWire(totalEdges);

            // 创建直段上的跳线(类似于拱形)
            var jumpWireRes = CreateDoubleRowJumpWire(totalEdges);

            // 连接弯头跨区
            var elbowJumpWireRes = CreateElbowStraitLinkJumpWire(totalEdges);

            // 连接T型跨区
            var threewayJumpWireRes = CreateThreeWayCornerStraitLinksJumpWire(totalEdges);

            // 创建十字路口的线
            var crossJumpWireRes = CreateCrossCornerStraitLinkJumpWire(totalEdges);

            // 收集创建的线            
            Wires = Wires.Union(jumpWireRes);
            Wires = Wires.Union(elbowJumpWireRes);
            Wires = Wires.Union(crossJumpWireRes);
            Wires = Wires.Union(threewayJumpWireRes);
            Wires = Wires.Union(linkWireObjs); // 切记：请在BreakWire之后，添加进去
            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 打断
            Wires = MergeWire(Wires);
        }

        private DBObjectCollection CreateSingleRowJumpWire(List<ThLightGraphService> graphs)
        {
            var results = new DBObjectCollection();
            graphs.ForEach(g =>
            {
                var sameLinks = FindLightNodeLinkOnSamePath(g.Links);               
                var branchBwtweenLinks = FindLightNodeLinkOnBetweenBranch(g);
                branchBwtweenLinks=branchBwtweenLinks.Where(o => !IsExsited(sameLinks, o)).ToList();
                BuildSameLink(sameLinks);          
                BuildSameLink(branchBwtweenLinks);
                sameLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
                branchBwtweenLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            });
            return results;
        }

        private Tuple<DBObjectCollection, List<Tuple<Line, Point3d>>>
            CreateSingleRowBranchCornerJumpWire(List<ThLightGraphService> graphs)
        {
            // 连接主分支到分支的跳线
            var wires = new DBObjectCollection();
            var branchPtPairs = new List<Tuple<Line, Point3d>>();
            graphs.ForEach(g =>
            {
                var res = FindLightNodeLinkOnMainBranch(g);
                BuildMainBranchLink(res.Item1);
                res.Item1.SelectMany(l => l.JumpWires).ForEach(e => wires.Add(e));
                branchPtPairs.AddRange(res.Item2);
            });
            return Tuple.Create(wires, branchPtPairs);
        }

        private DBObjectCollection CreateDoubleRowJumpWire(List<ThLightEdge> edges)
        {
            var results = new DBObjectCollection();
            // 创建跳接线
            var graphs = BuildGraphs(edges);
            graphs.ForEach(g =>
            {
                var lightNodeLinks = FindLightNodeLinkOnSamePath(g.Links);
                var branchBwtweenLinks = FindLightNodeLinkOnBetweenBranch(g);
                branchBwtweenLinks = branchBwtweenLinks.Where(o => !IsExsited(lightNodeLinks, o)).ToList();
                BuildSameLink(lightNodeLinks);
                BuildSameLink(branchBwtweenLinks);
                lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
                branchBwtweenLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            });
            return results;
        }

        private void BuildSameLink(List<ThLightNodeLink> lightNodeLinks)
        {
            var jumpWireFactory = new ThLightLinearJumpWireFactory(lightNodeLinks)
            {
                DefaultNumbers = this.DefaultNumbers,
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                OffsetDis2 = this.ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0,
            };
            jumpWireFactory.Build();
        }

        private void BuildMainBranchLink(List<ThLightNodeLink> lightNodeLinks)
        {
            // 用于单排布置
            var jumpWireFactory = new ThLightLinearJumpWireFactory(lightNodeLinks)
            {
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                OffsetDis2 = this.ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0,
            };
            jumpWireFactory.BuildStraitLinks();
        }

        public override void Reset()
        {
            ResetObjIds(ObjIds);
        }  
        
        public void Print(Database db)
        {
            SetDatabaseDefault(db);
            ObjIds.AddRange(PrintNumberTexts(db));
            ObjIds.AddRange(PrintWires(db));
            ObjIds.AddRange(PrintLightBlocks(db));
        }

        private ObjectIdList PrintWires(Database db)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                var objIds = new ObjectIdList();
                Wires.OfType<Line>().ForEach(l =>
                {
                    var objId = acadDatabase.ModelSpace.Add(l);
                    l.Layer = CableTrayParameter.JumpWireParameter.Layer;
                    l.ColorIndex = (int)ColorIndex.BYLAYER;
                    l.LineWeight = LineWeight.ByLayer;
                    l.Linetype = "ByLayer";
                    objIds.Add(objId);
                });
                return objIds;
            }
        }
    }
}
