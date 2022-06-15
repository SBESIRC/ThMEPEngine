using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPStructure.Reinforcement.Model;
using ThMEPStructure.Reinforcement.Service;
using ThMEPStructure.Reinforcement.Draw;

namespace ThMEPStructure.Reinforcement.Command
{
    public class ThReinforceDrawCmd : ThMEPBaseCommand, IDisposable
    {
        #region ---------- Input -----------
        public List<EdgeComponentExtractInfo> ExtractInfos { get; set; }
        public List<List<EdgeComponentExtractInfo>> ExtractInfoGroups { get; set; }
        #endregion
        private readonly string MarkLineLayer = "Num";
        private readonly string MarkTextLayer = "Num";
        private readonly string EdgeComponentLayer = "边构";
        public ThReinforceDrawCmd()
        {
            ActionName = "生成柱表";
            CommandName = "THQZPJ";
        }

        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            {
                var basePt = GetTblBasePt();
                if (!basePt.HasValue)
                {
                    return;
                }
                Import();
                Draw(basePt.Value);
            }
        }
        private void Draw(Point3d basePt)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                // 得到的是标准的构件
                var edgeComponents = GetEdgeComponents();
                // 绘制配筋表
                Draw(edgeComponents.Select(o => o.Item2).ToList(), basePt);
                // 绘制轮廓、标注
                Draw(edgeComponents.Select(o => o.Item1).ToList());
            }
        }
        private void Draw(List<ThEdgeComponent> edgeComponents,Point3d basePt)
        {
            // 绘制柱表
            using (var acadDb = AcadDatabase.Active())
            {
                var frame = ThWallColumnReinforceConfig.Instance.Frame;
                var elevation = ThWallColumnReinforceConfig.Instance.Elevation;
                var drawScale = ThWallColumnReinforceConfig.Instance.DrawScale;
                var tblRowHeight = ThWallColumnReinforceConfig.Instance.TableRowHeight;
                var extents = CreateExtents(frame);
                if (extents.MaxPoint.X - extents.MinPoint.X < 1.0 ||
                    extents.MaxPoint.Y - extents.MinPoint.Y < 1.0)
                {
                    return; // 范围框不合法
                }
                if (tblRowHeight <= 0.0 || edgeComponents.Count == 0)
                {
                    return; // 行高不合法, 构件数量为0
                }
                var tblBuilder = new ThReinforceTableBuilder(
                    extents, elevation, drawScale, tblRowHeight);
                var results = tblBuilder.Build(edgeComponents);

                // 移动位置
                var ucs2Wcs = Active.Editor.UCS2WCS();
                var wcs2Ucs = Active.Editor.WCS2UCS();

                // 把绘制的物体从框左上方移下来
                var mt1 = Matrix3d.Displacement(new Vector3d(0, -1 * extents.MaxPoint.Y, 0));
                results.OfType<Entity>().ForEach(e => e.TransformBy(mt1));
                // 偏移
                var newBasePt = basePt.TransformBy(wcs2Ucs);
                var mt2 = Matrix3d.Displacement(newBasePt - Point3d.Origin);
                results.OfType<Entity>().ForEach(e => e.TransformBy(mt2));

                // 旋转                
                results.OfType<Entity>().ForEach(e => e.TransformBy(ucs2Wcs));
                results.OfType<Entity>().ForEach(e=>
                {
                    acadDb.ModelSpace.Add(e);
                });
            }
        }
        private void Draw(List<EdgeComponentExtractInfo> infos)
        {
            // 绘制边构轮廓+编号+标注
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.CreateAILayer(MarkLineLayer,7);
                acadDb.Database.CreateAILayer(MarkTextLayer, 7);
                acadDb.Database.CreateAILayer(EdgeComponentLayer, 25);
                var results = new DBObjectCollection();
                infos.Where(o => o.IsStandard)
                    .Where(o => !string.IsNullOrEmpty(o.Number))
                    .Where(o => o.EdgeComponent != null)
                    .ForEach(o =>
                    {
                        results = results.Union(Mark(o.Number, o.EdgeComponent));
                        var clone = o.EdgeComponent.Clone() as Polyline;
                        clone.Layer = EdgeComponentLayer;
                        clone.ColorIndex = (int)ColorIndex.BYLAYER;
                        clone.LineWeight = LineWeight.ByLayer;
                        clone.Linetype = "Bylayer";
                        results.Add(clone);
                    });
                if(ExtractInfoGroups.Count>0)
                {
                    infos.ForEach(o =>
                    {
                        var res = ExtractInfoGroups.Where(g => g.Contains(o));
                        if(res.Count()==1)
                        {
                            var group = res.First();
                            var index = group.IndexOf(o);
                            for(int i=0; i< group.Count;i++)
                            {
                                if(i == index)
                                {
                                    continue;
                                }
                                results = results.Union(Mark(group[i].Number, group[i].EdgeComponent));
                                var clone = group[i].EdgeComponent.Clone() as Polyline;
                                clone.Layer = EdgeComponentLayer;
                                clone.ColorIndex = (int)ColorIndex.BYLAYER;
                                clone.LineWeight = LineWeight.ByLayer;
                                clone.Linetype = "Bylayer";
                                results.Add(clone);
                            }
                        }
                    });
                }

                results.OfType<Entity>().ForEach(e => acadDb.ModelSpace.Add(e));
            }  
        }
        private DBObjectCollection Mark(string number,Polyline edgeComponent)
        {
            var results = new DBObjectCollection();            
            if (string.IsNullOrEmpty(number) || edgeComponent ==null)
            {
                return results;
            }
            var vertices = edgeComponent.Vertices();
            if (vertices.Count == 0)
            {
                return results;
            }
            var wcs2Ucs = Active.Editor.WCS2UCS();
            var ucs2wcs = Active.Editor.UCS2WCS();
            switch (ThEdgeComponentDrawConfig.Instance.MarkPosition)
            {
                case "右上":
                    var markPt1 = vertices
                        .OfType<Point3d>()
                        .Select(o=>o.TransformBy(wcs2Ucs))
                        .OrderByDescending(o => Math.Ceiling(o.Y))
                        .ThenByDescending(o => Math.Ceiling(o.X))
                        .First();                    
                    results = Mark(number, Point3d.Origin, new Vector3d(1, 1, 0), new Vector3d(1, 0, 0));
                    var mt1 = Matrix3d.Displacement(markPt1 - Point3d.Origin);
                    results.OfType<Entity>().ForEach(e => e.TransformBy(mt1));
                    results.OfType<Entity>().ForEach(e => e.TransformBy(ucs2wcs));
                    break;
                case "右下":
                    var markPt2 = vertices
                        .OfType<Point3d>()
                        .Select(o => o.TransformBy(wcs2Ucs))
                        .OrderBy(o=> Math.Ceiling(o.Y))
                        .ThenByDescending(o => Math.Ceiling(o.X))
                        .First();
                    results = Mark(number, Point3d.Origin, new Vector3d(1, -1, 0), new Vector3d(1, 0, 0));
                    var mt2 = Matrix3d.Displacement(markPt2 - Point3d.Origin);
                    results.OfType<Entity>().ForEach(e => e.TransformBy(mt2));
                    results.OfType<Entity>().ForEach(e => e.TransformBy(ucs2wcs));
                    break;
                case "左上":
                    var markPt3 = vertices
                        .OfType<Point3d>()
                        .Select(o => o.TransformBy(wcs2Ucs))
                        .OrderByDescending(o => Math.Ceiling(o.Y))
                        .ThenBy(o => Math.Ceiling(o.X))                        
                        .First();                    
                    results = Mark(number, Point3d.Origin, new Vector3d(-1, 1, 0), new Vector3d(-1, 0, 0));
                    var mt3 = Matrix3d.Displacement(markPt3 - Point3d.Origin);
                    results.OfType<Entity>().ForEach(e => e.TransformBy(mt3));
                    results.OfType<Entity>().ForEach(e => e.TransformBy(ucs2wcs));
                    break;
                case "左下":
                    var markPt4 = vertices
                       .OfType<Point3d>()
                       .Select(o => o.TransformBy(wcs2Ucs))
                       .OrderBy(o => Math.Ceiling(o.Y))
                       .ThenBy(o => Math.Ceiling(o.X))
                       .First();
                    results = Mark(number, Point3d.Origin, new Vector3d(-1, -1, 0), new Vector3d(-1, 0, 0));
                    var mt4 = Matrix3d.Displacement(markPt4 - Point3d.Origin);
                    results.OfType<Entity>().ForEach(e => e.TransformBy(mt4));
                    results.OfType<Entity>().ForEach(e => e.TransformBy(ucs2wcs));
                    break;
                default:
                    break;
            }
            return results;
        }
        private DBObjectCollection Mark(string number,Point3d basePt,
            Vector3d vec1,Vector3d vec2)
        {
            /*
             *                    vec2
             *                ------------
             *               /
             *              / vec1
             *             ^
             *           (basePt)
             */
            var results = new DBObjectCollection();         
            var length1 = 400.0/ Math.Sin(Math.PI / 4.0);
            var length2 = 900.0;
            var pt1 = basePt + vec1.GetNormal().MultiplyBy(length1);
            var pt2 = pt1 + vec2.GetNormal().MultiplyBy(length2);
            var pts = new Point3dCollection();
            pts.Add(basePt);
            pts.Add(pt1);
            pts.Add(pt2);
            var leaderLine =ThDrawTool.CreatePolyline(pts, false);
            leaderLine.Layer = MarkLineLayer;
            leaderLine.ColorIndex=(int)ColorIndex.BYLAYER;
            leaderLine.LineWeight = LineWeight.ByLayer;
            leaderLine.Linetype = "ByLayer";

            var dbText = new DBText();
            dbText.Position = pt1.GetMidPt(pt2);
            dbText.Rotation = 0.0;
            dbText.TextString = number;
            dbText.Height = 300;
            dbText.WidthFactor = 0.7;
            dbText.Layer = MarkTextLayer;
            dbText.HorizontalMode = TextHorizontalMode.TextCenter;
            dbText.VerticalMode = TextVerticalMode.TextBottom;
            dbText.AlignmentPoint = dbText.Position;
            dbText.ColorIndex = (int)ColorIndex.BYLAYER;
            dbText.TextStyleId = DbHelper.GetTextStyleId(
                ThImportTemplateStyleService.ThStyle3TextStyle);
         
            results.Add(leaderLine);
            results.Add(dbText);
            return results;
        }

        private Extents2d CreateExtents(string frame)
        {
            //"A0", "A1", "A2", "A3"
            // A0(1189,841) A1(841,594) A2(594,420) A3(420,297) A4(210,297)
            switch (frame)
            {
                case "A0":
                    return new Extents2d(0, 0, 118900, 84100); 
                case "A1":
                    return new Extents2d(0, 0, 84100, 59400);
                case "A2":
                    return new Extents2d(0, 0, 59400, 42000);
                case "A3":
                    return new Extents2d(0, 0, 42000, 29700);
                case "A4":
                    return new Extents2d(0, 0, 21000, 29700);
                default:
                    return new Extents2d(0,0,0,0);
            }
        }
        private List<Tuple<EdgeComponentExtractInfo,ThEdgeComponent>> GetEdgeComponents()
        {
            using (var query = new ThBuiltinWallColumnTableQueryService())
            {
                var results = new List<Tuple<EdgeComponentExtractInfo, ThEdgeComponent>>();
                ExtractInfos.Where(o => o.IsStandard).ForEach(o =>
                {
                    var bwOrHc2 = GetBwOrHc2(o);
                    var specDict = GetSpecDict(o);
                    if (bwOrHc2.HasValue && specDict.Count>0)
                    {
                        if (o.ComponentType == ComponentType.YBZ)
                        {
                            var edgeComponent = query.Query(o.ShapeCode, bwOrHc2.Value, specDict, o.StirrupRatio,
                                ThWallColumnReinforceConfig.Instance.AntiSeismicGrade,
                                ThWallColumnReinforceConfig.Instance.ConcreteStrengthGrade);
                            if(edgeComponent!=null)
                            {
                                SetValueToEdgeComponent(edgeComponent, o, o.ComponentType);
                                results.Add(Tuple.Create(o,edgeComponent));
                            }                            
                        }
                        else if (o.ComponentType == ComponentType.GBZ)
                        {
                            var edgeComponent = query.Query(o.ShapeCode, bwOrHc2.Value, specDict,
                                ThWallColumnReinforceConfig.Instance.WallLocation,
                                ThWallColumnReinforceConfig.Instance.AntiSeismicGrade);
                            if (edgeComponent != null)
                            {
                                SetValueToEdgeComponent(edgeComponent, o, o.ComponentType);
                                results.Add(Tuple.Create(o, edgeComponent));
                            }                            
                        }
                    }                    
                });
                return results;
            }            
        }
        private void SetValueToEdgeComponent(ThEdgeComponent edgeComponent,
            EdgeComponentExtractInfo info,ComponentType componentType)
        {
            // isFind 根据Cad计算书中的箍筋体积配箍率，在内置表中确实找到符合条件的列
            if (edgeComponent ==null || info == null)
            {
                return;
            }
            else if(edgeComponent is ThRectangleEdgeComponent rectEdgeComponent)
            {
                var bw = info.QuerySpec(ThHuaRunSecAnalysisService.BwKword);
                var hc = info.QuerySpec(ThHuaRunSecAnalysisService.HcKword);
                rectEdgeComponent.Bw = bw.HasValue ? bw.Value : 0;
                rectEdgeComponent.Hc = hc.HasValue ? hc.Value : 0;
            }
            else if(edgeComponent is ThLTypeEdgeComponent lEdgeComponent)
            {
                var bw = info.QuerySpec(ThHuaRunSecAnalysisService.BwKword);
                var bf = info.QuerySpec(ThHuaRunSecAnalysisService.BfKword);
                var hc1 = info.QuerySpec(ThHuaRunSecAnalysisService.Hc1Kword);
                var hc2 = info.QuerySpec(ThHuaRunSecAnalysisService.Hc2Kword);
                lEdgeComponent.Bw = bw.HasValue ? bw.Value : 0;
                lEdgeComponent.Bf = bf.HasValue ? bf.Value : 0;
                lEdgeComponent.Hc1 = hc1.HasValue ? hc1.Value : 0;
                lEdgeComponent.Hc2 = hc2.HasValue ? hc2.Value : 0;
            }
            else if(edgeComponent is ThTTypeEdgeComponent tEdgeComponent)
            {
                var bw = info.QuerySpec(ThHuaRunSecAnalysisService.BwKword);
                var bf = info.QuerySpec(ThHuaRunSecAnalysisService.BfKword);
                var hc1 = info.QuerySpec(ThHuaRunSecAnalysisService.Hc1Kword);
                var hc2s = info.QuerySpec(ThHuaRunSecAnalysisService.Hc2sKword);
                var hc2l = info.QuerySpec(ThHuaRunSecAnalysisService.Hc2lKword);
                tEdgeComponent.Bw = bw.HasValue ? bw.Value : 0;
                tEdgeComponent.Bf = bf.HasValue ? bf.Value : 0;
                tEdgeComponent.Hc1 = hc1.HasValue ? hc1.Value : 0;
                tEdgeComponent.Hc2s = hc2s.HasValue ? hc2s.Value : 0;
                tEdgeComponent.Hc2l = hc2l.HasValue ? hc2l.Value : 0;
            }
            edgeComponent.Number = info.Number;
            edgeComponent.C = (float)ThWallColumnReinforceConfig.Instance.C;
            edgeComponent.AntiSeismicGrade = ThWallColumnReinforceConfig.Instance.AntiSeismicGrade;
            edgeComponent.Type = info.TypeCode;
            edgeComponent.IsCalculation = info.IsCalculation;
            edgeComponent.LinkWallPos = info.LinkWallPos;
            edgeComponent.ConcreteStrengthGrade = ThWallColumnReinforceConfig.Instance.ConcreteStrengthGrade;
            edgeComponent.PointReinforceLineWeight = ThWallColumnReinforceConfig.Instance.PointReinforceLineWeight;
            edgeComponent.StirrupLineWeight = ThWallColumnReinforceConfig.Instance.StirrupLineWeight;
            if(info.IsCalculation)
            {
                // YBZ和GBZ均放大纵筋
                edgeComponent.EnhancedReinforce = GetEnhancedReinforce(
                    edgeComponent.Reinforce, info.AllReinforceArea);

                if (componentType == ComponentType.YBZ)
                {
                    // For YBZ, 放大箍筋和拉筋
                    GetEnhancedStirrupAndLinks(edgeComponent, info.StirrupRatio);
                }
            }            
        }
        
        private void GetEnhancedStirrupAndLinks(ThEdgeComponent edgeComponent,double stirrupRatio)
        {
            ThEdgeComponentStirrupEnhanceService enhanceService = null;
            if(edgeComponent is ThRectangleEdgeComponent rectEdgeComponent)
            {
                enhanceService = new ThRectEdgeComponentStirrupEnhanceService(
                    rectEdgeComponent, stirrupRatio);
            }
            else if(edgeComponent is ThLTypeEdgeComponent lTypeEdgeComponent)
            {
                enhanceService = new ThLTypeEdgeComponentStirrupEnhanceService(
                    lTypeEdgeComponent, stirrupRatio);
            }
            else if(edgeComponent is ThTTypeEdgeComponent tTypeEdgeComponent)
            {
                enhanceService = new ThTTypeEdgeComponentStirrupEnhanceService(
                    tTypeEdgeComponent, stirrupRatio);
            }
            if(enhanceService!=null)
            {
                enhanceService.Enhance();
            }
        }

        private string GetEnhancedReinforce(string reinforce,double calculationReinforceArea)
        {
            var enhance = new ThReinforceEnhanceService(reinforce, calculationReinforceArea);
            enhance.Enhance();
            return enhance.EnhancedReinforce;
        }

        private int? GetBwOrHc2(EdgeComponentExtractInfo info)
        {
            int? bwOrHc2 = null;           
            if (info.ShapeCode == ShapeCode.Rect)
            {
                bwOrHc2 = info.QuerySpec(ThHuaRunSecAnalysisService.BwKword);
            }
            else if (info.ShapeCode == ShapeCode.L)
            {
                bwOrHc2 = info.QuerySpec(ThHuaRunSecAnalysisService.Hc2Kword);
            }
            else if (info.ShapeCode == ShapeCode.T)
            {
                var bf = info.QuerySpec(ThHuaRunSecAnalysisService.BfKword);
                var hc2s = info.QuerySpec(ThHuaRunSecAnalysisService.Hc2sKword);
                var hc2l = info.QuerySpec(ThHuaRunSecAnalysisService.Hc2lKword);
                if (bf.HasValue && hc2s.HasValue && hc2l.HasValue)
                {
                    bwOrHc2 = hc2s.Value + bf.Value + hc2l.Value;
                }
            }
            return bwOrHc2;
        }
        private Dictionary<string, int> GetSpecDict(EdgeComponentExtractInfo info)
        {
            var specDict = new Dictionary<string, int>();
            if (info.ShapeCode == ShapeCode.Rect)
            {
                var hcValue = info.QuerySpec(ThHuaRunSecAnalysisService.HcKword);
                if(hcValue.HasValue)
                {
                    specDict.Add(ThHuaRunSecAnalysisService.HcKword, hcValue.Value);
                }
                return specDict;
            }
            else if (info.ShapeCode == ShapeCode.L || info.ShapeCode == ShapeCode.T)
            {
                var bwValue = info.QuerySpec(ThHuaRunSecAnalysisService.BwKword);
                var bfValue = info.QuerySpec(ThHuaRunSecAnalysisService.BfKword);
                var hc1Value = info.QuerySpec(ThHuaRunSecAnalysisService.Hc1Kword);
                if(bwValue.HasValue && bfValue.HasValue && hc1Value.HasValue)
                {
                    specDict.Add(ThHuaRunSecAnalysisService.BwKword, bwValue.Value);
                    specDict.Add(ThHuaRunSecAnalysisService.BfKword, bfValue.Value);
                    specDict.Add(ThHuaRunSecAnalysisService.Hc1Kword, hc1Value.Value);
                }
                return specDict;
            }
            else
            {
                return specDict;
            }
        }
        private void Import()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.Import();
            }
        }
        private Point3d? GetTblBasePt()
        {
            var ppo = new PromptPointOptions("\n请选择配筋表的基点");
            ppo.AllowArbitraryInput = true;
            ppo.AllowNone = false;
            var ppr = Active.Editor.GetPoint(ppo);
            if(ppr.Status == PromptStatus.OK)
            {
                var pt = ppr.Value;
                return pt.TransformBy(Active.Editor.UCS2WCS());
            }
            else
            {
                return null;
            }
        }
    }
}
