using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;
using ThPlatform3D.ArchitecturePlane.Service;
using ThPlatform3D.Common;
using Autodesk.AutoCAD.Geometry;
using System.IO;
using ThMEPEngineCore.IO.JSON;
using ThMEPTCH.CAD;
using ThPlatform3D.Service;

namespace ThPlatform3D.ArchitecturePlane.Print
{
    /// <summary>
    /// 平面图
    /// </summary>
    internal class ThArchPlanDrawingPrinter: ThArchDrawingPrinter
    {
        public ThArchPlanDrawingPrinter(ThSvgParseInfo input, ThPlanePrintParameter printParameter) 
            :base(input, printParameter)
        {              
        }
        public override void Print(Database db)
        {
            // 前处理
            Preprocess();

            // 打印对象
            PrintGeos(db, Geos);

            // 打印轴网
            PrintGrids(db);

            //// 获取墙线          
            //var tesslateLength = ThArchitecturePlaneCommon.Instance.WallArcTessellateLength;
            //var wallLines = Geos.GetShearWalls().ToLines(tesslateLength);
            //wallLines = wallLines.FilterSmallLines(5.0);

            // 打印门垛、门窗标注 
            var doorComponents = ComponentInfos.GetDoorComponents();
            var windowComponents = ComponentInfos.GetWindowComponents();
            AppendToObjIds(PrintDoorStones(db, doorComponents));
            AppendToObjIds(PrintDoorMarks(db, doorComponents));
            AppendToObjIds(PrintWindowMarks(db, windowComponents));

            // 插入块
            AppendToObjIds(InsertDoors(db, doorComponents));
            AppendToObjIds(InsertWindows(db, windowComponents));

            // 调整窗块的比列
            UpdateWindowBlkThick(db, windowComponents);

            // 打印标题文字
            AppendToObjIds(PrintHeadText(db));

            // 释放
            //wallLines.MDispose();
        }
       
        private void Preprocess()
        {
            // 过滤门洞处隐藏的看线
            FilterDoorHiddenKanXian();

            // 
        }

        private void FilterDoorHiddenKanXian()
        {
            var hiddenKanxians = Geos.GetHiddenKanXians();
            var doorComponents = ComponentInfos.GetDoorComponents();
            var filter = new ThDoorHiddenKanxianFilter(doorComponents, hiddenKanxians);
            var removeKanxians = filter.Filter();
            Geos = Geos.Except(removeKanxians).ToList();
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
                    if (category == ThIfcCategoryManager.WallCategory ||
                        category == ThIfcCategoryManager.WallStandardCaseCategory)
                    {
                        if(isKanXian)
                        {
                            AppendToObjIds(PrintKanXian(db, o));
                        }
                        else
                        {
                            AppendToObjIds(PrintCommon(db, o.Boundary as Curve));
                        }
                    }                    
                    else if (category == ThIfcCategoryManager.SlabCategory)
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
                        if (isKanXian)
                        {
                            AppendToObjIds(PrintKanXian(db, o));
                        }
                        else
                        {
                            AppendToObjIds(PrintBeam(db, o.Boundary as Curve));
                        }
                    }
                    else if (category == ThIfcCategoryManager.RailingCategory)
                    {
                        AppendToObjIds(PrintRailing(db, o.Boundary as Curve));
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
                        AppendToObjIds(PrintHatch(db,o.Boundary, material));
                    }                    
                    else
                    {
                        if(isKanXian)
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

        private ObjectIdCollection PrintDoorStones(Database db,List<ThComponentInfo> doors)
        {
            var results = new ObjectIdCollection();
            // 创建门标注
            var creator = new ThDoorStoneCreator();
            var stones = creator.Create(doors);

            // 打印
            var config = ThDoorStonePrinter.GetConfig();
            var printer = new ThDoorStonePrinter(config);
            stones.OfType<Polyline>().ForEach(o =>
            {
                results.AddRange(printer.Print(db, o));
            });
            return results;
        }

        private ObjectIdCollection PrintDoorMarks(Database db, List<ThComponentInfo> doors)
        {
            var results = new ObjectIdCollection();
            // 创建门标注
            var creator = new ThDoorNumberCreator();
            var numbers = creator.Create(doors);
            
            // 打印，为了设置好文字高度和样式
            var config = ThDoorMarkPrinter.GetConfig(PrintParameter.DrawingScale);
            var printer = new ThDoorMarkPrinter(config);
            numbers.ForEach(o =>
            {
                results.AddRange(printer.Print(db,o.Mark));
            });

            return results;
        }

        private ObjectIdCollection PrintWindowMarks(Database db, List<ThComponentInfo> windows)
        {
            var results = new ObjectIdCollection();
            // 创建门标注
            var creator = new ThWindowNumberCreator();
            var numbers = creator.Create(windows);

            // 打印，为了设置好文字高度和样式
            var config = ThWindowMarkPrinter.GetConfig(PrintParameter.DrawingScale);
            var printer = new ThWindowMarkPrinter(config);
            numbers.ForEach(o =>
            {
                results.AddRange(printer.Print(db, o.Mark));
            });
            return results;
        }

        private ObjectIdCollection PrintDuanmian(Database db, Entity entity)
        {
            var config = ThAEwallPrinter.GetAEWallConfig();
            var printer = new ThAEwallPrinter(null, config);
            return printer.Print(db, entity);
        }

        private ObjectIdCollection PrintHatch(Database db, Entity entity,string materialName)
        {
            var config = ThPlanMaterialMapConfig.GetHatchPrintConfig(materialName);
            if(config == null)
            {
                return new ObjectIdCollection();
            }
            else
            {
                return PrintHatch(db, entity, config);
            }
        }

        private ObjectIdCollection PrintRailing(Database db, Curve curve)
        {
            var config = ThRailingPrinter.GetPlanConfig();
            var printer = new ThRailingPrinter(config);
            return printer.Print(db, curve);
        }

        private ObjectIdCollection InsertDoors(Database database,List<ThComponentInfo> doors)
        {
            var results = new ObjectIdCollection();
            var tchDoors = doors.Where(o => ThTCHBlockMapConfig.GetDoorBlkName(o.BlockName) != "").ToList();
            var otherDoors = doors.Where(o=>!tchDoors.Contains(o)).ToList();
            
            var doorPrinter = new ThDoorBlkPrinter();
            results.AddRange(doorPrinter.Print(database, otherDoors));

            var tchDoorPrinter = new ThTchDoorBlkPrinter();
            results.AddRange(tchDoorPrinter.Print(database, tchDoors));

            return results;
        }

        private ObjectIdCollection InsertWindows(Database database, List<ThComponentInfo> windows)
        {
            var windowPrinter = new ThWindowBlkPrinter();
            return windowPrinter.Print(database, windows);
        }

        private void UpdateWindowBlkThick(Database database, List<ThComponentInfo> windows)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                var validWindows = windows.Where(o => o.Element != null && o.Element.ObjectId.IsValid).ToList();
                validWindows.ForEach(o => acadDb.Element<BlockReference>(o.Element.ObjectId, true));
                var handler = new ThAdjustWindowBlkYScaleService();
                handler.Adjust(validWindows);
            }  
        }
        private ObjectIdCollection PrintHeadText(Database database)
        {
            var flrRange = FlrBottomEle.GetFloorRange(FloorInfos);
            if(!string.IsNullOrEmpty(flrRange))
            {
                flrRange += " 层建筑平面图";
                return PrintHeadText(database, flrRange);
            }
            else
            {
                return new ObjectIdCollection();
            }
        }

