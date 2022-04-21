using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPStructure.StructPlane.Print;
using System.Text.RegularExpressions;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThSvgEntityPrintService
    {
        public double FlrBottomEle { get; private set; }
        public double FlrElevation { get; private set; }
        public double FlrHeight 
        { 
            get
            {
                return FlrElevation - FlrBottomEle;
            }
        }
        /// <summary>
        /// 收集所有当前图纸打印的物体
        /// </summary>
        public ObjectIdCollection ObjIds { get; private set; }
        private List<ThGeometry> Geos { get; set; } = new List<ThGeometry>();
        private Dictionary<string, string> DocProperties {get;set;} = new Dictionary<string, string>(); 
        public ThSvgEntityPrintService(List<ThGeometry> geos,Dictionary<string,string> docProperties)
        {
            Geos = geos;            
            DocProperties = docProperties;
            ObjIds = new ObjectIdCollection();
            FlrElevation = DocProperties.GetFloorElevation();
            FlrBottomEle = DocProperties.GetFloorBottomElevation();            
        }
        public void Print(Database db)
        {
            // 从模板导入要打印的图层
            Import(db);

            // 获取楼板的标高                
            var elevations = GetSlabElevations(Geos);
            elevations = FilterSlabElevations(elevations);
            var slabHatchConfigs = GetSlabHatchConfigs(elevations);

            // 打印对象
            var res = PrintGeos(db, Geos, slabHatchConfigs); //BeamLines,BeamTexts

            // 过滤多余文字
            var beamLines = ToDBObjectCollection(db, res.Item1);
            var beamTexts = ToDBObjectCollection(db, res.Item2);
            var removedTexts = FilterBeamMarks(beamLines, beamTexts);
            //SetColor(db, removedTexts, 1);
            //removedTexts.OfType<DBText>()
            //    .Select(o => new Circle(o.Position, Vector3d.ZAxis, 500))
            //    .ToCollection()
            //    .Print(db);
            Erase(db, removedTexts);
            
            // 打印柱表
            var maxX= Geos.Where(o => o.Boundary.GeometricExtents != null).Select(o => o.Boundary.GeometricExtents
                .MaxPoint.X).OrderByDescending(o => o).FirstOrDefault();
            var minY = Geos.Where(o => o.Boundary.GeometricExtents != null).Select(o => o.Boundary.GeometricExtents
                 .MinPoint.Y).OrderBy(o => o).FirstOrDefault();
            var elevationTblBasePt = new Point3d(maxX+1000.0, minY,0);
            //PrintElevationTable(db,elevationTblBasePt, 0.0);

            // 打印楼板填充
            // 表右上基点
            var slabPatternTblRightUpBasePt = new Point3d(elevationTblBasePt.X, 
                elevationTblBasePt.Y-1000.0,0);
            PrintSlabPatternTable(db, slabPatternTblRightUpBasePt, slabHatchConfigs);

            // 过滤无效Id
            ObjIds = ObjIds.OfType<ObjectId>().Where(o => o.IsValid && !o.IsErased).ToCollection();
        }
        private DBObjectCollection ToDBObjectCollection(Database db,ObjectIdCollection objIds)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                return objIds
                    .OfType<ObjectId>()
                    .Select(o => acadDb.Element<Entity>(o)).ToCollection();
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
        private void SetColor(Database db, DBObjectCollection objs,short colorIndex)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                objs.OfType<Entity>().ForEach(e =>
                {
                    var entity = acadDb.Element<Entity>(e.ObjectId, true);
                    entity.ColorIndex = colorIndex;
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
                FlrBottomEle = DocProperties.GetFloorBottomElevation(),
                FlrElevation = DocProperties.GetFloorElevation(),
            };
            var builder = new ThSlabPatternTableBuilder(tblParameter);
            var results = builder.Build();
            Append(results.OfType<Entity>().Select(o => o.ObjectId).ToCollection());
        }
        private void PrintElevationTable(Database db, Point3d basePt,double rotateRad)
        {
            var elevationInfos= new List<ElevationInfo>();
            var flrBottomElevation = FlrBottomEle/1000.0;
            var flrElevation = FlrElevation / 1000.0;
            elevationInfos.Add(new ElevationInfo()
            {
                FloorNo = "1",
                Elevation = flrBottomElevation.ToString("0.000"),
                FloorHeight = (flrElevation - flrBottomElevation).ToString("0.000"),
                WallColumnGrade="",
                BeamBoardGrade="",
            }) ;            
            var tblBuilder = new ThElevationTableBuilder(elevationInfos);
            var objs = tblBuilder.Build();
            var mt1 = Matrix3d.Rotation(rotateRad, Vector3d.ZAxis, Point3d.Origin);
            var mt2 = Matrix3d.Displacement(basePt-Point3d.Origin);
            objs.OfType<Entity>().ForEach(e=>e.TransformBy(mt1.PreMultiplyBy(mt2)));
            Append(objs.Print(db));
        }
        private Tuple<ObjectIdCollection, ObjectIdCollection> PrintGeos(
            Database db, List<ThGeometry> geos, 
            Dictionary<string, HatchPrintConfig> slabHatchConfigs)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var beamLines = new ObjectIdCollection();
                var beamTexts = new ObjectIdCollection();

                // 打印到图纸中
                geos.ForEach(o =>
                {
                    // Svg解析的属性信息存在于Properties中
                    string category = o.Properties.GetCategory();
                    if(o.Boundary is DBText dbText)
                    {
                        // 文字为注释
                        if (category == "IfcSlab")
                        {
                            var printer = new ThSlabAnnotationPrinter();
                            Append(printer.Print(db, dbText));
                        }
                        else if (category == "IfcBeam")
                        {
                            if (IsEqualElevation(dbText.TextString))
                            {
                                dbText.TextString = GetBeamSpec(dbText.TextString);
                            }
                            else
                            {
                                // update to BG 
                                dbText.TextString = UpdateBGElevation(dbText.TextString);
                            }
                            var config = ThAnnotationPrinter.GetAnnotationConfig();
                            var printer = new ThAnnotationPrinter(config);
                            var beamAnnotions =printer.Print(db, dbText);
                            Append(beamAnnotions);
                            beamAnnotions.OfType<ObjectId>().ForEach(e => beamTexts.Add(e));
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
                        if (category == "IfcBeam")
                        {
                            var config = GetBeamConfig(o.Properties);
                            var printer = new ThBeamPrinter(config);
                            var beamRes = printer.Print(db, o.Boundary as Curve);
                            Append(beamRes);
                            beamRes.OfType<ObjectId>().ForEach(e => beamLines.Add(e));
                        }
                        else if (category == "IfcColumn")
                        {
                            var outlineConfig = GetColumnOutlineConfig(o.Properties);
                            var hatchConfig = GetColumnHatchConfig(o.Properties);
                            var printer = new ThColumnPrinter(hatchConfig, outlineConfig);
                            Append(printer.Print(db, o.Boundary as Polyline));
                        }
                        else if (category == "IfcWall")
                        {
                            var outlineConfig = GetShearWallConfig(o.Properties);
                            var hatchConfig = GetShearWallHatchConfig(o.Properties);
                            var printer = new ThShearwallPrinter(hatchConfig, outlineConfig);
                            Append(printer.Print(db, o.Boundary as Polyline));
                        }
                        else if (category == "IfcSlab")
                        {
                            var outlineConfig = GetSlabConfig(o.Properties);
                            var bg = o.Properties.GetElevation();
                            if(slabHatchConfigs.ContainsKey(bg))
                            {
                                var hatchConfig = slabHatchConfigs[bg];
                                var printer = new ThSlabPrinter(hatchConfig, outlineConfig);
                                Append(printer.Print(db, o.Boundary as Polyline));
                            }
                        }
                        else if (category == "IfcOpeningElement")
                        {
                            var outlineConfig = GetOpeningConfig(o.Properties);
                            var hatchConfig = GetOpeningHatchConfig(o.Properties);
                            var printer = new ThHolePrinter(hatchConfig, outlineConfig);
                            Append(printer.Print(db, o.Boundary as Polyline));
                        }
                    }
                });

                return Tuple.Create(beamLines, beamTexts);
            }   
        }
        private string UpdateBGElevation(string elevation)
        {
            // 200x530(BG+5.670) 
            var spec = GetBeamSpec(elevation);
            var bg = GetElevation(elevation);
            if(bg.HasValue)
            {
                var minus = (bg.Value - FlrHeight)/1000.0; //mm to m
                if(minus>=0)
                {
                    return spec + "(BG+" + minus.ToString("0.000")+")";
                }
                else
                {
                    return spec + "(BG" + minus.ToString("0.000") + ")";
                }
            }
            else
            {
                return elevation;
            }
        }
        private string GetBeamSpec(string beamElevation)
        {
            // 200x530(BG+5.670) 
            var newElevation = beamElevation.Replace("（","(");
            var firstIndex = newElevation.IndexOf('(');
            if (firstIndex > 0)
            {
                return newElevation.Substring(0, firstIndex);
            }
            else
            {
                return newElevation;
            }
        }
        private double? GetElevation(string elevation)
        {
            string pattern1 = @"[+-]{1}\s{0,}\d+[.]?\d+";
            var mt1 = Regex.Matches(elevation, pattern1);
            if (mt1.Count == 1)
            {
                var value = mt1[0].Value;
                string pattern2 = @"\d+[.]?\d+";
                var mt2 = Regex.Matches(value, pattern2);
                double plus = 1.0;
                var firstChar = value[0];
                if (firstChar == '-')
                {
                    plus *= -1.0;
                }
                if (mt2.Count == 1)
                {
                    var dValue = double.Parse(mt2[0].Value);
                    dValue *= plus;
                    dValue *= 1000.0; // m To mm
                    return dValue;
                }
            }
            return null;
        }
        private bool IsEqualElevation(string beamBGMark)
        {
            var elevation = GetElevation(beamBGMark);
            if(elevation.HasValue)
            {
                return Math.Abs(elevation.Value - FlrHeight) <= 1.0;
            }
            else
            {
                return false;
            }
        }
        private List<string> GetSlabElevations(List<ThGeometry> geos)
        {
            var groups = geos
                .Where(g => g.Properties.GetCategory() == "IfcSlab" && !(g.Boundary is DBText))
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
        private void Import(Database database)
        {
            using (var acadDb = AcadDatabase.Use(database))
            using (var blockDb = AcadDatabase.Open(ThCADCommon.StructPlanePath(), DwgOpenMode.ReadOnly, false))
            {
                // 导入图层
                ThPrintLayerManager.AllLayers.ForEach(layer =>
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(layer), true);
                });

                // 导入样式
                ThPrintStyleManager.AllTextStyles.ForEach(style =>
                {
                    acadDb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(style), false);
                });

                // 导入块
                ThPrintBlockManager.AllBlockNames.ForEach(b =>
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(b), true);
                });
            }
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
        private PrintConfig GetColumnOutlineConfig(Dictionary<string, object> properties)
        {
            var fillColor = properties.GetFillColor();
            if(fillColor == "#7f3f3f") // 上层柱
            {
                return ThColumnPrinter.GetUpperColumnConfig();
            }
            else if(fillColor == "#ff0000" || fillColor == "Red") //下层柱
            {
                return ThColumnPrinter.GetBelowColumnConfig();
            }
            else
            {
                return new PrintConfig();
            }
        }
        private HatchPrintConfig GetColumnHatchConfig(Dictionary<string, object> properties)
        {
            var fillColor = properties.GetFillColor();
            if (fillColor == "#7f3f3f") // 上层柱
            {
                return ThColumnPrinter.GetUpperColumnHatchConfig();
            }
            else if (fillColor == "#ff0000" || fillColor == "Red") //下层柱
            {
                return ThColumnPrinter.GetBelowColumnHatchConfig();
            }
            else
            {
                return new HatchPrintConfig();                
            }
        }
        private PrintConfig GetShearWallConfig(Dictionary<string, object> properties)
        {
            var fillColor = properties.GetFillColor();
            if (fillColor == "#ff7f00") // 上层墙
            {
                return ThShearwallPrinter.GetUpperShearWallConfig();
            }
            else if (fillColor == "#ffff00" || fillColor == "Yellow") //下层墙
            {
                return ThShearwallPrinter.GetBelowShearWallConfig();
            }
            else
            {
                return new PrintConfig();
            }
        }
        private HatchPrintConfig GetShearWallHatchConfig(Dictionary<string, object> properties)
        {
            var fillColor = properties.GetFillColor();
            if (fillColor == "#ff7f00") // 上层墙
            {
                return ThShearwallPrinter.GetUpperShearWallHatchConfig();
            }
            else if (fillColor == "#ffff00" || fillColor == "Yellow") //下层墙
            {
                return ThShearwallPrinter.GetBelowShearWallHatchConfig();
            }
            else
            {
                return new HatchPrintConfig();
            }
        }
        private PrintConfig GetSlabConfig(Dictionary<string, object> properties)
        {
            return ThSlabPrinter.GetSlabConfig();
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
        private PrintConfig GetOpeningConfig(Dictionary<string, object> properties)
        {
            return ThHolePrinter.GetHoleConfig();
        }
        private HatchPrintConfig GetOpeningHatchConfig(Dictionary<string, object> properties)
        {
            return ThHolePrinter.GetHoleHatchConfig();
        }
        private void Append(ObjectIdCollection objIds)
        {
            foreach(ObjectId objId in objIds)
            {
                ObjIds.Add(objId);
            }
        }
    }
}
