using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NetTopologySuite.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using ThCADExtension;
using ThMEPWSS.PumpSectionalView.Utils;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;


namespace ThMEPWSS.PumpSectionalView.Service.Impl
{
    /// <summary>
    /// 高位消防水箱
    /// </summary>
    public static class ThHighFireWaterTankService
    {

        //插入具有属性的块
        /*public static void InsertBlockWithAttribute(Point3d insertPt)
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

            }
        }
        */


        //插入动态块，先插入再修改
        //插入点，块名称，自定义属性
        public static void InsertBlockWithDynamic(Point3d insertPt, string insertBlkName, Dictionary<string, object> dynValue)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //插入属性块
                var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
                var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
                var blkName = insertBlkName;
                var pt = insertPt;
                //double rotateAngle = angle;
                double rotateAngle = 0;
                var scale = 1;
                var layerName = ThHighFireWaterTankCommon.BlkToLayer[insertBlkName];
                //var attNameValues = new Dictionary<string, string>();

                var attNameValues = GetHighFireWaterTankAttr(ThHighFireWaterTankCommon.Input_Type);

                var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                          layerName,
                                          blkName,
                                          pt,
                                          new Scale3d(scale),
                                          rotateAngle,
                                          attNameValues);

                //修改
                foreach (var dyn in dynValue)
                {
                    objId.SetDynBlockValue(dyn.Key, dyn.Value);
                }

