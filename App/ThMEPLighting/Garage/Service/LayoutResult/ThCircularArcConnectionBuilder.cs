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
            // 创建连接线，按照灯长度把灯所在的边打断
            var linkWireObjs = CreateSingleRowLinkWire();

            // 建议允许最大的回路编号是4
            var jumpWireRes = CreateSingleRowJumpWire(Graphs);

            // 收集创建的线            
            Wires = Wires.Union(jumpWireRes);
            Wires = Wires.Union(linkWireObjs); // 切记：请在BreakWire之后，添加进去
        }

        private void BuildDoubleRow()
        {
            // 连接交叉处
            var linkEdges = AddLinkCrossEdges();
            var totalEdges = new List<ThLightEdge>();
            totalEdges.AddRange(linkEdges);
            totalEdges.AddRange(GetEdges());

            // 将1、2线边上的灯线用灯块打断，并过滤末端
            var linkWireObjs = CreateDoubleRowLinkWire(totalEdges);

            // 创建直段上的跳线(类似于拱形)
            var jumpWireRes = CreateDoubleRowJumpWire(totalEdges);
            // 与灯具避梁
            var avoidService = new ThCircularArcConflictAvoidService(
                ArrangeParameter.LampLength, jumpWireRes, LightPositionDict);
            avoidService.Avoid();
            jumpWireRes = avoidService.Results;

            // 连接T型跨区
            var threewayJumpWireRes = CreateThreeWayCornerJumpWire();

            // 创建十字路口的线
            var crossJumpWireRes = CreateCrossCornerStraitLinkJumpWire();

            // 收集创建的线            
            Wires = Wires.Union(jumpWireRes);
            Wires = Wires.Union(crossJumpWireRes);
            Wires = Wires.Union(threewayJumpWireRes);
            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 打断
            Wires = Wires.Union(linkWireObjs); // 切记：请在BreakWire之后，添加进去
        }

        private DBObjectCollection CreateSingleRowJumpWire(List<ThLightGraphService> graphs)
        {
            var results = new DBObjectCollection();
            graphs.ForEach(g =>
            {
                var sameLinks = FindLightNodeLinkOnSamePath(g.Links);
                var branchCornerLinks = FindLightNodeLinkOnMainBranch(g);
                var branchBwtweenLinks = FindLightNodeLinkOnBetweenBranch(g);
                branchBwtweenLinks = branchBwtweenLinks.Where(o => !IsExsited(sameLinks, o)).ToList();
                BuildSameLink(sameLinks);
                BuildBranchCornerLink(branchCornerLinks);
                BuildSameLink(branchBwtweenLinks);
                sameLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
                branchCornerLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
                branchBwtweenLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            });
            return results;
        }

        private DBObjectCollection CreateDoubleRowJumpWire(List<ThLightEdge> edges)
        {
            // 绘制同一段上的具有相同编号的跳线
            var results = new DBObjectCollection();
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
