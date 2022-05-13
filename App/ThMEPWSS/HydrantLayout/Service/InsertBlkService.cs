using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;

using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using Dreambuild.AutoCAD;

using ThCADExtension;

using ThMEPWSS.HydrantLayout.Engine;

namespace ThMEPWSS.HydrantLayout.Service
{
    internal class InsertBlkService
    {
        public static void LoadBlockLayerToDocument(Database database, List<string> blockNames, List<string> layerNames)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            {
                //解锁0图层，后面块有用0图层的
                DbHelper.EnsureLayerOn("0");
            }
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
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
        
        public static void InsertWarning(List<OutPutModel> insertBlkInfo, string layerName, double radius, int colorIndex)
        {
            if (insertBlkInfo == null || insertBlkInfo.Count == 0)
                return;

            using (var db = AcadDatabase.Active())
            {
                var color = Color.FromColorIndex(ColorMethod.ByColor, (short)colorIndex);

                ThMEPEngineCore.Diagnostics.DrawUtils.CreateLayer(layerName, color, ReplaceLayerSetting: true);

                foreach (var insertInfo in insertBlkInfo)
                {
                    if (insertInfo.OriginModel != null)
                    {
                        var pt = insertInfo.OriginModel.Center;
                        var c = new Circle(pt, Vector3d.ZAxis, radius);
                        c.Layer = layerName;
                        db.ModelSpace.Add(c);
                    }
                }
            }
        }

        public static void InsertTooFar(List<OutPutModel> insertBlkInfo, string layerName, double radius, int colorIndex)
        {
            if (insertBlkInfo == null || insertBlkInfo.Count == 0)
                return;

            using (var db = AcadDatabase.Active())
            {
                var color = Color.FromColorIndex(ColorMethod.ByColor, (short)colorIndex);

                ThMEPEngineCore.Diagnostics.DrawUtils.CreateLayer(layerName, color, ReplaceLayerSetting: true);

                foreach (var insertInfo in insertBlkInfo)
                {

                    var pt = insertInfo.CenterPoint;
                    var c = new Circle(pt, Vector3d.ZAxis, radius);
                    c.Layer = layerName;
                    db.ModelSpace.Add(c);

                }
            }
        }


        public static void InsertBlock(List<OutPutModel> insertBlkInfo, double scale)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var ptInfo in insertBlkInfo)
                {
                    try
                    {
                        if (ptInfo.IfFind == false)
                        {
                            continue;
                        }
                        var layerName = "";
                        var blkName = "";
                        double rotateAngle = 0;
                        Point3d inputPt = new Point3d();
                        var attNameValuesOutput = new Dictionary<string, string>() { };

                        // 0:立柱 1:消防栓 2：灭火器
                        if (ptInfo.Type == 0)
                        {
                            blkName = ThHydrantCommon.BlkName_Vertical150;
                            inputPt = ptInfo.CenterPoint;
                            rotateAngle = Vector3d.YAxis.GetAngleTo(ptInfo.Dir, Vector3d.ZAxis);
                            layerName = ThHydrantCommon.Layer_Vertical;
                        }
                        else if (ptInfo.Type == 1)
                        {
                            var blk = ptInfo.OriginModel.Data as BlockReference;
                            blkName = ThHydrantCommon.BlkName_Hydrant;
                            var cpt = ptInfo.CenterPoint;
                            inputPt = FindInsertPt(ptInfo);
                            rotateAngle = Vector3d.YAxis.GetAngleTo(ptInfo.Dir, Vector3d.ZAxis);
                            layerName = blk.Layer;

                        }
                        else if (ptInfo.Type == 2)
                        {
                            var blk = ptInfo.OriginModel.Data as BlockReference;
                            blkName = ThHydrantCommon.BlkName_Hydrant_Extinguisher;
                            inputPt = FindInsertPt(ptInfo);
                            rotateAngle = Vector3d.YAxis.GetAngleTo(ptInfo.Dir, Vector3d.ZAxis);
                            layerName = blk.Layer;
                        }

                        var id = db.ModelSpace.ObjectId.InsertBlockReference(
                                                  layerName,
                                                  blkName,
                                                  inputPt,
                                                  new Scale3d(scale),
                                                  rotateAngle,
                                                  attNameValuesOutput);

                        //翻转
                        if (ptInfo.Type == 1)
                        {
                            var blk = ptInfo.OriginModel.Data as BlockReference;
                            var visibleAtt = blk.Id.GetDynProperties();
                            var propValue = (ptInfo.DoorOpenDir % 2) == 0 ? (short)1 : (short)0; //0左  1右
                            foreach (DynamicBlockReferenceProperty prop in visibleAtt)
                            {
                                if (prop.PropertyName.Contains(ThHydrantCommon.BlkVisibility_Turn))
                                {
                                    //获取动态属性值并结束遍历
                                    id.SetDynBlockValue(prop.PropertyName, propValue);
                                }
                            }

                        }

                        //可见性
                        if (ptInfo.Type == 0)
                        {
                            id.SetDynBlockValue(ThHydrantCommon.BlkVisibility_Att_1, ThHydrantCommon.BlkVisibility_Att_1_Value);
                            id.SetDynBlockValue(ThHydrantCommon.BlkVisibility_Angle, 0.0);
                        }
                        if (ptInfo.Type == 1 || ptInfo.Type == 2)
                        {
                            var blk = ptInfo.OriginModel.Data as BlockReference;
                            var visible = blk.Id.GetDynBlockValue(ThHydrantCommon.BlkVisibility_Att);
                            id.SetDynBlockValue(ThHydrantCommon.BlkVisibility_Att, visible);
                        }
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.StackTrace;
                    }
                }
            }
        }


        private static Point3d FindInsertPt(OutPutModel output)
        {
            var cpt = output.CenterPoint;
            var boundary = output.OriginModel.Outline;
            var moveDir = new Vector3d(0, 0, 0);
            //for (int i = 0; i < 4; i++) //应该是1，或者2点
            //{
            //    var pt = boundary.GetPoint3dAt(i);
            //    var vector = pt - output.OriginModel.Center;
            //    var angle = output.OriginModel.BlkDir.GetAngleTo(vector, Vector3d.ZAxis);
            //    if ((Math.PI/2) < angle && angle < (Math.PI ))
            //    {
            //        moveDir = vector;
            //        break;
            //    }
            //}

            var lShort = (boundary.GetPoint3dAt(1) - boundary.GetPoint3dAt(0)).Length;
            var lLong = (boundary.GetPoint3dAt(2) - boundary.GetPoint3dAt(1)).Length;
            if (lLong < lShort)
            {
                var lTemp = lShort;
                lShort = lLong;
                lLong = lTemp;
            }


            var returnPt = cpt + output.Dir.RotateBy(Math.PI / 2, Vector3d.ZAxis) * lLong / 2 - output.Dir * lShort / 2;
            return returnPt;

        }

        /// <summary>
        /// 无法删除块中的entity
        /// </summary>
        /// <param name="e"></param>
        public static void CleanEntity(Entity e)
        {
            var dbTrans = new DBTransaction();
            var objId = e.ObjectId;
            var obj = dbTrans.GetObject(objId, OpenMode.ForWrite, false);
            obj.UpgradeOpen();
            obj.Erase();
            obj.DowngradeOpen();
            dbTrans.Commit();
            // Data.Remove(x);
        }

    }
}
