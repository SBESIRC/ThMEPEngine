using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using ThCADCore.NTS;
using AcHelper;
using DotNetARX;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPWSS.HydrantLayout.Model;
using ThMEPWSS.PumpSectionalView.Service;
using Autodesk.AutoCAD.EditorInput;
using ThMEPWSS.PumpSectionalView.Service.Impl;
using ThMEPWSS.PumpSectionalView.Utils;
using ThMEPWSS.PumpSectionalView.Model;
using NetTopologySuite.Algorithm;
using AcHelper.Commands;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using ThMEPEngineCore.Diagnostics;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Org.BouncyCastle.Crypto;
using ThMEPEngineCore.Service;
using System.Windows.Documents;
using ThMEPWSS.HydrantLayout.Service;
using NPOI.SS.Formula.Functions;
using ExCSS;
using System.Security.Cryptography;

namespace ThMEPWSS.PumpSectionalView
{
    /// <summary>
    /// 高位消防水箱
    /// </summary>
    public class ThHighFireWaterTankCmd : ThMEPBaseCommand, IDisposable
    {  
        public ThHighFireWaterTankCmd()
        {    
            InitialCmdInfo();
        }
        private void InitialCmdInfo()
        {
            ActionName = "高位消防水箱";
            CommandName = "THBFPMT"; //泵房剖面图
        }



        public void setInput(double Length, double Width, double High, double Volume, double BaseHigh, string Type1, string Type2)
        {
            ThHighFireWaterTankCommon.Input_Length = Length;
            ThHighFireWaterTankCommon.Input_Width = Width;
            ThHighFireWaterTankCommon.Input_Height = High;
            ThHighFireWaterTankCommon.Input_Volume = Volume;
            ThHighFireWaterTankCommon.Input_BasicHeight = BaseHigh;
            ThHighFireWaterTankCommon.Input_Type1 = Type1;
            ThHighFireWaterTankCommon.Input_Type2 = Type2;
        }
        //业务流程
        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                //用户输入 待改 固定输入

                string type1 = ThHighFireWaterTankCommon.Input_Type1;
                string type2 = ThHighFireWaterTankCommon.Input_Type2;

                string block1 = ThHighFireWaterTankCommon.TypeToBlk[type1];
                string block2 = ThHighFireWaterTankCommon.TypeToBlk[type2];
                string layer = ThHighFireWaterTankCommon.Layer;


                var blkList = new List<string> { block1, block2 };//块列表

                var layerList = new List<string> { layer };//层


                ThHighFireWaterTankService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);

                var ppo = Active.Editor.GetPoint("\n选择插入点");
                if (ppo.Status == PromptStatus.OK)
                {
                    var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);//插入点位置

                    //插入第一张
                    //动态块-自定义
                    var dynDic = new Dictionary<string, object>() {
                        { ThHighFireWaterTankCommon.TypeToAttr[type1], type1} ,
                    };

                    //根据输入插入动态块
                    // 插入点，块名称，自定义属性
                    Matrix3d m = ThHighFireWaterTankService.InsertBlockWithDynamic(wcsPt, block1, dynDic, type1, new Matrix3d());

                    //插入第二张
                    //动态块-自定义
                    dynDic = new Dictionary<string, object>() {
                        { ThHighFireWaterTankCommon.TypeToAttr[type2], type2} ,
                    };

