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
using ThMEPWSS.PumpSectionalView.Utils;
using ThCADExtension;
using Org.BouncyCastle.Crypto.Macs;
using System.Web.Routing;
using Autodesk.AutoCAD.EditorInput;
using ExCSS;
using System.Security.Cryptography;

namespace ThMEPWSS.PumpSectionalView.Service.Impl
{
    /// <summary>
    /// 生活泵房
    /// </summary>
    /// 
    
    public class ThLifePumpService
    {
        AcadDatabase acadDatabase;

        public ThLifePumpService(AcadDatabase acadDatabase)
        {
            this.acadDatabase = acadDatabase;
        }
        public void CallLifePump()
        {
            //用户输入 待改 固定输入
            //string block = ThLifePumpCommon.BlkName;
            //string layer = ThLifePumpCommon.Layer;
            //var blkList = new List<string> { block };//块列表
            //var layerList = new List<string> { layer };//层
            var blkList = ThLifePumpCommon.BlkName;//块列表
            var layerList = ThLifePumpCommon.Layer;//层

            ThLifePumpService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);


            var ppo = Active.Editor.GetPoint("\n选择插入点");
            if (ppo.Status == PromptStatus.OK)
            {
                var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);//插入点位置？
                                                                                             //var suggestDict = vm.SuggestDist;

                BlockTable bt = (BlockTable)acadDatabase.Database.BlockTableId.GetObject(OpenMode.ForRead);
                var blk = new BlockReference(wcsPt, bt[ThLifePumpCommon.BlkName[0]]);
                var obj = new DBObjectCollection();
                blk.Explode(obj);//炸开

                var plList = obj.OfType<Polyline>().ToList();//所有polyline 14
                plList = getSortedPolylineByLocate(plList);//根据minX、minY排序

                var lList = obj.OfType<Line>().ToList();//所有Line 14
                var bList = obj.OfType<BlockReference>().ToList();//所有block 14            
                var tList = obj.OfType<DBText>().ToList();//所有文字 7
                var aList = obj.OfType<AlignedDimension>().ToList();//对齐标注 9
                var hList = obj.OfType<Hatch>().ToList();//混凝土 1

                List<Polyline> frame = calLifePumpList(plList, lList, bList, tList, aList);//改大小并且得到外框


                //加入图纸
                var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
                var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);//旋转角度
                var rotaM = Matrix3d.Rotation(angle, Vector3d.ZAxis, blk.Position);
                foreach (var p in plList)
                {
                    //先转到用户坐标系
                    p.TransformBy(rotaM);

                    //在插入
                    acadDatabase.ModelSpace.Add(p);
                }

                foreach (var p in lList)
                {
                    p.TransformBy(rotaM);

                    acadDatabase.ModelSpace.Add(p);
                }

                foreach (var p in bList)
                {
                    p.TransformBy(rotaM);

                    acadDatabase.ModelSpace.Add(p);
                }

                foreach (var p in tList)
                {
                    p.TransformBy(rotaM);

                    acadDatabase.ModelSpace.Add(p);
                }

                foreach (var p in aList)
                {
                    p.TransformBy(rotaM);

                    acadDatabase.ModelSpace.Add(p);
                }

                modefyData(bList, tList, aList);//修改块的数据

                setOutFrame(hList, frame);//混凝土

                //多行文字
                MTextLifePump m = new MTextLifePump(wcsPt);
                MText mw = m.WriteIntro();
                mw.TransformBy(rotaM);
                acadDatabase.ModelSpace.Add(mw);
                


                //材料表头
                var attri = new Dictionary<string, string>();
                attri.Add("", "");
                var Id=acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", ThLifePumpCommon.BlkName[1], new Point3d(wcsPt.X, wcsPt.Y - 10000, 0), new Scale3d(1), 0, attri);
                BlockReference b = (BlockReference)Id.GetObject(OpenMode.ForRead);
                b.UpgradeOpen();
                b.TransformBy(rotaM);
                b.DowngradeOpen();

                //材料表格
                List<ObjectId> blkM = new List<ObjectId>();
                for (int i = 0; i < ThLifePumpCommon.Input_PumpList.Count + 2; i++)//做出相应数量的块
                {
                    var att = new Dictionary<string, string>() { { "", "" } };
                    var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", ThLifePumpCommon.BlkName[2], new Point3d(wcsPt.X, wcsPt.Y - 10000 - 373.3 * (i + 1), 0), new Scale3d(1), 0, att);

                    BlockReference bl = (BlockReference)id.GetObject(OpenMode.ForRead);
                    bl.UpgradeOpen();
                    bl.TransformBy(rotaM);
                    bl.DowngradeOpen();

                    blkM.Add(id);
                }
                modefyMaterial(blkM);


                //表格文字
                DBText t1 = getText("住宅生活水泵房主要设备表：", new Point3d(wcsPt.X, wcsPt.Y - 10000 + 400, 0), 262.5, 0.8);
                t1.TransformBy(rotaM);
                acadDatabase.ModelSpace.Add(t1);

