using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPStructure.GirderConnect.Data;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
namespace ThMEPStructure.GirderConnect.Service
{
    public static class ImportService
    {
        public static void ImportMainBeamInfo()
        {
            using (var doclock = AcApp.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(BeamConfig.MainBeamLayerName);
                acadDatabase.Database.ImportLayer(BeamConfig.MainBeamTextLayerName);
            }
        }

        public static void ImportSecondaryBeamInfo()
        {
            using (var doclock = AcApp.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(BeamConfig.SecondaryBeamLayerName);
                acadDatabase.Database.ImportLayer(BeamConfig.SecondaryBeamTextLayerName);
                acadDatabase.Database.ImportLayer(BeamConfig.ErrorLayerName);
            }
        }

        public static void ImportTextStyle()
        {
            using (var doclock = AcApp.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportTextStyle(BeamConfig.BeamTextStyleName);
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
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.StructTemplatePath(), DwgOpenMode.ReadOnly, false))
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
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.StructTemplatePath(), DwgOpenMode.ReadOnly, false))
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
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.StructTemplatePath(), DwgOpenMode.ReadOnly, false))
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
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.StructTemplatePath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(textStyle), false);
            }
        }
        #endregion
    }
}
