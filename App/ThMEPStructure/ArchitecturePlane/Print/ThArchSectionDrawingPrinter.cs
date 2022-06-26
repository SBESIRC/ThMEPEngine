using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.ArchitecturePlane.Service;
using ThMEPStructure.Common;

namespace ThMEPStructure.ArchitecturePlane.Print
{
    /// <summary>
    /// 剖面图
    /// </summary>
    internal class ThArchSectionDrawingPrinter : ThArchDrawingPrinter
    {
        public ThArchSectionDrawingPrinter(ThArchSvgInput input, ThPlanePrintParameter printParameter)
            : base(input, printParameter)
        {
        }
        public override void Print(Database db)
        {
            // 从模板导入要打印的图层
            if (!ThImportDatabaseService.ImportArchDwgTemplate(db))
            {
                return;
            }

            // 打印对象,结果存于ObjIds中
            PrintGeos(db, Geos);

            // 获取墙线          
            //var tesslateLength = ThArchitecturePlaneCommon.Instance.WallArcTessellateLength;
            //var wallLines = Geos.GetShearWalls().ToLines(tesslateLength);
            //wallLines = wallLines.FilterSmallLines(5.0);

            AppendToObjIds(PrintDoorMarks(db, ComponentInfos.Where(o => o.Type.IsDoor()).ToList()));
            AppendToObjIds(PrintWindowMarks(db, ComponentInfos.Where(o => o.Type.IsWindow()).ToList()));

            // 绘制门、窗
            AppendToObjIds(PrintDoors(db, ComponentInfos.Where(o => o.Type.IsDoor()).ToList()));
            AppendToObjIds(PrintWindows(db, ComponentInfos.Where(o => o.Type.IsWindow()).ToList()));

            // 打印标题
            AppendToObjIds(PrintHeadText(db));

            // 释放
            //wallLines.MDispose();
        }

