using System;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThChainConnectionBuilder : ThLightWireBuilder, IPrinter
    {
        #region ----------外部传入----------
        public Dictionary<string, int> DirectionConfig { get; set; }
        public Matrix3d CurrentUserCoordinateSystem { get; set; } = Matrix3d.Identity;
        #endregion
        public ObjectIdList ObjIds { get; }
        public DBObjectCollection Wires {get;private set;}
        public ThChainConnectionBuilder(List<ThLightGraphService> graphs):base(graphs)
        {
            Wires = new DBObjectCollection();
            ObjIds = new ObjectIdList(); 
            DirectionConfig = new Dictionary<string, int>();
        }
        public override void Build()
        {
            // 布灯点
            LightPositionDict = BuildLightPos();

            // 创建连接线，按照灯长度把灯所在的边打断
            var firstEdges = Graphs.SelectMany(g => g.GraphEdges).Where(o => o.EdgePattern == EdgePattern.First).ToList();
            var secondEdges = Graphs.SelectMany(g => g.GraphEdges).Where(o => o.EdgePattern == EdgePattern.Second).ToList();
            var firstLinkWireObjs = CreateLinkWire(firstEdges);
            firstLinkWireObjs = FilerLinkWire(firstLinkWireObjs);
            var secondLinkWireObjs = CreateLinkWire(secondEdges);
            secondLinkWireObjs = FilerLinkWire(secondLinkWireObjs);

            // 建议允许最大的回路编号是4
            var ductions = new List<Point3dCollection>();
            var jumpWireRes = CreateJumpWire(out ductions);

            // 从连接线减去要扣减的
            if (ductions.Count > 0)
            {
                //linkWireObjs = DetuctLinkWire(linkWireObjs, ductions);
            }

            // 创建T字型路口的线
            // var threewayJumpWireRes = CreateThreeWayJumpWire();
            var threewayJumpWireRes = new DBObjectCollection();

            // 创建十字路口的线
            // var crossJumpWireRes = CreateCrossJumpWire();
            var crossJumpWireRes = new DBObjectCollection();

            // 收集创建的线            
            Wires = Wires.Union(jumpWireRes);
            Wires = Wires.Union(crossJumpWireRes);
            Wires = Wires.Union(threewayJumpWireRes);        
            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 打断
            Wires = Wires.Union(firstLinkWireObjs); // 切记：请在BreakWire之后，添加进去
            Wires = Wires.Union(secondLinkWireObjs);// 切记：请在BreakWire之后，添加进去

            // 创建灯文字
            NumberTexts = BuildNumberText(
                ArrangeParameter.JumpWireOffsetDistance,
                ArrangeParameter.LightNumberTextGap,
                ArrangeParameter.LightNumberTextHeight,
                ArrangeParameter.LightNumberTextWidthFactor);
        }

        private DBObjectCollection CreateJumpWire(out List<Point3dCollection> ductions)
        {
            // 创建跳接线
            var results = new DBObjectCollection();
            ductions = new List<Point3dCollection>();
            var ductionCollector = new List<Point3dCollection>();
            Graphs.ForEach(g =>
            {
                var linkService = new ThLightNodeSameLinkService(g.Links);
                var lightNodeLinks = linkService.FindLightNodeLink2();
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
                ductionCollector.AddRange(jumpWireFactory.Deductions);
                lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e=> results.Add(e));
            });
            ductions = ductionCollector;
            return results;
        }

        private DBObjectCollection CreateThreeWayJumpWire()
        {
            var results = new DBObjectCollection();
            var lightNodeLinks = GetThreeWayJumpWireLinks();
            var jumpWireFactory = new ThLightLinearJumpWireFactory(lightNodeLinks)
            {
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                OffsetDis2 = this.ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0,
            };
            jumpWireFactory.BuildSideLinesSpatialIndex();
            jumpWireFactory.BuildCrossLinks();
            lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            return results;
        }

        private DBObjectCollection CreateCrossJumpWire()
        {
            var results = new DBObjectCollection();
            var lightNodeLinks = GetCrossJumpWireLinks();
            var jumpWireFactory = new ThLightLinearJumpWireFactory(lightNodeLinks)
            {
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                OffsetDis2 = this.ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0,
            };
            jumpWireFactory.BuildSideLinesSpatialIndex();
            jumpWireFactory.BuildCrossLinks();
            lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            return results;
        }

        private DBObjectCollection DetuctLinkWire(DBObjectCollection linkWires,List<Point3dCollection> deductions)
        {
            var results = new DBObjectCollection();
            var deductLines = deductions.SelectMany(d=> ToLines(d)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(deductLines);
            linkWires.Cast<Line>().ForEach(l =>
                {
                   var newLine = l.ExtendLine(-1.0);
                   var rec = newLine.Buffer(0.5);
                   var objs = spatialIndex.SelectCrossingPolygon(rec);
                    if (objs.Count > 0)
                    {
                        var lines = objs
                        .OfType<Line>()
                        .Where(o => ThGeometryTool.IsCollinearEx(l.StartPoint, l.EndPoint, o.StartPoint, o.EndPoint))
                        .ToList();
                        var newLines = l.Difference(lines);
                        newLines.ForEach(o => results.Add(o));
                    }
                    else
                    {
                        results.Add(l);
                    }
                });
            return results;
        }

        private List<Line> ToLines(Point3dCollection pts)
        {
            var lines = new List<Line>();
            for(int i =0;i< pts.Count-1;i++)
            {
                lines.Add(new Line(pts[i], pts[i + 1]));
            }
            return lines;
        }

        public override void Reset()
        {
            ResetObjIds(ObjIds);
        }  
        private DBObjectCollection BreakWire(DBObjectCollection objs,Matrix3d currentUserCoordinateSystem,double length)
        {
            var breakService = new ThBreakLineService(currentUserCoordinateSystem, length);
            return breakService.Break(objs);
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
