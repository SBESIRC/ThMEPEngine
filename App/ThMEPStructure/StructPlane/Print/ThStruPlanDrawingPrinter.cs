using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.Model.Printer;
using ThMEPStructure.Common;
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane.Print
{
    internal class ThStruPlanDrawingPrinter:ThStruDrawingPrinter
    {
        public ThStruPlanDrawingPrinter(ThSvgInput input,ThPlanePrintParameter printParameter) 
            :base(input, printParameter)
        {                     
        }
        public override void Print(Database db)
        {
            // 获取楼板的标高                
            var elevations = Geos.GetSlabElevations();
            elevations = elevations.FilterSlabElevations(FlrHeight);
            var slabHatchConfigs = GetSlabHatchConfigs(elevations);

            //调整梁标注的方向
            var stairSlabCorners = new DBObjectCollection();
            UpdateBeamTextRotation(Geos.GetBeamMarks());
            // 创建楼梯板对角线            
            var tenThckSlabMarks = Geos.GetTenThickSlabMarks();            
            if (tenThckSlabMarks.Count>0)
            {
                var slabs = Geos.GetSlabGeos().Select(o => o.Boundary).ToCollection();
                var tenThickSlabTexts = tenThckSlabMarks.Select(o => o.Boundary).ToCollection();                
                stairSlabCorners = CreateStairSlabCorner(tenThickSlabTexts, slabs);
                Geos = Geos.Except(tenThckSlabMarks).ToList(); // 10mm厚的楼板标注不要打印
            }
            
            // 处理双梁
            // 双梁是要单独处理的
            var dblRowbeamMarks = FilterDoubleRowBeamMarks(Geos.GetBeamMarks());
            Geos = Geos.Except(dblRowbeamMarks.SelectMany(o => o)).ToList();

            // 打印对象
            var res = PrintGeos(db, Geos, slabHatchConfigs); //BeamLines,BeamTexts
            var dblRowBeamMarkIds = PrintDoubleRowBeams(db,dblRowbeamMarks);
            dblRowBeamMarkIds.ForEach(o=> Append(o.Item1));
            Append(PrintStairSlabCorner(db, stairSlabCorners));

            // 过滤多余文字
            var beamLines = res.Item1.ToDBObjectCollection(db);
            var beamTexts = res.Item2.Keys.ToCollection().ToDBObjectCollection(db);
            var removedTexts = FilterBeamMarks(beamLines, beamTexts);

            // 对双梁文字调整位置
            AdjustDblRowMarkPos(db, dblRowBeamMarkIds, beamLines);

            // 将带有标高的文字，换成两行
            var beamTextInfos = new Dictionary<DBText, Vector3d>();
            beamTexts.Difference(removedTexts)
                .OfType<DBText>().ForEach(o => beamTextInfos.Add(o, res.Item2[o.ObjectId]));           
            var adjustService = new ThAdjustBeamMarkService(db,beamLines, beamTextInfos);
            adjustService.Adjust();
            adjustService.DoubleRowTexts.ForEach(x => removedTexts.Add(x.Item1));

            // 将生成的文字打印出来
            var config = ThAnnotationPrinter.GetAnnotationConfig(PrintParameter.DrawingScale);
            var printer = new ThAnnotationPrinter(config);
            adjustService.DoubleRowTexts.ForEach(x =>
            {
                ObjIds.Remove(x.Item1.ObjectId);
                var specAnnotions = printer.Print(db, x.Item2);
                var bgAnnotions = printer.Print(db, x.Item3);
                Append(specAnnotions);
                Append(bgAnnotions);
            });
            
            // 删除不要的文字
            Erase(db, removedTexts);

            // 打印标题
            PrintHeadText(db);

            // 打印柱表
            var maxX= Geos.Where(o => o.Boundary.GeometricExtents != null).Select(o => o.Boundary.GeometricExtents
                .MaxPoint.X).OrderByDescending(o => o).FirstOrDefault();
            var minY = Geos.Where(o => o.Boundary.GeometricExtents != null).Select(o => o.Boundary.GeometricExtents
                 .MinPoint.Y).OrderBy(o => o).FirstOrDefault();
            var elevationTblBasePt = new Point3d(maxX+1000.0, minY,0);
            var elevationInfos = GetElevationInfos();
            elevationInfos = elevationInfos.OrderBy(o => int.Parse(o.FloorNo)).ToList(); // 按自然层编号排序

            PrintElevationTable(db, elevationTblBasePt, elevationInfos);

            // 打印楼板填充
            // 表右上基点
            var slabPatternTblRightUpBasePt = new Point3d(elevationTblBasePt.X, 
                elevationTblBasePt.Y-1000.0,0);
            PrintSlabPatternTable(db, slabPatternTblRightUpBasePt, slabHatchConfigs);

            // 过滤无效Id
            ObjIds = ObjIds.OfType<ObjectId>().Where(o => o.IsValid && !o.IsErased).ToCollection();
        }

        private DBObjectCollection CreateStairSlabCorner(DBObjectCollection tenThckSlabTexts,
            DBObjectCollection slabs)
        {
            // 创建楼梯间楼板斜线标记
            var builder = new ThBuildStairSlabLineService();
            return builder.Build(tenThckSlabTexts, slabs);
        }

        private void AdjustDblRowMarkPos(Database db, List<Tuple<ObjectIdCollection, Vector3d>> dblRowTexts,DBObjectCollection beamLines)
        {
            // 调整双梁标注文字的位置
            using (var acadDb = AcadDatabase.Use(db))
            {
                var handler = new ThAdjustBeamMarkPosService(beamLines, 70, 50);
                dblRowTexts.ForEach(g =>
                {
                    var beamTexts = g.Item1
                    .OfType<ObjectId>()
                    .Select(o=>acadDb.Element<DBObject>(o,true))
                    .ToCollection();
                    handler.Adjust(beamTexts, g.Item2);
                });
            }
        }

        private DBObjectCollection FilterBeamMarks(
            DBObjectCollection beamLines,DBObjectCollection beamTexts)
        {
            // 处理多余的标注文字
            var markFilter = new ThMultipleMarkFilter(beamLines, beamTexts);
            markFilter.Filter();
            return markFilter.Results; // 要删除的对象
        }
        private void Erase(Database db,DBObjectCollection objs)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                objs.OfType<Entity>().ForEach(e =>
                {
                    var entity = acadDb.Element<Entity>(e.ObjectId, true);
                    entity.Erase();
                });
            }
        }
        private void PrintSlabPatternTable(Database db,Point3d rightUpbasePt, 
            Dictionary<string, HatchPrintConfig> hatchConfigs)
        {
            // 在原点创建的
            var cloneHPC = new Dictionary<string, HatchPrintConfig>();
            hatchConfigs.ForEach(h =>
            {
                var clone = h.Value.Clone() as HatchPrintConfig;
                clone.PatternScale = 0.5 * clone.PatternScale;
                clone.PatternSpace = 0.5 * clone.PatternSpace;
                cloneHPC.Add(h.Key, clone);
            });
            var tblParameter = new SlabPatternTableParameter()
            {
                Database = db,
                HatchConfigs = cloneHPC,
                RightUpbasePt = rightUpbasePt,
                FlrBottomEle = this.FlrBottomEle,
                FlrHeight = this.FlrHeight,
            };
            var builder = new ThSlabPatternTableBuilder(tblParameter);
            var results = builder.Build();
            Append(results.OfType<Entity>().Select(o => o.ObjectId).ToCollection());
        }
        private void PrintElevationTable(Database db, Point3d basePt,List<ElevationInfo> infos)
        {
            var tblBuilder = new ThElevationTableBuilder(infos);
            var objs = tblBuilder.Build();
            var mt = Matrix3d.Displacement(basePt-Point3d.Origin);
            objs.OfType<Entity>().ForEach(e=>e.TransformBy(mt));
            Append(objs.Print(db));
        }

        private Tuple<ObjectIdCollection, Dictionary<ObjectId, Vector3d>> PrintGeos(
            Database db, List<ThGeometry> geos, 
            Dictionary<string, HatchPrintConfig> slabHatchConfigs)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var beamLines = new ObjectIdCollection();
                var beamTexts = new Dictionary<ObjectId,Vector3d>();

                // 打印到图纸中
                geos.ForEach(o =>
                {
                    // Svg解析的属性信息存在于Properties中
                    string category = o.Properties.GetCategory();
                    if(o.Boundary is DBText dbText)
                    {
                        // 文字为注释
                        if (category == ThIfcCategoryManager.SlabCategory)
                        {
                            var printer = new ThSlabAnnotationPrinter();
                            Append(printer.Print(db, dbText));
                        }
                        else if (category == ThIfcCategoryManager.BeamCategory)
                        {
                            UpdateBeamText(o);
                            Vector3d textMoveDir = new Vector3d();
                            if(o.Properties.ContainsKey(ThSvgPropertyNameManager.DirPropertyName))
                            {
                                textMoveDir = o.Properties.GetDirection().ToVector();
                            }
                            if(textMoveDir.Length<=1e-6)
                            {
                                textMoveDir = Vector3d.XAxis.RotateBy(dbText.Rotation, Vector3d.ZAxis).GetPerpendicularVector().Negate();
                            }
                            var beamAnnotions = PrintBeams(db, dbText);
                            Append(beamAnnotions);
                            beamAnnotions.OfType<ObjectId>().ForEach(e => beamTexts.Add(e, textMoveDir)); // 把文字的移动方向传出去
                        }
                        else
                        {
                            var config = ThAnnotationPrinter.GetAnnotationConfig(PrintParameter.DrawingScale);
                            var printer = new ThAnnotationPrinter(config);
                            Append(printer.Print(db, dbText));
                        }
                    }
                    else
                    {
                        if (category == ThIfcCategoryManager.BeamCategory)
                        {
                            var config = GetBeamConfig(o.Properties);
                            var printer = new ThBeamPrinter(config);
                            var beamRes = printer.Print(db, o.Boundary as Curve);
                            Append(beamRes);
                            beamRes.OfType<ObjectId>().ForEach(e => beamLines.Add(e));
                        }
                        else if (category == ThIfcCategoryManager.ColumnCategory)
                        {
                            if(o.IsUpperFloorColumn())
                            {
                                Append(PrintUpperColumn(db, o));
                            }
                            else if(o.IsBelowFloorColumn())
                            {
                                Append(PrintBelowColumn(db,o));
                            }
                        }
                        else if (category == ThIfcCategoryManager.WallCategory)
                        {
                            if (o.IsUpperFloorShearWall())
                            {
                                Append(PrintUpperShearWall(db, o));
                            }
                            else if (o.IsBelowFloorShearWall())
                            {
                                Append(PrintBelowShearWall(db, o));
                            }
                        }
                        else if (category == ThIfcCategoryManager.SlabCategory)
                        {
                            var outlineConfig = ThSlabPrinter.GetSlabConfig();
                            var bg = o.Properties.GetElevation();  
                            var hatchConfig = slabHatchConfigs.ContainsKey(bg) ? slabHatchConfigs[bg] : null;
                            if(hatchConfig!=null)
                            {
                                var printer = new ThSlabPrinter(hatchConfig, outlineConfig);
                                if (o.Boundary is Polyline polyline)
                                {
                                    Append(printer.Print(db, polyline));
                                }
                                else if (o.Boundary is MPolygon mPolygon)
                                {
                                    Append(printer.Print(db, mPolygon));
                                }
                            }
                        }
                        else if (category == ThIfcCategoryManager.OpeningElementCategory)
                        {
                            var outlineConfig = ThHolePrinter.GetHoleConfig();
                            var hatchConfig = ThHolePrinter.GetHoleHatchConfig();
                            var printer = new ThHolePrinter(hatchConfig, outlineConfig);
                            Append(printer.Print(db, o.Boundary as Polyline));
                        }
                    }
                });

                return Tuple.Create(beamLines, beamTexts);
            }   
        }

        private List<Tuple<ObjectIdCollection,Vector3d>> PrintDoubleRowBeams(Database db, List<List<ThGeometry>> doubleRowBeams)
        {
            var results = new List<Tuple<ObjectIdCollection, Vector3d>>();
            // 打印到图纸中
            doubleRowBeams.ForEach(g =>
            {
                var beamIds = new ObjectIdCollection();
                Vector3d textMoveDir = new Vector3d();
                int i = 1;
                g.ForEach(o =>
                {
                    if (o.Boundary is DBText dbText)
                    {
                        if (o.Properties.ContainsKey(ThSvgPropertyNameManager.DirPropertyName) && textMoveDir.Length <= 1e-6)
                        {
                            textMoveDir = o.Properties.GetDirection().ToVector();
                        }
                        dbText.TextString = dbText.TextString + "（" + i++ + "）";                        
                        beamIds.AddRange(PrintBeams(db, dbText));                        
                    }
                });
                if(textMoveDir.Length <= 1e-6 && g.Count>0)
                {
                    var dbText = g.First().Boundary as DBText;
                    textMoveDir = Vector3d.XAxis.RotateBy(dbText.Rotation, Vector3d.ZAxis).GetPerpendicularVector().Negate();
                }
                results.Add(Tuple.Create(beamIds,textMoveDir));
            });
            return results;
        }

        private void UpdateBeamText(ThGeometry beamMark)
        {
            if(beamMark.Boundary is DBText dbText)
            {
                var decription = beamMark.Properties.GetDescription();
                if (string.IsNullOrEmpty(decription))
                {
                    if (dbText.TextString.IsEqualElevation(FlrHeight))
                    {
                        dbText.TextString = dbText.TextString.GetBeamSpec();
                    }
                    else
                    {
                        // update to BG 
                        dbText.TextString = dbText.TextString.UpdateBGElevation(FlrHeight);
                    }
                }
                else
                {
                    var spec = dbText.TextString.GetBeamSpec();
                    var elevation = decription.GetObliqueBeamBGElevation();
                    dbText.TextString = spec + elevation;
                }
            }
        }

        private ObjectIdCollection PrintBeams(Database db,DBText dbText)
        {
            var config = ThAnnotationPrinter.GetAnnotationConfig(PrintParameter.DrawingScale);
            var printer = new ThAnnotationPrinter(config);
            return printer.Print(db, dbText);
        }

        private ObjectIdCollection PrintStairSlabCorner(Database db,DBObjectCollection corners)
        {
            var results = new ObjectIdCollection();
            if (corners.Count > 0)
            {
                var textConfig = ThStairLineMarkPrinter.GetTextConfig(PrintParameter.DrawingScale);
                var lineConfig = ThStairLineMarkPrinter.GetLineConfig();
                var stairLinePrinter = new ThStairLineMarkPrinter(lineConfig, textConfig);
                corners.OfType<Line>().ForEach(l => results.AddRange(stairLinePrinter.Print(db, l)));
            }
            return results;
        }

        private PrintConfig GetBeamConfig(Dictionary<string,object> properties)
        {
            var config =ThBeamPrinter.GetBeamConfig();
            var lineType = properties.GetLineType();
            if (string.IsNullOrEmpty(lineType))
            {
                return config;
            }
            else
            {
                // 根据模板来设置
                if(lineType.ToUpper()== "CONTINUOUS")
                {
                    config.LineType = "ByBlock";
                }
                else
                {
                    config.LineType = "ByLayer";
                }
                return config;
            }
        }        
        
        private Dictionary<string,HatchPrintConfig> GetSlabHatchConfigs(List<string> elevations)
        {
            var results = new Dictionary<string,HatchPrintConfig>();
            var configs = ThSlabPrinter.GetSlabHatchConfigs(); 
            for(int i=0;i< elevations.Count;i++)
            {
                if(i< configs.Count)
                {
                    results.Add(elevations[i], configs[i]);
                }
                else
                {
                    results.Add(elevations[i], null);
                }
            }
            return results;
        }
        private List<List<ThGeometry>> FilterDoubleRowBeamMarks(List<ThGeometry> beamMarks)
        {
            var results = new List<List<ThGeometry>>(); 
            var beamTexts = beamMarks.Select(o => o.Boundary).ToCollection();
            var handler = new ThDoubleRowBeamMarkHandler();
            var groups = handler.Handle(beamTexts);
            groups.ForEach(g =>
            {
                var groupMarks = new List<ThGeometry>();
                g.OfType<DBObject>().ForEach(b =>
                {
                    var index = beamTexts.IndexOf(b);
                    groupMarks.Add(beamMarks[index]);
                });
                results.Add(groupMarks);
            });
            return results;
        }
        private void UpdateBeamTextRotation(List<ThGeometry> beamMarks)
        {
            // svg转换的文字角度是0
            beamMarks.ForEach(o =>
            {
                if(o.Boundary is DBText dbText)
                {
                    var textMoveDir = new Vector3d();
                    if (o.Properties.ContainsKey(ThSvgPropertyNameManager.DirPropertyName))
                    {
                        textMoveDir = o.Properties.GetDirection().ToVector();
                    }
                    if (textMoveDir.Length == 0.0)
                    {
                        textMoveDir = Vector3d.XAxis.RotateBy(dbText.Rotation, Vector3d.ZAxis).GetPerpendicularVector();
                    }
                    ThAdjustDbTextRotationService.Adjust(dbText, textMoveDir.GetPerpendicularVector());
                }
            });
        }
    }
}
