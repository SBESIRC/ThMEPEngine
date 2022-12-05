using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThPlatform3D.Common;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;
using ThPlatform3D.Model.Printer;
using ThPlatform3D.StructPlane.Service;
using ThPlatform3D.StructPlane.Model;

namespace ThPlatform3D.StructPlane.Print
{
    internal class ThStruPlanDrawingPrinter:ThStruDrawingPrinter
    {
        private AnnotationPrintConfig _beamTextConfig;
        public ThStruPlanDrawingPrinter(ThSvgParseInfo input,ThPlanePrintParameter printParameter) 
            :base(input, printParameter)
        {
            _beamTextConfig = ThBeamPrinter.GetBeamTextConfig(_printParameter.DrawingScale);
        }
        public override void Print(Database database)
        {
            #region ---------- 前处理 -----------
            // 获取楼板的填充
            var elevations = _geos.GetSlabElevations();
            elevations = elevations.FilterSlabElevations(_flrHeight);
            var slabHatchConfigs = GetSlabHatchConfigs(elevations);

            // 创建楼梯板对角线            
            var stairSlabCorners = new DBObjectCollection();
            var tenThckSlabMarks = _geos.GetTenThickSlabMarks();
            if (tenThckSlabMarks.Count > 0)
            {
                var slabs = _geos.GetSlabGeos().Select(o => o.Boundary).ToCollection();
                var tenThickSlabTexts = tenThckSlabMarks.Select(o => o.Boundary).ToCollection();
                stairSlabCorners = CreateStairSlabCorner(tenThickSlabTexts, slabs);
                _geos = _geos.Except(tenThckSlabMarks).ToList(); // 10mm厚的楼板标注不要打印
            }

            //调整梁标注的方向
            var beamGeos = _geos.GetBeamGeos(); // 梁线
            var beamMarks = _geos.GetBeamMarks(); // 梁标注           
            beamMarks.ForEach(o => UpdateBeamText(o)); // 更新文字内容
            UpdateBeamTextRotation(beamMarks); // 更新文字方向

            // 构件梁区域模型
            var grouper = new ThBeamMarkCurveGrouper(beamGeos, beamMarks);
            grouper.Group();
            var beamPolygonCenters = grouper.CreateBeamPolygons();
            _geos = _geos.Except(grouper.InvalidBeamMarks).ToList();

            var removedBeamMarks = ThMultipleMarkFilter.Filter(grouper.Groups);
            _geos = _geos.Except(removedBeamMarks).ToList();
            removedBeamMarks.Select(o => o.Boundary).ToCollection().MDispose();

            // 记录梁标注文字的原始位置
            var beamMarkOriginTextPos = new Dictionary<DBText, Point3d>();
            _geos.GetBeamMarks()
                .Select(o => o.Boundary)
                .OfType<DBText>()
                .ForEach(o => beamMarkOriginTextPos.Add(o, o.AlignmentPoint));

            // 计算梁标注原始区域
            var beamMarkAreas = ThBeamMarkOriginAreaCalculator.Calculate(_geos.GetBeamMarks(), 0.0);

            // 处理双梁
            // 双梁是要单独处理的
            var dblRowBeamMarks = FilterDoubleRowBeamMarks(_geos.GetBeamMarks());
            _geos = _geos.Except(dblRowBeamMarks.SelectMany(o => o)).ToList();
            #endregion

            // 转换成块的文字集合
            var convertBlkGroups = new List<ThBeamMarkBlkInfo>(); // 成块的梁文字，标注原始区域，文字移动方向            
            var updateGeneratedBeamBlks = new List<ThBeamMarkBlkInfo>();// 已生成的梁标注块，更新的梁文字组合，标注原始区域,文字移动方向
            var removedTexts = new DBObjectCollection(); 
            var dwgExistedElements = new DBObjectCollection();            
            using (var acadDb = AcadDatabase.Use(database))
            {
                var beamTextGroupObjs = new List<ThBeamMarkBlkInfo>(); // 成块的梁文字，标注原始区域,文字移动方向
                var geoExtents = _geos.Select(o => o.Boundary).ToCollection().ToExtents2d(); // 获取ObjIds的范围
                geoExtents = geoExtents.Enlarge(_printParameter.FloorSpacing * 0.1); // 把范围扩大指定距离
                dwgExistedElements = GetAllObjsInRange(acadDb, geoExtents); // 获取Dwg此范围内的所有"Major:Structure"对象
                if(_printParameter.ShowSlabHatchAndMark==false)
                {
                    // 保留之前生成的楼板边界，楼板填充，楼板标记
                    dwgExistedElements = FilterSlabRelatedElements(dwgExistedElements);
                }
                var dwgExistedBeamMarkBlks = GetBeamMarks(dwgExistedElements); // 图纸上已存在的梁标注(块)

                // 打印对象            
                // 打印楼梯板对角线及标注
                Append(PrintStairSlabCorner(acadDb, stairSlabCorners));

                // 打印墙、柱、楼板、梁、洞、标注
                var res = PrintGeos(acadDb, _geos, slabHatchConfigs); //BeamLines,BeamTexts
                var beamLines = res.Item1.ToDBObjectCollection(acadDb);
                var beamTexts = res.Item2.Keys.ToCollection().ToDBObjectCollection(acadDb);
                var beamTextInfos = new Dictionary<DBText, Vector3d>();
                beamTexts.OfType<DBText>().ForEach(o => beamTextInfos.Add(o, res.Item2[o.ObjectId]));

                // 打印双梁标注
                // 用于把打印的文字转成块,最后把梁文字删除掉  
                var dblRowBeamMarkIds = PrintDoubleRowBeams(acadDb, dblRowBeamMarks);
                dblRowBeamMarkIds.ForEach(o => Append(o.Item1));
                dblRowBeamMarkIds.ForEach(o =>
                {
                    var texts = o.Item1.ToDBObjectCollection(database);
                    var areas = texts.OfType<DBText>().Select(x => beamMarkAreas[x].Item1).Where(x => x.Count>0).ToList();
                    beamTextGroupObjs.Add(new ThBeamMarkBlkInfo(texts, UnionAreas(areas),o.Item2));
                });

                // 对双梁文字调整位置(后处理)  
                AdjustDblRowMarkPos(acadDb, dblRowBeamMarkIds, beamLines);

                // 将带有标高的文字，换成两行(后处理)                           
                var adjustService = new ThAdjustBeamMarkService(beamLines, beamTextInfos);
                adjustService.Adjust(acadDb);

                // 将生成的文字打印出来
                adjustService.DoubleRowTexts.ForEach(x =>
                {
                    // item1 被分为两行字 item2 and item3, item1被删除
                    removedTexts.Add(x.Item1);
                    ObjIds.Remove(x.Item1.ObjectId);
                    var dblRowTextIds = new ObjectIdCollection();
                    dblRowTextIds.AddRange(ThAnnotationPrinter.Print(acadDb, x.Item2, _beamTextConfig));
                    dblRowTextIds.AddRange(ThAnnotationPrinter.Print(acadDb, x.Item3, _beamTextConfig));
                    Append(dblRowTextIds);
                    beamTextGroupObjs.Add(new ThBeamMarkBlkInfo(new DBObjectCollection() { x.Item2, x.Item3 }, beamMarkAreas[x.Item1].Item1, beamMarkAreas[x.Item1].Item2));
                    var item1Origin = beamMarkOriginTextPos[x.Item1];
                    beamMarkOriginTextPos.Add(x.Item2, item1Origin);
                    beamMarkOriginTextPos.Add(x.Item3, item1Origin);
                });

                // 把不是双行标注的文字加入到beamTextObjIds中
                beamTexts.Difference(removedTexts).OfType<DBText>()
                    .ForEach(o => beamTextGroupObjs.Add(new ThBeamMarkBlkInfo(new DBObjectCollection() {o}, beamMarkAreas[o].Item1, beamMarkAreas[o].Item2)));

                // 寻找梁区域内指定范围是否已存在标注
                using (var filter = new ThGeneratedBeamMarkFilter(dwgExistedBeamMarkBlks))
                {
                    //filter.FilterBySide(beamTextGroupObjs.Select(o => o.Item1).ToList(), beamMarkOriginTextPos, beamPolygonCenters);
                    filter.FilterByArea(beamTextGroupObjs);                    
                    convertBlkGroups.AddRange(filter.Results); // 需要转换成块的文字组合
                    // 把要保留的梁标注块添加到ObjIds集合中
                    Append(filter.KeepGeneratedBeamMarkBlks.OfType<DBObject>().Select(o => o.ObjectId).ToCollection());
                    dwgExistedElements = dwgExistedElements.Difference(filter.KeepGeneratedBeamMarkBlks);
                    updateGeneratedBeamBlks = filter.UpdateGeneratedBeamBlks;
                }
                  
                // 把梁文字装入到removedTexts中，梁文字最后要删除
                beamTextGroupObjs.ForEach(o => removedTexts.AddRange(o.Marks));

                // 释放 beamPolygonCenters
                var beamPolygonCentersKeys = beamPolygonCenters.Keys.ToCollection();
                var beamPolygonCentersValues = beamPolygonCenters.Values.ToCollection();
                beamPolygonCentersKeys.MDispose();
                beamPolygonCentersValues.MDispose();
            }

            // 转换块、打印标题、层高表，
            // 这儿开一个事务的目的因为前面生成的梁文字只有提交后位置是准的，否则转换块的时候会跑偏
            using (var acadDb = AcadDatabase.Use(database))
            {
                // 把梁文字转换成块                
                var converter = new ThBeamTextBlkConverter();
                var beamBlkIds = converter.Convert(acadDb, convertBlkGroups);
                var updateBlkIds = converter.Update(acadDb,updateGeneratedBeamBlks);

                // 把转化的块和图纸上需要保留的块加入到ObjIds中
                Append(beamBlkIds);
                Append(updateBlkIds);

                // 打印标题
                var textRes = PrintHeadText(acadDb);
                Append(textRes.Item1);
                Append(textRes.Item2);
                AppendToBlockObjIds(textRes.Item2);

                // 插入基点
                var basePointId = InsertBasePoint(acadDb, _printParameter.BasePoint);
                Append(basePointId);
                AppendToBlockObjIds(basePointId);

                // 打印柱表
                var elevationTblBasePt = GetElevationBasePt(acadDb);
                var elevationInfos = GetElevationInfos();
                elevationInfos = elevationInfos.OrderBy(o => int.Parse(o.FloorNo)).ToList(); // 按自然层编号排序
                Append(PrintElevationTable(acadDb, elevationTblBasePt, elevationInfos));

                if(_printParameter.ShowSlabHatchAndMark)
                {
                    // 打印楼板填充
                    // 表右上基点
                    var slabPatternTblRightUpBasePt = new Point3d(elevationTblBasePt.X, elevationTblBasePt.Y - 1000.0, 0);
                    Append(PrintSlabPatternTable(acadDb, slabPatternTblRightUpBasePt, slabHatchConfigs));
                }
                
                // 删除不要的文字
                Erase(acadDb, removedTexts);
                Erase(acadDb, dwgExistedElements);

                // 过滤无效Id
                ObjIds = ObjIds.OfType<ObjectId>().Where(o => o.IsValid && !o.IsErased).ToCollection();
                ObjIds = Difference(ObjIds);

                // 成块的对象
                AppendToBlockObjIds(GetBlockObjIds(acadDb, ObjIds));
            }
        }

