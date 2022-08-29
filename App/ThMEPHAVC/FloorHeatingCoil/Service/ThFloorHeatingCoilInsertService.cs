using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using ThCADExtension;


namespace ThMEPHVAC.FloorHeatingCoil.Service
{
    public static class ThFloorHeatingCoilInsertService
    {
        public static void InsertSuggestBlock(Point3d insertPt, int route, double suggestDist, double length, string insertBlkName, bool withColor = false)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var attNameValues = BuildRootSuggestAttr(route, suggestDist, length);

                var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
                var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
                var blkName = insertBlkName;
                var pt = insertPt;
                double rotateAngle = angle;//TransformBy(Active.Editor.UCS2WCS());
                var scale = 1;
                var layerName = ThFloorHeatingCommon.BlkLayerDict[insertBlkName];
                var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                          layerName,
                                          blkName,
                                          pt,
                                          new Scale3d(scale),
                                          rotateAngle,
                                          attNameValues);

                if (withColor == true)
                {
                    var blk = acadDatabase.Element<BlockReference>(objId);
                    blk.UpgradeOpen();//切换属性对象为写的状态
                    blk.ColorIndex = route % 6;
                    blk.DowngradeOpen();//为了安全，将属性对象的状态改为读
                }
            }
        }

        public static void UpdateSuggestBlock(BlockReference blk, int route, double suggestDist, double length, bool withColor = false)
        {
            var attNameValues = BuildRootSuggestAttr(route, suggestDist, length);
            blk.ObjectId.UpdateAttributesInBlock(attNameValues);
            if (withColor == true)
            {
                blk.UpgradeOpen();//切换属性对象为写的状态
                blk.ColorIndex = route % 6;
                blk.DowngradeOpen();//为了安全，将属性对象的状态改为读
            }
        }

        public static void InsertCoil(List<Polyline> pipes, bool withColor = false)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                if (pipes != null)
                {
                    for (int i = 0; i < pipes.Count; i++)
                    {
                        var poly = pipes[i];
                        poly.Linetype = "ByLayer";
                        poly.Layer = ThFloorHeatingCommon.Layer_Coil;
                        if (withColor == true)
                        {
                            poly.ColorIndex = i % 6;
                        }
                        acadDatabase.ModelSpace.Add(poly);
                    }
                }
            }
        }

        private static Dictionary<string, string> BuildRootSuggestAttr(int route, double suggestDist, double lenth)
        {
            var attNameValues = new Dictionary<string, string>();
            var routeS = route == -1 ? "*" : route.ToString();
            var sdS = suggestDist == -1 ? "*" : suggestDist.ToString();
            var lengthS = lenth == -1 ? "*" : lenth.ToString();

            attNameValues.Add(ThFloorHeatingCommon.BlkSettingAttrName_RoomSuggest_Route, string.Format("HL{0}", routeS));
            attNameValues.Add(ThFloorHeatingCommon.BlkSettingAttrName_RoomSuggest_Dist, string.Format("A={0}", sdS));
            attNameValues.Add(ThFloorHeatingCommon.BlkSettingAttrName_RoomSuggest_Length, string.Format("L≈{0}m", lengthS));

            return attNameValues;
        }

        public static void ShowConnectivity(List<Polyline> roomgraph, int colorIndex)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var poly in roomgraph)
                {
                    poly.Linetype = "ByLayer";
                    poly.Layer = ThFloorHeatingCommon.Layer_RoomSetFrame;
                    poly.ColorIndex = colorIndex;
                    var objid = acadDatabase.ModelSpace.Add(poly);


                    var ids = new ObjectIdCollection();
                    ids.Add(objid);
                    var hatch = new Hatch();
                    hatch.PatternScale = 1;
                    hatch.ColorIndex = colorIndex;
                    hatch.CreateHatch(HatchPatternType.PreDefined, "SOLID", true);
                    hatch.AppendLoop(HatchLoopTypes.Outermost, ids);
                    hatch.EvaluateHatch(true);

                }

            }
        }

        public static void InsertBlk(Point3d insertPt, string insertBlkName, Dictionary<string, object> dynValue)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
                var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
                var blkName = insertBlkName;
                var pt = insertPt;
                double rotateAngle = angle;//TransformBy(Active.Editor.UCS2WCS());
                var scale = 1;
                var layerName = ThFloorHeatingCommon.BlkLayerDict[insertBlkName];
                var attNameValues = new Dictionary<string, string>();
                var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                          layerName,
                                          blkName,
                                          pt,
                                          new Scale3d(scale),
                                          rotateAngle,
                                          attNameValues);

                foreach (var dyn in dynValue)
                {
                    objId.SetDynBlockValue(dyn.Key, dyn.Value);
                }
            }
        }


        public static void LoadBlockLayerToDocument(Database database, List<string> blockNames, List<string> layerNames)
        {
            //插入模版图块时调用了WblockCloneObjects方法。需要之后做QueueForGraphicsFlush更新transaction。并且最后commit此transaction
            //参考
            //https://adndevblog.typepad.com/autocad/2015/01/using-wblockcloneobjects-copied-modelspace-entities-disappear-in-the-current-drawing.html

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                LoadBlockLayerToDocumentWithoutTrans(database, blockNames, layerNames);
                transaction.TransactionManager.QueueForGraphicsFlush();
                transaction.Commit();
            }
        }

        private static void LoadBlockLayerToDocumentWithoutTrans(Database database, List<string> blockNames, List<string> layerNames)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            {
                //解锁0图层，后面块有用0图层的
                DbHelper.EnsureLayerOn("0");
                DbHelper.EnsureLayerOn("DEFPOINTS");
            }
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                foreach (var item in blockNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var block = blockDb.Blocks.ElementOrDefault(item);
                    if (null == block)
                        continue;
                    currentDb.Blocks.Import(block, true);
                }
                foreach (var item in layerNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var layer = blockDb.Layers.ElementOrDefault(item);
                    if (null == layer)
                        continue;
                    currentDb.Layers.Import(layer, true);
                }
            }
        }

    }
}
