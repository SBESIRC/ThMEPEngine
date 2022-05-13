using System.Collections.Generic;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.ArchiecturePlane.Print;

namespace ThMEPStructure.ArchiecturePlane.Service
{
    internal class ThArchSvgEntityPrintService
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
        public List<ThFloorInfo> FloorInfos { get; set; }
        /// <summary>
        /// 收集所有当前图纸打印的物体
        /// </summary>
        public ObjectIdCollection ObjIds { get; private set; }
        private List<ThGeometry> Geos { get; set; } = new List<ThGeometry>();
        private Dictionary<string, string> DocProperties {get;set;} = new Dictionary<string, string>(); 
        public ThArchSvgEntityPrintService(
            List<ThGeometry> geos,
            List<ThFloorInfo> floorInfos,
            Dictionary<string,string> docProperties)
        {
            Geos = geos;
            FloorInfos = floorInfos;
            DocProperties = docProperties;
            ObjIds = new ObjectIdCollection();          
        }
        public void Print(Database db)
        {
            // 从模板导入要打印的图层
            //Import(db);

            // 打印对象
            PrintGeos(db, Geos); 
        }


        private void PrintGeos(Database db, List<ThGeometry> geos)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                // 打印到图纸中
                geos.ForEach(o =>
                {                    
                    // Svg解析的属性信息存在于Properties中
                    string category = o.Properties.GetCategory();
                    if(o.Boundary is DBText dbText)
                    {
                       //ToDO
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(category))
                        {
                            var config = ThCommonPrinter.GetCommonConfig();
                            var printer = new ThCommonPrinter(config);
                            var res = printer.Print(db, o.Boundary as Curve);
                            Append(res);
                        }                        
                    }
                });
            }   
        }          
       
        private void Import(Database database)
        {
            using (var acadDb = AcadDatabase.Use(database))
            using (var blockDb = AcadDatabase.Open(ThCADCommon.StructPlanePath(), DwgOpenMode.ReadOnly, false))
            {
                // 导入图层
                ThArchPrintLayerManager.AllLayers.ForEach(layer =>
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(layer), true);
                });

                // 导入样式
                ThArchPrintStyleManager.AllTextStyles.ForEach(style =>
                {
                    acadDb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(style), false);
                });

                // 导入块
                ThArchPrintBlockManager.AllBlockNames.ForEach(b =>
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(b), true);
                });
            }
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
