using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.ArchitecturePlane.Service;
using ThMEPStructure.Common;

namespace ThMEPStructure.ArchitecturePlane.Print
{
    /// <summary>
    /// 立面图
    /// </summary>
    internal class ThArchElevationDrawingPrinter : ThArchDrawingPrinter
    {
        public ThArchElevationDrawingPrinter(ThArchSvgInput input, ThPlanePrintParameter printParameter)
            : base(input, printParameter)
        {
        }
        public override void Print(Database database)
        {
            // 从模板导入要打印的图层
            if (!ThImportDatabaseService.ImportArchDwgTemplate(database))
            {
                return;
            }

            // 打印对象
            PrintGeos(database, Geos);

            // 打印标题,结果存于ObjIds中
            PrintHeadText(database);
        }

        private void PrintGeos(Database db, List<ThGeometry> geos)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var doorComponents = new List<ThComponentInfo>();
                var windowComponents = new List<ThComponentInfo>();

                // 打印到图纸中
                geos.ForEach(o =>
                {
                    // 立面图不分图层
                    AppendToObjIds(PrintElev3(db, o.Boundary as Curve));
                    string category = o.Properties.GetCategory(); //type
                    if (category == ThIfcCategoryManager.WindowCategory)
                    {
                        windowComponents.Add(ParseComponentInfo(o.Properties));
                    }
                    else if(category == ThIfcCategoryManager.DoorCategory)
                    {
                        doorComponents.Add(ParseComponentInfo(o.Properties));
                    }
                });

                // 创建门文字编号标注
                AppendToObjIds(PrintDoorMarks(db, doorComponents));

                // 创建窗户编号标注
                AppendToObjIds(PrintWindowMarks(db, windowComponents));
            }
        }

        private ObjectIdCollection PrintDoorMarks(Database db, List<ThComponentInfo> doors)
        {
            var results = new ObjectIdCollection();
            // 创建门标注
            var creator = new ThDoorNumberCreator();
            var numbers = creator.CreateElevationMarks(doors);

            // 打印，为了设置好文字高度和样式
            var config = ThDoorMarkPrinter.GetConfig();
            var printer = new ThDoorMarkPrinter(config);
            numbers.ForEach(o =>
            {
                results.AddRange(printer.Print(db, o.Mark));
            });

            return results;
        }
        private ObjectIdCollection PrintWindowMarks(Database db, List<ThComponentInfo> windows)
        {
            var results = new ObjectIdCollection();
            // 创建门标注
            var creator = new ThWindowNumberCreator();
            var numbers = creator.CreateElevationMarks(windows);

            // 打印，为了设置好文字高度和样式
            var config = ThWindowMarkPrinter.GetConfig();
            var printer = new ThWindowMarkPrinter(config);
            numbers.ForEach(o =>
            {
                results.AddRange(printer.Print(db, o.Mark));
            });
            return results;
        }

        private ObjectIdCollection PrintElev3(Database db, Curve curve)
        {
            var config = ThElevationElementPrinter.GetConfig();
            var printer = new ThElevationElementPrinter(config);
            return printer.Print(db, curve);
        }

        private ThComponentInfo ParseComponentInfo(Dictionary<string,object> properties)
        {
            var tempProperties = new Dictionary<string,string>();
            foreach(var item in properties)
            {
                if(item.Value!=null && item.Value.GetType()== typeof(string))
                {
                    tempProperties.Add(item.Key, item.Value.ToString());
                }
            }
            return tempProperties.ParseComponentInfo();
        }
        private ObjectIdCollection PrintHeadText(Database database)
        {
            var flrRange = FlrBottomEle.GetFloorRange(FloorInfos);
            if (!string.IsNullOrEmpty(flrRange))
            {
                flrRange += " 层建筑立面图";
                return PrintHeadText(database, flrRange);
            }          
            else
            {
                return new ObjectIdCollection();
            }
        }
    }
}
