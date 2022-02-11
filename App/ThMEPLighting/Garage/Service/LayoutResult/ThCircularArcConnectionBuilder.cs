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
            var branchFilterPaths = 
                CreateSingleRowBranchCornerJumpWire(Graphs);
            // *** 过滤多余的线
            var jumpWires = FilterJumpWire();

            // 过滤默认编号上的灯线
            var linkWires = FindWires(DefaultNumbers[0]);
            linkWires = FilterLinkWire2(linkWires, GetFilterPath(branchFilterPaths, DefaultNumbers[0]));
            linkWires = FilerLinkWire1(linkWires, edges);
            FilterUnLinkWireLight(linkWires, DefaultNumbers[0]); // 过滤没有连接线的灯

            // *** 用非默认编号打断默认灯线
            linkWires = BreakWire(linkWires);

            // *** 收集创建的线
            Wires = Wires.Union(linkWires);
            Wires = Wires.Union(jumpWires);
            Wires = MergeWire(Wires);
            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 打断
        }

        private void BuildDoubleRow()
        {
            var totalEdges = GetEdges();
            // *** 创建
            // 将1、2线边上的灯线用灯块打断
            CreateDoubleRowLinkWire(totalEdges);

            // 创建直段上的跳线(类似于拱形)            
            CreateSingleRowJumpWire(Graphs);
            // 连接分支
            var branchFilterPaths = 
                CreateSingleRowBranchCornerJumpWire(Graphs);
            // 连接弯头跨区
            CreateElbowStraitLinkJumpWire(totalEdges);
            // 连接T型跨区
            CreateThreeWayStraitLinksJumpWire(totalEdges);
      
            // *** 过滤
            // 过滤跳接线
            var jumpWireRes = FilterJumpWire();

            // 过滤默认编号的连接线
            var linkWires = new DBObjectCollection();
            var firstLinkWires = FindWires(DefaultNumbers[0]);
            firstLinkWires = FilterLinkWire2(firstLinkWires, GetFilterPath(branchFilterPaths, DefaultNumbers[0]));
            firstLinkWires = FilerLinkWire1(firstLinkWires, GetEdges(totalEdges, EdgePattern.First));
            FilterUnLinkWireLight(firstLinkWires, DefaultNumbers[0]);

            var secondLinkWires = FindWires(DefaultNumbers[1]);
            secondLinkWires = FilterLinkWire2(secondLinkWires, GetFilterPath(branchFilterPaths, DefaultNumbers[1]));
            secondLinkWires = FilerLinkWire1(secondLinkWires, GetEdges(totalEdges, EdgePattern.Second));
            FilterUnLinkWireLight(secondLinkWires, DefaultNumbers[1]);
            linkWires = linkWires.Union(firstLinkWires);
            linkWires = linkWires.Union(secondLinkWires);

            // *** 把非默认灯两边打断 
            linkWires = BreakWire(linkWires);

            // *** 打断 + 合并
            Wires = Wires.Union(linkWires);
            Wires = Wires.Union(jumpWireRes);
            Wires = MergeWire(Wires);
            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 打断
            
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
