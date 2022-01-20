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

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThCircularArcConnectionBuilder : ThLightWireBuilder, IPrinter
    {
        public ThCircularArcConnectionBuilder(List<ThLightGraphService> graphs) : base(graphs)
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
                0.0,
                ArrangeParameter.LightNumberTextGap / 2.0,
                ArrangeParameter.LightNumberTextHeight,
                ArrangeParameter.LightNumberTextWidthFactor);
        }

        private void BuildSingleRow()
        {
            // *** 创建连接线
            var edges = GetEdges();
            CreateLinkWire(DefaultNumbers[0], edges); 
            CreateSingleRowJumpWire(Graphs);
            CreateSingleRowBranchCornerJumpWire(Graphs);

            // *** 过滤多余的线
            var linkWires = FindWires(DefaultNumbers[0]);
            linkWires = FilerLinkWire(linkWires, edges, LightPositionDict);
            var jumpWires = FilterJumpWire();

            // *** 用非默认编号打断默认灯线
            linkWires = BreakWire(linkWires);

            // *** 收集创建的线
            Wires = Wires.Union(linkWires);
            Wires = Wires.Union(jumpWires);
            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 打断
            Wires = MergeWire(Wires);
        }

        private void BuildDoubleRow()
        {
            var totalEdges = GetEdges();
            // *** 创建
            // 将1、2线边上的灯线用灯块打断
            CreateDoubleRowLinkWire(totalEdges);
            // 创建直段上的跳线(类似于拱形)
            CreateDoubleRowJumpWire(totalEdges);
            // 连接弯头跨区
            CreateElbowStraitLinkJumpWire(totalEdges);
            // 连接T型跨区
            CreateThreeWayCornerStraitLinksJumpWire(totalEdges);
            // 创建十字路口的线
            CreateCrossCornerStraitLinkJumpWire(totalEdges);

            // *** 过滤
            var linkWires = new DBObjectCollection();
            var firstLinkWires = FilterDoubleRowLinkWire(GetEdges(totalEdges, EdgePattern.First), DefaultNumbers[0]);
            var secondLinkWires = FilterDoubleRowLinkWire(GetEdges(totalEdges, EdgePattern.Second), DefaultNumbers[1]);
            linkWires = linkWires.Union(firstLinkWires);
            linkWires = linkWires.Union(secondLinkWires);
            // 过滤跳接线
            var jumpWireRes = FilterJumpWire();

            // *** 把非默认灯两边打断 
            linkWires = BreakWire(linkWires);

            // *** 打断 + 合并
            Wires = Wires.Union(linkWires);
            Wires = Wires.Union(jumpWireRes);
            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 打断
            Wires = MergeWire(Wires);

            // 与灯具避梁
            //var avoidService = new ThCircularArcConflictAvoidService(
            //    ArrangeParameter.LampLength, jumpWireRes, LightPositionDict);
            //avoidService.Avoid();
            //jumpWireRes = avoidService.Results;
        }

        private void CreateSingleRowJumpWire(List<ThLightGraphService> graphs)
        {
            var results = new DBObjectCollection();
            graphs.ForEach(g =>
            {
                var sameLinks = FindLightNodeLinkOnSamePath(g.Links);
                var branchBetweenLinks = FindLightNodeLinkOnBetweenBranch(g);
                branchBetweenLinks = branchBetweenLinks.Where(o => !IsExsited(sameLinks, o)).ToList();
                BuildSameLink(sameLinks);               
                BuildSameLink(branchBetweenLinks);
                sameLinks.ForEach(l => AddToLoopWireGroup(l));
                branchBetweenLinks.ForEach(l => AddToLoopWireGroup(l));
            });
        }

        private void CreateSingleRowBranchCornerJumpWire(List<ThLightGraphService> graphs)
        {
            // 连接主分支到分支的跳线
            graphs.ForEach(g =>
            {
                var nodeLinks = FindLightNodeLinkOnMainBranch(g);
                BuildBranchCornerLink(nodeLinks);
                nodeLinks.ForEach(l => AddToLoopWireGroup(l));
            });
        }

        private void CreateDoubleRowJumpWire(List<ThLightEdge> edges)
        {
            // 绘制同一段上的具有相同编号的跳线
            var graphs = BuildGraphs(edges);
            graphs.ForEach(g =>
            {
                var lightNodeLinks = FindLightNodeLinkOnSamePath(g.Links);
                var branchBwtweenLinks = FindLightNodeLinkOnBetweenBranch(g);
                branchBwtweenLinks = branchBwtweenLinks.Where(o => !IsExsited(lightNodeLinks, o)).ToList();
                BuildSameLink(lightNodeLinks);
                BuildSameLink(branchBwtweenLinks);
                lightNodeLinks.ForEach(l => AddToLoopWireGroup(l));
                branchBwtweenLinks.ForEach(l => AddToLoopWireGroup(l));
            });
        }

        private void BuildSameLink(List<ThLightNodeLink> lightNodeLinks)
        {
            var jumpWireFactory = new ThLightCircularArcJumpWireFactory(lightNodeLinks)
            {
                DefaultNumbers = this.DefaultNumbers,
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                Gap = this.ArrangeParameter.CircularArcTopDistanceToDxLine,
            };
            jumpWireFactory.Build();
        }

        private void BuildBranchCornerLink(List<ThLightNodeLink> lightNodeLinks)
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

        //private void BuildBranchCornerLink(List<ThLightNodeLink> lightNodeLinks)
        //{
        //    var jumpWireFactory = new ThLightCircularArcJumpWireFactory(lightNodeLinks)
        //    {
        //        CenterSideDicts = this.CenterSideDicts,
        //        DirectionConfig = this.DirectionConfig,
        //        LampLength = this.ArrangeParameter.LampLength,
        //        LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
        //        Gap = this.ArrangeParameter.CircularArcTopDistanceToDxLine,
        //    };
        //    jumpWireFactory.BuildCrossLinks();
        //}

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
                Wires.OfType<Curve>().ForEach(c =>
                {
                    var objId = acadDatabase.ModelSpace.Add(c);
                    c.Layer = CableTrayParameter.JumpWireParameter.Layer;
                    c.ColorIndex = (int)ColorIndex.BYLAYER;
                    c.LineWeight = LineWeight.ByLayer;
                    c.Linetype = "ByLayer";
                    objIds.Add(objId);
                });
                return objIds;
            }
        }
    }
}
