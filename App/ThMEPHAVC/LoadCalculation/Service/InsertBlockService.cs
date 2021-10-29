using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPHVAC.LoadCalculation.Model;

namespace ThMEPHVAC.LoadCalculation.Service
{
    public static class InsertBlockService
    {
        public static void initialization()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(LoadCalculationParameterFromConfig.RoomFunctionLayer);
                acadDatabase.Database.ImportLayer(LoadCalculationParameterFromConfig.LoadCalculationTableLayer);
                acadDatabase.Database.ImportBlock(LoadCalculationParameterFromConfig.RoomFunctionBlockName);
                acadDatabase.Database.ImportTableStyles(LoadCalculationParameterFromConfig.LoadCalculationTableName);
            }
        }
        
        public static void InsertTable(List<Table> tables)
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                foreach (var table in tables)
                {
                    table.TableStyle = acad.TableStyles.ElementOrDefault(LoadCalculationParameterFromConfig.LoadCalculationTableName).ObjectId;
                    table.Layer = LoadCalculationParameterFromConfig.LoadCalculationTableLayer;
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

        public static void InsertRoomFunctionBlock(string roomNumber ,string roomFunctionName, Point3d point)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objId = acadDatabase.Database.InsertBlock(
                    LoadCalculationParameterFromConfig.RoomFunctionLayer,
                    LoadCalculationParameterFromConfig.RoomFunctionBlockName,
                    point,
                    new Scale3d(),
                    0,
                    true,
                    new Dictionary<string, string>()
                    {
                        { "房间编号",roomNumber },{ "房间功能",roomFunctionName },{ "房间净高","3.00" }
                    });
                var blkref = acadDatabase.Element<BlockReference>(objId, true);
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
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.LoadCalculationDwgPath(), DwgOpenMode.ReadOnly, false))
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
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.LoadCalculationDwgPath(), DwgOpenMode.ReadOnly, false))
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
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.LoadCalculationDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.TableStyles.Import(blockDb.TableStyles.ElementOrDefault(tableStyles), false);
            }
        }
        #endregion
    }
}
