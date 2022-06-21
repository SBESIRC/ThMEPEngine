﻿using System.IO;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.ArchiecturePlane.Print
{
    internal class ThImportDatabaseService
    {
        public static bool ImportArchDwgTemplate(Database database)
        {
            var res1 = ImportDoorWindowTemplate(database);
            var res2 = ImportPlaneTemplate(database);
            return res1;
        }
        private static bool ImportDoorWindowTemplate(Database database)
        {
            var doorWindowTemplateDwgPath = ThCADCommon.ArchitectureDoorWindowTemplatePath();
            var fileInfo = new FileInfo(doorWindowTemplateDwgPath);
            if(!fileInfo.Exists)
            {
                return false;
            }
            using (var acadDb = AcadDatabase.Use(database))
            using (var blockDb = AcadDatabase.Open(doorWindowTemplateDwgPath, DwgOpenMode.ReadOnly, false))
            {
                var dwgName = Path.GetFileNameWithoutExtension(doorWindowTemplateDwgPath);
                // 导入图层
                ThArchPrintLayerManager.Instance.DwgLayerInfos.ForEach(o =>
                {
                    if (dwgName == o.Key)
                    {
                        o.Value.ForEach(layer =>
                        {
                            acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(layer), true);
                        });
                    }
                });

                // 导入样式
                ThArchPrintStyleManager.Instance.DwgStyleInfos.ForEach(o =>
                {
                    if (dwgName == o.Key)
                    {
                        o.Value.ForEach(style =>
                        {
                            acadDb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(style), false);
                        });
                    }
                });

                // 导入块
                ThArchPrintBlockManager.Instance.DwgBlockInfos.ForEach(o =>
                {
                    if (dwgName == o.Key)
                    {
                        o.Value.ForEach(block =>
                        {
                            acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(block), true);
                        });
                    }
                });

                return true;
            }
        }
        private static bool ImportPlaneTemplate(Database database)
        {
            var planeTemplateDwgPath = ThCADCommon.ArchitectureTemplatePath();
            var fileInfo = new FileInfo(planeTemplateDwgPath);
            if (!fileInfo.Exists)
            {
                return false;
            }
            using (var acadDb = AcadDatabase.Use(database))
            using (var blockDb = AcadDatabase.Open(planeTemplateDwgPath, DwgOpenMode.ReadOnly, false))
            {
                var dwgName = Path.GetFileNameWithoutExtension(planeTemplateDwgPath);
                // 导入图层
                ThArchPrintLayerManager.Instance.DwgLayerInfos.ForEach(o =>
                {
                    if (dwgName == o.Key)
                    {
                        o.Value.ForEach(layer =>
                        {
                            acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(layer), true);
                        });
                    }
                });

                // 导入样式
                ThArchPrintStyleManager.Instance.DwgStyleInfos.ForEach(o =>
                {
                    if (dwgName == o.Key)
                    {
                        o.Value.ForEach(style =>
                        {
                            acadDb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(style), false);
                        });
                    }
                });

                // 导入块
                ThArchPrintBlockManager.Instance.DwgBlockInfos.ForEach(o =>
                {
                    if (dwgName == o.Key)
                    {
                        o.Value.ForEach(block =>
                        {
                            acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(block), true);
                        });
                    }
                });

                // 导入线型
                acadDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(ThArchPrintLineTypeManager.Hidden,false));
                
                return true;
            }
        }
    }
}