                    // 插入点，块名称，自定义属性
                    Point3d pos = new Point3d(wcsPt.X - 3000, wcsPt.Y - 10000, wcsPt.Z);
                    ThHighFireWaterTankService.InsertBlockWithDynamic(pos, block2, dynDic, type2, m);
                }
            }
        }

        //析构 释放对象 无操作
        public void Dispose()
        {
        }


    }


    /// <summary>
    /// 生活泵房
    /// </summary>
    public class ThLifePumpCmd : ThMEPBaseCommand, IDisposable
    {
        private string button;
        public ThLifePumpCmd()
        {
            button = ThLifePumpCommon.Button_Name;
            InitialCmdInfo();
            //InitialSetting();
        }
        private void InitialCmdInfo()
        {
            ActionName = "生" + button;
            CommandName = "THBFPMT"; //泵房剖面图
        }

        //业务流程
        public override void SubExecute()
        {
            if (button == "自动选泵")
            {
                return;
            }
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThLifePumpService lp = new ThLifePumpService(acadDatabase);
                lp.CallLifePump(button);
                
            }
        }


        //析构 释放对象 无操作
        public void Dispose()
        {
        }

    }

    /// <summary>
    /// 消防泵房
    /// </summary>
    public class ThFirePumpCmd : ThMEPBaseCommand, IDisposable
    {
        private string button;
        public ThFirePumpCmd()
        {
            button = ThFirePumpCommon.Button_Name;
            InitialCmdInfo();
            //InitialSetting();
        }
        private void InitialCmdInfo()
        {
            ActionName = "消" + button;
            CommandName = "THBFPMT"; //泵房剖面图
        }

        //业务流程
        public override void SubExecute()
        {
            if (button == "自动选泵")
            {
                return;
            }
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string type = ThFirePumpCommon.Input_Type;

                var blkList = ThFirePumpCommon.BlkName;//块列表
                var layerList = new List<string> { ThFirePumpCommon.Layer };//层

                ThFirePumpService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);


                var ppo = Active.Editor.GetPoint("\n选择插入点");
                if (ppo.Status == PromptStatus.OK)
                {
                    var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);

                    if (button == "生成剖面图")
                    {
                        DrawFirePump(wcsPt,type);
                    }
                    else if (button == "生成说明")
                    {
                        DrawWord(wcsPt,acadDatabase);
                    }
                    else if (button == "生成材料表")
                    {
                        DrawGraph(wcsPt, acadDatabase);
                    }

                    /*
                    //先插入图块
                    //if(button=="生成剖面图")
                    //动态块-自定义
                    var dynDic = new Dictionary<string, object>() {
                        { ThFirePumpCommon.TypeToAttr[type], type} ,
                    };

                    //根据输入插入动态块-消防泵房剖面1
                    // 插入点，块名称，自定义属性
                    //以该块为基点旋转
                    var rotaM = ThFirePumpService.InsertBlockWithDynamic(wcsPt, ThFirePumpCommon.BlkName[0], dynDic);


                    //插入属性块-消防泵房剖面2
                    Point3d wcsPt2 = new Point3d(wcsPt.X, wcsPt.Y - 9000, wcsPt.Z);
                    ThFirePumpService.InsertBlockWithAttribute(wcsPt2, ThFirePumpCommon.BlkName[1], rotaM);

                    //插入文字
                    DBText t1 = ThFirePumpService.GetText("消防水泵房：", new Point3d(wcsPt.X + 1000, wcsPt.Y - 11000, 0), 264.3, 0.7, "W-WSUP-NOTE");
                    t1.TransformBy(rotaM);
                    acadDatabase.ModelSpace.Add(t1);

                    DBText t2 = ThFirePumpService.GetText("标高剖面示意图", new Point3d(wcsPt.X + 3000, wcsPt.Y - 11000, 0), 264.3, 0.7, "W-WSUP-NOTE");
                    t2.TransformBy(rotaM);
                    acadDatabase.ModelSpace.Add(t2);

                    //插入多段线
                    Polyline p1 = ThFirePumpService.GetPolyline(wcsPt.X + 1000, wcsPt.X + 4300, wcsPt.Y - 11200, "W-WSUP-NOTE", 35);
                    p1.TransformBy(rotaM);
                    acadDatabase.ModelSpace.Add(p1);

                    Polyline p2 = ThFirePumpService.GetPolyline(wcsPt.X + 1000, wcsPt.X + 4300, wcsPt.Y - 11250, "W-WSUP-NOTE", 0);
                    p2.TransformBy(rotaM);
                    acadDatabase.ModelSpace.Add(p2);

                    //插入多行文字
                    MText m = ThFirePumpService.GetIntro(new Point3d(wcsPt.X + 1000, wcsPt.Y - 11360, 0));
                    m.TransformBy(rotaM);
                    acadDatabase.ModelSpace.Add(m);

                    //材料表头
                    var attri = new Dictionary<string, string>();
                    attri.Add("", "");
                    var Id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", ThFirePumpCommon.BlkName[2], new Point3d(wcsPt.X + 1000, wcsPt.Y - 15360, 0), new Scale3d(1), 0, attri);
                    BlockReference b = (BlockReference)Id.GetObject(OpenMode.ForRead);
                    b.UpgradeOpen();
                    b.TransformBy(rotaM);
                    b.DowngradeOpen();

                    //材料表格
                    List<ObjectId> blkM = new List<ObjectId>();
                    for (int i = 0; i < ThFirePumpCommon.Input_PumpList.Count + 1; i++)//做出相应数量的块
                    {
                        var att = new Dictionary<string, string>() { { "", "" } };
                        var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", ThFirePumpCommon.BlkName[3], new Point3d(wcsPt.X + 1000, wcsPt.Y - 15360 - 373.3 * (i + 1), 0), new Scale3d(1), 0, att);

                        BlockReference bl = (BlockReference)id.GetObject(OpenMode.ForRead);
                        bl.UpgradeOpen();
                        bl.TransformBy(rotaM);
                        bl.DowngradeOpen();

                        blkM.Add(id);
                    }
                    ThFirePumpService.ModefyMaterial(blkM);
                    */
                }


            }
        }

        /// <summary>
        /// 生成剖面图
        /// </summary>
        /// <param name="wcsPt"></param>
        /// <param name="type"></param>
        private void DrawFirePump(Point3d wcsPt,string type)
        {
            //动态块-自定义
            var dynDic = new Dictionary<string, object>() {
                        { ThFirePumpCommon.TypeToAttr[type], type} ,
                    };

            //根据输入插入动态块-消防泵房剖面1
            // 插入点，块名称，自定义属性
            //以该块为基点旋转
            var rotaM = ThFirePumpService.InsertBlockWithDynamic(wcsPt, ThFirePumpCommon.BlkName[0], dynDic);


            //插入属性块-消防泵房剖面2
            Point3d wcsPt2 = new Point3d(wcsPt.X, wcsPt.Y - 9000, wcsPt.Z);
            ThFirePumpService.InsertBlockWithAttribute(wcsPt2, ThFirePumpCommon.BlkName[1], rotaM);
        }

        /// <summary>
        /// 生成说明文字
        /// </summary>
        /// <param name="wcsPt"></param>
        private void DrawWord(Point3d wcsPt, AcadDatabase acadDatabase)
        {
            //以多行文字为旋转基点
            var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
            var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);

            //插入多行文字
            MText m = ThFirePumpService.GetIntro(new Point3d(wcsPt.X, wcsPt.Y, 0));
            
            //BlockReference blk = (BlockReference)objId.GetObject(OpenMode.ForRead);
            var rotaM = Matrix3d.Rotation(angle, Vector3d.ZAxis, m.Location);
            m.TransformBy(rotaM);
            var objId=acadDatabase.ModelSpace.Add(m);
            //blk.UpgradeOpen();
            //blk.TransformBy(rotaM);
            //blk.DowngradeOpen();
         

            //插入文字
            DBText t1 = ThFirePumpService.GetText("消防水泵房：", new Point3d(wcsPt.X , wcsPt.Y +360, 0), 264.3, 0.7, "W-WSUP-NOTE");//+360
            t1.TransformBy(rotaM);
            acadDatabase.ModelSpace.Add(t1);

            DBText t2 = ThFirePumpService.GetText("标高剖面示意图", new Point3d(wcsPt.X + 2000, wcsPt.Y +360, 0), 264.3, 0.7, "W-WSUP-NOTE");//+360
            t2.TransformBy(rotaM);
            acadDatabase.ModelSpace.Add(t2);

            //插入多段线
            Polyline p1 = ThFirePumpService.GetPolyline(wcsPt.X , wcsPt.X + 3300, wcsPt.Y +160, "W-WSUP-NOTE", 35);//+160
            p1.TransformBy(rotaM);
            acadDatabase.ModelSpace.Add(p1);

            Polyline p2 = ThFirePumpService.GetPolyline(wcsPt.X , wcsPt.X + 3300, wcsPt.Y +110, "W-WSUP-NOTE", 0);//+110
            p2.TransformBy(rotaM);
            acadDatabase.ModelSpace.Add(p2);

           
        }

        /// <summary>
        /// 生成说明表格
        /// </summary>
        /// <param name="wcsPt"></param>
        /// <param name="acadDatabase"></param>
        private void DrawGraph(Point3d wcsPt, AcadDatabase acadDatabase)
        {
            //以材料表头为旋转基点
            var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
            var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);

            //材料表头
            var attri = new Dictionary<string, string>();
            attri.Add("", "");
            var Id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", ThFirePumpCommon.BlkName[2], new Point3d(wcsPt.X, wcsPt.Y , 0), new Scale3d(1), 0, attri);
            BlockReference b = (BlockReference)Id.GetObject(OpenMode.ForRead);
            var rotaM = Matrix3d.Rotation(angle, Vector3d.ZAxis, b.Position);
            b.UpgradeOpen();
            b.TransformBy(rotaM);
            b.DowngradeOpen();

            //材料表格
            List<ObjectId> blkM = new List<ObjectId>();
            for (int i = 0; i < ThFirePumpCommon.Input_PumpList.Count + 1; i++)//做出相应数量的块
            {
                var att = new Dictionary<string, string>() { { "", "" } };
                var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", ThFirePumpCommon.BlkName[3], new Point3d(wcsPt.X, wcsPt.Y  - 373.3 * (i + 1), 0), new Scale3d(1), 0, att);

                BlockReference bl = (BlockReference)id.GetObject(OpenMode.ForRead);
                bl.UpgradeOpen();
                bl.TransformBy(rotaM);
                bl.DowngradeOpen();

                blkM.Add(id);
            }
            ThFirePumpService.ModefyMaterial(blkM);


        }
        
        

        //析构 释放对象 无操作
        public void Dispose()
        {
        }


    }
}
