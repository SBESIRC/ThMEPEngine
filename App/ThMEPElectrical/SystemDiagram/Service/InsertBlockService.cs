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
        public static Vector3d offset = new Vector3d();
        public static Matrix3d conversionMatrix = new Matrix3d();

        public static void SetOffset(Vector3d Toffset, Matrix3d ConversionMatrix)
        {
            offset = Toffset;
            conversionMatrix = ConversionMatrix;
        }

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
                string CountLayerName = ThAutoFireAlarmSystemCommon.CountBlockByLayer;
                acadDatabase.Database.ImportBlockLayer(CountLayerName);
            }
        }

        public static void InsertFireDistrictByLayer(AcadDatabase acadDatabase)
        {
            string LayerName = ThAutoFireAlarmSystemCommon.FireDistrictByLayer;
            acadDatabase.Database.ImportBlockLayer(LayerName);
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
                        var objId=acadDatabase.Database.InsertBlock(LayerName, ThAutoFireAlarmSystemCommon.OuterBorderBlockName, new Point3d(3000 * j, 3000 * i, 0).Add(offset), new Scale3d(1), 0, false, null);
                        var blkref = acadDatabase.Element<BlockReference>(objId, true);
                        blkref.TransformBy(conversionMatrix);
                    }
                }
            }
        }

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
                    try
                    {
                        string BlockName = BlockInfo.Value.BlockName;
                        if (!ImportBlockSet.Contains(BlockName))
                        {
                            acadDatabase.Database.ImportBlock(BlockName);
                            ImportBlockSet.Add(BlockName);
                        }
                        //消火栓泵直接启动信号线 和 喷淋泵直接启动信号线 比较特殊，无需扩大100倍
                        if (BlockName.Contains("直接启动信号线"))
                        {
                            var objId = acadDatabase.Database.InsertBlock(LayerName, BlockName, BlockInfo.Key.Add(offset), new Scale3d(1), 0, BlockInfo.Value.ShowAtt, BlockInfo.Value.attNameValues);
                            var blkref = acadDatabase.Element<BlockReference>(objId, true);
                            blkref.TransformBy(conversionMatrix);
                            blkref.ExplodeToOwnerSpace();
                            blkref.Erase();
                        }
                        else
                        {
                            var objId = acadDatabase.Database.InsertBlock(LayerName, BlockName, BlockInfo.Key.Add(offset), new Scale3d(100), 0, BlockInfo.Value.ShowAtt, BlockInfo.Value.attNameValues);
                            var blkref = acadDatabase.Element<BlockReference>(objId, true);
                            blkref.TransformBy(conversionMatrix);
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        /// <summary>
        /// 插入底部固定图块
        /// </summary>
        /// <param name="block"></param>
        /// <param name="vector">偏移量</param>
        public static void InsertSpecifyBlock(string BlockName)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string LayerName = ThAutoFireAlarmSystemCommon.CountBlockByLayer;
                acadDatabase.Database.ImportBlock(BlockName);
                try
                {
                    var objId = acadDatabase.Database.InsertBlock(LayerName, BlockName, new Point3d(-3000, 0, 0).Add(offset), new Scale3d(), 0, false, null);
                    var blkref = acadDatabase.Element<BlockReference>(objId, true);
                    blkref.TransformBy(conversionMatrix);
                    blkref.ExplodeToOwnerSpace();
                    blkref.Erase();
                }
                catch (Exception ex)
                {
                }
            }
        }

        /// <summary>
        /// 插入联动关闭排烟风机信号线
        /// </summary>
        /// <param name="block"></param>
        /// <param name="vector">偏移量</param>
        public static void InsertSmokeExhaust(Vector3d vector)
        {
            //仅画一次
            if (ThAutoFireAlarmSystemCommon.CanDrawFixedPartSmokeExhaust)
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    string LayerName = ThAutoFireAlarmSystemCommon.CountBlockByLayer;
                    string BlockName = ThAutoFireAlarmSystemCommon.FixedPartSmokeExhaust;
                    acadDatabase.Database.ImportBlock(BlockName);
                    try
                    {
                        var objId = acadDatabase.Database.InsertBlock(LayerName, BlockName, new Point3d(3000*14+750, -450, 0).Add(offset), new Scale3d(), 0, false, null);
                        var blkref = acadDatabase.Element<BlockReference>(objId, true);
                        blkref.TransformBy(conversionMatrix);
                        var objs = new DBObjectCollection();
                        blkref.Explode(objs);
                        MLeader mLeader = objs[1] as MLeader;
                        mLeader.SetFirstVertex(1, mLeader.GetFirstVertex(1).Add(vector.TransformBy(conversionMatrix)));
                        blkref.Erase();
                        ThAutoFireAlarmSystemCommon.CanDrawFixedPartSmokeExhaust = false;
                        acadDatabase.ModelSpace.Add(objs[0] as MText);
                        acadDatabase.ModelSpace.Add(mLeader);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// 插入计数图块
        /// </summary>
        /// <param name="block"></param>
        /// <param name="vector">偏移量</param>
        public static void InsertCountBlock(Point3d position, Scale3d scale, double angle, Dictionary<string, string> attNameValues)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string LayerName = ThAutoFireAlarmSystemCommon.CountBlockByLayer;
                string BlockName = ThAutoFireAlarmSystemCommon.CountBlockName;
                try
                {
                    var objId=acadDatabase.Database.InsertBlock(LayerName, BlockName, position.Add(offset), scale, angle, true, attNameValues);
                    var blkref = acadDatabase.Element<BlockReference>(objId, true);
                    blkref.TransformBy(conversionMatrix);
                }
                catch (Exception ex)
                {

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
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), true);
            }
        }
    }
}
