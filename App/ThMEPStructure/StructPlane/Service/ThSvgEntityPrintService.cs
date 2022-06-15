﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NFox.Cad;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using ThMEPEngineCore.Model;
using ThMEPStructure.StructPlane.Print;
using ThMEPEngineCore.IO.SVG;
using ThMEPEngineCore.CAD;
using ThMEPStructure.Model.Printer;
using ThMEPStructure.Common;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThSvgEntityPrintService
    {
        private double LtScale = 500;
        private int Measurement = 0;
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
            // 从模板导入要打印的图层
            Import(db);

            // 设置系统变量
            SetSysVariables();

            // 获取楼板的标高                
            var elevations = GetSlabElevations(Geos);
            elevations = FilterSlabElevations(elevations);
            var slabHatchConfigs = GetSlabHatchConfigs(elevations);

            // 给墙造洞
            var buildAreaSevice =new ThWallBuildAreaService();
            var newGeos =buildAreaSevice.BuildArea(Geos);

            // 打印对象
            var res = PrintGeos(db, newGeos, slabHatchConfigs); //BeamLines,BeamTexts

            // 过滤多余文字
            var beamLines = ToDBObjectCollection(db, res.Item1);
            var beamTexts = ToDBObjectCollection(db, res.Item2.Keys.ToCollection());
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

        private void SetSysVariables()
        {
            acadApp.Application.SetSystemVariable("LTSCALE", LtScale);
            acadApp.Application.SetSystemVariable("MEASUREMENT", Measurement);
        }

        private DBObjectCollection ToDBObjectCollection(Database db,ObjectIdCollection objIds)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                return objIds
                    .OfType<ObjectId>()
                    .Where(o=>!o.IsErased)
                    .Select(o => acadDb.Element<Entity>(o)).ToCollection();
            }
        }
        private Extents2d ToExtents2d(DBObjectCollection objs)
        {
            var extents = new Extents2d();
            double minX = double.MaxValue, minY = double.MaxValue,
                maxX = double.MinValue, maxY = double.MinValue;
            objs.OfType<Curve>().ForEach(entity =>
            {
                if (!entity.IsErased && entity.GeometricExtents != null)
                {
                    if (entity.GeometricExtents.MinPoint.X < minX)
                    {
                        minX = entity.GeometricExtents.MinPoint.X;
                    }
                    if (entity.GeometricExtents.MinPoint.Y < minY)
                    {
                        minY = entity.GeometricExtents.MinPoint.Y;
                    }
                    if (entity.GeometricExtents.MaxPoint.X > maxX)
                    {
                        maxX = entity.GeometricExtents.MaxPoint.X;
                    }
                    if (entity.GeometricExtents.MaxPoint.Y > maxY)
                    {
                        maxY = entity.GeometricExtents.MaxPoint.Y;
                    }
                }
            });
            extents = new Extents2d(minX, minY, maxX, maxY);
            return extents;
        }
        private void PrintHeadText(Database database)
        {
            // 打印自然层标识, eg 一层~五层结构平面层
            var flrRange = GetFloorRange();
            if (string.IsNullOrEmpty(flrRange))
            {
                return;
            }
            var extents = ToExtents2d(ToDBObjectCollection(database, ObjIds));
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
                        if (category == "IfcSlab")
                        {
                            if(IsTenThickSlab(dbText.TextString))
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

                            // svg文字是在原点生成
                            Vector3d textMoveDir = new Vector3d();
                            if(o.Properties.ContainsKey(ThSvgPropertyNameManager.DirPropertyName))
                            {
                                textMoveDir = ToVector(o.Properties[ThSvgPropertyNameManager.DirPropertyName].ToString());
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
                            if (o.Boundary is Polyline polyline)
                            {
                                Append(printer.Print(db, polyline));
                            }
                            else if (o.Boundary is MPolygon mPolygon)
                            {
                                Append(printer.Print(db, mPolygon));
                            }                            
                        }
                        else if (category == "IfcSlab")
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
                        else if (category == "IfcOpeningElement")
                        {
                            var outlineConfig = GetOpeningConfig(o.Properties);
                            var hatchConfig = GetOpeningHatchConfig(o.Properties);
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

        private bool IsTenThickSlab(string content)
        {
            var values = content.GetDoubles();
            if(values.Count==1)
            {
                return Math.Abs(values[0] - 10.0) <= 1e-4;
            }
            return false;
        }

        private Vector3d ToVector(string vecContent)
        {
            if(string.IsNullOrEmpty(vecContent))
            {
                return new Vector3d();
            }
            else
            {
                var values = vecContent.Split(',');
                if(values.Length==2)
                {
                    double value1 = 0.0,value2 =0.0;
                    if (double.TryParse(values[0], out value1) &&
                       double.TryParse(values[1], out value2))
                    {
                        return new Vector3d(value1, value2, 0);
                    }
                    else
                    {
                        return new Vector3d();
                    }
                }
                else
                {
                    return new Vector3d();
                }
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