                DBText t2 = getText("注：水泵吸水管与吸水母管应管顶平接", new Point3d(wcsPt.X, wcsPt.Y - 10000 - 373.3 * (ThLifePumpCommon.Input_PumpList.Count + 2) - 165, 0), 120, 0.7);
                t2.TransformBy(rotaM);
                acadDatabase.ModelSpace.Add(t2);

                DBText t3 = getText("旋流防止器顶标高距水箱最低有效水位大于200mm", new Point3d(wcsPt.X, wcsPt.Y - 10000 - 373.3 * (ThLifePumpCommon.Input_PumpList.Count + 2) - 295, 0), 120, 0.7);
                t3.TransformBy(rotaM);
                acadDatabase.ModelSpace.Add(t3);
                


            }
  
        }

        /// <summary>
        /// 生活泵房所有构件转换位置
        /// </summary>
        /// <param name="po"></param>
        /// <param name="li"></param>
        /// <param name="br"></param>
        /// <param name="ta"></param>
        /// <param name="ad"></param>
        /// <returns>图纸的外框</returns>
        private List<Polyline> calLifePumpList(List<Polyline> po, List<Line> li, List<BlockReference> br, List<DBText> ta, List<AlignedDimension> ad)
        {
            //polyline
            PolyLineLifePump p = new PolyLineLifePump(po);//使用地址传递
            p.setPolylineList();

            //line
            LineLifePump l = new LineLifePump(p.getWaterLocate(), li);
            l.setLineList();

            BlockLifePump b = new BlockLifePump(br, p.getOverflowHighestLocate(), l.getCallout1Locate(), p.getLevelGaugeLocate(), p.getWaterLocate());
            b.setBlockList();

            TALifePump t = new TALifePump(ta, p.getOverflowMaxXMidY(), l.getOverflowDiaLocate());
            t.setTAList();

            ADLifePump a = new ADLifePump(ad, l.getCallout1Locate(), l.getFiveEightTwo(), l.getFiveEightThree(), l.getOverflowDiaLocate(), b.getArchTickLocate(), p.getWaterLocate());
            a.setADList();



            return p.getOutFrame();

        }

        /// <summary>
        /// 钢筋混凝土 hatch
        /// </summary>
        /// <param name="h"></param>
        /// <param name="frame"></param>
        private void setOutFrame(List<Hatch> h, List<Polyline> frame)
        {

            foreach (var poly in frame)
            {

                var objid = poly.ObjectId;

                var ids = new ObjectIdCollection();
                ids.Add(objid);
                var hatch = new Hatch();
                hatch.PatternScale = h[0].PatternScale;
                hatch.ColorIndex = h[0].ColorIndex;
                hatch.Layer = h[0].Layer;
                hatch.CreateHatch(HatchPatternType.PreDefined, h[0].PatternName, true);//已经加入数据库
                hatch.AppendLoop(HatchLoopTypes.Outermost, ids);
                //hatch.Transparency = new Autodesk.AutoCAD.Colors.Transparency((byte)51); //80%
                hatch.EvaluateHatch(true);

                //poly.Erase();
            }

        }

        /// <summary>
        /// 根据minX、minY对polyline进行排序
        /// </summary>
        /// <param name="p"></param>
        /// <returns>List<PolylineSort></returns>
        private List<Polyline> getSortedPolylineByLocate(List<Polyline> p)
        {
            List<PolylineSort> ps = new List<PolylineSort>();
            for (int i = 0; i < p.Count; i++)
            {
                PolylineSort sort = new PolylineSort();
                sort.index = i;
                sort.minX = PolyLineLifePump.getPolylineLocate(p[i], 0);
                sort.minY = PolyLineLifePump.getPolylineLocate(p[i], 2);
                ps.Add(sort);
            }
            ps = ps.OrderBy(i => i.minX).ThenBy(i => i.minY).ToList();

            List<Polyline> pn = new List<Polyline>();
            for (int i = 0; i < p.Count; i++)
            {
                int index = ps[i].index;
                pn.Add(p[index]);
            }
            return pn;
        }
        private class PolylineSort
        {
            public int index { get; set; }
            public double minX { get; set; }
            public double minY { get; set; }
        }

        /// <summary>
        /// 修改块数据
        /// </summary>
        private void modefyData(List<BlockReference> br, List<DBText> ta, List<AlignedDimension> ad)
        {
            ResetLifePumpData r = new ResetLifePumpData(br, ta, ad);
            r.setLifePumpData();
        }
        private void modefyMaterial(List<ObjectId> m)
        {
            BlockMaterialLifePump b = new BlockMaterialLifePump(m);
            b.setMaterial();
        }

        /// <summary>
        /// 得到文字
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        private DBText getText(string text, Point3d pt, double height, double widthFactor)
        {
            DBText t = new DBText();
            t.Position = pt;
            t.TextString = text;
            t.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
            t.Height = height;
            t.Layer = "W-WSUP-DIMS";
            t.WidthFactor = widthFactor;
            return t;
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