        private ObjectIdCollection GetBlockObjIds(AcadDatabase acadDb, ObjectIdCollection floorObjIds)
        {
            var blkIds = new ObjectIdCollection();
            if (floorObjIds.Count == 0)
            {
                return blkIds;
            }
            floorObjIds.OfType<ObjectId>().ForEach(o =>
            {
                var entity = acadDb.Element<Entity>(o);
                if (entity.Layer == ThPrintLayerManager.BeamLayerName ||
                entity.Layer == ThPrintLayerManager.BelowColumnLayerName ||
                entity.Layer == ThPrintLayerManager.BelowColumnHatchLayerName ||
                entity.Layer == ThPrintLayerManager.ColumnLayerName ||
                entity.Layer == ThPrintLayerManager.ColumnHatchLayerName ||
                entity.Layer == ThPrintLayerManager.BelowShearWallLayerName ||
                entity.Layer == ThPrintLayerManager.BelowShearWallHatchLayerName ||
                entity.Layer == ThPrintLayerManager.ShearWallLayerName ||
                entity.Layer == ThPrintLayerManager.ShearWallHatchLayerName ||
                entity.Layer == ThPrintLayerManager.ConstructColumnLayerName ||
                entity.Layer == ThPrintLayerManager.ConstructColumnHatchLayerName ||
                entity.Layer == ThPrintLayerManager.PassHeightWallLayerName ||
                entity.Layer == ThPrintLayerManager.PassHeightWallHatchLayerName ||
                entity.Layer == ThPrintLayerManager.WindowWallLayerName ||
                entity.Layer == ThPrintLayerManager.WindowWallHatchLayerName)
                {
                    blkIds.Add(o);
                }
            });
            return blkIds;
        }

