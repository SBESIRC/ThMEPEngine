using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

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
            var linkWireObjs = CreateLinkWire();

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
            Wires = Wires.Union(linkWireObjs); // 切记：请在BreakWire之后，添加进去

            // 创建灯文字
            NumberTexts = BuildNumberText(
                ArrangeParameter.JumpWireOffsetDistance,
                ArrangeParameter.LightNumberTextGap,
                ArrangeParameter.LightNumberTextHeight,
                ArrangeParameter.LightNumberTextWidthFactor);
        }
        #region ---------- 对灯线和灯打断 -----------
        private DBObjectCollection CreateLinkWire()
        {
            if(ArrangeParameter.IsSingleRow)
            {
                return CreateSingleRowLinkWire();
            }
            else
            {
                return CreateDoubleRowLinkWire();
            }
        }
        private DBObjectCollection CreateSingleRowLinkWire()
        {
            var edges = GetEdges();
            var linkWireObjs = CreateLinkWire(edges);
            linkWireObjs = FilerLinkWire(linkWireObjs);
            return linkWireObjs;
        }
        private DBObjectCollection CreateDoubleRowLinkWire()
        {
            var results = new DBObjectCollection();
            var firstEdges = GetEdges(EdgePattern.First);
            var secondEdges = GetEdges(EdgePattern.Second);
            var firstLinkWireObjs = CreateLinkWire(firstEdges);
            firstLinkWireObjs = FilerLinkWire(firstLinkWireObjs);
            var secondLinkWireObjs = CreateLinkWire(secondEdges);
            secondLinkWireObjs = FilerLinkWire(secondLinkWireObjs);
            results = results.Union(firstLinkWireObjs);
            results = results.Union(secondLinkWireObjs);
            return results;
        }
        #endregion

        #region ---------- 绘制同一段上的具有相同编号的跳线 ----------
        private DBObjectCollection CreateJumpWire(out List<Point3dCollection> ductions)
        {
            ductions = new List<Point3dCollection>();
            if (ArrangeParameter.ArrangeEdition== ArrangeEdition.Second)
            {
                return CreateJumpWire1(out ductions);
            }
            else if (ArrangeParameter.ArrangeEdition == ArrangeEdition.Third)
            {
                if (ArrangeParameter.IsSingleRow)
                {
                    return CreateJumpWire1(out ductions);
                }
                else
                {
                    return CreateJumpWire2(out ductions);
                } 
            }
            else
            {
                return new DBObjectCollection();
            }
        }
        private DBObjectCollection CreateJumpWire1(out List<Point3dCollection> ductions)
        {
            // 创建跳接线
            return CreateJumpWire(Graphs, out ductions);
        }
        private DBObjectCollection CreateJumpWire2(out List<Point3dCollection> ductions)
        {
            // 创建跳接线
            var results = new DBObjectCollection();
            var ductionCollector = new List<Point3dCollection>();
            var firstEdges = GetEdges(EdgePattern.First);
            var secondEdges = GetEdges(EdgePattern.Second);
            firstEdges.ForEach(o => o.IsTraversed = false);
            secondEdges.ForEach(o => o.IsTraversed = false);

            var graphs = new List<ThLightGraphService>();
            graphs.AddRange(firstEdges.CreateGraphs());
            graphs.AddRange(secondEdges.CreateGraphs());

            return CreateJumpWire(graphs, out ductions);
        }
        private DBObjectCollection CreateJumpWire(List<ThLightGraphService> graphs,out List<Point3dCollection> ductions)
        {
            // 创建跳接线
            var results = new DBObjectCollection();
            ductions = new List<Point3dCollection>();
            var ductionCollector = new List<Point3dCollection>();
            graphs.ForEach(g =>
            {
                var linkService = new ThLightNodeSameLinkService(g.Links);
                var lightNodeLinks = linkService.FindLightNodeLink1();
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
                lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            });
            ductions = ductionCollector;
            return results;
        }
        #endregion

        #region ---------- 绘制T型路口跨区具有相同编号的的跳线 ----------
        private DBObjectCollection CreateThreeWayJumpWire()
        {
            var results = new DBObjectCollection();
            if(ArrangeParameter.IsSingleRow)
            {
                return results;
            }
            if (ArrangeParameter.ArrangeEdition == ArrangeEdition.Second)
            {
                results = CreateThreeWayOppositeJumpWire();
            }
            else if (ArrangeParameter.ArrangeEdition == ArrangeEdition.Third)
            {
                results = results.Union(CreateThreeWayOppositeJumpWire());
                results = results.Union(CreateThreeWayAdjacentJumpWire());
            }
            return results;
        }
        private DBObjectCollection CreateThreeWayOppositeJumpWire()
        {
            var results = new DBObjectCollection();
            var lightNodeLinks = GetThreeWayOppositeLinks();
            if(lightNodeLinks.Count==0)
            { 
                return results;
            }
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
        private DBObjectCollection CreateThreeWayAdjacentJumpWire()
        {
            var results = new DBObjectCollection();
            var lightNodeLinks = GetThreeWayAdjacentLinks();
            if (lightNodeLinks.Count == 0)
            {
                return results;
            }
            var jumpWireFactory = new ThLightLinearJumpWireFactory(lightNodeLinks)
            {
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                OffsetDis2 = this.ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0,
            };
            jumpWireFactory.BuildSideLinesSpatialIndex();
            jumpWireFactory.BuildCrossAdjacentLinks(); 
            lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            return results;
        }
        #endregion

        #region ---------- 绘制十字路口跨区具有相同编号的的跳线 ----------
        private DBObjectCollection CreateCrossJumpWire()
        {
            var results = new DBObjectCollection();
            if (ArrangeParameter.IsSingleRow)
            {
                return results;
            }
            if (ArrangeParameter.ArrangeEdition== ArrangeEdition.Second)
            {
                results = CreateCrossOppositeJumpWire();
            }
            else if (ArrangeParameter.ArrangeEdition == ArrangeEdition.Third)
            {
                results = results.Union(CreateCrossOppositeJumpWire());
                results = results.Union(CreateCrossAdjacentJumpWire());
            }
            return results;
        }
        private DBObjectCollection CreateCrossOppositeJumpWire()
        {
            var results = new DBObjectCollection();
            var lightNodeLinks = GetCrossOppositeLinks();
            if (lightNodeLinks.Count == 0)
            {
                return results;
            }
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
        private DBObjectCollection CreateCrossAdjacentJumpWire()
        {
            var results = new DBObjectCollection();
            var lightNodeLinks = GetCrossAdjacentLinks();
            if (lightNodeLinks.Count == 0)
            {
                return results;
            }
            var jumpWireFactory = new ThLightLinearJumpWireFactory(lightNodeLinks)
            {
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                OffsetDis2 = this.ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0,
            };
            jumpWireFactory.BuildSideLinesSpatialIndex();
            jumpWireFactory.BuildCrossAdjacentLinks();
            lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            return results;
        }
        #endregion

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