        private void PrintGeos(Database db, List<ThGeometry> geos)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                // 打印到图纸中
                geos.ForEach(o =>
                {
                    // Svg解析的属性信息存在于Properties中
                    bool isKanXian = o.Properties.IsKanXian();
                    bool isDuanMian = o.Properties.IsDuanMian();
                    string category = o.Properties.GetCategory(); //type
                    string material = o.Properties.GetMaterial();
                    if (category == ThIfcCategoryManager.WallCategory)
                    {
                        if (isKanXian)
                        {
                            AppendToObjIds(PrintKanXian(db, o));
                        }
                        else
                        {
                            AppendToObjIds(PrintAEWall(db, o.Boundary as Curve));
                        }
                    }
                    else if (category == ThIfcCategoryManager.BeamCategory)
                    {
                        if(isKanXian)
                        {
                            AppendToObjIds(PrintKanXian(db, o));
                        }
                        else
                        {
                            AppendToObjIds(PrintBeam(db, o.Boundary as Curve));
                        }                        
                    }
                    else if(category == ThIfcCategoryManager.RailingCategory)
                    {
                        if (isKanXian)
                        {
                            AppendToObjIds(PrintKanXian(db, o));
                        }
                        else
                        {
                            AppendToObjIds(PrintRailing(db, o.Boundary as Curve));
                        }
                    }
                    else if(category == ThIfcCategoryManager.SlabCategory)
                    {
                        if(isKanXian)
                        {
                            AppendToObjIds(PrintKanXian(db, o));
                        }
                        else
                        {
                            AppendToObjIds(PrintSlab(db, o.Boundary));
                        }
                    }
                    else if (category == ThIfcCategoryManager.BuildingElementProxyCategory)
                    {
                        if (isKanXian)
                        {
                            AppendToObjIds(PrintKanXian(db, o));
                        }
                        else
                        {
                            AppendToObjIds(PrintCommon(db, o.Boundary as Curve));
                        }
                    }
                    else if(ThTextureMaterialManager.AllMaterials.Contains(material))
                    {
                        AppendToObjIds(PrintHatch(db, o.Boundary, material));
                    }                               
                    else
                    {
                        if (isKanXian)
                        {
                            AppendToObjIds(PrintKanXian(db, o));
                        }
                        else if(isDuanMian)
                        {
                            AppendToObjIds(PrintDuanmian(db, o.Boundary));
                        }
                        else
                        {
                            AppendToObjIds(PrintCommon(db, o.Boundary as Curve));
                        }
                    }
                });
            }
        }

        private ObjectIdCollection PrintDoorMarks(Database db,List<ThComponentInfo> doors)
        {
            var results = new ObjectIdCollection();
            // 创建门标注
            var creator = new ThDoorNumberCreator();
            var numbers = creator.Create(doors);

            // 打印，为了设置好文字高度和样式
            var config = ThDoorMarkPrinter.GetConfig();
            var printer = new ThDoorMarkPrinter(config);
            numbers.ForEach(o =>
            {
                results.AddRange(printer.Print(db, o.Mark));
            });
            return results;
        }

        private ObjectIdCollection PrintWindowMarks(Database db,List<ThComponentInfo> windows)
        {
            var results = new ObjectIdCollection();
            // 创建门标注
            var creator = new ThWindowNumberCreator();
            var numbers = creator.Create(windows);

            // 打印，为了设置好文字高度和样式
            var config = ThWindowMarkPrinter.GetConfig();
            var printer = new ThWindowMarkPrinter(config);
            numbers.ForEach(o =>
            {
                results.AddRange(printer.Print(db, o.Mark));
            });
            return results;
        }

        private ObjectIdCollection PrintSlab(Database db, Entity entity)
        {
            var hatchConfig = ThSlabPrinter.GetAESTRUHatchConfig();
            var outlineConfig = ThSlabPrinter.GetAESTRUOutlineConfig();
            var printer = new ThSlabPrinter(hatchConfig, outlineConfig);
            return printer.Print(db, entity);
        }

        private ObjectIdCollection PrintRailing(Database db, Curve curve)
        {
            var config = ThRailingPrinter.GetSectionConfig();
            var printer = new ThRailingPrinter(config);
            return printer.Print(db, curve);
        }

        private ObjectIdCollection PrintDoors(Database database, List<ThComponentInfo> doors)
        {
            var results = new ObjectIdCollection();
            var creator = new ThDoorOutlineCreator();
            var doorOutlines = creator.Create(doors);
            var config = ThDoorOutlinePrinter.GetConfig();
            var printer = new ThDoorOutlinePrinter(config);
            doorOutlines.OfType<Polyline>().ForEach(p =>
            {
                results.AddRange(printer.Print(database, p));
            });
            return results;
        }

        private ObjectIdCollection PrintWindows(Database database, List<ThComponentInfo> windows)
        {
            var results = new ObjectIdCollection();
            var creator = new ThWindowOutlineCreator();
            var windowOutlines = creator.Create(windows);
            var config = ThWindowOutlinePrinter.GetConfig();
            var printer = new ThWindowOutlinePrinter(config);
            windowOutlines.OfType<Polyline>().ForEach(p =>
            {
                results.AddRange(printer.Print(database, p));
            });
            return results;
        }

        private ObjectIdCollection PrintDuanmian(Database db, Entity entity)
        {
            var config = ThAEwallPrinter.GetAEWallConfig();
            var printer = new ThAEwallPrinter(null, config);
            return printer.Print(db, entity);
        }

        private ObjectIdCollection PrintHatch(Database db, Entity entity, string materialName)
        {
            var config = ThSectionMaterialMapConfig.GetHatchPrintConfig(materialName);
            if (config == null)
            {
                return new ObjectIdCollection();
            }
            else
            {
                return PrintHatch(db, entity, config);
            }
        }
        private ObjectIdCollection PrintHeadText(Database database)
        {
            var flrRange = FlrBottomEle.GetFloorRange(FloorInfos);
            if (!string.IsNullOrEmpty(flrRange))
            {
                flrRange += " 层建筑剖面图";
                return PrintHeadText(database, flrRange);
            }
            else
            {
                return new ObjectIdCollection();
            }
        }
    }
}
