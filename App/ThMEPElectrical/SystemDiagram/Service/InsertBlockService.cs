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
using ThMEPElectrical.CAD;
using ThMEPElectrical.SystemDiagram.Model;

namespace ThMEPElectrical.SystemDiagram.Service
{
    public static class InsertBlockService
    {

        public static void InsertLineType(string LayerName,string LineType)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (AcadDatabase currentDb = AcadDatabase.Use(acadDatabase.Database))
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(LayerName), false);
                    currentDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(LineType),false);
                }
            }
        }

        public static void InsertOuterBorderBlockLayer()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string LayerName = ThAutoFireAlarmSystemCommon.OuterBorderBlockByLayer;
                acadDatabase.Database.ImportBlockLayer(LayerName);
            }
        }

        public static void InsertOuterBorderBlock(int RowNum, int ColNum)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string LayerName = ThAutoFireAlarmSystemCommon.OuterBorderBlockByLayer;
                //acadDatabase.Database.ImportBlockLayer(LayerName);
                acadDatabase.Database.ImportBlock(ThAutoFireAlarmSystemCommon.OuterBorderBlockName);
                for (int i = 0; i < RowNum; i++)
                {
                    for (int j = -1; j < ColNum; j++)
                    {
                        acadDatabase.Database.InsertBlock(LayerName, ThAutoFireAlarmSystemCommon.OuterBorderBlockName, new Point3d(3000 * j, 3000 * i, 0), new Scale3d(1), 0, false, null);
                    }
                }
            }
        }

        /// <summary>
        /// 插入指定图块
        /// </summary>
        /// <param name="block"></param>
        /// <param name="vector">偏移量</param>
        //public static void InsertSpecifyBlock(ThBlockModel block, int RowIndex)
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        string LayerName = ThAutoFireAlarmSystemCommon.BlockByLayer;
        //        acadDatabase.Database.ImportBlock(block.BlockName, LayerName);
        //        acadDatabase.Database.InsertBlock(LayerName, block.BlockName, new Point3d(3000 * (block.Index - 1) + block.Position.X, 3000 * (RowIndex - 1) + block.Position.Y, 0), new Scale3d(100), 0);
        //    }
        //}

        /// <summary>
        /// 插入指定图块
        /// </summary>
        /// <param name="block"></param>
        /// <param name="vector">偏移量</param>
        public static void InsertSpecifyBlock(Dictionary<Point3d, ThBlockModel> dicBlockPoints)
        {
            List<string> ImportBlockSet = new List<string>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string LayerName = ThAutoFireAlarmSystemCommon.BlockByLayer;
                acadDatabase.Database.ImportBlockLayer(LayerName);
                foreach (var BlockInfo in dicBlockPoints)
                {
                    string BlockName = BlockInfo.Value.BlockName;
                    if (!ImportBlockSet.Contains(BlockName))
                    {
                        acadDatabase.Database.ImportBlock(BlockName);
                        ImportBlockSet.Add(BlockName);
                    }
                    acadDatabase.Database.InsertBlock(LayerName, BlockName, BlockInfo.Key, new Scale3d(100), 0, BlockInfo.Value.ShowAtt, BlockInfo.Value.attNameValues);
                }
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
        private static void InsertBlock(this Database database, string layer, string name, Point3d position, Scale3d scale, double angle, bool showAtt, Dictionary<string, string> attNameValues)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (showAtt)
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(layer, name, position, scale, angle, attNameValues);
                else
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(layer, name, position, scale, angle);
            }
        }

        /// <summary>
        /// 导入图块
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        //private static void ImportBlock(this Database database, string name, string layer)
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
        //    using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
        //    {
        //        acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
        //        acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);
        //    }
        //}

        /// <summary>
        /// 导入图层
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        private static void ImportBlockLayer(this Database database, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);
            }
        }

        /// <summary>
        /// 导入图块
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        private static void ImportBlock(this Database database, string name)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
            }
        }
    }
}
