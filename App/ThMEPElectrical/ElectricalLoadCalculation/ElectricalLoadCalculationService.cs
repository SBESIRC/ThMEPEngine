using AcHelper;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThMEPElectrical.ElectricalLoadCalculation
{
    public static class ElectricalLoadCalculationService
    {
        public static void initialization()
        {
            using (var doclock = AcApp.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(ElectricalLoadCalculationConfig.RoomFunctionLayer);
                acadDatabase.Database.ImportBlock(ElectricalLoadCalculationConfig.RoomFunctionBlockName);
                acadDatabase.Database.ImportTextStyle(ElectricalLoadCalculationConfig.DefaultTableTextStyle);

                acadDatabase.Database.ImportElectricalLayer(ElectricalLoadCalculationConfig.LoadCalculationTableLayer);
                acadDatabase.Database.ImportTableStyles(ElectricalLoadCalculationConfig.LoadCalculationTableName);
                try
                {
                    //acadDatabase.Database.CreateAILayer("0", 255);

                    acadDatabase.Database.CreateAILayer("0", 255);
                    acadDatabase.Database.CreateAILayer(ElectricalLoadCalculationConfig.RoomFunctionLayer, (short)ColorIndex.BYLAYER);
                    acadDatabase.Database.CreateAILayer(ElectricalLoadCalculationConfig.LoadCalculationTableLayer, (short)ColorIndex.BYLAYER);
                }
                catch { }
            }
        }

        public static void InsertTable(List<Table> tables)
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                var textStyle = acad.TextStyles.Element(ElectricalLoadCalculationConfig.DefaultTableTextStyle);
                foreach (var table in tables)
                {
                    table.Position = table.Position.TransformBy(Active.Editor.WCS2UCS());
                    table.TableStyle = acad.TableStyles.ElementOrDefault(ElectricalLoadCalculationConfig.LoadCalculationTableName).ObjectId;
                    table.Layer = ElectricalLoadCalculationConfig.LoadCalculationTableLayer;
                    table.ColorIndex = 2;
                    table.TransformBy(Active.Editor.UCS2WCS());
                    for (int i = 0; i < table.Rows.Count(); i++)
                    {
                        table.Cells[i, 0].TextStyleId = textStyle.Id;
                        table.Cells[i, 1].TextStyleId = textStyle.Id;
                    }
                    acad.ModelSpace.Add(table);
                }
            }
        }

        public static void DeleteTable(List<Table> tables)
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                foreach (var table in tables)
                {
                    table.UpgradeOpen();
                    table.Erase();
                    table.DowngradeOpen();
                }
            }
        }

        public static void InsertRoomFunctionBlock(string roomNumber, string roomFunctionName, Point3d point)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objId = acadDatabase.Database.InsertBlock(
                    ElectricalLoadCalculationConfig.RoomFunctionLayer,
                    ElectricalLoadCalculationConfig.RoomFunctionBlockName,
                    point.TransformBy(Active.Editor.WCS2UCS()),
                    new Scale3d(),
                    0,
                    true,
                    new Dictionary<string, string>()
                    {
                        { "房间编号",roomNumber },{ "房间功能",roomFunctionName },{ "房间净高","3.00" }
                    });
                var blkref = acadDatabase.Element<BlockReference>(objId, true);
                blkref.TransformBy(Active.Editor.UCS2WCS());
            }
        }

        /// <summary>
        /// 插入图块
        /// </summary>
        /// <param name="database"></param>
        /// <param name="layer"></param>
        /// <param name="name"></param>
        /// <param name="position"></param>
        /// <param name="scale"></param>
        /// <param name="angle"></param>
        private static ObjectId InsertBlock(this Database database, string layer, string name, Point3d position, Scale3d scale, double angle, bool showAtt, Dictionary<string, string> attNameValues)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (showAtt)
                    return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(layer, name, position, scale, angle, attNameValues);
                else
                    return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(layer, name, position, scale, angle);
            }
        }

        #region Import
        /// <summary>
        /// 导入图块
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        private static void ImportBlock(this Database database, string block)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(block), false);
            }
        }

        /// <summary>
        /// 导入图层
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        private static void ImportLayer(this Database database, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);
            }
        }

        /// <summary>
        /// 导入电气图层
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        private static void ImportElectricalLayer(this Database database, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);
            }
        }

        /// <summary>
        /// 导入表格样式
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        private static void ImportTableStyles(this Database database, string tableStyles)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.TableStyles.Import(blockDb.TableStyles.ElementOrDefault(tableStyles), false);
            }
        }

        /// <summary>
        /// 导入文字样式
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        private static void ImportTextStyle(this Database database, string textStyle)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(textStyle), false);
            }
        }
        #endregion
    }
}
