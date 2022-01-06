using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.SystemDiagram.Model;
using NFox.Cad;
using Dreambuild.AutoCAD;
using AcHelper;

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

        public static void InsertCircuitLayerAndLineType(string layer, string linetype)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);
                acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(linetype), false);
            }
        }

        public static void InsertDiagramLayerAndStyle()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.TextStyles.Import(blockDb.TextStyles.ElementOrDefault("TH-STYLE3"), false);
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(ThAutoFireAlarmSystemCommon.CountBlockByLayer), false);
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(ThAutoFireAlarmSystemCommon.OuterBorderBlockByLayer), false);
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThAutoFireAlarmSystemCommon.CountBlockName), false);
            }
        }

        public static void ImportCloudBlock(Database database,string BlockName)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(BlockName));
            }
        }

        /// <summary>
        /// 插入云线图块
        /// </summary>
        /// <param name="BlockName"></param>
        public static ObjectId InsertCloudBlock(Database database, string BlockName, Point3d point)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var objId = acadDatabase.Database.InsertBlock(
                    ThAutoFireAlarmSystemCommon.CountBlockByLayer,
                    BlockName,
                    point,
                    new Scale3d(),
                    0,
                    false,
                    null);
                var blkref = acadDatabase.Element<BlockReference>(objId, true);
                ObjectId revcloud = ObjectId.Null;
                void handler(object s, ObjectEventArgs e)
                {
                    if (e.DBObject is Polyline polyline)
                    {
                        revcloud = e.DBObject.ObjectId;
                    }
                }
                database.ObjectAppended +=handler;
                blkref.ExplodeToOwnerSpace();
                database.ObjectAppended -=handler;
                blkref.Erase();
                return revcloud;
            }
        }

        public static void ImportFireDistrictLayerAndStyle(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.TextStyles.Import(blockDb.TextStyles.ElementOrDefault("TH-STYLE1"), false);
                acadDatabase.TextStyles.Import(blockDb.TextStyles.ElementOrDefault("TH-STYLE3"), false);
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(ThAutoFireAlarmSystemCommon.FireDistrictByLayer), false);
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(ThAutoFireAlarmSystemCommon.WireCircuitByLayer), false);
            }

        }
        public static ObjectIdList InsertOuterBorderBlock(int RowNum, int ColNum)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                string BlockName = ThAutoFireAlarmSystemCommon.OuterBorderBlockName;
                string LayerName = ThAutoFireAlarmSystemCommon.OuterBorderBlockByLayer;
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(BlockName));
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(LayerName));

                ObjectIdList objectIds = new ObjectIdList();
                for (int j = -1; j < ColNum; j++)
                {
                    var objId = acadDatabase.Database.InsertBlock(
                        LayerName,
                        BlockName, 
                        new Point3d(3000 * j, 3000 * (RowNum - 1), 0).Add(offset), 
                        new Scale3d(1), 
                        0, 
                        false, 
                        null);
                    var blkref = acadDatabase.Element<BlockReference>(objId, true);
                    blkref.TransformBy(conversionMatrix);
                    objectIds.Add(objId);
                }
                return objectIds;
            }
        }

        /// <summary>
        /// 插入指定图块
        /// </summary>
        /// <param name="block"></param>
        /// <param name="vector">偏移量</param>
        public static ObjectIdList InsertSpecifyBlock(Dictionary<Point3d, ThBlockModel> dicBlockPoints)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                string LayerName = ThAutoFireAlarmSystemCommon.BlockByLayer;
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(LayerName), true);

                ObjectIdList objectIds = new ObjectIdList();
                foreach (var BlockInfo in dicBlockPoints)
                {
                    string BlockName = BlockInfo.Value.BlockName;
                    //特殊处理：旧块存在同属性Key值对应不同Value值情况出现，替换成张皓做的新块
                    if (BlockName == "E-BFAS731")
                    {
                        BlockName = "E-BFAS731-1";
                    }
                    
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(BlockName));
                    var objId = acadDatabase.Database.InsertBlock(
                        LayerName, 
                        BlockName, 
                        BlockInfo.Key.Add(offset), 
                        new Scale3d(100), 
                        0, 
                        BlockInfo.Value.ShowAtt, 
                        BlockInfo.Value.attNameValues);
                    var blkref = acadDatabase.Element<BlockReference>(objId, true);
                    blkref.TransformBy(conversionMatrix);
                    objectIds.Add(objId);
                }
                return objectIds;
            }
        }

        public static ObjectIdList InsertEntity(List<Entity> ents)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ObjectIdList objectIds = new ObjectIdList();
                foreach (var item in ents)
                {
                    if (item is BlockReference br)
                    {
                        objectIds.Add(br.Id);
                    }
                    else
                    {
                        item.Move(offset);
                        item.TransformBy(conversionMatrix);
                        var objId = acadDatabase.ModelSpace.Add(item);
                        objectIds.Add(objId);
                    }
                }
                return objectIds;
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
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(BlockName));
                var objId = acadDatabase.Database.InsertBlock(
                    ThAutoFireAlarmSystemCommon.CountBlockByLayer,
                    BlockName,
                    new Point3d(-3000, 0, 0).Add(offset),
                    new Scale3d(),
                    0,
                    false,
                    null);
                var blkref = acadDatabase.Element<BlockReference>(objId, true);
                blkref.TransformBy(conversionMatrix);
                blkref.ExplodeToOwnerSpace();
                blkref.Erase();
            }
        }

        /// <summary>
        /// 插入消火栓泵直接启动信号线
        /// </summary>
        /// <param name="block"></param>
        /// <param name="vector">偏移量</param>
        public static void InsertFireHydrantPump(Vector3d vector)
        {
            if (ThAutoFireAlarmSystemCommon.CanDrawFireHydrantPump)
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    string BlockName = ThAutoFireAlarmSystemCommon.FireHydrantPumpDirectStartSignalLine;
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(BlockName));
                    var objId = acadDatabase.Database.InsertBlock(
                        ThAutoFireAlarmSystemCommon.CountBlockByLayer,
                        BlockName,
                        new Point3d(3000 * 18, 0, 0).Add(offset),
                        new Scale3d(),
                        0,
                        false,
                        null);
                    var blkref = acadDatabase.Element<BlockReference>(objId, true);
                    blkref.TransformBy(conversionMatrix);
                    var objs = new DBObjectCollection();
                    blkref.Explode(objs);
                    blkref.Erase();
                    MLeader mLeader = objs[0] as MLeader;
                    mLeader.SetFirstVertex(1, mLeader.GetFirstVertex(1).Add(vector.TransformBy(conversionMatrix)));
                    acadDatabase.ModelSpace.Add(mLeader);
                    ThAutoFireAlarmSystemCommon.CanDrawFireHydrantPump = false;
                }
            }
        }

        /// <summary>
        /// 插入喷淋泵直接启动信号线
        /// </summary>
        /// <param name="block"></param>
        /// <param name="vector">偏移量</param>
        public static void InsertSprinklerPump(Vector3d vector)
        {
            if (ThAutoFireAlarmSystemCommon.CanDrawSprinklerPump)
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    string BlockName = ThAutoFireAlarmSystemCommon.SprinklerPumpDirectStartSignalLine;
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(BlockName));
                    var objId = acadDatabase.Database.InsertBlock(
                        ThAutoFireAlarmSystemCommon.CountBlockByLayer,
                        BlockName,
                        new Point3d(3000 * 19, 0, 0).Add(offset),
                        new Scale3d(),
                        0,
                        false,
                        null);
                    var blkref = acadDatabase.Element<BlockReference>(objId, true);
                    blkref.TransformBy(conversionMatrix);
                    var objs = new DBObjectCollection();
                    blkref.Explode(objs);
                    blkref.Erase();
                    MLeader mLeader = objs[0] as MLeader;
                    mLeader.SetFirstVertex(1, mLeader.GetFirstVertex(1).Add(vector.TransformBy(conversionMatrix)));
                    acadDatabase.ModelSpace.Add(mLeader);
                    ThAutoFireAlarmSystemCommon.CanDrawSprinklerPump = false;
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
            if (ThAutoFireAlarmSystemCommon.CanDrawFixedPartSmokeExhaust)
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    string BlockName = ThAutoFireAlarmSystemCommon.FixedPartSmokeExhaust;
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(BlockName));
                    var objId = acadDatabase.Database.InsertBlock(
                        ThAutoFireAlarmSystemCommon.CountBlockByLayer,
                        BlockName,
                        new Point3d(3000 * 14 + 750, -450, 0).Add(offset),
                        new Scale3d(),
                        0,
                        false,
                        null);
                    var blkref = acadDatabase.Element<BlockReference>(objId, true);
                    blkref.TransformBy(conversionMatrix);
                    var objs = new DBObjectCollection();
                    blkref.Explode(objs);
                    blkref.Erase();
                    MLeader mLeader = objs[1] as MLeader;
                    mLeader.SetFirstVertex(1, mLeader.GetFirstVertex(1).Add(vector.TransformBy(conversionMatrix)));
                    acadDatabase.ModelSpace.Add(objs[0] as MText);
                    acadDatabase.ModelSpace.Add(mLeader);
                    ThAutoFireAlarmSystemCommon.CanDrawFixedPartSmokeExhaust = false;
                }
            }
        }

        /// <summary>
        /// 插入计数图块
        /// </summary>
        /// <param name="block"></param>
        /// <param name="vector">偏移量</param>
        public static ObjectId InsertCountBlock(Point3d position, Scale3d scale, double angle, Dictionary<string, string> attNameValues)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objId = acadDatabase.Database.InsertBlock(
                    ThAutoFireAlarmSystemCommon.CountBlockByLayer,
                    ThAutoFireAlarmSystemCommon.CountBlockName,
                    position.Add(offset),
                    scale,
                    angle,
                    true,
                    attNameValues);
                var blkref = acadDatabase.Element<BlockReference>(objId, true);
                blkref.TransformBy(conversionMatrix);
                return objId;
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
    }
}