        private ObjectIdCollection PrintGrids(Database db)
        {
            using (var acadDb =  AcadDatabase.Use(db))
            {
                var gridIds = new ObjectIdCollection();
                if (!System.IO.File.Exists(PrintParameter.GridDataFile))
                {
                    return gridIds;
                }
                var gridSystemData = Deserialize(PrintParameter.GridDataFile);
                var builder = new ThGridSystemBuilder(gridSystemData);
                builder.Build();

                var gridLineConfig = ThGridPrinter.GridLineConfig;
                builder.GridLines.OfType<Curve>().ForEach(c =>
                {
                    var objIds = ThGridPrinter.Print(acadDb, c, gridLineConfig);
                    gridIds.AddRange(objIds);
                });

                //var dimensionStyleId = DbHelper.GetDimstyleId(ThArchPrintDimStyleManager.TCHARCH);
                var dimensionConfig = ThGridPrinter.DimensionConfig;
                builder.DimensionGroups.ForEach(o =>
                {
                    o.OfType<AlignedDimension>().ForEach(a =>
                    {
                        var objIds = ThGridPrinter.Print(acadDb,a,dimensionConfig);
                        gridIds.AddRange(objIds);
                    });
                });

                //
                builder.CircleLabelGroups.ForEach(o =>
                {
                    o.ForEach(a =>
                    {
                        a.OfType<Entity>().ForEach(e =>
                        {
                            if(e is Line || e is Polyline)
                            {
                                var objIds = ThGridPrinter.Print(acadDb, e as Curve, ThGridPrinter.CircleLabelLeaderConfig);
                                gridIds.AddRange(objIds);
                            }
                            else if(e is Circle circle)
                            {
                                var objIds = ThGridPrinter.Print(acadDb, circle, ThGridPrinter.CircleLabelCircleConfig);
                                gridIds.AddRange(objIds);
                            }
                            else if(e is DBText text)
                            {
                                var objIds = ThGridPrinter.Print(acadDb, text, ThGridPrinter.CircleLabelTextConfig);
                                gridIds.AddRange(objIds);
                            }
                        });                        
                    });
                });

                return gridIds;
            }
        }

        private ThGridLineSyetemData Deserialize(string gridFile)
        {
            var gridData = new ThGridLineSyetemData();
            try
            {
                var jsonString = File.ReadAllText(gridFile);
                gridData = JsonHelper.DeserializeJsonToObject<ThGridLineSyetemData>(jsonString);
            }
            catch
            {
                //
            }
            return gridData;
        }
    }
}
