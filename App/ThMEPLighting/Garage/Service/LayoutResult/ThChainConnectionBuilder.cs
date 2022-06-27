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
            #region ---------- 连线 ----------
            CreateSingleRowJumpWire(Graphs);
            var branchFilterPaths = new List<BranchLinkFilterPath>();
            CreateSingleRowBranchCornerJumpWire(Graphs, branchFilterPaths);
            #endregion

            #region ---------- 过滤 ----------
            // 创建默认编号连接线
            AddToLoopWireGroup(DefaultNumbers[0], GetEdges().Select(o => o.Edge).ToCollection());
            CutLinkWireByLights(DefaultNumbers[0]); // 是为了过滤端部未连接灯的线

            // 过滤跳接线
            var jumpWires = FilterJumpWire();

            // 过滤默认编号上的灯线
            var defaultLinkWires = FilterDefaultLinkWires(DefaultNumbers[0], branchFilterPaths);
            #endregion

            #region ---------- 后处理 ----------            
            defaultLinkWires = BreakWire(defaultLinkWires);  // 用非默认编号打断默认灯线
            Wires = Wires.Union(defaultLinkWires);           // 收集创建的线
            Wires = Wires.Union(jumpWires);                  // 收集创建的线
            Wires = MergeWire(Wires);                        // 合并线 
            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 灯线之间打断
            Wires = BreakByLights(Wires);                    // 灯线与灯之间相交打断
            #endregion
        }

        private void BuildDoubleRow()
        {
            #region ---------- 连线 ----------
            // 创建直段上的跳线(类似于拱形)            
            CreateSingleRowJumpWire(Graphs);

            // 连接分支
            var branchFilterPaths = new List<BranchLinkFilterPath>();
            CreateSingleRowBranchCornerJumpWire(Graphs, branchFilterPaths);

            // 连接跨区的灯连线
            var totalEdges = GetEdges();
            CreateStraitLinkJumpWire(totalEdges);
            #endregion

            #region ---------- 过滤 ----------
            // 将1、2线边上的灯线用灯块打断
            AddToLoopWireGroup(DefaultNumbers[0], GetEdges(totalEdges, EdgePattern.First).Select(o => o.Edge).ToCollection());
            AddToLoopWireGroup(DefaultNumbers[1], GetEdges(totalEdges, EdgePattern.Second).Select(o => o.Edge).ToCollection());
            CutLinkWireByLights(DefaultNumbers[0]); // 是为了过滤端部未连接灯的线
            CutLinkWireByLights(DefaultNumbers[1]); // 是为了过滤端部未连接灯的线

            // 过滤跳接线
            var jumpWireRes = FilterJumpWire();

            // 过滤默认编号的连接线
            var default1LinkWires = FilterDefaultLinkWires(DefaultNumbers[0], branchFilterPaths);
            var default2LinkWires = FilterDefaultLinkWires(DefaultNumbers[1], branchFilterPaths);
            #endregion

            #region --------- 后处理 ----------
            // 收集创建的线
            var defaultLinkWires = new DBObjectCollection();
            defaultLinkWires = defaultLinkWires.Union(default1LinkWires);
            defaultLinkWires = defaultLinkWires.Union(default2LinkWires);

            // 把非默认灯两边打断 
            defaultLinkWires = BreakWire(defaultLinkWires);

            // 打断 + 合并
            Wires = Wires.Union(defaultLinkWires);
            Wires = Wires.Union(jumpWireRes);
            Wires = MergeWire(Wires);
            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 打断
            Wires = BreakByLights(Wires);
            #endregion
        }

        private void CreateSingleRowJumpWire(List<ThLightGraphService> graphs)
        {
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
                Wires.OfType<Curve>().ForEach(l =>
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
