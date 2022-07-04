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
using ThMEPStructure.StructPlane.Print;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThSvgEntityPrintService
    {       
        // 标题文字距离图纸底部的距离
        private double HeadTextDisToPaperBottom = 3500.0;
        /// <summary>
        /// 楼层底部标高
        /// </summary>
        public double FlrBottomEle { get; private set; }
        /// <summary>
        /// 楼层高度
        /// </summary>
        public double FlrHeight { get; private set; }
        private string DrawingScale { set; get; }
        public List<ThFloorInfo> FloorInfos { get; set; }
        public bool IsFilterCantiSlab { get; set; } = true;
        /// <summary>
        /// 收集所有当前图纸打印的物体
        /// </summary>
        public ObjectIdCollection ObjIds { get; private set; }
        private List<ThGeometry> Geos { get; set; } = new List<ThGeometry>();
        private Dictionary<string, string> DocProperties {get;set;} = new Dictionary<string, string>(); 
        public ThSvgEntityPrintService(
            List<ThGeometry> geos,
            List<ThFloorInfo> floorInfos,
            Dictionary<string,string> docProperties,
            string drawingSacle="")
        {
            Geos = geos;
            FloorInfos = floorInfos;
            DocProperties = docProperties;
            ObjIds = new ObjectIdCollection();
            FlrHeight = DocProperties.GetFloorHeight();
            DrawingScale = drawingSacle;
            FlrBottomEle = DocProperties.GetFloorBottomElevation();            
        }
        public void Print(Database db)
        {
            // 过滤空调板及其标注
            if(IsFilterCantiSlab)
            {
                FilterCantiSlabAndMark(Geos);
            }          

            // 获取楼板的标高                
            var elevations = GetSlabElevations(Geos);
            elevations = FilterSlabElevations(elevations);
            var slabHatchConfigs = elevations.GetSlabHatchConfigs();

            // 给墙造洞
            var buildAreaSevice =new ThWallBuildAreaService();
            var newGeos =buildAreaSevice.BuildArea(Geos);

            // 打印对象
            var res = PrintGeos(db, newGeos, slabHatchConfigs); //BeamLines,BeamTexts

            // 过滤多余文字
            var beamLines = res.Item1.ToDBObjectCollection(db);
            var beamTexts = res.Item2.Keys.ToCollection().ToDBObjectCollection(db);
            var removedTexts = FilterBeamMarks(beamLines, beamTexts);

            // 将带有标高的文字，换成两行
            var beamTextInfos = new Dictionary<DBText, Vector3d>();
            beamTexts.Difference(removedTexts)
                .OfType<DBText>().ForEach(o => beamTextInfos.Add(o, res.Item2[o.ObjectId]));           
            var adjustService = new ThAdjustBeamMarkService(db,beamLines, beamTextInfos);
            adjustService.Adjust();
            adjustService.DoubleRowTexts.ForEach(x => removedTexts.Add(x.Item1));

            // 将生成的文字打印出来
            var config = ThAnnotationPrinter.GetAnnotationConfig();
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
            var maxX= newGeos.Where(o => o.Boundary.GeometricExtents != null).Select(o => o.Boundary.GeometricExtents
                .MaxPoint.X).OrderByDescending(o => o).FirstOrDefault();
            var minY = newGeos.Where(o => o.Boundary.GeometricExtents != null).Select(o => o.Boundary.GeometricExtents
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


        private void PrintHeadText(Database database)
        {
            // 打印自然层标识, eg 一层~五层结构平面层
            var flrRange = GetFloorRange();
            if (string.IsNullOrEmpty(flrRange))
            {
                return;
            }
            var extents = ObjIds.ToDBObjectCollection(database).ToExtents2d();
            var textCenter = new Point3d((extents.MinPoint.X + extents.MaxPoint.X) / 2.0,
                extents.MinPoint.Y - HeadTextDisToPaperBottom, 0.0); // 3500 是文字中心到图纸底部的高度
            var printService = new ThPrintDrawingHeadService()
            {
                Head = flrRange,
                DrawingSacle = this.DrawingScale,
                BasePt = textCenter,
            };
            Append(printService.Print(database)); // 把结果存到ObjIds中
        }
        private string GetFloorRange()
        {
            var result = "";
            var stdFloors = FloorInfos.Where(o =>
            {
                double bottomElevation = 0.0;
                if (double.TryParse(o.Bottom_elevation,out bottomElevation))
                {
                    if(Math.Abs(bottomElevation-FlrBottomEle)<=1e-4)
                    {
                        return true;
                    }
                }
                return false;
            });
            if(stdFloors.Count()==1)
            {
                var stdFlr = stdFloors.First().StdFlrNo;
                var floors = FloorInfos.Where(o => o.StdFlrNo == stdFlr);
                if(floors.Count()==1)
                {
                    result = floors.First().FloorNo.NumToChinese()+"层结构平面图";
                }
                else if (floors.Count() > 1)
                {
                    var startRange = floors.First().FloorNo.NumToChinese();
                    var endRange = floors.Last().FloorNo.NumToChinese();
                    result = startRange+"~"+endRange+ "层结构平面图";
                }
            }
            return result;
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

        private List<ElevationInfo> GetElevationInfos()
        {
            var results =new List<ElevationInfo>();
            FloorInfos.ForEach(o =>
            {
                double flrBottomElevation = 0.0;
                if (double.TryParse(o.Bottom_elevation, out flrBottomElevation))
                {
                    flrBottomElevation /= 1000.0;
                }
                double flrHeight = 0.0;
                if (double.TryParse(o.Height, out flrHeight))
                {
                    flrHeight /= 1000.0;
                }
                results.Add(new ElevationInfo()
                {
                    FloorNo = o.FloorNo,
                    BottomElevation = flrBottomElevation.ToString("0.000"),
                    FloorHeight = flrHeight.ToString("0.000"),
                    WallColumnGrade = "",
                    BeamBoardGrade = "",
                });
            });
            return results;
        }

        private Tuple<ObjectIdCollection, Dictionary<ObjectId, Vector3d>> PrintGeos(
            Database db, List<ThGeometry> geos, 
            Dictionary<string, HatchPrintConfig> slabHatchConfigs)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var beamLines = new ObjectIdCollection();
                var beamTexts = new Dictionary<ObjectId,Vector3d>();

                // 这两个数据是为了给10mm楼板用的
                var slabs = new DBObjectCollection();
                var tenThckSlabTexts = new DBObjectCollection();
                
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
                            if(dbText.TextString.IsTenThickSlab())
                            {
                                // 不要打印到界面上
                                //tenThckSlabTexts.Add(dbText); // 后面打开

                                var printer = new ThSlabAnnotationPrinter();
                                Append(printer.Print(db, dbText));
                            }
                            else
                            {
                                var printer = new ThSlabAnnotationPrinter();
                                Append(printer.Print(db, dbText));
                            }
                        }
                        else if (category == ThIfcCategoryManager.BeamCategory)
                        {
                            var decription = o.Properties.GetDescription();
                            if(string.IsNullOrEmpty(decription))
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
                                dbText.TextString = spec+ elevation;
                            }

                            // svg转换的文字角度是0
                            Vector3d textMoveDir = new Vector3d();
                            if(o.Properties.ContainsKey(ThSvgPropertyNameManager.DirPropertyName))
                            {
                                textMoveDir = o.Properties.GetDirection().ToVector();
                            }
                            if(textMoveDir.Length==0.0)
                            {
                                textMoveDir = Vector3d.XAxis.RotateBy(dbText.Rotation, Vector3d.ZAxis).GetPerpendicularVector();
                            }
                            ThAdjustDbTextRotationService.Adjust(dbText, textMoveDir.GetPerpendicularVector());
                            var config = ThAnnotationPrinter.GetAnnotationConfig();
                            var printer = new ThAnnotationPrinter(config);
                            var beamAnnotions = printer.Print(db, dbText);
                            Append(beamAnnotions);
                            beamAnnotions.OfType<ObjectId>().ForEach(e => beamTexts.Add(e, textMoveDir));
                        }
                        else
                        {
                            var config = ThAnnotationPrinter.GetAnnotationConfig();
                            var printer = new ThAnnotationPrinter(config);
                            Append(printer.Print(db, dbText));
                        }
                    }
                    else
                    {
                        if (category == ThIfcCategoryManager.BeamCategory)
                        {
                            var config = o.Properties.GetBeamConfig();
                            var printer = new ThBeamPrinter(config);
                            var beamRes = printer.Print(db, o.Boundary as Curve);
                            Append(beamRes);
                            beamRes.OfType<ObjectId>().ForEach(e => beamLines.Add(e));
                        }
                        else if (category == ThIfcCategoryManager.ColumnCategory)
                        {
                            var outlineConfig = o.Properties.GetColumnOutlineConfig();
                            var hatchConfig = o.Properties.GetColumnHatchConfig();
                            var printer = new ThColumnPrinter(hatchConfig, outlineConfig);
                            Append(printer.Print(db, o.Boundary as Polyline));
                        }
                        else if (category == ThIfcCategoryManager.WallCategory)
                        {
                            var outlineConfig = o.Properties.GetShearWallConfig();
                            var hatchConfig = o.Properties.GetShearWallHatchConfig();
                            var printer = new ThShearwallPrinter(hatchConfig, outlineConfig);
                            if (o.Boundary is Polyline polyline)
                            {
                                Append(printer.Print(db, polyline));
                            }
                            else if (o.Boundary is MPolygon mPolygon)
                            {
                                Append(printer.Print(db, mPolygon));
                            }                            
                        }
                        else if (category == ThIfcCategoryManager.SlabCategory)
                        {
                            var outlineConfig = ThSlabPrinter.GetSlabConfig();
                            var bg = o.Properties.GetElevation();                            
                            if(slabHatchConfigs.ContainsKey(bg))
                            {
                                var hatchConfig = slabHatchConfigs[bg];
                                var printer = new ThSlabPrinter(hatchConfig, outlineConfig);
                                if (o.Boundary is Polyline polyline)
                                {
                                    slabs.Add(polyline);
                                    Append(printer.Print(db, polyline));
                                }
                                else if (o.Boundary is MPolygon mPolygon)
                                {
                                    slabs.Add(mPolygon);
                                    Append(printer.Print(db, mPolygon));
                                }
                            }                            
                        }
                        else if (category == ThIfcCategoryManager.OpeningElementCategory)
                        {
                            var outlineConfig = o.Properties.GetOpeningConfig();
                            var hatchConfig = o.Properties.GetOpeningHatchConfig();
                            var printer = new ThHolePrinter(hatchConfig, outlineConfig);
                            Append(printer.Print(db, o.Boundary as Polyline));
                        }
                    }
                });

                // 创建楼梯间楼板斜线标记
                var builder = new ThBuildStairSlabLineService();
                var slabCorners = builder.Build(tenThckSlabTexts,slabs);
                if(slabCorners.Count>0)
                {
                    var textConfig = ThStairLineMarkPrinter.GetTextConfig();
                    var lineConfig = ThStairLineMarkPrinter.GetLineConfig();
                    var stairLinePrinter = new ThStairLineMarkPrinter(lineConfig, textConfig);
                    slabCorners.OfType<Line>().ForEach(l => Append(stairLinePrinter.Print(db, l)));
                }
                
                return Tuple.Create(beamLines, beamTexts);
            }   
        }    

        private List<string> GetSlabElevations(List<ThGeometry> geos)
        {
            var groups = geos
                .Where(g => g.Properties.GetCategory() == ThIfcCategoryManager.SlabCategory && !(g.Boundary is DBText))
                .Select(g => g.Properties.GetElevation())
                .Where(g => !string.IsNullOrEmpty(g))
                .GroupBy(o => o);
            return groups.OrderByDescending(o => o.Count()).Select(o => o.Key).ToList();
        }
        private List<string> FilterSlabElevations(List<string> elevations)
        {
            return elevations.Where(o =>
             {
                 if (string.IsNullOrEmpty(o))
                 {
                     return false;
                 }
                 else
                 {
                     double tempV = 0.0;
                     if (double.TryParse(o, out tempV))
                     {
                         return Math.Abs(tempV - FlrHeight) <= 1.0 ? false:true ;
                     }
                     else
                     {
                         return false;
                     }
                 }
             }).ToList();
        }
        
        private void Append(ObjectIdCollection objIds)
        {
            foreach(ObjectId objId in objIds)
            {
                ObjIds.Add(objId);
            }
        }
        private void FilterCantiSlabAndMark(List<ThGeometry> geos)
        {
            // 找到空调板
            var cantiGeos = geos.Where(o => o.Properties.IsSlab() && o.Properties.IsCantiSlab()).ToList();
            // 找到楼板文字
            var slabTextGeos = geos.Where(o => o.Properties.IsSlab() && o.Boundary is DBText).ToList();
            // 找到包含在空调板里的文字
            var cantislabTextGeos = slabTextGeos.Where(o =>
            {
                if (o.Boundary is DBText dbText)
                {
                    return cantiGeos.Where(cg => cg.Boundary.EntityContains(dbText.Position)).Any();
                }
                else
                {
                    return false;
                }
            }).ToList();


            // 移除
            geos = geos.Where(o => !cantiGeos.Contains(o)).ToList();
            geos = geos.Where(o => !cantislabTextGeos.Contains(o)).ToList();
        }
    }
}