        private Point3d GetElevationBasePt(AcadDatabase acadDb)
        {
            var extents = GetPrintObjsExtents(acadDb);
            var maxX = extents.MaxPoint.X;
            var minY = extents.MinPoint.Y;
            return  new Point3d(maxX + 1500.0, minY, 0);
        }

        private DBObjectCollection GetBeamMarks(DBObjectCollection objs)
        {
            return objs.OfType<BlockReference>()
                .Where(o => o.Layer == ThPrintLayerManager.BeamTextLayerName)
                .ToCollection();
        }

        private DBObjectCollection GetAllObjsInRange(AcadDatabase acadDb, Extents2d extents)
        {
            return acadDb.ModelSpace.OfType<Entity>().Where(o =>
            {
                return IsStructureMajor(o) && 
                (o.GeometricExtents.MinPoint.IsIn(extents, false) ||
                o.GeometricExtents.MaxPoint.IsIn(extents, false));
            }).ToCollection();
        }

        private DBObjectCollection FilterSlabRelatedElements(DBObjectCollection objs)
        {
            var slabElements = objs
                .OfType<Entity>()
                .Where(o =>
            {
                return
                ThSlabPrinter.IsSlabEdge(o) ||
                ThSlabPrinter.IsSlabHatch(o) ||
                ThSlabAnnotationPrinter.IsSlabAnnotation(o) ||
                ThSlabPrinter.IsSlabTableEntity(o);
            }).ToHashSet();

            return  objs.OfType<Entity>().ToHashSet().Except(slabElements).ToCollection();
        }

