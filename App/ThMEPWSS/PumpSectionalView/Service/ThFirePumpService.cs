using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NetTopologySuite.Algorithm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Windows;
using ThCADExtension;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPWSS.PumpSectionalView.Utils;


namespace ThMEPWSS.PumpSectionalView.Service.Impl
{
    /// <summary>
    /// 高位消防水箱
    /// </summary>
    public static class ThFirePumpService
    {

        //插入具有属性的块 -- 消防泵房剖面2
        public static void InsertBlockWithAttribute(Point3d insertPt, string insertBlkName, Matrix3d rotaM)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
                var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
                var blkName = insertBlkName;
                var pt = insertPt;
                double rotateAngle = 0;//TransformBy(Active.Editor.UCS2WCS());
                var scale = 1;
                var layerName = ThFirePumpCommon.Layer;

                var ess = CalFirePumpEssentialAttr();//基础属性
                var attNameValues = GetFirePumpAttr_2(ess);

                var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                          layerName,
                                          blkName,
                                          pt,
                                          new Scale3d(scale),
                                          rotateAngle,
                                          attNameValues);

                //旋转一下
                BlockReference blk = (BlockReference)objId.GetObject(OpenMode.ForRead);
                blk.UpgradeOpen();
                blk.TransformBy(rotaM);
                blk.DowngradeOpen();
            }
        }


        /// <summary>
        /// 插入动态块，先插入再修改 -- 消防泵房剖面1
        /// </summary>
        /// <param name="insertPt">插入点</param>
        /// <param name="insertBlkName">块名称</param>
        /// <param name="dynValue">自定义属性</param>
        /// <returns>旋转角</returns>
        public static Matrix3d InsertBlockWithDynamic(Point3d insertPt, string insertBlkName, Dictionary<string, object> dynValue)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //插入属性块
                var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
                var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
                var blkName = insertBlkName;
                var pt = insertPt;
                //double rotateAngle = angle;//TransformBy(Active.Editor.UCS2WCS());
                double rotateAngle = 0;
                var scale = 1;
                var layerName = ThFirePumpCommon.Layer;
                //var attNameValues = new Dictionary<string, string>();

                var ess = CalFirePumpEssentialAttr();//基础属性
                var attNameValues = GetFirePumpAttr_1(ess);

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

                //拿到旋转角，以该块为基点旋转
                BlockReference blk = (BlockReference)objId.GetObject(OpenMode.ForRead);
                var rotaM = Matrix3d.Rotation(angle, Vector3d.ZAxis, blk.Position);
                blk.UpgradeOpen();
                blk.TransformBy(rotaM);
                blk.DowngradeOpen();
                return rotaM;
                
            }

        }


        //获得消防泵房1的属性
        private static Dictionary<string, string> GetFirePumpAttr_1(Dictionary<string, string> ess)
        {
            var a1=new Dictionary<string, string>();

            a1.Add(ThFirePumpCommon.AirVentHeight, ess[ThFirePumpCommon.AirVentHeight]);
            a1.Add(ThFirePumpCommon.BuildingFinish, ess[ThFirePumpCommon.BuildingFinish]);
            a1.Add(ThFirePumpCommon.PoolTopHeight, ess[ThFirePumpCommon.PoolTopHeight]);
            a1.Add(ThFirePumpCommon.MinimumAlarmWaterLevel, ess[ThFirePumpCommon.MinimumAlarmWaterLevel]);
            a1.Add(ThFirePumpCommon.MaximumEffectiveWaterLevel, ess[ThFirePumpCommon.MaximumEffectiveWaterLevel]);
            a1.Add(ThFirePumpCommon.MaximumAlarmWaterLevel, ess[ThFirePumpCommon.MaximumAlarmWaterLevel]);
            a1.Add(ThFirePumpCommon.OverflowWaterLevel, ess[ThFirePumpCommon.OverflowWaterLevel]);

            a1.Add("水泵出水横管1", "DN" + CalOutletHorizontal().ToString());
            a1.Add("水泵出水横管2", "DN" + CalOutletHorizontal().ToString());
            a1.Add("水泵出水横管3", "DN" + CalOutletHorizontal().ToString());
            a1.Add("水泵出水横管4", "DN" + CalOutletHorizontal().ToString());

            a1.Add(ThFirePumpCommon.HighPumpFoundation, ess[ThFirePumpCommon.HighPumpFoundation]);

            a1.Add("水泵出水立管", "DN" + CalOutlet().ToString());
            a1.Add(ThFirePumpCommon.PumpSuctionPipeDiameter, "DN" + CalSuction().ToString());
            a1.Add(ThFirePumpCommon.WaterSuctionPipeDiameter, "DN" + CalSuctionTotal().ToString());

            a1.Add(ThFirePumpCommon.CrossTubeHeight, ess[ThFirePumpCommon.CrossTubeHeight]);


            return a1;

        }

        //获得消防泵房2的属性
        private static Dictionary<string, string> GetFirePumpAttr_2(Dictionary<string, string> ess)
        {
           
            var a1 = new Dictionary<string, string>();

            a1.Add("出水横管管径", "DN" + CalOutletHorizontal().ToString());

            a1.Add(ThFirePumpCommon.SnorkelHeight, ess[ThFirePumpCommon.SnorkelHeight]);
            a1.Add("横管高度1", ess[ThFirePumpCommon.CrossTubeHeight]);

            a1.Add("吸水母管管径1", "DN" + CalSuctionTotal().ToString());
            a1.Add("吸水母管管径", "DN" + CalSuctionTotal().ToString());

            a1.Add("横管高度", ess[ThFirePumpCommon.CrossTubeHeight]);
            a1.Add(ThFirePumpCommon.OverflowWaterLevel, ess[ThFirePumpCommon.OverflowWaterLevel]);
            a1.Add(ThFirePumpCommon.MaximumAlarmWaterLevel, ess[ThFirePumpCommon.MaximumAlarmWaterLevel]);
            a1.Add(ThFirePumpCommon.MaximumEffectiveWaterLevel, ess[ThFirePumpCommon.MaximumEffectiveWaterLevel]);
            a1.Add(ThFirePumpCommon.MinimumAlarmWaterLevel, ess[ThFirePumpCommon.MinimumAlarmWaterLevel]);
            a1.Add(ThFirePumpCommon.InletPipeHeight, ess[ThFirePumpCommon.InletPipeHeight]);

            return a1;

        }

        //計算消防泵房基础属性
        private static Dictionary<string, string> CalFirePumpEssentialAttr()
        {
            var attNameValues = new Dictionary<string, string>();
            attNameValues.Add(ThFirePumpCommon.BuildingFinish,  ThFirePumpCommon.Input_BuildingFinishHeight.ToString("0.00"));
            attNameValues.Add(ThFirePumpCommon.PoolTopHeight,  ThFirePumpCommon.Input_RoofHeight.ToString("0.00"));
            attNameValues.Add(ThFirePumpCommon.HighPumpFoundation, "h+" + ThFirePumpCommon.Input_BasicHeight.ToString("0.00"));
            //attNameValues.Add(ThFirePumpCommon.AirVentHeight, Convert.ToString(ThFirePumpCommon.Input_BasicHeight)+"+"+ ThFirePumpCommon.Input_PumpList[ThFirePumpCommon.PumpOutletHorizontalPipeDiameterChoice-1].Hole / 1000);
            attNameValues.Add(ThFirePumpCommon.AirVentHeight, "h+"+(ThFirePumpCommon.Input_BasicHeight+ ThFirePumpCommon.Input_PumpList[ThFirePumpCommon.choice - 1].Hole / 1000).ToString("0.00"));
            attNameValues.Add(ThFirePumpCommon.MinimumAlarmWaterLevel,"h+"+(1.35 + ThFirePumpCommon.Input_EffectiveDepth).ToString("0.00"));
            attNameValues.Add(ThFirePumpCommon.MaximumEffectiveWaterLevel, "h+"+(1.40 + ThFirePumpCommon.Input_EffectiveDepth).ToString("0.00"));
            attNameValues.Add(ThFirePumpCommon.MaximumAlarmWaterLevel, "h+" + (1.45 + ThFirePumpCommon.Input_EffectiveDepth).ToString("0.00"));
            attNameValues.Add(ThFirePumpCommon.OverflowWaterLevel, "h+" + (1.50 + ThFirePumpCommon.Input_EffectiveDepth).ToString("0.00"));
            attNameValues.Add(ThFirePumpCommon.CrossTubeHeight, "h+" + (1.70 + ThFirePumpCommon.Input_EffectiveDepth).ToString("0.00"));
            attNameValues.Add(ThFirePumpCommon.InletPipeHeight ,"h+" + (1.80 + ThFirePumpCommon.Input_EffectiveDepth).ToString("0.00"));
            attNameValues.Add(ThFirePumpCommon.SnorkelHeight, "h+" +( 2 + ThFirePumpCommon.Input_EffectiveDepth).ToString("0.00"));

            return attNameValues;
        }

        //计算吸水母管管径
        private static int CalSuctionTotal()
        {
            List<Pump_Arr> pl = ThFirePumpCommon.Input_PumpList;

            double sum = 0;
            foreach (var i in pl)
            {
                sum += i.Flow_Info * i.Num;
            }

            if (0 < sum && sum <=40)
                return 200;
            else if (40 < sum && sum <= 120)
                return 200;
            else if (120 < sum )
                return 300;

            return -1;//数据错误
        }

        //计算水泵吸水管管径
        private static int CalSuction()
        {
            List<Pump_Arr> pl = ThFirePumpCommon.Input_PumpList;
            //int choice = ThFirePumpCommon.PumpSuctionPipeDiameterChoice;
            int choice = ThFirePumpCommon.choice;

            double sum = pl[choice - 1].Flow_Info;
            if (0 < sum && sum <=20)
                return 150;
            else if (20 < sum && sum <= 40)
                return 200;
            else if (40 < sum)
                return 300;

            return -1;//数据错误
        }

        //计算水泵出水立管管径
        private static int CalOutlet()
        {
            List<Pump_Arr> pl = ThFirePumpCommon.Input_PumpList;
            //int choice = ThFirePumpCommon.PumpOutletPipeDiameterChoice;
            int choice = ThFirePumpCommon.choice;

            double sum = pl[choice - 1].Flow_Info;
            if (0 < sum && sum <= 15)
                return 100;
            else if (15 < sum && sum <= 35)
                return 150;
            else if (35 < sum )
                return 200;
          
            return -1;//数据错误
        }

        //计算水泵出水横管管径
        private static int CalOutletHorizontal()
        {
            List<Pump_Arr> pl = ThFirePumpCommon.Input_PumpList;
            //int choice = ThFirePumpCommon.PumpOutletHorizontalPipeDiameterChoice;
            int choice = ThFirePumpCommon.choice;


            double sum = pl[choice - 1].Flow_Info * pl[choice - 1].Num;
            if (0 < sum && sum <= 15)
                return 100;
            else if (15 < sum && sum <= 35)
                return 150;
            else if (35 < sum )
                return 200;

            return -1;//数据错误
        }


        /// <summary>
        /// 得到文字
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static DBText GetText(string text, Point3d pt, double height, double widthFactor,string layer)
        {
            DBText t = new DBText();
            t.Position = pt;
            t.TextString = text;
            t.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
            t.Height = height;
            t.Layer = layer;
            t.WidthFactor = widthFactor;
            return t;
        }

        /// <summary>
        /// 插入多段线
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="maxX"></param>
        /// <param name="Y"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static Polyline GetPolyline(double minX,double maxX,double Y,string layer,double width)
        {
            var pt1 = new Point3d(minX, Y, 0);
            var pt2 = new Point3d(maxX, Y, 0);
           
            Polyline frame = new Polyline { Closed = true };
            frame.Layer = layer;
            frame.AddVertexAt(0, pt1.ToPoint2D(), 0, width, width);
            frame.AddVertexAt(1, pt2.ToPoint2D(), 0, width, width);
            frame.ConstantWidth = width;

            return frame;
        }

        /// <summary>
        /// 插入多行文字
        /// </summary>
        /// <param name="database"></param>
        /// <param name="blockNames"></param>
        /// <param name="layerNames"></param>
        public static MText GetIntro(Point3d pt)
        {
            var text = new MText();
            text.Location = pt;
            text.Contents = getText();
            //text.Rotation = 0;
            text.Height = 250;
            text.TextHeight = 150;
            text.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
            text.Layer = "W-NOTE";
            text.Width = 4000;
            text.Height = 4500;
            return text;
        }

        private static string getText()
        {
            int index = 0;
            string s = "";
            if (ThFirePumpCommon.Input_FirePressure != 0&& ThFirePumpCommon.Input_WaterPressure!=0)
            {
                index++;
                s += index + ".室内（外）消火栓出水干管上设置的压力开关设定值为" + ThFirePumpCommon.Input_FirePressure + "MPa，当管道压力降低到设定值，主泵启动。" +
                    "喷淋出水干管上设置的压力开关设定值为"+ThFirePumpCommon.Input_WaterPressure+ "MPa，当管道压力降低到设定值，主泵启动。\r\n";
            }
            else if(ThFirePumpCommon.Input_FirePressure != 0 && ThFirePumpCommon.Input_WaterPressure == 0)
            {
                index++;
                s += index + ".室内（外）消火栓出水干管上设置的压力开关设定值为" + ThFirePumpCommon.Input_FirePressure + "MPa，当管道压力降低到设定值，主泵启动。\r\n";
            }
            else if (ThFirePumpCommon.Input_FirePressure == 0 && ThFirePumpCommon.Input_WaterPressure != 0)
            {
                index++;
                s += index + ".喷淋出水干管上设置的压力开关设定值为" + ThFirePumpCommon.Input_WaterPressure + "MPa，当管道压力降低到设定值，主泵启动。\r\n";
            }

            double flow_fire = -1,flow_water=-1;
            for(int i=0;i< ThFirePumpCommon.Input_PumpList.Count; i++)
            {
                if (ThFirePumpCommon.Input_PumpList[i].No.Contains("消火栓"))
                {
                    if(ThFirePumpCommon.Input_PumpList[i].Flow_Info> flow_fire)
                    {
                        flow_fire=ThFirePumpCommon.Input_PumpList[i].Flow_Info;
                    }
                }
                if (ThFirePumpCommon.Input_PumpList[i].No.Contains("喷淋"))
                {
                    if (ThFirePumpCommon.Input_PumpList[i].Flow_Info > flow_water)
                    {
                        flow_water = ThFirePumpCommon.Input_PumpList[i].Flow_Info;
                    }
                }
            }

            double range = Math.Ceiling(flow_fire * 1.75 / 0.75);
            double pre = Math.Ceiling(ThFirePumpCommon.Input_FirePressure * 1.65 / 0.75);
            index++;
            s += index+".";
            s += String.Format(
                "室内消火栓泵流量监测装置的计量精度为0.4级，最大量程为{0}L/S；压力监测装置的计量精度为0.5级，最大量程为{1}MPa。", range,pre);
            if (flow_water != -1)
            {
                range = Math.Ceiling(flow_water * 1.75 / 0.75);
                pre = Math.Ceiling(ThFirePumpCommon.Input_WaterPressure * 1.65 / 0.75);
                s += String.Format(
                "喷淋泵流量监测装置的计量精度为0.4级，最大量程为{0}L/S；压力监测装置的计量精度为0.5级，最大量程为{1}MPa。\r\n", range, pre);
            }
            else
                s += "\r\n";

            index++;
            s += index + ".本图尺寸除标高以米计, 其余均以毫米计，h为泵房完成面及水池结构面标高。\r\n";
            index++;
            s += index + ".消防水泵外壳为球墨铸铁， 叶轮为青铜或不锈钢 。\r\n";
            index++;
            s += index + ".消防水泵吸水管和出水管上应设置压力表，出水管压力表的最大量程不应低于 4.0MPa ； 吸水管压力表的最大量程为 0.70MPa ； 压力表的直径不应小于 100mm ，应采用直径不小于 6mm 的管道与消防水泵进出口管相接，并应设置关断阀门 。\r\n";
            

            return s;
        }

        public static void ModefyMaterial(List<ObjectId> blkM)
        {
            
            int i = 0;
            //水泵
            for (; i < ThFirePumpCommon.Input_PumpList.Count; i++)
            {
                string s = String.Format("Q={0}L/s，h={1}m，N={2}kW", ThFirePumpCommon.Input_PumpList[i].Flow_Info, ThFirePumpCommon.Input_PumpList[i].Head, ThFirePumpCommon.Input_PumpList[i].Power);
                string n = "";
                if (!String.IsNullOrEmpty(ThFirePumpCommon.Input_PumpList[i].Note))
                    n +="，"+ ThFirePumpCommon.Input_PumpList[i].Note;
                var value = new Dictionary<string, string>() { { "序号", (i+1).ToString() }, { "设备名称", ThFirePumpCommon.Input_PumpList[i].No },
                    { "规格型号",s},{ "单位","台"},{ "数量",ThFirePumpCommon.Input_PumpList[i].Num.ToString()},{ "放气孔高度", ThFirePumpCommon.Input_PumpList[i].Hole+"mm"},{ "备注",ThFirePumpCommon.Input_PumpList[i].NoteSelect+","+ThFirePumpCommon.Input_PumpList[i].Type+n} };
                
               
                blkM[i].UpdateAttributesInBlock(value);
            }

            //消防水池
            var v = new Dictionary<string, string>() { { "序号", (i+1).ToString() }, { "设备名称", "消防水池" },
                    { "规格型号",String.Format("钢筋混凝土，有效容积{0}m%%1403%%141，面积为{1}m%%1402%%141",ThFirePumpCommon.Input_Volume,ThFirePumpCommon.Input_PoolArea)},{ "单位","座"},{ "数量","1"},{ "放气孔高度", ""}};
            blkM[i].UpdateAttributesInBlock(v);
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
