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
using ThMEPEngineCore.CAD;
using ThMEPStructure.Common;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.Model.Printer;
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
            #region ---------- 前处理 -----------
            var geoExtents = _geos.Select(o=>o.Boundary)
                .ToCollection().ToExtents2d(); // 获取ObjIds的范围
            geoExtents = geoExtents.Enlarge(_printParameter.FloorSpacing * 0.1); // 把范围扩大指定距离
            var dwgExistedElements = GetAllObjsInRange(db, geoExtents); // 获取Dwg此范围内的所有对象
            var dwgExistedBeamMarkBlks = GetBeamMarks(dwgExistedElements); // 图纸上已存在的梁标注(块)

            // 获取楼板的填充
            var elevations = _geos.GetSlabElevations();
            elevations = elevations.FilterSlabElevations(_flrHeight);
            var slabHatchConfigs = GetSlabHatchConfigs(elevations);

            // 创建楼梯板对角线            
            var stairSlabCorners = new DBObjectCollection();
            var tenThckSlabMarks = _geos.GetTenThickSlabMarks();            
            if (tenThckSlabMarks.Count>0)
            {
                var slabs = _geos.GetSlabGeos().Select(o => o.Boundary).ToCollection();
                var tenThickSlabTexts = tenThckSlabMarks.Select(o => o.Boundary).ToCollection();                
                stairSlabCorners = CreateStairSlabCorner(tenThickSlabTexts, slabs);
                _geos = _geos.Except(tenThckSlabMarks).ToList(); // 10mm厚的楼板标注不要打印
            }

            //调整梁标注的方向
            var beamMarks = _geos.GetBeamMarks(); // 梁标注            
            var beamGeos = _geos.GetBeamGeos(); // 梁线
            UpdateBeamTextRotation(beamMarks);
            // 构件梁区域模型
            var grouper = new ThBeamMarkCurveGrouper(beamGeos, beamMarks);
            var groupInfs = grouper.Group();
            var beamPolygonCenters = grouper.CreateBeamPolygons(groupInfs);
            var removedBeamMarks = ThMultipleMarkFilter.Filter(groupInfs);
            _geos = _geos.Except(removedBeamMarks).ToList();
            removedBeamMarks.Select(o => o.Boundary).ToCollection().MDispose();

            // 处理双梁
            // 双梁是要单独处理的
            var dblRowBeamMarks = FilterDoubleRowBeamMarks(_geos.GetBeamMarks());
            _geos = _geos.Except(dblRowBeamMarks.SelectMany(o => o)).ToList();
            dblRowBeamMarks.SelectMany(o => o).ForEach(o => UpdateBeamText(o));
            #endregion

            // 打印对象
            // 记录梁文字原始位置
            var beamMarkOriginTextPos = new Dictionary<DBText, Point3d>();
            // 用于把打印的文字转成块,最后把梁文字删除掉  
            var beamTextGroupObjIds = new List<ObjectIdCollection>();

            // 打印楼梯板对角线及标注
            Append(PrintStairSlabCorner(db, stairSlabCorners));

            // 打印墙、柱、楼板、梁、洞、标注
            var res = PrintGeos(db, _geos, slabHatchConfigs); //BeamLines,BeamTexts
            var beamLines = res.Item1.ToDBObjectCollection(db);
            var beamTexts = res.Item2.Keys.ToCollection().ToDBObjectCollection(db);
            var beamTextInfos = new Dictionary<DBText, Vector3d>();
            beamTexts.OfType<DBText>().ForEach(o => beamTextInfos.Add(o, res.Item2[o.ObjectId]));
            
            // 打印双梁标注
            var dblRowBeamMarkIds = PrintDoubleRowBeams(db,dblRowBeamMarks);
            dblRowBeamMarkIds.ForEach(o=> Append(o.Item1));
            dblRowBeamMarkIds.ForEach(o => beamTextGroupObjIds.Add(o.Item1));

            // 记录梁标注文字的原始位置
            _geos.GetBeamMarks()
                .Select(o => o.Boundary)
                .OfType<DBText>()
                .ForEach(o => beamMarkOriginTextPos.Add(o, o.GetCenterPointByOBB()));

            // 对双梁文字调整位置(后处理)  
            AdjustDblRowMarkPos(db, dblRowBeamMarkIds, beamLines);

            // 将带有标高的文字，换成两行(后处理)                           
            var adjustService = new ThAdjustBeamMarkService(db,beamLines, beamTextInfos);
            adjustService.Adjust();

            // 将生成的文字打印出来
            var config = ThAnnotationPrinter.GetAnnotationConfig(_printParameter.DrawingScale);
            var printer = new ThAnnotationPrinter(config);
            var removedTexts = new DBObjectCollection();
            adjustService.DoubleRowTexts.ForEach(x =>
            {
                removedTexts.Add(x.Item1);
                ObjIds.Remove(x.Item1.ObjectId);
                var dblRowTextIds = new ObjectIdCollection();
                dblRowTextIds.AddRange(printer.Print(db, x.Item2));
                dblRowTextIds.AddRange(printer.Print(db, x.Item3));
                Append(dblRowTextIds);
                beamTextGroupObjIds.Add(dblRowTextIds);
                // item1 被分为两行字 item2 and item3, item1被删除
                var item1Origin = beamMarkOriginTextPos[x.Item1];
                beamMarkOriginTextPos.Add(x.Item2, item1Origin);
                beamMarkOriginTextPos.Add(x.Item3, item1Origin);
            });

            // 把不是双行标注的文字加入到beamTextObjIds中
            beamTexts.Difference(removedTexts).OfType<DBText>()
                .ForEach(o=> beamTextGroupObjIds.Add(new ObjectIdCollection { o.ObjectId}));

            // 寻找梁区域内指定范围是否已存在标注
            var beamTextGroupObjs = beamTextGroupObjIds.Select(o => o.ToDBObjectCollection(db)).ToList();
            var existedBeamFilterRes = FilterExistedBeamMarks(beamMarkOriginTextPos, beamPolygonCenters, 
                dwgExistedBeamMarkBlks, beamTextGroupObjs);
            // 需要转换成块的文字组合
            var convertBlkGroupIds = existedBeamFilterRes.Item1
                .Select(o => o.OfType<DBObject>()
                .Select(k => k.ObjectId).ToCollection())
                .ToList();

            // 把梁文字转换成块
            var beamBlkIds = ConvertToBlock(db, convertBlkGroupIds);
            // 把转化的块和图纸上需要保留的块加入到ObjIds中
            Append(beamBlkIds);
            Append(existedBeamFilterRes.Item2.OfType<DBObject>().Select(o => o.ObjectId).ToCollection());
            dwgExistedElements = dwgExistedElements.Difference(existedBeamFilterRes.Item2);

            // 把梁文字装入到removedTexts中，梁文字最后要删除
            beamTextGroupObjIds.ForEach(o => removedTexts.AddRange(o.ToDBObjectCollection(db)));

            // 打印标题
            Append(PrintHeadText(db));

            // 打印柱表
            var elevationTblBasePt = GetElevationBasePt(db);
            var elevationInfos = GetElevationInfos();
            elevationInfos = elevationInfos.OrderBy(o => int.Parse(o.FloorNo)).ToList(); // 按自然层编号排序
            Append(PrintElevationTable(db, elevationTblBasePt, elevationInfos));

            // 打印楼板填充
            // 表右上基点
            var slabPatternTblRightUpBasePt = new Point3d(elevationTblBasePt.X,elevationTblBasePt.Y-1000.0,0);
            Append(PrintSlabPatternTable(db, slabPatternTblRightUpBasePt, slabHatchConfigs));

            // 删除不要的文字
            Erase(db, removedTexts);
            Erase(db, dwgExistedElements);

            // 过滤无效Id
            ObjIds = ObjIds.OfType<ObjectId>().Where(o => o.IsValid && !o.IsErased).ToCollection();
            ObjIds = Difference(ObjIds);

            // 释放 beamPolygonCenters
            var beamPolygonCentersKeys = beamPolygonCenters.Keys.ToCollection();
            var beamPolygonCentersValues = beamPolygonCenters.Values.ToCollection();
            beamPolygonCentersKeys.MDispose();
            beamPolygonCentersValues.MDispose();
        }

        private Point3d GetElevationBasePt(Database db)
        {
            var extents = GetPrintObjsExtents(db);
            var maxX = extents.MaxPoint.X;
            var minY = extents.MinPoint.Y;
            return  new Point3d(maxX + 1000.0, minY, 0);
        }

        private DBObjectCollection GetBeamMarks(DBObjectCollection objs)
        {
            return objs.OfType<BlockReference>()
                .Where(o => o.Layer == ThPrintLayerManager.BeamTextLayerName)
                .ToCollection();
        }

        private DBObjectCollection GetAllObjsInRange(Database db, Extents2d extents)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                return acadDb.ModelSpace.OfType<Entity>().Where(o =>
                {
                    return o.GeometricExtents.MinPoint.IsIn(extents,false) ||
                    o.GeometricExtents.MaxPoint.IsIn(extents, false);
                }).ToCollection();
            }
        }

        private Tuple<List<DBObjectCollection>, DBObjectCollection> FilterExistedBeamMarks(
           Dictionary<DBText, Point3d> beamMarkOriginTextPos,
           Dictionary<Polyline, Curve> beamPolygonCenters,
           DBObjectCollection generatedBeamMarkBlks,
           List<DBObjectCollection> beamMarkGroups)
        {
            var filter = new ThGeneratedBeamMarkFilter(beamMarkOriginTextPos, beamPolygonCenters, generatedBeamMarkBlks);
            filter.Filter(beamMarkGroups);
            return Tuple.Create(filter.Results, filter.KeepBeamMarkBlks);
        }

        private ObjectIdCollection ConvertToBlock(Database db, List<ObjectIdCollection> beamTextObjIds)
        {
            // 需要把生成的文字转成块
            var beamTextObjs = beamTextObjIds.Select(o => o.ToDBObjectCollection(db)).ToList();
            var converter = new ThBeamTextBlkConverter();
            return converter.Convert(db, beamTextObjs);
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
        private ObjectIdCollection PrintSlabPatternTable(Database db,Point3d rightUpbasePt, 
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
                FlrBottomEle = this._flrBottomEle,
                FlrHeight = this._flrHeight,
            };
            var builder = new ThSlabPatternTableBuilder(tblParameter);
            var results = builder.Build();
            return results.OfType<Entity>().Select(o => o.ObjectId).ToCollection();
        }
        private ObjectIdCollection PrintElevationTable(Database db, Point3d basePt,List<ElevationInfo> infos)
        {
            var tblBuilder = new ThElevationTableBuilder(infos);
            var objs = tblBuilder.Build();
            var mt = Matrix3d.Displacement(basePt-Point3d.Origin);
            objs.OfType<Entity>().ForEach(e=>e.TransformBy(mt));
            return objs.Print(db);
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
                            var config = ThAnnotationPrinter.GetAnnotationConfig(_printParameter.DrawingScale);
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
                // 更新梁规格是0x0
                if(dbText.TextString.Contains("0x0"))
                {
                    var profileName = beamMark.Properties.GetProfileName();
                    if (!string.IsNullOrEmpty(profileName))
                    {
                        var beamSpec = profileName.GetReducingBeamSpec();
                        if (!string.IsNullOrEmpty(beamSpec))
                        {
                            var bgContent = dbText.TextString.GetBGContent();
                            dbText.TextString = beamSpec + bgContent;
                        }
                    }
                }
                var decription = beamMark.Properties.GetDescription();
                if (string.IsNullOrEmpty(decription))
                {
                    if (dbText.TextString.IsEqualElevation(_flrHeight))
                    {
                        dbText.TextString = dbText.TextString.GetBeamSpec();
                    }
                    else
                    {
                        // update to BG 
                        dbText.TextString = dbText.TextString.UpdateBGElevation(_flrHeight);
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
            var config = ThAnnotationPrinter.GetAnnotationConfig(_printParameter.DrawingScale);
            var printer = new ThAnnotationPrinter(config);
            return printer.Print(db, dbText);
        }

        private ObjectIdCollection PrintStairSlabCorner(Database db,DBObjectCollection corners)
        {
            var results = new ObjectIdCollection();
            if (corners.Count > 0)
            {
                var textConfig = ThStairLineMarkPrinter.GetTextConfig(_printParameter.DrawingScale);
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
        private ObjectIdCollection PrintHeadText(Database database)
        {
            // 打印自然层标识, eg 一层~五层结构平面层
            var flrRange = _floorInfos.GetFloorRange(_flrBottomEle);
            if (string.IsNullOrEmpty(flrRange))
            {
                return new ObjectIdCollection();
            }
            return PrintHeadText(database, flrRange);
        }
    }
}
