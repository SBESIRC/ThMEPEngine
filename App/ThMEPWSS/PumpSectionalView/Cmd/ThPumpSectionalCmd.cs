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

namespace ThMEPWSS.PumpSectionalView
{
    /// <summary>
    /// 高位消防水箱
    /// </summary>
    public class ThHighFireWaterTankCmd : ThMEPBaseCommand, IDisposable
    {
        //private HighFireWaterTankViewModel highVM { get; set; }



        public void setInput(double Length, double Width, double High, double Volume, double BaseHigh, string Type)
        {
            ThHighFireWaterTankCommon.Input_Length = Length;
            ThHighFireWaterTankCommon.Input_Width = Width;
            ThHighFireWaterTankCommon.Input_Height = High;
            ThHighFireWaterTankCommon.Input_Volume = Volume;
            ThHighFireWaterTankCommon.Input_BasicHeight = BaseHigh;
            ThHighFireWaterTankCommon.Input_Type = Type;
        }
        //业务流程
        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                //用户输入 待改 固定输入

                string type = ThHighFireWaterTankCommon.Input_Type;

                string block = ThHighFireWaterTankCommon.TypeToBlk[type];
                string layer = ThHighFireWaterTankCommon.BlkToLayer[block];


                var blkList = new List<string> { block };//块列表
                var layerList = new List<string> { layer };//层


                ThHighFireWaterTankService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);

                var ppo = Active.Editor.GetPoint("\n选择插入点");
                if (ppo.Status == PromptStatus.OK)
                {
                    var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);//插入点位置？
                                                                                                 //var suggestDict = vm.SuggestDist;

                    //动态块-自定义
                    var dynDic = new Dictionary<string, object>() {
                        { ThHighFireWaterTankCommon.TypeToAttr[type], type} ,
                    };

                    //根据输入插入动态块
                    // 插入点，块名称，自定义属性
                    ThHighFireWaterTankService.InsertBlockWithDynamic(wcsPt, block, dynDic);
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

        //业务流程
        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThLifePumpService lp = new ThLifePumpService(acadDatabase);
                lp.CallLifePump();
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

        //业务流程
        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                //用户输入 待改 固定输入

                string type = ThFirePumpCommon.Input_Type;

                var blkList = ThFirePumpCommon.BlkName;//块列表
                var layerList = new List<string> { ThFirePumpCommon.Layer };//层


                ThFirePumpService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);

                var ppo = Active.Editor.GetPoint("\n选择插入点");
                if (ppo.Status == PromptStatus.OK)
                {
                    var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);


                    //动态块-自定义
                    var dynDic = new Dictionary<string, object>() {
                        { ThFirePumpCommon.TypeToAttr[type], type} ,
                    };



                    //根据输入插入动态块-消防泵房剖面1
                    // 插入点，块名称，自定义属性
                    //以该块为基点旋转
                    var rotaM=ThFirePumpService.InsertBlockWithDynamic(wcsPt, ThFirePumpCommon.BlkName[0], dynDic);


                    //插入属性块-消防泵房剖面2
                    Point3d wcsPt2 = new Point3d(wcsPt.X, wcsPt.Y - 9000, wcsPt.Z);
                    ThFirePumpService.InsertBlockWithAttribute(wcsPt2, ThFirePumpCommon.BlkName[1], rotaM);

                    //插入文字
                    DBText t1 = ThFirePumpService.GetText("消防水泵房：", new Point3d(wcsPt.X+1000, wcsPt.Y - 11000, 0), 264.3, 0.7,"W-WSUP-NOTE");
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
                    MText m= ThFirePumpService.GetIntro(new Point3d(wcsPt.X + 1000,wcsPt.Y - 11360,0));
                    m.TransformBy(rotaM);
                    acadDatabase.ModelSpace.Add(m);

                    //材料表头
                    var attri = new Dictionary<string, string>();
                    attri.Add("", "");
                    var Id=acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", ThFirePumpCommon.BlkName[2], new Point3d(wcsPt.X+1000, wcsPt.Y -15360, 0), new Scale3d(1), 0, attri);
                    BlockReference b = (BlockReference)Id.GetObject(OpenMode.ForRead);
                    b.UpgradeOpen();
                    b.TransformBy(rotaM);
                    b.DowngradeOpen();

                    //材料表格
                    List<ObjectId> blkM = new List<ObjectId>();
                    for (int i = 0; i < ThFirePumpCommon.Input_PumpList.Count + 1; i++)//做出相应数量的块
                    {
                        var att = new Dictionary<string, string>() { { "", "" } };
                        var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", ThFirePumpCommon.BlkName[3], new Point3d(wcsPt.X+1000, wcsPt.Y - 15360 - 373.3 * (i + 1), 0), new Scale3d(1), 0, att);
                        
                        BlockReference bl = (BlockReference)id.GetObject(OpenMode.ForRead);
                        bl.UpgradeOpen();
                        bl.TransformBy(rotaM);
                        bl.DowngradeOpen();
                        
                        blkM.Add(id);
                    }
                    ThFirePumpService.ModefyMaterial(blkM);
                }
            }
        }


        //析构 释放对象 无操作
        public void Dispose()
        {
        }


    }
}