        private bool IsStructureMajor(Entity entity)
        {
            return entity.Hyperlinks
                .OfType<HyperLink>()
                .Where(h =>
            {
                if (h.Name == "Info")
                {
                    return h.Description == "Major:Structure";
                }
                else
                {
                    return false;
                }
            }).Any();
        }

        private DBObjectCollection CreateStairSlabCorner(DBObjectCollection tenThckSlabTexts,
            DBObjectCollection slabs)
        {
            // 创建楼梯间楼板斜线标记
            var builder = new ThBuildStairSlabLineService();
            return builder.Build(tenThckSlabTexts, slabs);
        }

        private void AdjustDblRowMarkPos(AcadDatabase acadDb, List<Tuple<ObjectIdCollection, Vector3d>> dblRowTexts,DBObjectCollection beamLines)
        {
            // 调整双梁标注文字的位置
            var handler = new ThAdjustBeamMarkPosService(beamLines, 70, 50);
            dblRowTexts.ForEach(g =>
            {
                var beamTexts = g.Item1
                .OfType<ObjectId>()
                .Select(o => acadDb.Element<DBObject>(o, true))
                .ToCollection();
                handler.Adjust(beamTexts, g.Item2);
            });
        }

        private void Erase(AcadDatabase acadDb, DBObjectCollection objs)
        {
            objs.OfType<Entity>().ForEach(e =>
            {
                var entity = acadDb.Element<Entity>(e.ObjectId, true);
                entity.Erase();
            });
        }
        private ObjectIdCollection PrintSlabPatternTable(AcadDatabase acadDb,Point3d rightUpbasePt, 
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
                HatchConfigs = cloneHPC,
                RightUpbasePt = rightUpbasePt,
                FlrBottomEle = this._flrBottomEle,
                FlrHeight = this._flrHeight,
            };
            var builder = new ThSlabPatternTableBuilder(tblParameter);
            var results = builder.Build(acadDb);
            return results.OfType<Entity>().Select(o => o.ObjectId).ToCollection();
        }
        private ObjectIdCollection PrintElevationTable(AcadDatabase acadDb, Point3d basePt,List<ElevationInfo> infos)
        {
            var tblBuilder = new ThElevationTableBuilder(infos);
            var objs = tblBuilder.Build();
            var mt = Matrix3d.Displacement(basePt-Point3d.Origin);
            objs.OfType<Entity>().ForEach(e=>e.TransformBy(mt));
            return objs.Print(acadDb);
        }