                //旋转一下
                BlockReference blk = (BlockReference)objId.GetObject(OpenMode.ForRead);
                var rotaM = Matrix3d.Rotation(angle, Vector3d.ZAxis, blk.Position);
                blk.UpgradeOpen();
                blk.TransformBy(rotaM);
                blk.DowngradeOpen();
            }

        }

        //根据type获得屋顶高位水箱的属性
        private static Dictionary<string, string> GetHighFireWaterTankAttr(string type)
        {
            double l = ThHighFireWaterTankCommon.Input_Length;
            double w = ThHighFireWaterTankCommon.Input_Width;
            double hs = ThHighFireWaterTankCommon.Input_Height;
            double v = ThHighFireWaterTankCommon.Input_Volume;
            double h0 = ThHighFireWaterTankCommon.Input_BasicHeight;

            var calValues = CalHighFireWaterTankAttr(l, w, hs, v, h0);


            if (ThHighFireWaterTankCommon.Type_WithRoofWithPump == type)
            {
                return GetWithRoofWithPumpAttr(calValues);
            }
            else if(ThHighFireWaterTankCommon.Type_WithRoofNoPump == type)
            {
                return GetWithRoofNoPumpAttr(calValues);
            }
            else if (ThHighFireWaterTankCommon.Type_NoRoofWithPump == type)
            {
                return GetNoRoofWithPumpAttr(calValues);
            }
            else if (ThHighFireWaterTankCommon.Type_NoRoofNoPump == type)
            {
                return GetNoRoofNoPumpAttr(calValues);
            }
            else if (ThHighFireWaterTankCommon.Type_WithRoof == type)
            {
                return GetWithRoofAttr(calValues);
            }
            else if (ThHighFireWaterTankCommon.Type_NoRoof == type)
            {
                return GetNoRoofAttr(calValues);
            }
            else
            {
                return new Dictionary<string, string>();
            }

        }

        //有顶有稳压泵
        private static Dictionary<string, string> GetWithRoofWithPumpAttr(Dictionary<string, string> calValues)
        {
            var attNameValues = new Dictionary<string, string>();
            

            attNameValues.Add(ThHighFireWaterTankCommon.EffectiveWaterDepth, calValues[ThHighFireWaterTankCommon.EffectiveWaterDepth]);
            attNameValues.Add(ThHighFireWaterTankCommon.BottomHeight, calValues[ThHighFireWaterTankCommon.BottomHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.BaseHeight, calValues[ThHighFireWaterTankCommon.BaseHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.MinimumAlarmWaterLevel, calValues[ThHighFireWaterTankCommon.MinimumAlarmWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.MaximumWaterLevel, calValues[ThHighFireWaterTankCommon.MaximumWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.TankTopHeight, calValues[ThHighFireWaterTankCommon.TankTopHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.WaterInletHorizontalPipe, calValues[ThHighFireWaterTankCommon.WaterInletHorizontalPipe]);
            attNameValues.Add(ThHighFireWaterTankCommon.OverflowWaterLevel, calValues[ThHighFireWaterTankCommon.OverflowWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.ClearHeight, calValues[ThHighFireWaterTankCommon.ClearHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.EffectiveVolume, Convert.ToString(ThHighFireWaterTankCommon.Input_Volume));
       
            return attNameValues;
        }

        //有顶无稳压泵
        private static Dictionary<string, string> GetWithRoofNoPumpAttr(Dictionary<string, string> calValues)
        {
            var attNameValues = new Dictionary<string, string>();
            

            attNameValues.Add(ThHighFireWaterTankCommon.EffectiveWaterDepth, calValues[ThHighFireWaterTankCommon.EffectiveWaterDepth]);
            attNameValues.Add(ThHighFireWaterTankCommon.BottomHeight, calValues[ThHighFireWaterTankCommon.BottomHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.BaseHeight, calValues[ThHighFireWaterTankCommon.BaseHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.MinimumAlarmWaterLevel, calValues[ThHighFireWaterTankCommon.MinimumAlarmWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.MaximumWaterLevel, calValues[ThHighFireWaterTankCommon.MaximumWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.TankTopHeight, calValues[ThHighFireWaterTankCommon.TankTopHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.WaterInletHorizontalPipe, calValues[ThHighFireWaterTankCommon.WaterInletHorizontalPipe]);
            attNameValues.Add(ThHighFireWaterTankCommon.OverflowWaterLevel, calValues[ThHighFireWaterTankCommon.OverflowWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.ClearHeight, calValues[ThHighFireWaterTankCommon.ClearHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.EffectiveVolume, Convert.ToString(ThHighFireWaterTankCommon.Input_Volume));

            return attNameValues;
        }

        //无顶有稳压泵
        private static Dictionary<string, string> GetNoRoofWithPumpAttr(Dictionary<string, string> calValues)
        {
            var attNameValues = new Dictionary<string, string>();
            /*double l = ThHighFireWaterTankCommon.Input_Length;
            double w = ThHighFireWaterTankCommon.Input_Width;
            double hs = ThHighFireWaterTankCommon.Input_Height;
            double v = ThHighFireWaterTankCommon.Input_Volume;
            double h0 = ThHighFireWaterTankCommon.Input_BasicHeight;

            var calValues = CalHighFireWaterTankAttr(l, w, hs, v, h0);*/

            attNameValues.Add(ThHighFireWaterTankCommon.EffectiveWaterDepth, calValues[ThHighFireWaterTankCommon.EffectiveWaterDepth]);
            attNameValues.Add(ThHighFireWaterTankCommon.BottomHeight, calValues[ThHighFireWaterTankCommon.BottomHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.BaseHeight, calValues[ThHighFireWaterTankCommon.BaseHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.MinimumAlarmWaterLevel, calValues[ThHighFireWaterTankCommon.MinimumAlarmWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.MaximumWaterLevel, calValues[ThHighFireWaterTankCommon.MaximumWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.TankTopHeight, calValues[ThHighFireWaterTankCommon.TankTopHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.WaterInletHorizontalPipe, calValues[ThHighFireWaterTankCommon.WaterInletHorizontalPipe]);
            attNameValues.Add(ThHighFireWaterTankCommon.OverflowWaterLevel, calValues[ThHighFireWaterTankCommon.OverflowWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.EffectiveVolume, Convert.ToString(ThHighFireWaterTankCommon.Input_Volume));

            return attNameValues;
        }

        //无顶无稳压泵
        private static Dictionary<string, string> GetNoRoofNoPumpAttr(Dictionary<string, string> calValues)
        {
            var attNameValues = new Dictionary<string, string>();
    

            attNameValues.Add(ThHighFireWaterTankCommon.EffectiveWaterDepth, calValues[ThHighFireWaterTankCommon.EffectiveWaterDepth]);
            attNameValues.Add(ThHighFireWaterTankCommon.BottomHeight, calValues[ThHighFireWaterTankCommon.BottomHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.BaseHeight, calValues[ThHighFireWaterTankCommon.BaseHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.MinimumAlarmWaterLevel, calValues[ThHighFireWaterTankCommon.MinimumAlarmWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.MaximumWaterLevel, calValues[ThHighFireWaterTankCommon.MaximumWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.TankTopHeight, calValues[ThHighFireWaterTankCommon.TankTopHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.WaterInletHorizontalPipe, calValues[ThHighFireWaterTankCommon.WaterInletHorizontalPipe]);
            attNameValues.Add(ThHighFireWaterTankCommon.OverflowWaterLevel, calValues[ThHighFireWaterTankCommon.OverflowWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.EffectiveVolume, Convert.ToString(ThHighFireWaterTankCommon.Input_Volume));

            return attNameValues;
        }

        //有顶
        private static Dictionary<string, string> GetWithRoofAttr(Dictionary<string, string> calValues)
        {
            var attNameValues = new Dictionary<string, string>();
     

            attNameValues.Add(ThHighFireWaterTankCommon.MaximumWaterLevel, calValues[ThHighFireWaterTankCommon.MaximumWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.MinimumAlarmWaterLevel, calValues[ThHighFireWaterTankCommon.MinimumAlarmWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.ElectricValveClosingWaterLevel, calValues[ThHighFireWaterTankCommon.ElectricValveClosingWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.BottomOfWaterInletPipe, calValues[ThHighFireWaterTankCommon.BottomOfWaterInletPipe]);
            attNameValues.Add(ThHighFireWaterTankCommon.Snorkel_1, calValues[ThHighFireWaterTankCommon.Snorkel_1]);
            attNameValues.Add(ThHighFireWaterTankCommon.Snorkel_2, calValues[ThHighFireWaterTankCommon.Snorkel_2]);
            attNameValues.Add(ThHighFireWaterTankCommon.TankTopHeight, calValues[ThHighFireWaterTankCommon.TankTopHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.ClearHeight, calValues[ThHighFireWaterTankCommon.ClearHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.BottomHeight, calValues[ThHighFireWaterTankCommon.BottomHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.BaseHeight, calValues[ThHighFireWaterTankCommon.BaseHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.TankHeight, calValues[ThHighFireWaterTankCommon.TankHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.LevelGaugeHeight, calValues[ThHighFireWaterTankCommon.LevelGaugeHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.OverflowWaterLevel, calValues[ThHighFireWaterTankCommon.OverflowWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.OverflowWaterLevel+"1", calValues[ThHighFireWaterTankCommon.OverflowWaterLevel]);

            return attNameValues;
        }

        //无顶
        private static Dictionary<string, string> GetNoRoofAttr(Dictionary<string, string> calValues)
        {
            var attNameValues = new Dictionary<string, string>();
            /*double l = ThHighFireWaterTankCommon.Input_Length;
            double w = ThHighFireWaterTankCommon.Input_Width;
            double hs = ThHighFireWaterTankCommon.Input_Height;
            double v = ThHighFireWaterTankCommon.Input_Volume;
            double h0 = ThHighFireWaterTankCommon.Input_BasicHeight;

            var calValues = CalHighFireWaterTankAttr(l, w, hs, v, h0);*/

            attNameValues.Add(ThHighFireWaterTankCommon.MaximumWaterLevel, calValues[ThHighFireWaterTankCommon.MaximumWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.MinimumAlarmWaterLevel, calValues[ThHighFireWaterTankCommon.MinimumAlarmWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.ElectricValveClosingWaterLevel, calValues[ThHighFireWaterTankCommon.ElectricValveClosingWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.BottomOfWaterInletPipe, calValues[ThHighFireWaterTankCommon.BottomOfWaterInletPipe]);
            attNameValues.Add(ThHighFireWaterTankCommon.Snorkel_1, calValues[ThHighFireWaterTankCommon.Snorkel_1]);
            attNameValues.Add(ThHighFireWaterTankCommon.Snorkel_2, calValues[ThHighFireWaterTankCommon.Snorkel_2]);
            attNameValues.Add(ThHighFireWaterTankCommon.TankTopHeight, calValues[ThHighFireWaterTankCommon.TankTopHeight]);

            attNameValues.Add(ThHighFireWaterTankCommon.BottomHeight, calValues[ThHighFireWaterTankCommon.BottomHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.BaseHeight, calValues[ThHighFireWaterTankCommon.BaseHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.TankHeight, calValues[ThHighFireWaterTankCommon.TankHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.LevelGaugeHeight, calValues[ThHighFireWaterTankCommon.LevelGaugeHeight]);
            attNameValues.Add(ThHighFireWaterTankCommon.OverflowWaterLevel, calValues[ThHighFireWaterTankCommon.OverflowWaterLevel]);
            attNameValues.Add(ThHighFireWaterTankCommon.OverflowWaterLevel + "1", calValues[ThHighFireWaterTankCommon.OverflowWaterLevel]);

            return attNameValues;
        }

        //计算高位消防水箱属性
        private static Dictionary<string, string> CalHighFireWaterTankAttr(double l,double w,double hs,double v,double h0)
        {
            var attNameValues = new Dictionary<string, string>();
            attNameValues.Add(ThHighFireWaterTankCommon.LevelGaugeHeight, ((hs-0.2)*1000).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.MinimumAlarmWaterLevel, "H+"+(hs -0.45).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.MaximumWaterLevel, "H+" + (hs - 0.35).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.ElectricValveClosingWaterLevel, "H+" + (hs - 0.30).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.OverflowWaterLevel, "H+" + (hs - 0.25).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.BottomOfWaterInletPipe, "H+" + (hs - 0.10).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.TankTopHeight, "H+" +(hs).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.WaterInletHorizontalPipe, "H+" + (hs+0.2).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.Snorkel_1, "H+" + (hs+0.3).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.Snorkel_2, "H+" + (hs + 0.6).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.BaseHeight, (h0*1000).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.BottomHeight, ((h0+0.1)*1000).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.EffectiveWaterDepth, ((hs - 0.7)*1000).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.TankHeight, (hs*1000).ToString("0.00"));
            attNameValues.Add(ThHighFireWaterTankCommon.ClearHeight, "≥" + ((hs +h0+0.9)*1000).ToString("0.00"));

            return attNameValues;
        }

        //刷新图纸
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

                    LayerTools.UnLockLayer(database, item);
                    LayerTools.UnFrozenLayer(database, item);
                    LayerTools.UnOffLayer(database, item);
                }
            }
        }
    }
}