        private Tuple<ObjectIdCollection, Dictionary<ObjectId, Vector3d>> PrintGeos(
            AcadDatabase acadDb, List<ThGeometry> geos, 
            Dictionary<string, HatchPrintConfig> slabHatchConfigs)
        {
            var beamLines = new ObjectIdCollection();
            var beamTexts = new Dictionary<ObjectId, Vector3d>();
            // 打印到图纸中
            geos.ForEach(o =>
            {
                // Svg解析的属性信息存在于Properties中
                string category = o.Properties.GetCategory();
                if (o.Boundary is DBText dbText)
                {
                    // 文字为注释
                    if (category == ThIfcCategoryManager.SlabCategory)
                    {
                        Append(ThSlabAnnotationPrinter.Print(acadDb, dbText));
                    }
                    else if (category == ThIfcCategoryManager.BeamCategory)
                    {                        
                        Vector3d textMoveDir = new Vector3d();
                        if (o.Properties.ContainsKey(ThSvgPropertyNameManager.DirPropertyName))
                        {
                            textMoveDir = o.Properties.GetDirection().ToVector();
                        }
                        if (textMoveDir.Length <= 1e-6)
                        {
                            textMoveDir = Vector3d.XAxis.RotateBy(dbText.Rotation, Vector3d.ZAxis).GetPerpendicularVector().Negate();
                        }
                        var beamAnnotions = ThAnnotationPrinter.Print(acadDb, dbText, _beamTextConfig);
                        Append(beamAnnotions);
                        beamAnnotions.OfType<ObjectId>().ForEach(e => beamTexts.Add(e, textMoveDir)); // 把文字的移动方向传出去
                    }
                    else
                    {
                        //Unknown text                           
                    }
                }
                else
                {
                    if (category == ThIfcCategoryManager.BeamCategory)
                    {
                        var config = ThBeamPrinter.GetBeamConfig(o.Properties);
                        var beamRes = ThBeamPrinter.Print(acadDb, o.Boundary as Curve, config);
                        Append(beamRes);
                        beamRes.OfType<ObjectId>().ForEach(e => beamLines.Add(e));
                    }
                    else if (category == ThIfcCategoryManager.ColumnCategory)
                    {
                        var description = o.Properties.GetDescription();
                        if(description.IsStandardColumn())
                        {
                            if (o.IsUpperFloorColumn())
                            {
                                Append(PrintUpperColumn(acadDb, o));
                            }
                            else if (o.IsBelowFloorColumn())
                            {
                                Append(PrintBelowColumn(acadDb, o));
                            }
                        }
                        else
                        {
                            if(description.IsConstructColumn())
                            {
                                Append(PrintConstructColumn(acadDb, o));
                            }
                        }
                    }
                    else if (category == ThIfcCategoryManager.WallCategory)
                    {
                        var description = o.Properties.GetDescription();
                        if(description.IsStandardWall())
                        {
                            if (o.IsUpperFloorShearWall())
                            {
                                Append(PrintUpperShearWall(acadDb, o));
                            }
                            else if (o.IsBelowFloorShearWall())
                            {
                                Append(PrintBelowShearWall(acadDb, o));
                            }
                        }
                        else
                        {
                            if(description.IsPassHeightWall())
                            {
                                Append(PrintPassHeightWall(acadDb, o));
                            }
                            else if(description.IsWindowWall())
                            {
                                Append(PrintWindowWall(acadDb, o));
                            }
                        }
                    }
                    else if (category == ThIfcCategoryManager.SlabCategory)
                    {
                        var outlineConfig = ThSlabPrinter.GetSlabConfig();
                        var bg = o.Properties.GetElevation();
                        var hatchConfig = slabHatchConfigs.ContainsKey(bg) ? slabHatchConfigs[bg] : null;
                        if (hatchConfig != null)
                        {
                            if (o.Boundary is Polyline polyline)
                            {
                                Append(ThSlabPrinter.Print(acadDb, polyline, outlineConfig, hatchConfig));
                            }
                            else if (o.Boundary is MPolygon mPolygon)
                            {
                                Append(ThSlabPrinter.Print(acadDb, mPolygon, outlineConfig, hatchConfig));
                            }
                        }
                    }
                    else if (category == ThIfcCategoryManager.OpeningElementCategory)
                    {
                        var outlineConfig = ThHolePrinter.GetHoleConfig();
                        var hatchConfig = ThHolePrinter.GetHoleHatchConfig();
                        Append(ThHolePrinter.Print(acadDb, o.Boundary as Polyline, outlineConfig, hatchConfig));
                    }
                }
            });

            return Tuple.Create(beamLines, beamTexts);
        }
        private Point3dCollection UnionAreas(List<Point3dCollection> overlapPolygons)
        {
            if(overlapPolygons.Count==0)
            {
                return new Point3dCollection();
            }
            else if(overlapPolygons.Count == 1)
            {
                return overlapPolygons[0];
            }
            else
            {
                var polys = overlapPolygons
                    .Select(o => o.CreatePolyline())
                    .ToCollection();
                if(polys.Count>0)
                {
                    var res = polys.FilterSmallArea(1.0).UnionPolygons(false);
                    if (res.Count > 0 && res.OfType<Polyline>().Where(p=>p.Area>0.0).Any())
                    {
                        var area = res.OfType<Polyline>().OrderByDescending(p => p.Area).First();
                        var pts = area.Vertices();
                        res.MDispose();
                        polys.MDispose();                        
                        return pts;
                    }
                    else
                    {
                        polys.MDispose();
                        return new Point3dCollection();
                    }
                }
                else
                {
                    return new Point3dCollection();
                }
            }
        }

        private List<Tuple<ObjectIdCollection,Vector3d>> PrintDoubleRowBeams(AcadDatabase acadDb, List<List<ThGeometry>> doubleRowBeams)
        {
            var results = new List<Tuple<ObjectIdCollection, Vector3d>>();
            // 打印到图纸中
            doubleRowBeams.ForEach(g =>
            {
                var beamIds = new ObjectIdCollection();
                Vector3d textMoveDir = new Vector3d();
                for(int i=0;i<g.Count;i++)
                {
                    if (g[i].Boundary is DBText dbText)
                    {
                        if (g[i].Properties.ContainsKey(ThSvgPropertyNameManager.DirPropertyName) && textMoveDir.Length <= 1e-6)
                        {
                            textMoveDir = g[i].Properties.GetDirection().ToVector();
                        }                        
                    }
                    if(textMoveDir.Length > 1e-6)
                    {
                        break;
                    }
                }                
                if (textMoveDir.Length <= 1e-6)
                {
                    foreach (var geo in g)
                    {
                        if (geo.Boundary is DBText dbText)
                        {
                            textMoveDir = Vector3d.XAxis.RotateBy(dbText.Rotation, Vector3d.ZAxis).GetPerpendicularVector().Negate();
                            break;
                        }
                    }
                }
                if (g.Count==2 && g[0].Boundary is DBText text1 && g[1].Boundary is DBText text2)
                {
                    var firstSpec = text1.TextString.GetBeamSpec();
                    var secondSpec = text2.TextString.GetBeamSpec();
                    if(firstSpec == secondSpec)
                    {
                        var firstBG = text1.TextString.GetBGContent();
                        var secondBG = text2.TextString.GetBGContent();
                        if(string.IsNullOrEmpty(firstBG))
                        {
                            firstBG = "BG";
                        }
                        else
                        {
                            firstBG = firstBG.Replace("(", "");
                            firstBG = firstBG.Replace(")", "");
                        }
                        if (string.IsNullOrEmpty(secondBG))
                        {
                            secondBG = "BG";
                        }
                        else
                        {
                            secondBG = secondBG.Replace("(", "");
                            secondBG = secondBG.Replace(")", "");
                        }
                        text1.TextString = firstSpec;
                        text2.TextString = "("+ firstBG+"、"+secondBG+")";
                    }
                }
                g.ForEach(o =>
                {
                    if (o.Boundary is DBText dbText)
                    {
                        if (dbText.ObjectId == ObjectId.Null)
                        {
                            beamIds.AddRange(ThAnnotationPrinter.Print(acadDb, dbText, _beamTextConfig));
                        }
                    }
                });                
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

        private ObjectIdCollection PrintStairSlabCorner(AcadDatabase acadDb, DBObjectCollection corners)
        {
            var results = new ObjectIdCollection();
            if (corners.Count > 0)
            {
                var textConfig = ThStairLineMarkPrinter.GetTextConfig(_printParameter.DrawingScale);
                var lineConfig = ThStairLineMarkPrinter.GetLineConfig();
                corners.OfType<Line>().ForEach(l => results.AddRange(
                    ThStairLineMarkPrinter.Print(acadDb, l, lineConfig, textConfig)));
            }
            return results.OfType<ObjectId>().ToCollection();
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
                if (o.Boundary is DBText dbText)
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

        private Tuple<ObjectIdCollection, ObjectIdCollection> PrintHeadText(AcadDatabase acadDb)
        {
            // 打印自然层标识, eg 一层~五层结构平面层
            var flrRange = _floorInfos.GetFloorRange(_flrBottomEle);
            if (string.IsNullOrEmpty(flrRange))
            {
                return Tuple.Create(new ObjectIdCollection(),new ObjectIdCollection());
            }
            var stdFlrInfo = _floorInfos.GetStdFlrInfo(_flrBottomEle);
            return PrintHeadText(acadDb, flrRange, stdFlrInfo);
        }
    }
}
